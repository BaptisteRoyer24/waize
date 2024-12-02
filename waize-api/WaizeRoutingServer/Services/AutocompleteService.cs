using WaizeRoutingServer.Models;

namespace WaizeRoutingServer.Services;
using System.Text.Json;

public interface IAutocompleteService
{
    Task<List<Suggestion>> GetAutocompleteAsync(string input);
}

public class AutocompleteService : IAutocompleteService
{
    private readonly HttpClient _httpClient;
    
    public AutocompleteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Suggestion>> GetAutocompleteAsync(string input)
    {
        try
        {
            var url = $"https://nominatim.openstreetmap.org/search?q={input}&format=json&addressdetails=1&limit=5";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var suggestions = new List<Suggestion>();

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var displayName = element.GetProperty("display_name").GetString();
                    Coordinate coordinate = new Coordinate
                    {
                        Latitude = double.Parse(element.GetProperty("lat").GetString(), System.Globalization.CultureInfo.InvariantCulture),
                        Longitude = double.Parse(element.GetProperty("lon").GetString(), System.Globalization.CultureInfo.InvariantCulture),
                    };
                    suggestions.Add(new Suggestion
                    {
                        DisplayName = displayName,
                        Coordinates = coordinate
                    });
                }
            }

            return suggestions;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Error: {ex.Message}");
            throw new Exception("Unable to access the API to fetch autocomplete suggestions.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected Error: {ex.Message}");
            throw new Exception("Unexpected error while fetching autocomplete suggestions.");
        }
    }
}

public class Suggestion
{
    public string DisplayName { get; set; }
    public Coordinate Coordinates { get; set; }
}