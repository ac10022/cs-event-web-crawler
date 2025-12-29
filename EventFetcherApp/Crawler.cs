using CsvHelper.Configuration.Attributes;
using EventFetcherApp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFetcher
{
    public class Event
    {
        private DateTime date;
        private string? rawDate; // fallback in case date is in an invalid format

        private string title;
        private string url;
        private string eventType;
        private string location;
        private string description;

        [Format("dd/MM/yyyy")]
        public DateTime Date => date;
        public string? RawDate => rawDate;

        public string Title => title;
        public string Url => url;
        public string EventType => eventType;
        public string Location => location;
        public string Description => description;

        public Event(string title, string url, string eventType, string location, string description, object date)
        {
            this.title = title;
            this.url = url;
            this.eventType = eventType;
            this.location = location;
            this.description = description;

            if (date is DateTime)
            {
                this.date = (DateTime)date;
                this.rawDate = null;
            }
            else
            {
                this.rawDate = (string)date;
                this.date = DateTime.MinValue;
            }
        }

        public override string ToString()
        {
            return $"Title: '{title}'\nUrl: '{url}'\nEvent Type: '{eventType}'\nLocation: '{location}'\nDescription: '{description}'\nDate: {date}\nRawDate: '{rawDate}'";
        }
    }

    public enum CrawlerTarget
    {
        TechUK
    }

    public class Wrapper
    {
        private string baseUrl;
        private string articleWrapper;
        private string titleWrapper;
        private string dateWrapper;
        private string descriptionWrapper;
        private string eventTypeWrapper;
        private string locationWrapper;

        public string BaseUrl => baseUrl;
        public string ArticleWrapper => articleWrapper;
        public string TitleWrapper => titleWrapper;
        public string DateWrapper => dateWrapper;
        public string DescriptionWrapper => descriptionWrapper;
        public string EventTypeWrapper => eventTypeWrapper;
        public string LocationWrapper => locationWrapper;

        public Wrapper(string baseUrl, string articleWrapper, string titleWrapper, string dateWrapper, string descriptionWrapper, string eventTypeWrapper, string locationWrapper)
        {
            this.baseUrl = baseUrl;
            this.articleWrapper = articleWrapper;
            this.titleWrapper = titleWrapper;
            this.dateWrapper = dateWrapper;
            this.descriptionWrapper = descriptionWrapper;
            this.eventTypeWrapper = eventTypeWrapper;
            this.locationWrapper = locationWrapper;
        }
    }

    internal class Crawler
    {
        const string TU_BASE_URL = "https://www.techuk.org/what-we-deliver/events.html?page_size=1000";
        const string TU_ARTICLE_WRAPPER = "div.article-wrapper";
        const string TU_TITLE_WRAPPER = ".article-title a";
        const string TU_DATE_WRAPPER = ".article-date";
        const string TU_DESC_WRAPPER = ".article-teaser";
        const string TU_EVENTTYPE_WRAPPER = ".event-type-badge";
        const string TU_LOCATION_WRAPPER = ".event-venue";

        private Wrapper? wrapper;

        public IList<Event>? CrawlEvents(CrawlerTarget target)
        {   
            switch (target)
            {
                case CrawlerTarget.TechUK:
                    wrapper = new Wrapper(TU_BASE_URL, TU_ARTICLE_WRAPPER, TU_TITLE_WRAPPER, TU_DATE_WRAPPER, TU_DESC_WRAPPER, TU_EVENTTYPE_WRAPPER, TU_LOCATION_WRAPPER);
                    
                    HtmlAgilityPack.HtmlDocument? doc = FetchHTML();
                    if (doc == null) return new List<Event>();

                    IList<HtmlNode> elems = FetchEvents(doc);
                    IList<Event> events = DeentitizeEvents(elems);
                    return events;
                default:
                    return new List<Event>();
            }
        }

        private HtmlAgilityPack.HtmlDocument? FetchHTML()
        {
            if (wrapper == null)
            {
                Throw("Wrapper is not identified; i.e. no target has been selected");
                return null;
            }
            
            try
            {
                HtmlWeb web = new HtmlWeb();
                HtmlAgilityPack.HtmlDocument doc = web.Load(wrapper.BaseUrl);
                return doc;
            }
            catch (Exception e)
            {
                Throw($"Unable to fetch from this webpage: {wrapper.BaseUrl}.", e);
                return null;
            }
        }

        private IList<HtmlNode> FetchEvents(HtmlAgilityPack.HtmlDocument doc)
        {
            if (wrapper == null)
            {
                Throw("Wrapper is not identified; i.e. no target has been selected, no result.");
                return new List<HtmlNode>();
            }

            List<Event> events = new List<Event>();
            IList<HtmlNode> eventHtmlElements = doc.DocumentNode.QuerySelectorAll(wrapper.ArticleWrapper);
            return eventHtmlElements;
        }

        private IList<Event> DeentitizeEvents(IList<HtmlNode> htmlEvents)
        {
            IList<Event> events = new List<Event>();

            foreach (HtmlNode node in htmlEvents)
            {
                if (node == null) 
                    continue;

                Event? curEvent = ProcessNode(node);
                if (curEvent != null) 
                    events.Add(curEvent);
            }

            return events;
        }

        private Event? ProcessNode(HtmlNode node)
        {
            if (wrapper == null)
            {
                Throw("Wrapper is not identified; i.e. no target has been selected");
                return null;
            }

            Event curEvent;

            string title, url, eventType, location, description;
            string? rawDate;
            DateTime date;

            HtmlNode titleNode = node.QuerySelector(wrapper.TitleWrapper);
            if (titleNode != null)
            {
                title = HtmlEntity.DeEntitize(titleNode.InnerText).Trim();
                url = HtmlEntity.DeEntitize(titleNode.GetAttributeValue("href", String.Empty)).Trim();
            }
            else
            {
                // we make the assumtion that if there is no "title" field, then we do not have a valid event, so skip this event.
                return null;
            }

            HtmlNode dateNode = node.QuerySelector(wrapper.DateWrapper);
            if (dateNode != null)
            {
                string rawHtmlDate = HtmlEntity.DeEntitize(dateNode.InnerText).Trim();
                bool ok = DateTime.TryParse(rawHtmlDate, out date);

                if (!ok) rawDate = rawHtmlDate;
                else rawDate = null;
            }
            else
            {
                // we make the assumtion that if there is no "date" field, then we do not have a valid event, so skip this event.
                return null;
            }

            HtmlNode descriptionNode = node.QuerySelector(wrapper.DescriptionWrapper);
            if (descriptionNode != null)
            {
                description = HtmlEntity.DeEntitize(descriptionNode.InnerText).Trim();
            }
            else
            {
                description = String.Empty;
            }

            HtmlNode typeNode = node.QuerySelector(wrapper.EventTypeWrapper);
            if (typeNode != null)
            {
                eventType = HtmlEntity.DeEntitize(typeNode.InnerText).Trim();
            }
            else
            {
                eventType = String.Empty;
            }

            HtmlNode locationNode = node.QuerySelector(wrapper.LocationWrapper);
            if (locationNode != null)
            {
                location = HtmlEntity.DeEntitize(locationNode.InnerText).Trim();
            }
            else
            {
                location = String.Empty;
            }

            if (rawDate == null) curEvent = new Event(title, url, eventType, location, description, date);
            else curEvent = new Event(title, url, eventType, location, description, rawDate);
            
            return curEvent;
        }

        private void Throw(string message, Exception? ex = null) 
            => ErrorHandler.Show("Crawl", message, ex);
    }
}
