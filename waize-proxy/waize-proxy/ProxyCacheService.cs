using CoreWCF;
using System.Runtime.Caching;

[ServiceContract]
public interface IProxyCacheService
{
    [OperationContract]
    string GetResponse(string url);

    [OperationContract]
    void SetCacheDuration(int seconds);
}

[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
public class ProxyCacheService : IProxyCacheService
{
    private readonly MemoryCache _cache = MemoryCache.Default;
    private int _cacheDuration = 60; // Durée de cache par défaut en secondes
    private readonly HttpClient _httpClient = new ();

    public string GetResponse(string url)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "WaizeRoutingServer/1.0 (contact@yourdomain.com)");

            // Vérifie si la réponse est dans le cache
            if (_cache.Contains(url))
            {
                return (string)_cache.Get(url);
            }

            // Si non trouvé, effectue une requête HTTP
            var response = _httpClient.GetStringAsync(url).Result;

            // Ajoute au cache
            _cache.Set(url, response, DateTimeOffset.Now.AddSeconds(_cacheDuration));

            return response;   
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP Request failed: {ex.Message}");
            throw new Exception("Error while calling external API", ex);
        }
    }

    public void SetCacheDuration(int seconds)
    {
        _cacheDuration = seconds;
    }
}