namespace WaizeRoutingServer.Services;
using Models;
using System.Text.Json;

public interface IRoutingService
{
    Task<RouteDetails> GetDirectionsAsync(Coordinate origin, Coordinate destination);
    Task CheckItineraryChanges(Coordinate origin, Coordinate destination);
}

public class RoutingService : IRoutingService
{
    private string JCDecauxAPIKey = "85bcea73cc7f98dda1446228be5c8818bbf1ef9c";
    private readonly IApacheService _apacheService;
    private readonly ProxyClient _proxyClient;
    private RouteDetails savedRouteDetails;
    private bool checkedWeather = false;

    public RoutingService(IApacheService apacheService, ProxyClient proxyClient)
    {
        _apacheService = apacheService;
        _proxyClient = proxyClient;
    }

    public async Task<RouteDetails> GetDirectionsAsync(Coordinate origin, Coordinate destination)
    {
        // Step 1: Retrieve the city name from the origin
        var cityName = await GetCityNameAsync(origin);

        // Step 2: Retrieve the contract name from the city name
        var contractName = await GetContractNameAsync(cityName);

        List<Station> stations;
        if (contractName == "Unknown contract")
        {
            // Step 3: Retrieve all stations
            stations = await getAllStations();
        }
        else
        {
            // Step 3: Retrieve the stations for the contract
            stations = await GetStationsByContractAsync(contractName);
        }

        // Step 4: Find the 3 nearest stations to the origin
        var nearestStationsFromOrigin = GetNearestStations(stations, origin, true);

        // Step 5: Find the best station for pickup (minimizing walking distance)
        var (nearestStationFromOrigin, originFootDistance) = await GetNearestStationByFoot(nearestStationsFromOrigin, origin);

        if (!checkedWeather)
        {
            var weatherData = await GetWeatherDescriptionAsync(destination);
            _apacheService.sendInformation(weatherData);
            checkedWeather = true;
        }
        
        // Step 6: Find the 3 nearest stations to the destination
        stations.Remove(nearestStationFromOrigin); // Remove the pickup station
        var nearestStationsFromDestination = GetNearestStations(stations, destination, false);

        // Step 7: Find the best station for drop-off (minimizing walking distance)
        var (nearestStationFromDestination, destinationFootDistance) = await GetNearestStationByFoot(nearestStationsFromDestination, destination);

        // If it is not worth it to take the bicycle
        if (await GetWalkingDistanceAsync(origin, destination) < (originFootDistance + destinationFootDistance))
        {
            _apacheService.sendInformation("It is faster to walk directly to your destination");
            var (originToDestinationFootCoordinates, originToDestinationFootSteps) = await GetRouteGeometryAsync(origin, destination, 3);
            return new RouteDetails
            {
                WalkingToStation = new RouteSection { Mode = "walking", Coordinates = originToDestinationFootCoordinates, Steps = originToDestinationFootSteps },
            };
        }
        
        // Step 8: Get the walking route from origin -> first station
        var (firstWalkCoordinates, firstWalkSteps) = await GetRouteGeometryAsync(origin, nearestStationFromOrigin.Coordinate, 1);

        // Step 9: Get the biking route from first station -> second station
        var (bikeCoordinates, bikeSteps) = await GetRouteGeometryAsync(nearestStationFromOrigin.Coordinate, nearestStationFromDestination.Coordinate, 2);

        // Step 10: Get the walking route from second station -> destination
        var (secondWalkCoordinates, secondWalkSteps) = await GetRouteGeometryAsync(nearestStationFromDestination.Coordinate, destination, 3);

        // Combine all sections into a list of RouteSection objects
        
        savedRouteDetails = new RouteDetails
        {
            WalkingToStation = new RouteSection { Mode = "walking", Coordinates = firstWalkCoordinates, Steps = firstWalkSteps },
            BikingBetweenStations = new RouteSection { Mode = "biking", Coordinates = bikeCoordinates, Steps = bikeSteps },
            WalkingToDestination = new RouteSection { Mode = "walking", Coordinates = secondWalkCoordinates, Steps = secondWalkSteps },
            PickupStation = nearestStationFromOrigin,
            DropOffStation = nearestStationFromDestination
        };

        return savedRouteDetails;
    }

