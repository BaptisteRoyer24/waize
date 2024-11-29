using Microsoft.AspNetCore.Mvc;

namespace WaizeRoutingServer.Controllers;
using Services;
using Models;

[ApiController]
[Route("api/[controller]")]
public class RoutingController : ControllerBase
{
    private readonly IRoutingService _routingService;

    public RoutingController(IRoutingService routingService)
    {
        _routingService = routingService;
    }

    [HttpGet("directions")]
    public async Task<IActionResult> GetDirections([FromQuery] double originLat, [FromQuery] double originLng, [FromQuery] double destLat, [FromQuery] double destLng)
    {
        var origin = new Coordinate { Latitude = originLat, Longitude = originLng };
        var destination = new Coordinate { Latitude = destLat, Longitude = destLng };

        var directions = await _routingService.GetDirectionsAsync(origin, destination);
        return Ok(directions);
    }
}