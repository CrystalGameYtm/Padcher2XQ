using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public class RetroAchievementsService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    
    // Тепер DI-контейнер сам передасть сюди SettingsService
    public RetroAchievementsService(SettingsService settings)
    {
        _httpClient = new HttpClient();
        _settings = settings;
    }

    public async Task<(bool isSupported, string gameTitle)> CheckHashSupportAsync(string md5Hash)
    {
        if (string.IsNullOrEmpty(md5Hash) || md5Hash == "---") return (false, "Invalid Hash");

        string apiUser = _settings.Config.RaUser; 
        string apiKey = _settings.Config.RaApiKey;

        if (string.IsNullOrWhiteSpace(apiUser) || string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "RA API Keys not configured in Settings");
        }

        try
        {
            // Формуємо запит до офіційного API
            string url = $"https://retroachievements.org/API/API_GetGameFromContext.php?u={apiUser}&y={apiKey}&c={md5Hash.ToLower()}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            
            if (responseBody.Contains("\"GameID\":0") || responseBody.Contains("Game not found"))
            {
                return (false, "Not Supported by RA");
            }

            using JsonDocument doc = JsonDocument.Parse(responseBody);
            string title = doc.RootElement.GetProperty("GameTitle").GetString() ?? "Unknown Game";

            return (true, title);
        }
        catch (Exception ex)
        {
            return (false, $"API Error: {ex.Message}");
        }
    }
}