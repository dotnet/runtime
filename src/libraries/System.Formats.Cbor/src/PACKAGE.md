## About

<!-- A description of the package and where one can find more documentation -->

Provides support for reading and writing values in Concise Binary Object Representation (CBOR) format, as originally defined in [IETF RFC 7049](https://www.ietf.org/rfc/rfc7049.html).


## Key Features

<!-- The key features of this package -->

* Reader and writer types for the CBOR format.
* Built-in support for different CBOR conformance modes.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Write and read primitives:

```csharp
using System.Formats.Cbor;

var cborWriter = new CborWriter(CborConformanceMode.Lax);
cborWriter.WriteTextString("Hello World");

var cborReader = new CborReader(cborWriter.Encode(), CborConformanceMode.Lax);
Console.WriteLine(cborReader.ReadTextString());
// Hello World
```

Write and read an array:

```csharp
var cborWriter = new CborWriter(CborConformanceMode.Lax);
cborWriter.WriteStartArray(5);
for (var index = 0; index < 5; index++)
{
    cborWriter.WriteInt32(index);
}
cborWriter.WriteEndArray();

var cborReader = new CborReader(cborWriter.Encode(), CborConformanceMode.Lax);
var arrayLength = cborReader.ReadStartArray();
for (var index = 0; index < arrayLength; index++)
{
    Console.Write(cborReader.ReadInt32());
}
// 01234
cborReader.ReadEndArray();
```

Inspect writer and reader state:

```csharp
var cborWriter = new CborWriter(CborConformanceMode.Lax);
cborWriter.WriteTextString("SomeArray");
Console.WriteLine(cborWriter.BytesWritten);
// 10
Console.WriteLine(cborWriter.IsWriteCompleted);
// True

var cborReader = new CborReader(cborWriter.Encode(), CborConformanceMode.Lax);
Console.WriteLine(cborReader.BytesRemaining);
// 10
Console.WriteLine(cborReader.ReadTextString());
// SomeArray
Console.WriteLine(cborReader.BytesRemaining);
// 0
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.Formats.Cbor.CborReader`
* `System.Formats.Cbor.CborWriter`
* `System.Formats.Cbor.CborReaderState`
* `System.Formats.Cbor.CborConformanceMode`
* `System.Formats.Cbor.CborContentException`
* `System.Formats.Cbor.CborTag`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.formats.cbor)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Formats.Cbor is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).