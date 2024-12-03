using Microsoft.AspNetCore.Mvc;

namespace WaizeRoutingServer.Controllers;
using Services;
using Models;

[ApiController]
[Route("api/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly IRoutingService _routingService;
    private readonly IAutocompleteService _autocompleteService;

    public RoutingController(IRoutingService routingService, IAutocompleteService autocompleteService)
    {
        _routingService = routingService;
        _autocompleteService = autocompleteService;
    }

    [HttpGet("directions")]
    public async Task<IActionResult> GetDirections([FromQuery] double originLat, [FromQuery] double originLng, [FromQuery] double destLat, [FromQuery] double destLng)
    {
        var origin = new Coordinate { Latitude = originLat, Longitude = originLng };
        var destination = new Coordinate { Latitude = destLat, Longitude = destLng };

        var directions = await _routingService.GetDirectionsAsync(origin, destination);
        return Ok(directions);
    }
    
    [HttpGet("check-itinerary")]
    public async Task<IActionResult> CheckItinerary([FromQuery] double originLat, [FromQuery] double originLng, [FromQuery] double destLat, [FromQuery] double destLng)
    {
        var origin = new Coordinate { Latitude = originLat, Longitude = originLng };
        var destination = new Coordinate { Latitude = destLat, Longitude = destLng };

        await _routingService.CheckItineraryChanges(origin, destination);
        return Ok("Itinerary check completed");
    }
    
    [HttpGet("autocomplete")]
    public async Task<IActionResult> GetAutocomplete([FromQuery] string input)
    {
        var autocomplete = await _autocompleteService.GetAutocompleteAsync(input);
        return Ok(autocomplete);
    }
}