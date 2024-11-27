using Microsoft.AspNetCore.Mvc;

namespace WaizeRoutingServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItineraryController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetItinerary(string origin, string destination)
    {
        var result = "hello world";
        return Ok(result);
    }
}