## About

This package provides collections that are thread safe and guaranteed to never change their contents, also known as immutable collections. Like strings, any methods that perform modifications will not change the existing instance but instead return a new instance. For efficiency reasons, the implementation uses a sharing mechanism to ensure that newly created instances share as much data as possible with the previous instance while ensuring that operations have a predictable time complexity.

The `System.Collections.Immutable` library is built-in as part of the shared framework in .NET Runtime. The package can be installed when you need to use it in other target frameworks.

For more information, see the documentation:

- [Collections and Data Structures](https://docs.microsoft.com/dotnet/standard/collections/)
- [System.Collections.Immutable API reference](https://docs.microsoft.com/dotnet/api/system.collections.immutable)
