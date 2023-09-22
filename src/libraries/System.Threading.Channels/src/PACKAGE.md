## About

<!-- A description of the package and where one can find more documentation -->

The `System.Threading.Channels` library provides types for passing data asynchronously between producers and consumers.

## Key Features

<!-- The key features of this package -->

* Abstractions representing channels for one or more producers to publish data to one or more consumers
* APIs focused on asynchronous production and consumption of data
* Factory methods for producing multiple kinds of channels

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```C#
using System;
using System.Threading.Channels;
using System.Threading.Tasks;

Channel<int> channel = Channel.CreateUnbounded<int>();

Task producer = Task.Run(async () =>
{
    int i = 0;
    while (true)
    {
        channel.Writer.TryWrite(i++);
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
});

Task consumer = Task.Run(async () =>
{
    await foreach (int value in channel.Reader.ReadAllAsync())
    {
        Console.WriteLine(value);
    }
});

await Task.WhenAll(producer, consumer);
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Threading.Channel<T>`
* `System.Threading.Channel`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Overview](https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.threading.channels)

## Related Packages

<!-- The related packages associated with this package -->

https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Threading.Channels is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).