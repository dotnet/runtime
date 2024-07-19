# System.Collections.NonGeneric
This is the assembly that generally surfaces most non-generic collections such as [`Stack`](https://learn.microsoft.com/dotnet/api/system.collections.stack) and [`Queue`](https://learn.microsoft.com/dotnet/api/system.collections.queue).

Non-generic collections that are used by lower-level parts of the framework, such as [`IList`](https://learn.microsoft.com/dotnet/api/system.collections.ilist) and [`Hashtable`](https://learn.microsoft.com/dotnet/api/system.collections.hashtable) are surfaced by the `System.Runtime` assembly. The implementations for these collections live in [System.Private.Corelib](../System.Private.Corelib/src/System/Collections/).

Documentation can be found at https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/collections.

## Contribution Bar
- [x] [We only consider fixes to maintain or improve quality](/src/libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](/src/libraries/README.md#secondary-bars)

## Deployment
`System.Collections.NonGeneric` is included in the shared framework and also provided as a [NuGet package](https://www.nuget.org/packages/System.Collections.NonGeneric).