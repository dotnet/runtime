## About

<!-- A description of the package and where one can find more documentation -->

Provides implementations for local and distributed in-memory cache. It stores and retrieves data in a fast and efficient way.

## Key Features

<!-- The key features of this package -->

* A concrete implementation of the IMemoryCache interface, which represents a local in-memory cache that stores and retrieves data in a fast and efficient way
* A distributed cache that supports higher scale-out than local cache
* Expiration and eviction policies for its entries
* Entry prioritization for when the cache size limit is exceeded and needs to be compacted by entry eviction
* Track of cache statictics

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Use Microsoft.Extensions.Caching.Memory over System.Runtime.Caching when working with ASP.NET Core as it provides better integration support. For example, IMemoryCache works natively with ASP.NET Core dependency injection.

Local in-memory serialization:
```csharp
using Microsoft.Extensions.Caching.Memory;

using MemoryCache cache = new(new MemoryCacheOptions());

object valueToCache = new();
string key = "key";

using (ICacheEntry entry = cache.CreateEntry(key))
{
    // Entries are committed after they are disposed therefore it does not exist yet.
    Console.WriteLine($"Exists: {cache.TryGetValue(key, out _)}\n");

    entry.Value = valueToCache;
    entry.SlidingExpiration = TimeSpan.FromSeconds(2);
}

bool exists = cache.TryGetValue(key, out object? cachedValue);
Console.WriteLine($"Exists: {exists}" );
Console.WriteLine($"cachedValue is valueToCache? {object.ReferenceEquals(cachedValue, valueToCache)}\n");

Console.WriteLine("Wait for the sliding expiration...");
Thread.Sleep(TimeSpan.FromSeconds(2));

Console.WriteLine("Exists: " + cache.TryGetValue(key, out _));

// You can also use the acceleration extensions to set and get entries
string key2 = "key2";
object value2 = new();

cache.Set("key2", value2);

object? cachedValue2 = cache.Get(key2);
Console.WriteLine($"cachedValue2 is value2? {object.ReferenceEquals(cachedValue2, value2)}");
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Caching.Memory.MemoryCache`
* `Microsoft.Extensions.Caching.Memory.MemoryCacheOptions`
* `Microsoft.Extensions.Caching.Distributed.MemoryDistributedCache`
* `Microsoft.Extensions.Caching.Memory.MemoryDistributedCacheOptions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/caching)
* [Cache in-memory in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/memory)
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory)

## Related Packages

<!-- The related packages associated with this package -->

[Microsoft.Extensions.Caching.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Abstractions)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Caching.Memory is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
