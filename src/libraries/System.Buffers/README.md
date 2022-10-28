# System.Buffers
Contains types used in creating and managing memory buffers, such as those represented by `Span<T>` and `Memory<T>`.

Documentation can be found here: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3Aarea-System.Buffers+label%3A%22help+wanted%22+) issues.

## Source
* [../../coreclr/System.Private.CoreLib/src/System](../../coreclr/System.Private.CoreLib/src/System)
* Tests for this library live in [./tests](./tests).

## Deployment
[System.Buffers](https://www.nuget.org/packages/System.Buffers) is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.1; it only needs to be installed when targeting .NET Standard 2.0.