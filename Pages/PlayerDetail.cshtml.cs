using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LeagueVisionSimple.Pages
{
    public class PlayerDetailModel : PageModel
    {
        public string PlayerName { get; set; } = "";
        public List<GameweekStat> Stats { get; set; } = new();

        public async Task OnGetAsync(int id)
        {
            using var client = new HttpClient();
            
            var bootstrapJson = await client.GetStringAsync("https://fantasy.premierleague.com/api/bootstrap-static/");
            var bootstrap = JsonSerializer.Deserialize<DetailFplData>(bootstrapJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            PlayerName = bootstrap?.elements.FirstOrDefault(p => p.id == id)?.web_name ?? "Unknown Player";

            var historyJson = await client.GetStringAsync($"https://fantasy.premierleague.com/api/element-summary/{id}/");
            var historyData = JsonSerializer.Deserialize<DetailSummary>(historyJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (historyData != null)
            {
                foreach (var h in historyData.history.Take(10))
                {
                    int predicted = (int)Math.Round(h.total_points * 0.8); 
                    Stats.Add(new GameweekStat
                    {
                        Gameweek = h.round,
                        ActualPoints = h.total_points,
                        PredictedPoints = predicted
                    });
                }
            }
        }
    }

    // Unique classes for the Player Detail Page
    public class DetailFplData
    {
        public List<DetailElement> elements { get; set; } = new();
    }
    public class DetailElement
    {
        public int id { get; set; }
        public string web_name { get; set; } = "";
    }
    public class DetailSummary
    {
        public List<DetailHistory> history { get; set; } = new();
    }
    public class DetailHistory
    {
        public int round { get; set; }
        public int total_points { get; set; }
    }
    public class GameweekStat
    {
        public int Gameweek { get; set; }
        public int ActualPoints { get; set; }
        public int PredictedPoints { get; set; }
    }
}