# System.Globalization.Extensions

This library originally intended to include extension interfaces to the globalization APIs. Later for simplicity all these extensions moved to [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization) library.
The Moved extensions mostly are [GlobalizationExtensions.GetStringComparer](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.globalizationextensions.getstringcomparer?view=net-7.0#system-globalization-globalizationextensions-getstringcomparer(system-globalization-compareinfo-system-globalization-compareoptions)), [StringNormalizationExtensions](https://learn.microsoft.com/en-us/dotnet/api/system.stringnormalizationextensions?view=net-7.0), and [IdnMapping](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.idnmapping?view=net-7.0).

Any future contribution to the globalization APIs should be considered to be done in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization) library. The API contracts should be exposed in [System.Runtime](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Runtime/ref/System.Runtime.cs). Any globalization tests should be added to [tests](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Globalization/tests) folder under the [System.Globalization](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Globalization) project.

Nothing should be added here except if there is a good reason to do so.

Globalization documentation can be found at https://learn.microsoft.com/en-us/dotnet/api/system.globalization?view=net-7.0.

## Contribution Bar
- [x] [We consider new features, bug fixes, new APIs and performance changes](../../libraries/README.md#primary-bar). The development should be done in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization).
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Source

https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization

## Deployment

All Globalization APIs are included in the shared framework.
