## About

As of C# 8, the C# language has support for producing and consuming asynchronous iterators. The library types in support of those features are available in .NET Core 3.0 and newer as well as in .NET Standard 2.1. This library provides the necessary definitions of those types to support these language features on .NET Framework and on .NET Standard 2.0. This library is not necessary nor recommended when *only* targeting versions of .NET that include the relevant support. When
multi-targeting to versions that lack support for the types provided by this package, this package should be referenced for
**all** target frameworks. This ensures consistent behavior and prevents issues with transitive dependencies by consuming apps.

In some cases, you may need to add a direct reference to this package to resolve issues with transitive dependencies
that expect these types to be available. This typically occurs when a dependency library multi-targets but does not
include this package for all target frameworks. In such cases, adding a top-level reference to Microsoft.Bcl.AsyncInterfaces
can resolve missing type issues and ensure proper type forwarding.

## Target Framework Support

The types provided by this package have native support in the following target frameworks:

* **IAsyncEnumerable\<T\>, IAsyncEnumerator\<T\>, IAsyncDisposable**: .NET Core 3.0+, .NET 5+, and .NET Standard 2.1+

## When You May Still Need This Package

Even when targeting supported frameworks, you may still need to reference this package in these scenarios:

### Multi-targeting scenarios

When multi-targeting and any target framework lacks native support for these types, reference this package for **all** target frameworks, including those with native support. This prevents type identity mismatches and ensures consistent behavior across all targets.

### Transitive dependency issues

If you encounter missing type errors when consuming libraries that use async interfaces, you may need to add a direct reference to this package. This can happen when:

* A dependency library multi-targets but inconsistently references this package across target frameworks
* The dependency resolution selects a target framework that doesn't include the package reference
* Type forwarding is needed to unify types from different assemblies

Adding a top-level package reference resolves these issues by ensuring the types are available and properly forwarded across all scenarios.

## Key Features

<!-- The key features of this package -->

* Enables the use of C# async iterators on older .NET platforms

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("Starting...");
        await foreach (var value in GetValuesAsync())
        {
            Console.WriteLine(value);
        }
        Console.WriteLine("Finished!");

        static async IAsyncEnumerable<int> GetValuesAsync()
        {
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                yield return i;
            }
        }
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `IAsyncEnumerable<T>`
* `IAsyncEnumerator<T>`
* `IAsyncDisposable<T>`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [C# Feature Specification](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-8.0/async-streams)
* [Walkthrough article](https://learn.microsoft.com/archive/msdn-magazine/2019/november/csharp-iterating-with-async-enumerables-in-csharp-8)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Bcl.AsyncInterfaces is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).