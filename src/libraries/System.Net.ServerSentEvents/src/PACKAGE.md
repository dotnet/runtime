## About

<!-- A description of the package and where one can find more documentation -->

System.Net.ServerSentEvents provides the `SseParser` type, which exposes factory methods for creating parsers for the events in a stream of server-sent events (SSE).

## Key Features

<!-- The key features of this package -->

* Parser for server-sent events (SSE)

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Asynchronously parsing event contents as strings

```csharp
using HttpClient client = new();
using Stream stream = await client.GetStreamAsync("https://localhost:12345/sse");
await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync())
{
    Console.WriteLine(item.Data);
}
```

Synchronously parsing event contents as JSON

```csharp
MemoryStream stream = new(data);
foreach (SseItem<Book> item in SseParser.Create(stream, (eventType, bytes) => JsonSerializer.Deserialize<Book>(bytes)).Enumerate())
{
    Console.WriteLine(item.Data.Author);
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Net.ServerSentEvents.SseParser`
* `System.Net.ServerSentEvents.SseParser<T>`
* `System.Net.ServerSentEvents.SseItem<T>`

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Net.ServerSentEvents is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
