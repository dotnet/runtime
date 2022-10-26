# System.Globalization
This mainly include the contracts of the Globalization interfaces and types e.g. [CultureInfo](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo?view=net-7.0), [DateTimeFormatInfo](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.datetimeformatinfo?view=net-7.0), [NumberFormatInfo](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo?view=net-7.0), [calenders](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.calendar?view=net-7.0)...etc. Most of these interfaces already exposed from [System.Runtime](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Runtime/ref/System.Runtime.cs) too. The implementation of the Globalization APIs exists in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization) library. Any Globalization product code changes should go to [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization). Any new API contracts should be exposed from [System.Runtime](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Runtime/ref/System.Runtime.cs). **All Globalization tests should be included here under the [tests](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Globalization/tests) folder**.

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/api/system.globalization?view=net-7.0.

## Contribution Bar
- [x] [We consider new features, bug fixes, new APIs and performance changes](../../libraries/README.md#primary-bar). The development should be done in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization).
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Source

https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization

## Deployment

All Globalization APIs are included in the shared framework.
