namespace WaizeRoutingServer.Services;
using CoreWCF;
using Models;

[ServiceContract]
public interface ISoapItineraryService
{
    [OperationContract]
    Task<ItineraryResponse> GetDetailedItinerary(string origin, string destination);
}

public class SoapItineraryService : ISoapItineraryService
{
    private readonly ItineraryService _itineraryService;

    // Injecter le service métier
    public SoapItineraryService(ItineraryService itineraryService)
    {
        _itineraryService = itineraryService;
    }

    // Implémentation de l'opération SOAP
    public async Task<ItineraryResponse> GetDetailedItinerary(string origin, string destination)
    {
        // Appelle le service métier pour obtenir l'itinéraire
        var result = await _itineraryService.ComputeItineraryAsync(origin, destination);

        // Transforme le résultat en ItineraryResponse
        return new ItineraryResponse
        {
            Origin = origin,
            Destination = destination,
            RouteDetails = result
        };
    }
}