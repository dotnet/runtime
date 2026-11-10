## About

<!-- A description of the package and where one can find more documentation -->

System.Net.ServerSentEvents provides the `SseParser` type, which exposes factory methods for creating parsers for the events in a stream of server-sent events (SSE).

When parsing data from untrusted sources, configure `SseParserOptions.MaxBufferSize` to a bounded value so the parser does not buffer arbitrarily large event payloads. If multiple parsers are running in parallel, keep the limit low enough to maintain an acceptable total memory footprint.

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

When connecting to an untrusted event source, use a buffer-size limit to handle oversized input. If you use multiple parsers at the same time, consider stricter limits to avoid unexpected memory growth:

```csharp
using System.Net.ServerSentEvents;
using System.Text;

using HttpClient client = new();
using Stream stream = await client.GetStreamAsync("https://localhost:12345/sse");
SseParserOptions<string> options = new(static (_, bytes) => Encoding.UTF8.GetString(bytes))
{
    MaxBufferSize = 64 * 1024 * 1024
};

await foreach (SseItem<string> item in SseParser.Create(stream, options).EnumerateAsync())
{
    Console.WriteLine(item.Data);
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
