## About

[System.Runtime.Caching](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching?view=dotnet-plat-ext-7.0)/[System.Runtime.Caching.MemoryCache](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache?view=dotnet-plat-ext-7.0) ([NuGet package](https://www.nuget.org/packages/System.Runtime.Caching/)) can be used with:

* .NET Standard 2.0 or later.
* Any [.NET implementation](/dotnet/standard/net-standard#net-implementation-support) that targets .NET Standard 2.0 or later. For example, ASP.NET Core 3.1 or later.
* .NET Framework 4.5 or later.

[Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)/`IMemoryCache` (described in this article) is recommended over `System.Runtime.Caching`/`MemoryCache` because it's better integrated into ASP.NET Core. For example, `IMemoryCache` works natively with ASP.NET Core [dependency injection](xref:fundamentals/dependency-injection).

Use `System.Runtime.Caching`/`MemoryCache` as a compatibility bridge when porting code from ASP.NET 4.x to ASP.NET Core.

## Key Features

* MemoryCache feature represents the type that implements an in-memory cache.

## Main Types

The main types provided by this library are:

* `System.Runtime.Caching.MemmoryCache`

## Remarks

[MemoryCache.PhysicalMemeoryLimit](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache.physicalmemorylimit?view=dotnet-plat-ext-7.0) property is only supported on windows.

## Addtional Documentation

* [Caching in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/caching)
* [Cache in-memory in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-7.0 )

## Related Packages

* [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)

## Feedback & Contributing

System.Runtime.Caching is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).