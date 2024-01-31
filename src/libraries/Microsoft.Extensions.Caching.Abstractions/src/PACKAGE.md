## About

<!-- A description of the package and where one can find more documentation -->

Provides the abstractions to create and use in-memory and distributed caching in your applications.

This library defines how in-memory and distributed caches should be implemented; it doesn’t contain any cache implementation.
With the abstractions provided in this library, various types of caches can be built and used interchangeably, whether the data is kept in memory, in files, or even across a network.

## Key Features

<!-- The key features of this package -->

* Interfaces for building and using in-memory and distributed caches.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

This package is typically used with an implementation of the caching abstractions, such as `Microsoft.Extensions.Caching.Memory` or `Microsoft.Extensions.Caching.SqlServer`.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `Microsoft.Extensions.Caching.Abstractions.ICacheEntry`
* `Microsoft.Extensions.Caching.Abstractions.IMemoryCache`
* `Microsoft.Extensions.Caching.Abstractions.IDistributedCache`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/core/extensions/caching)
* API documentation
  * [Microsoft.Extensions.Caching.Memory](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.memory)
  * [Microsoft.Extensions.Caching.Distributed](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching.distributed)

## Related Packages

<!-- The related packages associated with this package -->

* In-memory caching: [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)
* SQL Server caching: [Microsoft.Extensions.Caching.SqlServer](https://www.nuget.org/packages/Microsoft.Extensions.Caching.SqlServer/)
* Redis caching: [Microsoft.Extensions.Caching.StackExchangeRedis](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Caching.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
