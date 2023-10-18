## About

<!-- A description of the package and where one can find more documentation -->

Microsoft.Extensions.Caching.Abstractions offers the basics to create and use caching in your applications.

This library doesn’t hold the actual caches, but it defines how caches should behave.
With this, you or other libraries can build various types of caches that all apps can use in the same way, whether the data is kept in memory, in files, or even across a network.

## Key Features

<!-- The key features of this package -->

* Basic rules and tools for building different kinds of caches.
* Makes it easier to create or use caches in your apps, whether they're simple or complex.

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
* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.extensions.caching)

## Related Packages

<!-- The related packages associated with this package -->

* In-memory caching: [Microsoft.Extensions.Caching.Memory](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)
* SQL Server caching: [Microsoft.Extensions.Caching.SqlServer](https://www.nuget.org/packages/Microsoft.Extensions.Caching.SqlServer/)
* Redis caching: [Microsoft.Extensions.Caching.StackExchangeRedis](https://www.nuget.org/packages/Microsoft.Extensions.Caching.StackExchangeRedis/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Extensions.Caching.Abstractions is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
