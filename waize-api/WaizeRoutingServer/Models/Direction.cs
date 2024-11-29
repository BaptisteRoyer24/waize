namespace WaizeRoutingServer.Models;

public class Direction
{
    public Coordinate Start { get; set; }
    public Coordinate End { get; set; }
    public double Distance { get; set; }
    public double Duration { get; set; }
}