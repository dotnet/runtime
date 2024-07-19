# Microsoft.Extensions.Caching.Memory

In-memory caching provides a general purpose memory implementation of the core caching abstractions provided under `Microsoft.Extensions.Caching.Abstractions` and it is great for apps that run in a single server, where all cached data rents memory in the app's process.

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/caching.

## Example

The following example shows how to instantiate a single memory cache using `AddMemoryCache` API and via DI get it injected to enable them calling `GetCurrentStatistics`

```c#
// when using `services.AddMemoryCache(options => options.TrackStatistics = true);` to instantiate

    [EventSource(Name = "Microsoft-Extensions-Caching-Memory")]
    internal sealed class CachingEventSource : EventSource
    {
        public CachingEventSource(IMemoryCache memoryCache) { _memoryCache = memoryCache; }
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                if (_cacheHitsCounter == null)
                {
                    _cacheHitsCounter = new PollingCounter("cache-hits", this, () =>
                        _memoryCache.GetCurrentStatistics().CacheHits)
                    {
                        DisplayName = "Cache hits",
                    };
                }
            }
        }
    }
```

The stats can be viewed using the above event counter like below with `dotnet-counters` tool:

<img width="400" alt="image" src="https://user-images.githubusercontent.com/5897654/156053460-46db5070-04b0-478c-9013-ab0298a7b1ec.png">

To learn more about using memory cache and getting statistics check out issue dotnet/runtime#67770.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](/src/libraries/README.md#primary-bar)

The APIs and functionality need more investment in the upcoming .NET releases.

## Deployment
[Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.