## About

<!-- A description of the package and where one can find more documentation -->

This package provides collections that are thread safe and guaranteed to never change their contents, also known as immutable collections. Like strings, any methods that perform modifications will not change the existing instance but instead return a new instance. For efficiency reasons, the implementation uses a sharing mechanism to ensure that newly created instances share as much data as possible with the previous instance while ensuring that operations have a predictable time complexity.

The `System.Collections.Immutable` library is built-in as part of the shared framework in .NET Runtime. The package can be installed when you need to use it in other target frameworks.

## Key Features

<!-- The key features of this package -->

*
*
*

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* ``
* ``
* ``

## Additional Documentation

<!-- Links to further documentation -->

- [Collections and Data Structures](https://docs.microsoft.com/dotnet/standard/collections/)
- [API documentation](https://docs.microsoft.com/dotnet/api/system.collections.immutable)

## Related Packages

<!-- The related packages associated with this package -->

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Collections.Immutable is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).