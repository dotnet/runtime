## About

<!-- A description of the package and where one can find more documentation -->

A library designed to make it easier to do high-performance I/O.

Apps that parse streaming data are composed of boilerplate code having many specialized and unusual code flows.
The boilerplate and special case code is complex and difficult to maintain.

`System.IO.Pipelines` was architected to:

* Have high performance parsing streaming data.
* Reduce code complexity.

## Key Features

<!-- The key features of this package -->

* Single producer/single consumer byte buffer management.
* Reduction in code complexity and boilerplate code associated with I/O operations.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

The `Pipe` class can be used to create a `PipeWriter`/`PipeReader` pair.
All data written into the `PipeWriter` is available in the `PipeReader`.

```csharp
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

// This could be an external source like a Socket, for this example it's just a string
byte[] input = Encoding.UTF8.GetBytes("Hello, Pipelines!");

var pipe = new Pipe();
Task writing = FillPipeAsync(pipe.Writer, input);
Task reading = ReadPipeAsync(pipe.Reader);

await Task.WhenAll(reading, writing);

static async Task FillPipeAsync(PipeWriter writer, byte[] input)
{
    for (int i = 0; i < input.Length; i++)
    {
        Memory<byte> memory = writer.GetMemory(1);
        memory.Span[0] = input[i];
        writer.Advance(1);

        // Make the data available to the PipeReader.
        await writer.FlushAsync();
    }

    // By completing PipeWriter, tell the PipeReader that there's no more data coming.
    writer.Complete();
}

static async Task ReadPipeAsync(PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        foreach (ReadOnlyMemory<byte> segment in buffer)
        {
            string text = Encoding.UTF8.GetString(segment.Span);
            Console.Write(text);
        }

        // Tell the PipeReader how much of the buffer has been consumed.
        reader.AdvanceTo(buffer.End);

        // Stop reading if there's no more data coming.
        if (result.IsCompleted)
        {
            break;
        }
    }

    // Mark the PipeReader as complete.
    reader.Complete();
}

```

There are no explicit buffers allocated.
All buffer management is delegated to the `PipeReader` and `PipeWriter` implementations.

Delegating buffer management makes it easier for consuming code to focus solely on the business logic.

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.IO.Pipelines.Pipe`
* `System.IO.Pipelines.PipeWriter`
* `System.IO.Pipelines.PipeReader`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [Conceptual documentation](https://learn.microsoft.com/dotnet/standard/io/pipelines)
* [API documentation](https://learn.microsoft.com/dotnet/api/system.io.pipelines)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.IO.Pipelines is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
