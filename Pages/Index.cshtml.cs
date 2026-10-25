using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeagueVisionSimple.Pages
{
    public class IndexModel : PageModel
    {
        // A list to hold the news items
        public List<NewsItem> NewsList { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // We use Sky Sports free RSS feed for Premier League news
                string feedUrl = "https://www.skysports.com/rss/11661";
                using var client = new HttpClient();
                var xml = await client.GetStringAsync(feedUrl);
                
                // Parse the XML (RSS feed format)
                var doc = XDocument.Parse(xml);

                // Loop through the first 10 news items
                foreach (var item in doc.Descendants("item"))
                {
                    NewsList.Add(new NewsItem
                    {
                        Title = item.Element("title")?.Value ?? "No Title",
                        Link = item.Element("link")?.Value ?? "#",
                        Description = item.Element("description")?.Value ?? ""
                    });
                }
            }
            catch
            {
                // If the internet fails or feed is down, we just catch the error
            }
        }
    }

    // Simple class to store news data
    public class NewsItem
    {
        public string Title { get; set; } = "";
        public string Link { get; set; } = "";
        public string Description { get; set; } = "";
    }
}