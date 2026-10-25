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
    public class AnalyzeModel : PageModel
    {
        [BindProperty]
        public int TeamId { get; set; }
        
        public string Message { get; set; } = "";
        public List<TransferSuggestion> Suggestions { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (TeamId <= 0)
            {
                Message = "Please enter a valid Team ID.";
                return Page();
            }

            try
            {
                using var client = new HttpClient();
                
                var json = await client.GetStringAsync($"https://fantasy.premierleague.com/api/entry/{TeamId}/");
                var entry = JsonSerializer.Deserialize<AnalyzeEntry>(json);
                if (entry != null)
                {
                    Message = $"Squad analyzed for: {entry.name} (Rank: {entry.summary_overall_rank})";
                }

                var bootstrapJson = await client.GetStringAsync("https://fantasy.premierleague.com/api/bootstrap-static/");
                var bootstrap = JsonSerializer.Deserialize<AnalyzeFplData>(bootstrapJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (bootstrap == null) return Page();

                var currentGw = bootstrap.events.FirstOrDefault(e => e.is_current) ?? bootstrap.events.FirstOrDefault(e => e.is_next);
                if (currentGw == null) { Message += " Could not find current gameweek."; return Page(); }

                var picksJson = await client.GetStringAsync($"https://fantasy.premierleague.com/api/entry/{TeamId}/event/{currentGw.id}/picks/");
                var picksData = JsonSerializer.Deserialize<AnalyzePicksData>(picksJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (picksData == null || picksData.picks == null) { Message += " Could not load squad picks."; return Page(); }

                double bank = picksData.entry_history.bank / 10.0;
                var mySquadIds = picksData.picks.Select(p => p.element).ToList();
                var usedReplacementIds = new HashSet<int>();

                // Helper function to calculate Expected Points (xP)
                double GetXp(AnalyzeElement p)
                {
                    if (p.status != "a" || p.minutes < 90) return 0;
                    return double.TryParse(p.form, out double f) ? f : 0;
                }

                // 1. Calculate the xP for ALL players in the game so we can compare
                var allPlayersWithXp = bootstrap.elements.Select(p => new { 
                    Player = p, 
                    Xp = GetXp(p) 
                }).ToList();

                // 2. Loop through the user's squad
                foreach (var myPick in picksData.picks)
                {
                    var myPlayerData = allPlayersWithXp.FirstOrDefault(p => p.Player.id == myPick.element);
                    if (myPlayerData == null) continue;

                    var myPlayer = myPlayerData.Player;
                    double myXp = myPlayerData.Xp;

                    // SMART LOGIC: We ONLY try to sell this player if their Expected Points are really low (under 3.0)
                    // This stops the app from recommending you sell your top players!
                    if (myXp >= 3.0) continue;

                    double budget = (myPlayer.now_cost / 10.0) + bank;

                    // Find a replacement who has a MUCH better xP (at least +2.0 points better)
                    var bestReplacementData = allPlayersWithXp
                        .Where(p => p.Player.element_type == myPlayer.element_type) 
                        .Where(p => !mySquadIds.Contains(p.Player.id)) 
                        .Where(p => !usedReplacementIds.Contains(p.Player.id)) 
                        .Where(p => (p.Player.now_cost / 10.0) <= budget) 
                        .Where(p => p.Xp > myXp + 2.0) // Must be significantly better
                        .OrderByDescending(p => p.Xp) 
                        .FirstOrDefault();

                    if (bestReplacementData != null)
                    {
                        var bestReplacement = bestReplacementData.Player;
                        Suggestions.Add(new TransferSuggestion
                        {
                            SellName = myPlayer.web_name,
                            SellXp = myXp,
                            BuyName = bestReplacement.web_name,
                            BuyXp = bestReplacementData.Xp,
                            BuyCost = (bestReplacement.now_cost / 10.0).ToString("0.0")
                        });

                        usedReplacementIds.Add(bestReplacement.id);
                    }
                }
            }
            catch
            {
                Message = "Could not find a manager with that ID, or the API is down.";
            }

            return Page();
        }
    }

    // --- Unique classes for Analyze Page ---
    public class AnalyzeEntry
    {
        public string name { get; set; } = "";
        public int summary_overall_rank { get; set; }
    }
    public class AnalyzeFplData
    {
        public List<AnalyzeElement> elements { get; set; } = new();
        public List<AnalyzeEvent> events { get; set; } = new();
    }
    public class AnalyzeElement
    {
        public int id { get; set; }
        public string web_name { get; set; } = "";
        public int element_type { get; set; }
        public int now_cost { get; set; }
        public string form { get; set; } = "0";
        public string status { get; set; } = "a";
        public int minutes { get; set; }
    }
    public class AnalyzeEvent { public int id { get; set; } public bool is_current { get; set; } public bool is_next { get; set; } }
    public class AnalyzePicksData { public List<AnalyzePick> picks { get; set; } = new(); public AnalyzeEntryHistory entry_history { get; set; } = new(); }
    public class AnalyzePick { public int element { get; set; } }
    public class AnalyzeEntryHistory { public int bank { get; set; } }

    public class TransferSuggestion
    {
        public string SellName { get; set; } = "";
        public double SellXp { get; set; }
        public string BuyName { get; set; } = "";
        public double BuyXp { get; set; }
        public string BuyCost { get; set; } = "";
    }
}