# System.Collections.Concurrent
This is the assembly that surfaces concurrent collections such as [`ConcurrentDictionary<TKey, TValue>`](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2) and [`ConcurrentBag<T>`](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentbag-1). It provides thread-safe collections that should be used whenever multiple threads are accessing the collection concurrently.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.collections.concurrent.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)
- [x] [We don't accept refactoring changes due to new language features](../../libraries/README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Collections.Concurrent+label%3A%22help+wanted%22) issues.

## Deployment
`System.Collections.Concurrent` is included in the shared framework and also provided as a [NuGet package](https://www.nuget.org/packages/System.Collections.Concurrent).