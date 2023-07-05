# System.Globalization.Calendars

This library originally intended to include interfaces to the calendars globalization APIs. Later for simplicity all these APIs moved to [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization) library.
The Moved APIs mostly a concrete implementation for [Calendar](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.calendar?view=net-7.0) APIs.

Any future contribution to the globalization calendar APIs should be considered to be done in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization) library. The API contracts should be exposed in [System.Runtime](https://raw.githubusercontent.com/dotnet/runtime/main/src/libraries/System.Runtime/ref/System.Runtime.cs). Any globalization calendar tests should be added to [tests](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Globalization/tests) folder under the [System.Globalization](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Globalization) project.

Nothing should be added here except if there is a good reason to do so.

Globalization calendars documentation can be found at https://learn.microsoft.com/en-us/dotnet/api/system.globalization.calendar?view=net-7.0.

## Contribution Bar
- [x] [We consider new features, bug fixes, new calendar APIs and performance changes](../../libraries/README.md#primary-bar). The development should be done in [System.Private.Corelib](https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization).
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

## Source

https://github.com/dotnet/runtime/tree/main/src/libraries/System.Private.CoreLib/src/System/Globalization

## Deployment

All Globalization calendar APIs are included in the shared framework.
