using CoreWCF;

[ServiceContract]
public interface IProxyCacheService
{
    [OperationContract]
    string GetResponse(string url);
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
public class ProxyCacheService : IProxyCacheService
{
    private readonly GenericCache<string> _cache;
    private TimeSpan _cacheDuration = TimeSpan.FromSeconds(60);
    private readonly HttpClient _httpClient = new();

    public ProxyCacheService()
    {
        _cache = new GenericCache<string>("HttpResponseCache", _cacheDuration);
    }

    public string GetResponse(string url)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            return _cache.Get(url, () =>
            {
                var response = _httpClient.GetStringAsync(url).Result;
                return response;
            });
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request failed: {ex.Message}");
            throw new Exception("Error while calling external API", ex);
        }
    }
}