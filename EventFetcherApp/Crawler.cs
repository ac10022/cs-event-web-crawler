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

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventFetcher
{
    public class Event : IComparable
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

            if (description != String.Empty)
            {
                this.description = description
                    .Replace("\r", " ").Replace("\n", " ")
                    .Trim();
            }
            else this.description = description;

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

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj is Event) return this.Url == ((Event)obj).Url;
            return base.Equals(obj);
        }

        public override int GetHashCode()
            => base.GetHashCode();

        int IComparable.CompareTo(object? obj)
        {
            if (obj == null) return -1;
            if (obj is Event) return String.Compare(this.Url, ((Event)obj).Url);
            return -1;
        }

        public override string ToString()
        {
            return $"Title: '{title}'\nUrl: '{url}'\nEvent Type: '{eventType}'\nLocation: '{location}'\nDescription: '{description}'\nDate: {date}\nRawDate: '{rawDate}'";
        }
    }

    public enum CrawlerTarget
    {
        TechUK, CenturyClub, Meetup, Eventbrite
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

        const string CC_BASE_URL = "https://centuryclub.co.uk/tag/events/";
        const string CC_ARTICLE_WRAPPER = ".elementor-location-archive .e-loop-item";
        const string CC_TITLE_WRAPPER = ".elementor-widget-theme-post-title .elementor-heading-title a";
        const string CC_DATE_WRAPPER = ".elementor-widget-post-info .elementor-icon-list-item:has(.elementor-post-info__item--type-custom) .elementor-icon-list-text";
        const string CC_DESC_WRAPPER = ".articleItem--excerpt .elementor-icon-list-text";
        const string CC_EVENTTYPE_WRAPPER = ".elementor-post-info__item--type-terms .elementor-post-info__terms-list-item";
        const string CC_LOCATION_WRAPPER = null;

        private Wrapper? wrapper;
        private CrawlerTarget target;

        public CrawlerTarget Target => target;

        private IList<Event> Process()
        {
            HtmlAgilityPack.HtmlDocument? doc = FetchHTML();
            if (doc == null) return new List<Event>();

            IList<HtmlNode> elems = FetchEvents(doc);
            IList<Event> events = DeentitizeEvents(elems);
            return events;
        }

        public IList<Event>? CrawlEvents(CrawlerTarget target)
        {
            this.target = target;

            switch (target)
            {
                case CrawlerTarget.TechUK:
                    wrapper = new Wrapper(TU_BASE_URL, TU_ARTICLE_WRAPPER, TU_TITLE_WRAPPER, TU_DATE_WRAPPER, TU_DESC_WRAPPER, TU_EVENTTYPE_WRAPPER, TU_LOCATION_WRAPPER);
                    return Process();

                case CrawlerTarget.CenturyClub:
                    wrapper = new Wrapper(CC_BASE_URL, CC_ARTICLE_WRAPPER, CC_TITLE_WRAPPER, CC_DATE_WRAPPER, CC_DESC_WRAPPER, CC_EVENTTYPE_WRAPPER, CC_LOCATION_WRAPPER);
                    return Process();

                case CrawlerTarget.Meetup:
                    var meetupCrawler = new MeetupCrawler();
                    List<Event> meetupRes = new List<Event>();
                    meetupRes.AddRange(meetupCrawler.Fetch("Enterprise Technology"));
                    meetupRes.AddRange(meetupCrawler.Fetch("Fintech Events"));
                    meetupRes.AddRange(meetupCrawler.Fetch("Data Science"));
                    return meetupRes;

                case CrawlerTarget.Eventbrite:
                    var eventbriteCrawler = new EventbriteCrawler();
                    List<Event> eventbriteRes = new List<Event>();
                    eventbriteRes.AddRange(eventbriteCrawler.FetchAllPages("Enterprise Technology"));
                    eventbriteRes.AddRange(eventbriteCrawler.FetchAllPages("Fintech Events"));
                    eventbriteRes.AddRange(eventbriteCrawler.FetchAllPages("Data Science"));
                    return eventbriteRes;

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

        private HtmlNode? QuerySelectorSafe(HtmlNode node, string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
                return null;

            try
            {
                // try the normal css selector first
                return node.QuerySelector(selector);
            }
            catch (NotSupportedException)
            {
                if (!selector.Contains(":has("))
                    throw;

                int hasIndex = selector.IndexOf(":has(", StringComparison.Ordinal);
                int openParen = hasIndex + 5;
                int closeParen = selector.IndexOf(')', openParen);
                if (hasIndex < 0 || closeParen < 0)
                    return null;

                string beforePart = selector.Substring(0, hasIndex).Trim();
                string insidePart = selector.Substring(openParen, closeParen - openParen).Trim();
                string afterPart = selector.Substring(closeParen + 1).Trim();

                IEnumerable<HtmlNode> candidates;
                try
                {
                    candidates = node.QuerySelectorAll(beforePart);
                }
                catch
                {
                    return null;
                }

                foreach (HtmlNode candidate in candidates)
                {
                    HtmlNode? insideMatch = null;
                    try
                    {
                        insideMatch = candidate.QuerySelector(insidePart);
                    }
                    catch
                    {
                        insideMatch = null;
                    }

                    if (insideMatch != null)
                    {
                        if (string.IsNullOrWhiteSpace(afterPart))
                            return candidate;

                        try
                        {
                            HtmlNode? final = candidate.QuerySelector(afterPart);
                            if (final != null)
                                return final;
                        }
                        catch { }
                    }
                }

                return null;
            }
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

            HtmlNode titleNode = QuerySelectorSafe(node, wrapper.TitleWrapper);
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

            HtmlNode dateNode = QuerySelectorSafe(node, wrapper.DateWrapper);
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

            HtmlNode descriptionNode = QuerySelectorSafe(node, wrapper.DescriptionWrapper);
            if (descriptionNode != null)
            {
                description = HtmlEntity.DeEntitize(descriptionNode.InnerText);
            }
            else
            {
                if (target == CrawlerTarget.CenturyClub) return null;
                description = String.Empty;
            }

            HtmlNode typeNode = QuerySelectorSafe(node, wrapper.EventTypeWrapper);
            if (typeNode != null)
            {
                eventType = HtmlEntity.DeEntitize(typeNode.InnerText).Trim();
            }
            else
            {
                eventType = String.Empty;
            }

            if (wrapper.LocationWrapper == null)
            {
                location = String.Empty;
            }
            else
            {
                HtmlNode locationNode = QuerySelectorSafe(node, wrapper.LocationWrapper);
                if (locationNode != null)
                {
                    location = HtmlEntity.DeEntitize(locationNode.InnerText).Trim();
                }
                else
                {
                    location = String.Empty;
                }
            }

            if (rawDate == null) curEvent = new Event(title, url, eventType, location, description, date);
            else curEvent = new Event(title, url, eventType, location, description, rawDate);
            
            return curEvent;
        }

        private void Throw(string message, Exception? ex = null) 
            => ErrorHandler.Show("Crawl", message, ex);
    }
}
