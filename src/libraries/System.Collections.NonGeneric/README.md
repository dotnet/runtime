# System.Collections.NonGeneric
This is the assembly that generally surfaces most non-generic collections such as [`Stack`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.stack) and [`Queue`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.queue).

Non-generic collections that are used by lower-level parts of the framework, such as [`IList`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.ilist) and [`Hashtable`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.hashtable) are surfaced by the `System.Runtime` assembly. The implementations for these collections live in [System.Private.Corelib](../System.Private.Corelib/src/System/Collections/).

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/collections.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)
- [x] [We don't accept refactoring changes due to new language features](../../libraries/README.md#secondary-bars)

Although a lot of the types are mature, the code base continues to evolve for better performance and to keep up with runtime enhancements.

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Collections+label%3A%22help+wanted%22) issues.

## Deployment
`System.Collections.NonGeneric` is included in the shared framework and also provided as a [NuGet package](https://www.nuget.org/packages/System.Collections.NonGeneric).