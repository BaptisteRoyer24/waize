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

    public T Get(string key, Func<T> fetchFunc, TimeSpan? duration = null)
    {
        if (_cache.Contains(key))
        {
            return (T)_cache.Get(key);
        }

        var value = fetchFunc();

        _cache.Set(key, value, DateTimeOffset.Now.Add(duration ?? _defaultCacheDuration));

        return value;
    }
}
