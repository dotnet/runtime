# System.Runtime
Contains fundamental classes and base classes that define commonly-used value and reference data types, events and event handlers, interfaces, attributes, and processing exceptions.

Documentation can be found here: https://learn.microsoft.com/dotnet/api/system?view=net-7.0.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Runtime+label%3A%22help+wanted%22+) issues.

## Source
* [../System.Private.CoreLib/src/System](../System.Private.CoreLib/src/System)
* Tests for this library live in [./tests](./tests) and [../System.Runtime.Extensions/tests](../System.Runtime.Extensions/tests)

## Deployment
[System.Runtime](https://www.nuget.org/packages/System.Runtime) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.
