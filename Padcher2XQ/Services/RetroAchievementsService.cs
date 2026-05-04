using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public class RetroAchievementsService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    
    public RetroAchievementsService(SettingsService settings)
    {
        _httpClient = new HttpClient();
        // Сервери RA дуже не люблять "анонімні" запити, тому представляємось:
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Padcher2XQ/1.0");
        _settings = settings;
    }

    public async Task<(bool isSupported, string resultMessage)> CheckHashSupportAsync(string md5Hash)
    {
        if (string.IsNullOrEmpty(md5Hash) || md5Hash == "---") return (false, "Invalid Hash");

        string apiUser = _settings.Config.RaUser?.Trim() ?? ""; 
        string apiKey = _settings.Config.RaApiKey?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(apiUser) || string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "RA keys missing in settings");
        }

        try
        {
            // КРОК 1: Звертаємось до Емуляторного API (dorequest.php), яке знає хеші!
            string hashUrl = $"https://retroachievements.org/dorequest.php?r=gameid&m={md5Hash.ToLower()}";
            
            HttpResponseMessage hashResponse = await _httpClient.GetAsync(hashUrl);
            hashResponse.EnsureSuccessStatusCode();
            
            string hashBody = await hashResponse.Content.ReadAsStringAsync();
            
            if (string.IsNullOrWhiteSpace(hashBody) || hashBody == "{}"  || hashBody.Contains("\"GameID\":0"))
            {
                return (false, "Hash not found in RA Database");
            }
            using JsonDocument hashDoc = JsonDocument.Parse(hashBody);
            if (!hashDoc.RootElement.TryGetProperty("GameID", out JsonElement gameIdElement))
            {
                return (false, "Hash not found in RA Database");
            }
            int gameId = gameIdElement.GetInt32();
            if (gameId == 0) return (false, "Hash not found in RA Database");
            string gameUrl = $"https://retroachievements.org/API/API_GetGame.php?u={apiUser}&y={apiKey}&i={gameId}";
            HttpResponseMessage gameResponse = await _httpClient.GetAsync(gameUrl);
            
            if (gameResponse.IsSuccessStatusCode)
            {
                string gameBody = await gameResponse.Content.ReadAsStringAsync();
                using JsonDocument gameDoc = JsonDocument.Parse(gameBody);
                
                // Витягуємо красиву назву
                if (gameDoc.RootElement.TryGetProperty("Title", out JsonElement titleElement))
                {
                    return (true, titleElement.GetString() ?? $"Game ID: {gameId}");
                }
            }
            
            return (true, $"Supported (Game ID: {gameId})");
        }
        catch (Exception ex)
        {
            return (false, $"API Error: {ex.Message}");
        }
    }
}