namespace WaizeRoutingServer.Services;

public class ItineraryService
{
    public async Task<string> ComputeItineraryAsync(string origin, string destination)
    {
        await Task.Delay(500);
        return $"Itinerary from {origin} to {destination} calculated!";
    }
}