using CsvHelper;
using CsvHelper.Configuration;
using EventFetcherApp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EventFetcher
{
    public class EventMap : ClassMap<Event>
    {
        public EventMap()
        {
            // if no sanitised date, return raw date
            Map(x => x.Date).Convert(x =>
            {
                if (x.Value.Date == DateTime.MinValue) return x.Value.RawDate;
                else return x.Value.Date.ToString("dd/MM/yyyy");
            });
            Map(x => x.Title);
            Map(x => x.EventType);
            Map(x => x.Description);
            Map(x => x.Location);
            Map(x => x.Url);

            // do not display raw date
            Map(x => x.RawDate).Ignore();
        }
    }

    public enum ExporterResult
    {
        OK, Error
    }

    internal class Exporter
    {
        private IList<Event>? events;
        private ExporterResult result = ExporterResult.OK;
        public IList<Event>? Events => events;
        public ExporterResult Result => result;


        public Exporter(IList<Event> events)
        {
            this.events = events; 
        }

        public Exporter(IList<CrawlerTarget> targets, string destination)
        {
            TargetUnion(targets);
            Export(destination);
        }

        private void DebugEvents()
        {
            if (this.events != null)
            {
                foreach (var e in events)
                    Console.WriteLine(e);
            }
        }

        private void TargetUnion(IList<CrawlerTarget> targets)
        {
            var crawl = new Crawler();

            this.events = targets
                .SelectMany(t => crawl.CrawlEvents(t) ?? Enumerable.Empty<Event>())
                .Where(x => x.Date >= DateTime.Today)
                .DistinctBy(x => x.Url)
                .ToList();

            DebugEvents();
        }

        private void Export(string path)
        {
            if (events == null)
            {
                Throw("Could not fetch events.");
                result = ExporterResult.Error;
                return;
            }

            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            using (CsvWriter csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                try
                {
                    csvWriter.Context.RegisterClassMap<EventMap>();
                    csvWriter.WriteRecords(events);
                    result = ExporterResult.OK;
                }
                catch (Exception e)
                {
                    Throw($"Unable to export.", e);
                    result = ExporterResult.Error;
                    return;
                }
            }
        }

        private void Throw(string message, Exception? ex = null) 
            => ErrorHandler.Show("Export", message, ex);
    }
}
