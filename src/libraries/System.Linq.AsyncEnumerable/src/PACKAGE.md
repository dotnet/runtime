## About

The `System.Linq.AsyncEnumerable` library provides support for Language-Integrated Query (LINQ) over `IAsyncEnumerable<T>` sequences.

## Key Features

* Extension methods for performing operations on `IAsyncEnumerable<T>` sequences.

## How to Use

```C#
using System;
using System.IO;
using System.Linq;

static IAsyncEnumerable<City> DeserializeAndFilterData(Stream stream)
{
    IAsyncEnumerable<City> cities = JsonSerializer.DeserializeAsyncEnumerable<City>(stream);

    return from city in cities
           where city.Population > 10_000
           orderby city.Name
           select city;
}
```

## Main Types

The main type provided by this library is:

* `System.Linq.AsyncEnumerable`

## Additional Documentation

* [Overview](https://learn.microsoft.com/dotnet/csharp/linq/)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.linq)

## Feedback & Contributing

`System.Linq.AsyncEnumerable` is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
