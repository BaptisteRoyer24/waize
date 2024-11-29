namespace WaizeRoutingServer.Services;
using Models;
using System.Text.Json;

public interface IRoutingService
{
    Task<List<RouteSection>> GetDirectionsAsync(Coordinate origin, Coordinate destination);
}

public class RoutingService : IRoutingService
{
    private readonly HttpClient _httpClient;
    private string JCDecauxAPIKey = "85bcea73cc7f98dda1446228be5c8818bbf1ef9c";

    public RoutingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<RouteSection>> GetDirectionsAsync(Coordinate origin, Coordinate destination)
    {
        // Step 1: Retrieve the city name from the origin
        var cityName = await GetCityNameAsync(origin);
        Console.WriteLine(cityName);

        // Step 2: Retrieve the contract name from the city name
        var contractName = await GetContractNameAsync(cityName);
        Console.WriteLine(contractName);

        // Step 3: Retrieve the stations for the contract
        var stations = await GetStationsByContractAsync(contractName);

        // Step 4: Find the 3 nearest stations to the origin
        var nearestStationsFromOrigin = GetNearestStations(stations, origin, true);

        // Step 5: Find the best station for pickup (minimizing walking distance)
        var nearestStationFromOrigin = await GetNearestStationByFoot(nearestStationsFromOrigin, origin);

        // Step 6: Find the 3 nearest stations to the destination
        stations.Remove(nearestStationFromOrigin); // Remove the pickup station
        var nearestStationsFromDestination = GetNearestStations(stations, destination, false);

        // Step 7: Find the best station for drop-off (minimizing walking distance)
        var nearestStationFromDestination = await GetNearestStationByFoot(nearestStationsFromDestination, destination);

        // Step 8: Get the walking route from origin -> first station
        var firstWalkSection = await GetRouteGeometryAsync(origin, nearestStationFromOrigin.Coordinate, true);

        // Step 9: Get the biking route from first station -> second station
        var bikeSection = await GetRouteGeometryAsync(nearestStationFromOrigin.Coordinate, nearestStationFromDestination.Coordinate, false);

        // Step 10: Get the walking route from second station -> destination
        var secondWalkSection = await GetRouteGeometryAsync(nearestStationFromDestination.Coordinate, destination, true);

        // Combine all sections into a list of RouteSection objects
        return new List<RouteSection>
        {
            new RouteSection { Mode = "walking", Coordinates = firstWalkSection },
            new RouteSection { Mode = "biking", Coordinates = bikeSection },
            new RouteSection { Mode = "walking", Coordinates = secondWalkSection }
        };
    }

    private async Task<string> GetCityNameAsync(Coordinate coord)
    {
        try
        {
            string latitude = coord.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string longitude = coord.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

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
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error while calling nominatim API : {ex.Message}");
            throw new Exception("Can't retrieve city name");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Global error : {ex.Message}");
            throw new Exception("Unexpected error");
        }
    }

    private async Task<string> GetContractNameAsync(string cityName)
    {
        try
        {
            var url = "https://api.jcdecaux.com/vls/v1/contracts?apiKey=" + JCDecauxAPIKey;

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

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
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error HTTP : {ex.Message}");
            throw new Exception("Can't retrieve contract name");
        }
        catch (Exception)
        {
            throw new Exception("Unexpected error");
        }
    }

    private async Task<List<Station>> GetStationsByContractAsync(string contractName)
    {
        try
        {
            var url = $"https://api.jcdecaux.com/vls/v1/stations?contract={contractName}&apiKey=" + JCDecauxAPIKey;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

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

    private async Task<Station> GetNearestStationByFoot(List<Station> stations, Coordinate origin)
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

        Console.WriteLine($"Station optimale à pied : {nearestStation.Coordinate.Latitude}, {nearestStation.Coordinate.Longitude} | Distance : {shortestDistance} mètres");

        return nearestStation;
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

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

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
    
    private async Task<List<Coordinate>> GetRouteGeometryAsync(Coordinate origin, Coordinate destination, bool isWalking)
    {
        try
        {
            
            string mode = isWalking ? "routed-foot" : "routed-bike";
            
            string originLatitude = origin.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string originLongitude = origin.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLatitude = destination.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string destinationLongitude = destination.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://routing.openstreetmap.de/{mode}/route/v1/driving/{originLongitude},{originLatitude};{destinationLongitude},{destinationLatitude}?overview=full&geometries=polyline&steps=true";

            Console.WriteLine($"Appel de l'API pour les directions ({(isWalking ? "à pied" : "à vélo")}) : {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            using (var jsonDoc = JsonDocument.Parse(content))
            {
                var route = jsonDoc.RootElement.GetProperty("routes")[0];
                var encodedGeometry = route.GetProperty("geometry").GetString();

                return DecodePolyline(encodedGeometry);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la récupération de l'itinéraire : {ex.Message}");
            return null;
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
}

public class RouteSection
{
    public string Mode { get; set; } // "walking" or "biking"
    public List<Coordinate> Coordinates { get; set; }
}

public class Station
{
    public Coordinate Coordinate { get; set; }
    public int AvailableBikeStands { get; set; }
    public int AvailableBikes { get; set; }
    public string Status { get; set; }
}