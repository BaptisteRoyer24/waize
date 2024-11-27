namespace WaizeRoutingServer.Models;
using System.Runtime.Serialization;

[DataContract]
public class ItineraryResponse
{
    [DataMember]
    public string Origin { get; set; }

    [DataMember]
    public string Destination { get; set; }

    [DataMember]
    public string RouteDetails { get; set; }
}