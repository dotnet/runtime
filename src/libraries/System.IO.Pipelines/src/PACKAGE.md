## About

<!-- A description of the package and where one can find more documentation -->

A library designed to make it easier to do high-performance I/O.

Apps that parse streaming data are composed of boilerplate code having many specialized and unusual code flows.
The boilerplate and special case code is complex and difficult to maintain.

`System.IO.Pipelines` was architected to:

* Have high performance parsing streaming data.
* Reduce code complexity.

## Key Features

<!-- The key features of this package -->

* Single producer/single consumer byte buffer management.
* Reduction in code complexity and boilerplate code associated with I/O operations.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Check the [System.IO.Pipelines in .NET article](https://learn.microsoft.com/dotnet/standard/io/pipelines) for a full example.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.IO.Pipelines.Pipe`
* `System.IO.Pipelines.PipeWriter`
* `System.IO.Pipelines.PipeReader`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/standard/io/pipelines)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.io.pipelines)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.IO.Pipelines is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
