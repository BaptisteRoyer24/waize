using System.Runtime.Caching;

public class GenericCache<T>
{
    private readonly MemoryCache _cache;
    private readonly TimeSpan _defaultCacheDuration;

    public GenericCache(string cacheName, TimeSpan defaultCacheDuration)
    {
        _cache = new MemoryCache(cacheName);
        _defaultCacheDuration = defaultCacheDuration;
    }

    public T Get(string key, Func<T> fetchFunc, Action<CacheItemPolicy> configurePolicy = null)
    {
        if (_cache.Contains(key))
        {
            return (T)_cache.Get(key);
        }

        var value = fetchFunc();

        var policy = new CacheItemPolicy();
        configurePolicy?.Invoke(policy);

        _cache.Set(key, value, policy);

        return value;
    }
}
