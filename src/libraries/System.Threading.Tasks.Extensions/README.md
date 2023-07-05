# System.Threading.Tasks.Extensions
Provides types including [`ValueTask`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask) and [`ValueTask<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1) that enable writing memory-efficient asynchronous code.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.threading.tasks

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Source

* The source of this project can found in [../System.Private.CoreLib/src/System/Threading/Tasks](../System.Private.CoreLib/src/System/Threading/Tasks)

## Deployment
[System.Threading.Tasks.Extensions](https://www.nuget.org/packages/System.Threading.Tasks.Extensions) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.1; it only needs to be installed when targeting .NET Standard 2.0 or .NET Framework.