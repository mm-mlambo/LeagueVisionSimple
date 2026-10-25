using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeagueVisionSimple.Pages
{
    public class PlayersModel : PageModel
    {
        // CACHE: We save the players here in memory so we only download them once every 10 minutes
        private static List<PlayerRow> _cachedPlayers = new();
        private static DateTime _cacheTime = DateTime.MinValue;

        public List<PlayerRow> Players { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = "";

        public async Task OnGetAsync()
        {
            // 1. Check if the cache is empty OR if it is older than 10 minutes
            if (_cachedPlayers.Count == 0 || DateTime.Now > _cacheTime.AddMinutes(10))
            {
                using var client = new HttpClient();
                // Adding a User-Agent stops the FPL API from blocking our requests
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 LeagueVisionApp");

                var json = await client.GetStringAsync("https://fantasy.premierleague.com/api/bootstrap-static/");
                var data = JsonSerializer.Deserialize<PlayersFplData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data != null)
                {
                    _cachedPlayers.Clear(); // Clear the old cache

                    foreach (var p in data.elements)
                    {
                        // Calculate Expected Points (xP)
                        double xp = 0;
                        if (p.status == "a" && p.minutes > 90) 
                        {
                            xp = double.TryParse(p.form, out double f) ? f : 0;
                        }

                        _cachedPlayers.Add(new PlayerRow
                        {
                            Id = p.id,
                            Name = p.web_name,
                            Team = data.teams.FirstOrDefault(t => t.id == p.team)?.short_name ?? "?",
                            Position = data.element_types.FirstOrDefault(et => et.id == p.element_type)?.singular_name_short ?? "?",
                            Cost = (p.now_cost / 10.0).ToString("0.0"),
                            TotalPoints = p.total_points,
                            Xp = Math.Round(xp, 1)
                        });
                    }
                    
                    // Update the time we saved the cache
                    _cacheTime = DateTime.Now; 
                }
            }

            // 2. Now, we search the SAVED cache instead of downloading from the API again
            Players = _cachedPlayers;
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                Players = Players.Where(p => p.Name.ToLower().Contains(SearchTerm.ToLower())).ToList();
            }
        }
    }

    // --- Unique classes for Players Page ---
    public class PlayersFplData
    {
        public List<PlayersElement> elements { get; set; } = new();
        public List<PlayersTeam> teams { get; set; } = new();
        public List<PlayersElementType> element_types { get; set; } = new();
    }
    public class PlayersElement
    {
        public int id { get; set; }
        public string web_name { get; set; } = "";
        public int team { get; set; }
        public int element_type { get; set; }
        public int now_cost { get; set; }
        public int total_points { get; set; }
        public string form { get; set; } = "0";
        public string status { get; set; } = "a";
        public int minutes { get; set; }
    }
    public class PlayersTeam { public int id { get; set; } public string short_name { get; set; } = ""; }
    public class PlayersElementType { public int id { get; set; } public string singular_name_short { get; set; } = ""; }
    
    public class PlayerRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Team { get; set; } = "";
        public string Position { get; set; } = "";
        public string Cost { get; set; } = "";
        public int TotalPoints { get; set; }
        public double Xp { get; set; }
    }
}