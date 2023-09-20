## About

As of .NET Core 2.0 and .NET Standard 2.1, the C# language has support for math (System.MathF) functions with floats. This library provides the necessary definitions of those types to support these language features on .NET Framework and on .NET Standard 2.0. This library is not necessary nor recommended when targeting versions of .NET that include the relevant support.

## Key Features

<!-- The key features of this package -->

* Enables the use of MathF on older .NET platforms

## How to Use

```C#
using System;

internal static class Program
{
    private static async Task Main()
    {       
        Console.WriteLine(MathF.Max(1f, 5f)); // returns 5f
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.MathF`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.mathf)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Bcl.Numerics is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).