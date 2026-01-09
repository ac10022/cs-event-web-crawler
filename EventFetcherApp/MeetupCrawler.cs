using EventFetcherApp;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventFetcher
{
    internal class MeetupCrawler
    {
        private String GetUrl(String searchTerm)
            => $"https://www.meetup.com/find/?keywords={searchTerm.Trim().Replace(" ", "+")}&source=EVENTS&dateRange=next-week&distance=hundredMiles";

        private void Throw(string message, Exception? ex = null)
            => ErrorHandler.Show("Meetup Crawl", message, ex);

        public IList<Event> Fetch(String searchTerm)
        {
            var web = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc;

            try
            {
                doc = web.Load(GetUrl(searchTerm));
            }
            catch (Exception ex)
            {
                Throw("Error fetching Meetup page", ex);
                return new List<Event>();
            }

            var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scriptNodes == null) return new List<Event>();

            foreach (var node in scriptNodes)
            {
                try
                {
                    var meetupEvents = JsonSerializer.Deserialize<List<MeetupEventDto>>(node.InnerText);

                    if (meetupEvents != null)
                    {
                        return meetupEvents.Select(MapToDomainEvent).ToList();
                    }
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return new List<Event>();
        }

        private Event MapToDomainEvent(MeetupEventDto dto)
        {
            object dateParam;
            if (DateTime.TryParse(dto.StartDate, out DateTime dt))
            {
                dateParam = dt;
            }
            else
            {
                dateParam = dto.StartDate ?? "Unknown Date";
            }

            string location = "Unknown Location";

            if (dto.Location.ValueKind == JsonValueKind.Object)
            {
                if (dto.Location.TryGetProperty("@type", out var typeProp) &&
                    typeProp.GetString() == "VirtualLocation")
                {
                    location = "Online";
                }
                else if (dto.Location.TryGetProperty("address", out var addressProp) &&
                         addressProp.ValueKind == JsonValueKind.Object)
                {
                    if (addressProp.TryGetProperty("addressLocality", out var locality))
                    {
                        location = locality.GetString() ?? String.Empty;
                    }
                }
            }

            string eventType = "Meetup";
            if (dto.Organizer != null && !string.IsNullOrEmpty(dto.Organizer.Name))
            {
                eventType = $"{eventType} ({dto.Organizer.Name})";
            }

            return new Event(
                title: dto.Name ?? "Untitled Event",
                url: dto.Url ?? String.Empty,
                eventType: eventType,
                location: location,
                description: dto.Description ?? String.Empty,
                date: dateParam
            );
        }

        private class MeetupEventDto
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
            public JsonElement Location { get; set; } 

            [JsonPropertyName("organizer")]
            public MeetupOrganizerDto? Organizer { get; set; }
        }

        private class MeetupOrganizerDto
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}