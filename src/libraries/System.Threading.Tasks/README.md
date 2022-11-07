# System.Threading.Tasks
Provides types that simplify the work of writing concurrent and asynchronous code. The main types are [`Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task) which represents an asynchronous operation that can be waited on and cancelled, and [`Task<TResult>`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1), which is a task that can return a value. The [`TaskFactory`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.taskfactory) class provides static methods for creating and starting tasks, and the [`TaskScheduler`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.taskscheduler) class provides the default thread scheduling infrastructure.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.threading.tasks

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22+label%3Aarea-System.Threading.Tasks) issues.

## Source

* The source of this project can found in [../System.Private.CoreLib/src/System/Threading/Tasks](../System.Private.CoreLib/src/System/Threading/Tasks)

## Deployment
[System.Threading.Tasks](https://www.nuget.org/packages/System.Threading.Tasks) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.