    private async Task<string> GetCityNameAsync(Coordinate coord)
    {
        try
        {
            string latitude = coord.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string longitude = coord.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json";
            
            var content = await _proxyClient.GetResponseAsync(url);
            
            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var root = jsonDoc.RootElement;
            
                if (root.TryGetProperty("address", out var addressElement) &&
                    addressElement.TryGetProperty("city", out var cityElement))
                {
                    return cityElement.GetString() ?? "Unknown city";
                }
            }
            
            return "Unknown location";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while calling nominatim API : {ex.Message}");
            throw new Exception("Can't retrieve city name");
        }
    }

    private async Task<string> GetContractNameAsync(string cityName)
    {
        try
        {
            var url = "https://api.jcdecaux.com/vls/v1/contracts?apiKey=" + JCDecauxAPIKey;

            var content = await _proxyClient.GetResponseAsync(url);

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                foreach (var contractElement in jsonDoc.RootElement.EnumerateArray())
                {
                    if (contractElement.TryGetProperty("cities", out var citiesElement) && citiesElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var city in citiesElement.EnumerateArray())
                        {
                            if (city.GetString()?.Equals(cityName, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                if (contractElement.TryGetProperty("name", out var nameElement))
                                {
                                    return nameElement.GetString() ?? "Unknown contract";
                                }
                            }
                        }
                    }
                }
            }

            return "Unknown contract";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error : {ex.Message}");
            throw new Exception("Can't retrieve contract name");
        }
    }

    private async Task<List<Station>> GetStationsByContractAsync(string contractName)
    {
        try
        {
            var url = $"https://api.jcdecaux.com/vls/v1/stations?contract={contractName}&apiKey=" + JCDecauxAPIKey;

            var content = await _proxyClient.GetResponseAsync(url);

            var stations = new List<Station>();
            using (var jsonDoc = JsonDocument.Parse(content))
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var positionElement = element.GetProperty("position");
                        var station = new Station
                        {
                            Coordinate = new Coordinate
                            {
                                Latitude = positionElement.GetProperty("lat").GetDouble(),
                                Longitude = positionElement.GetProperty("lng").GetDouble()
                            },
                            AvailableBikeStands = element.GetProperty("available_bike_stands").GetInt32(),
                            AvailableBikes = element.GetProperty("available_bikes").GetInt32(),
                            Status = element.GetProperty("status").GetString()
                        };
                        stations.Add(station);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'analyse d'une station : {ex.Message}");
                    }
                }

                return stations;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erreur HTTP : {ex.Message}");
            throw new Exception("Impossible d'accéder à l'API JCDecaux pour récupérer les stations.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur inattendue : {ex.Message}");
            throw new Exception("Erreur inattendue lors de la récupération des stations.");
        }
    }

    private async Task<List<Station>> getAllStations()
    {
        try
        {
            var url = $"https://api.jcdecaux.com/vls/v1/stations?apiKey=" + JCDecauxAPIKey;

            var content = await _proxyClient.GetResponseAsync(url);

            var stations = new List<Station>();
            using (var jsonDoc = JsonDocument.Parse(content))
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var positionElement = element.GetProperty("position");
                        var station = new Station
                        {
                            Coordinate = new Coordinate
                            {
                                Latitude = positionElement.GetProperty("lat").GetDouble(),
                                Longitude = positionElement.GetProperty("lng").GetDouble()
                            },
                            AvailableBikeStands = element.GetProperty("available_bike_stands").GetInt32(),
                            AvailableBikes = element.GetProperty("available_bikes").GetInt32(),
                            Status = element.GetProperty("status").GetString()
                        };
                        stations.Add(station);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de l'analyse d'une station : {ex.Message}");
                    }
                }

                return stations;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Erreur HTTP : {ex.Message}");
            throw new Exception("Impossible d'accéder à l'API JCDecaux pour récupérer les stations.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur inattendue : {ex.Message}");
            throw new Exception("Erreur inattendue lors de la récupération des stations.");
        }
    }

    private List<Station> GetNearestStations(List<Station> stations, Coordinate origin, bool pick)
    {
        var closestStations = stations
            .Where(station =>
                    station.Status == "OPEN" &&
                    ((pick && station.AvailableBikes > 1) ||
                     (!pick && station.AvailableBikeStands > 1))
            )
            .Select(station => new
            {
                Station = station,
                Distance = CalculateDistance(origin, new Coordinate
                {
                    Latitude = station.Coordinate.Latitude,
                    Longitude = station.Coordinate.Longitude
                })
            })
            .OrderBy(x => x.Distance)
            .Take(3)
            .Select(x => x.Station)
            .ToList();

        return closestStations;
    }
    
    private double CalculateDistance(Coordinate coord1, Coordinate coord2)
    {
        const double EarthRadiusKm = 6371;

        double dLat = DegreesToRadians(coord2.Latitude - coord1.Latitude);
        double dLon = DegreesToRadians(coord2.Longitude - coord1.Longitude);

        double lat1 = DegreesToRadians(coord1.Latitude);
        double lat2 = DegreesToRadians(coord2.Latitude);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private async Task<(Station NearestStation, double Distance)> GetNearestStationByFoot(List<Station> stations, Coordinate origin)
    {
        Station nearestStation = null;
        double shortestDistance = double.MaxValue;

        foreach (var station in stations)
        {
            double walkingDistance = await GetWalkingDistanceAsync(origin, station.Coordinate);
            Console.WriteLine($"Distance à pied vers la station {station.Coordinate.Latitude}, {station.Coordinate.Longitude} : {walkingDistance} mètres");

            if (walkingDistance < shortestDistance)
            {
                shortestDistance = walkingDistance;
                nearestStation = station;
            }
        }

        Console.WriteLine($"Station optimale à pied : {nearestStation?.Coordinate.Latitude}, {nearestStation?.Coordinate.Longitude} | Distance : {shortestDistance} mètres");

        return (nearestStation, shortestDistance);
    }
    
    private async Task<double> GetWalkingDistanceAsync(Coordinate origin, Coordinate destination)
    {
        try
        {
            string originLatitude = origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string originLongitude = origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLatitude = destination.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLongitude = destination.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://routing.openstreetmap.de/routed-foot/route/v1/inutile/{originLongitude},{originLatitude};{destinationLongitude},{destinationLatitude}?overview=false&steps=false";

            var content = await _proxyClient.GetResponseAsync(url);

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var distance = jsonDoc.RootElement
                    .GetProperty("routes")[0]
                    .GetProperty("legs")[0]
                    .GetProperty("distance")
                    .GetDouble();
                return distance;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération de la distance à pied : {ex.Message}");
            return double.MaxValue;
        }
    }
    
    private async Task<(List<Coordinate>, List<DirectionStep>)> GetRouteGeometryAsync(Coordinate origin, Coordinate destination, int section)
    {
        try
        {
            bool isWalking = section == 1 || section == 3;
            string mode = isWalking ? "routed-foot" : "routed-bike";

            string originLatitude = origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string originLongitude = origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLatitude = destination.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLongitude = destination.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var url = $"https://routing.openstreetmap.de/{mode}/route/v1/driving/{originLongitude},{originLatitude};{destinationLongitude},{destinationLatitude}?overview=full&geometries=polyline&steps=true";

            Console.WriteLine($"API call for directions ({(isWalking ? "walking" : "biking")}): {url}");

            var content = await _proxyClient.GetResponseAsync(url);

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var route = jsonDoc.RootElement.GetProperty("routes")[0];

                var encodedGeometry = route.GetProperty("geometry").GetString();
                var coordinates = DecodePolyline(encodedGeometry);

                var steps = route.GetProperty("legs")[0].GetProperty("steps")
                    .EnumerateArray()
                    .Select(step =>
                    {
                        string instruction = step.GetProperty("maneuver").TryGetProperty("modifier", out var modifierProp)
                            ? modifierProp.GetString()
                            : null;

                        return new DirectionStep
                        {
                            Distance = step.GetProperty("distance").GetDouble(),
                            Instruction = instruction, // Assign the safely retrieved instruction
                            StreetName = step.GetProperty("name").GetString() ?? "Unknown street" // Default to "Unknown street" if name is null
                        };
                    })
                    .ToList();

                if (steps.Any())
                {
                    var lastStep = steps.Last();
                    switch (section)
                    {
                        case 1:
                            lastStep.Type = "pickup-station";
                            break;
                        case 2:
                            lastStep.Type = "dropoff-station";
                            break;
                        case 3:
                            lastStep.Type = "destination";
                            break;
                    }
                }

                return (coordinates, steps);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching route geometry and steps: {ex.Message}");
            return (new List<Coordinate>(), new List<DirectionStep>());
        }
    }
    
    public static List<Coordinate> DecodePolyline(string encodedPolyline)
    {
        var polyline = new List<Coordinate>();
        int index = 0, len = encodedPolyline.Length;
        int lat = 0, lng = 0;

        while (index < len)
        {
            int b, shift = 0, result = 0;
            do
            {
                b = encodedPolyline[index++] - 63;
                result |= (b & 0x1F) << shift;
                shift += 5;
            } while (b >= 0x20);
            int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            lat += dlat;

            shift = 0;
            result = 0;
            do
            {
                b = encodedPolyline[index++] - 63;
                result |= (b & 0x1F) << shift;
                shift += 5;
            } while (b >= 0x20);
            int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
            lng += dlng;

            var latitude = lat / 1E5;
            var longitude = lng / 1E5;
            polyline.Add(new Coordinate
            {
                Latitude = latitude,
                Longitude = longitude
            });
        }

        return polyline;
    }

    public async Task CheckItineraryChanges(Coordinate origin, Coordinate destination)
    {
        try
        {
            var currentRouteDetails = await GetDirectionsAsync(origin, destination);

            if (!currentRouteDetails.Equals(savedRouteDetails))
            {
                _apacheService.sendInformation("The itinerary has changed!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during itinerary check: {ex.Message}");
            throw;
        }
    }
    
    private async Task<string> GetWeatherDescriptionAsync(Coordinate coordinate)
    {
        try
        {
            string destinationLatitude = coordinate.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLongitude = coordinate.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var apiKey = "34539aa0008b0fe2b4598f3e8a6f6eb4";
            var url = $"https://api.openweathermap.org/data/2.5/weather?lat={destinationLatitude}&lon={destinationLongitude}&appid={apiKey}&units=metric";
            var content = await _proxyClient.GetResponseAsync(url);

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var weatherData = jsonDoc.RootElement
                    .GetProperty("weather")[0]
                    .GetProperty("description").GetString();
                return weatherData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération de la distance à pied : {ex.Message}");
            return "";
        }
    }
}

public class RouteDetails
{
    public RouteSection WalkingToStation { get; set; }
    public RouteSection BikingBetweenStations { get; set; }
    public RouteSection WalkingToDestination { get; set; }
    public Station PickupStation { get; set; }
    public Station DropOffStation { get; set; }
    
    public override bool Equals(object obj)
    {
        if (obj is not RouteDetails other)
            return false;

        return Equals(WalkingToStation, other.WalkingToStation) &&
               Equals(BikingBetweenStations, other.BikingBetweenStations) &&
               Equals(WalkingToDestination, other.WalkingToDestination) &&
               Equals(PickupStation, other.PickupStation) &&
               Equals(DropOffStation, other.DropOffStation);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(WalkingToStation, BikingBetweenStations, WalkingToDestination, PickupStation, DropOffStation);
    }
}

public class RouteSection
{
    public string Mode { get; set; } // "walking" or "biking"
    public List<Coordinate> Coordinates { get; set; }
    public List<DirectionStep> Steps { get; set; } // Steps for navigation

}

public class DirectionStep
{
    public double Distance { get; set; }
    public string Instruction { get; set; }
    public string StreetName { get; set; }
    public string Type { get; set; }
}

public class Station
{
    public Coordinate Coordinate { get; set; }
    public int AvailableBikeStands { get; set; }
    public int AvailableBikes { get; set; }
    public string Status { get; set; }
}