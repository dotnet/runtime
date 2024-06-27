# System.Collections
This is the assembly that generally surfaces generic collections such as [`List<T>`](https://learn.microsoft.com/dotnet/api/system.collections.generic.list-1) and [`PriorityQueue<TElement, TPriority>`](https://learn.microsoft.com/dotnet/api/system.collections.generic.priorityqueue-2).

Generic collection interfaces that are used by lower-level parts of the framework, such as [`IList<T>`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ilist-1) and [`IAsyncEnumerable<T>`](https://learn.microsoft.com/dotnet/api/system.collections.generic.iasyncenumerable-1) are surfaced by the `System.Runtime` assembly. The implementations for these collections live in [System.Private.Corelib](../System.Private.Corelib/src/System/Collections/Generic).

Documentation can be found at https://learn.microsoft.com/dotnet/csharp/programming-guide/concepts/collections.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)
- [x] [We don't accept refactoring changes due to new language features](../../libraries/README.md#secondary-bars)

Although a lot of the types are mature, the code base continues to evolve for better performance and to keep up with runtime enhancements.

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Collections+label%3A%22help+wanted%22) issues.

## Deployment
`System.Collections` is included in the shared framework and also provided as a [NuGet package](https://www.nuget.org/packages/System.Collections).