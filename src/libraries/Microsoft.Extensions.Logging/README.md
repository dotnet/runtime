# Microsoft.Extensions.Logging

`Microsoft.Extensions.Logging` is combined with a core logging abstraction under `Microsoft.Extensions.Logging.Abstractions`. This abstraction is available in our basic built-in implementations like console, event log, and debug (Debug.WriteLine). Also note, there is no dedicated built-in solution for file-based logging.

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/core/extensions/logging.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for improvements to the logging source generator](../../libraries/README.md#secondary-bars)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Logging](https://www.nuget.org/packages/Microsoft.Extensions.Logging) is not included in the shared framework. The package is deployed as out-of-band (OOB) and needs to be installed into projects directly.