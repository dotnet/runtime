## About

<!-- A description of the package and where one can find more documentation -->

Packaged set of simple caching API's derived from those of the same namespace available in .NET Framework since 4.0. This package is intended for use as a bridge when porting .NET Framework applications to .NET.

[Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)/[IMemoryCache](https://learn.microsoft.com/aspnet/core/performance/caching/memory?view=aspnetcore-7.0) is recommended over `System.Runtime.Caching`/`MemoryCache` because it's better integrated into ASP.NET Core. For example, `IMemoryCache` works natively with ASP.NET Core [dependency injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-7.0).

**Use `System.Runtime.Caching`/`MemoryCache` as a compatibility bridge when porting code from .NET 4.x to .NET Core.**

## Key Features

<!-- The key features of this package -->

* Use caching facilities like in ASP.NET, but without a dependency on the System.Web assembly.
* Extensible caching mechanism
* Possible to create custom caching providers

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Runtime.Caching.MemoryCache`

## Additional Documentation

<!-- Links to further documentation -->

[MemoryCache.PhysicalMemoryLimit](https://learn.microsoft.com/dotnet/api/system.runtime.caching.memorycache.physicalmemorylimit?view=dotnet-plat-ext-7.0) property is only supported on windows.

* [Caching in .NET](https://learn.microsoft.com/dotnet/core/extensions/caching)
* [Cache in-memory in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/caching/memory?view=aspnetcore-7.0 )

## Related Packages

<!-- The related packages associated with this package -->

* [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Runtime.Caching is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).