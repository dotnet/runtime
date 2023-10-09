## About

[System.Runtime.Caching](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching?view=dotnet-plat-ext-7.0) ([NuGet package](https://www.nuget.org/packages/System.Runtime.Caching/)) is a packaged set of simple caching API's derived from those of the same namespace available in .Net Framework since 4.0. This package is intended for use as a bridge when porting .Net Framework applications to .Net Core.

This `System.Runtime.Caching` package can be used with any [.NET implementation](/dotnet/standard/net-standard#net-implementation-support) that targets .NET Standard 2.0 or later. For example:
* .NET Core 3.1 or later.
* .Net Framework 4.5 or later.
* .Net 5.0 or late

[Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)/[IMemoryCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-7.0) is recommended over `System.Runtime.Caching`/`MemoryCache` because it's better integrated into ASP.NET Core. For example, `IMemoryCache` works natively with ASP.NET Core [dependency injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-7.0).

> > [!IMPORTANT]
>  Use `System.Runtime.Caching`/`MemoryCache` as a compatibility bridge when porting code from .NET 4.x to .NET Core.


## Main Types

The main types provided by this library are:

* `System.Runtime.Caching.MemoryCache`

## Remarks

[MemoryCache.PhysicalMemoryLimit](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache.physicalmemorylimit?view=dotnet-plat-ext-7.0) property is only supported on windows.

## Addtional Documentation

* [Caching in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching)
* [Cache in-memory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-7.0 )

## Related Packages

* [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)

## Feedback & Contributing

System.Runtime.Caching is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).