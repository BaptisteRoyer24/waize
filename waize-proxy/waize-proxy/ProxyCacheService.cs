using CoreWCF;
using System.Runtime.Caching;

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
    private readonly HttpClient _httpClient;

    public ProxyCacheService()
    {
        _cache = new GenericCache<string>("HttpResponseCache", _cacheDuration);


        var handler = new HttpClientHandler
        {
            MaxConnectionsPerServer = 10
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public string GetResponse(string url)
    {
        try
        {
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");
            }

            return _cache.Get(url, () =>
                {
                    var response = _httpClient.GetStringAsync(url).Result;
                    return response;
                },
                policy =>
                {
                    policy.AbsoluteExpiration = DateTimeOffset.Now.Add(_cacheDuration);
                    policy.Priority = CacheItemPriority.Default;
                });
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request failed: {ex.Message}");
            throw new Exception("Error while calling external API", ex);
        }
    }
}