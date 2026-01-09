using EventFetcherApp;
using HtmlAgilityPack;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventFetcher
{
    internal class EventbriteCrawler
    {
        private String curSearchTerm = String.Empty;

        private string GetUrl(string searchTerm, int pageNo)
        {
            var sanitizedTerm = searchTerm.Trim().Replace(" ", "-").ToLower();
            return $"https://www.eventbrite.co.uk/d/united-kingdom--london/events--next-week/{sanitizedTerm}/?page={pageNo}";
        }

        private void Throw(string message, Exception? ex = null)
            => ErrorHandler.Show("Eventbrite Crawl", message, ex);

        public IList<Event> FetchAllPages(string searchTerm)
        {
            List<Event> concat = new List<Event>();
            List<Event> result = new List<Event>();
            int i = 1;

            do
            {
                result = (List<Event>)FetchPage(searchTerm, i);
                concat.AddRange(result);
                i++;
            }
            while (result.Any());

            return concat;
        }

        public IList<Event> FetchPage(string searchTerm, int pageNo)
        {
            var web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc;

            try
            {
                doc = web.Load(GetUrl(searchTerm, pageNo));
            }
            catch (Exception ex)
            {
                Throw("Error fetching Eventbrite page", ex);
                return new List<Event>();
            }

            var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scriptNodes == null) return new List<Event>();

            foreach (var node in scriptNodes)
            {
                try
                {
                    using (JsonDocument document = JsonDocument.Parse(node.InnerText))
                    {
                        JsonElement root = document.RootElement;

                        if (root.ValueKind != JsonValueKind.Object) continue;

                        if (root.TryGetProperty("@type", out var typeProp) &&
                            typeProp.GetString() == "ItemList" &&
                            root.TryGetProperty("itemListElement", out var itemsProp))
                        {
                            var ebItems = JsonSerializer.Deserialize<List<EbItemListElementDto>>(itemsProp.GetRawText());

                            if (ebItems != null)
                            {
                                return ebItems
                                    .Select(x => x.Item)
                                    .Where(i => i != null)
                                    .Select(MapToDomainEvent)
                                    .ToList();
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return new List<Event>();
        }

        private Event MapToDomainEvent(EbEventDto dto)
        {
            object dateParam;
            if (DateTime.TryParse(dto.StartDate, out DateTime dt))
                dateParam = dt;
            else
                dateParam = dto.StartDate ?? "Unknown Date";

            string location = "Unknown Location";
            if (dto.Location != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(dto.Location.Name)) parts.Add(dto.Location.Name);

                if (dto.Location.Address != null)
                {
                    if (!string.IsNullOrWhiteSpace(dto.Location.Address.StreetAddress))
                        parts.Add(dto.Location.Address.StreetAddress);
                    if (!string.IsNullOrWhiteSpace(dto.Location.Address.AddressLocality))
                        parts.Add(dto.Location.Address.AddressLocality);
                }

                if (parts.Count > 0) location = string.Join(", ", parts);
            }

            string eventType = "Eventbrite Event";
            if (!String.IsNullOrEmpty(curSearchTerm)) eventType += $" ({curSearchTerm})";

            return new Event(
                title: dto.Name ?? "Untitled Event",
                url: dto.Url ?? String.Empty,
                eventType: eventType,
                location: location,
                description: dto.Description ?? String.Empty,
                date: dateParam
            );
        }

        private class EbItemListElementDto
        {
            [JsonPropertyName("item")]
            public EbEventDto? Item { get; set; }
        }

        private class EbEventDto
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("startDate")]
            public string? StartDate { get; set; }

            [JsonPropertyName("location")]
            public EbLocationDto? Location { get; set; }
        }

        private class EbLocationDto
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("address")]
            public EbAddressDto? Address { get; set; }
        }

        private class EbAddressDto
        {
            [JsonPropertyName("streetAddress")]
            public string? StreetAddress { get; set; }

            [JsonPropertyName("addressLocality")]
            public string? AddressLocality { get; set; }
        }
    }
}