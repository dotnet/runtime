## About

<!-- A description of the package and where one can find more documentation -->

Provides support for reading and writing values in Concise Binary Object Representation (CBOR), as originally defined in [IETF RFC 7049](https://www.ietf.org/rfc/rfc7049.html).


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
cborWriter.WriteStartArray(3);
cborWriter.WriteInt32(1);
cborWriter.WriteInt32(2);
cborWriter.WriteInt32(3);
cborWriter.WriteEndArray();

var cborReader = new CborReader(cborWriter.Encode(), CborConformanceMode.Lax);
Console.WriteLine(cborReader.ReadStartArray());
// 3
Console.WriteLine(cborReader.ReadInt32());
// 1
Console.WriteLine(cborReader.ReadInt32());
// 2
Console.WriteLine(cborReader.ReadInt32());
// 3
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

* [API documentation](https://learn.microsoft.com/en-us/dotnet/api/system.formats.cbor)

## Related Packages

<!-- The related packages associated with this package -->

.NETFramework 4.6.2:

* Provides HashCode types: [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode/)
* Resource pooling: [System.Buffers](https://www.nuget.org/packages/System.Buffers/)
* Efficient memory representation: [System.Memory](https://www.nuget.org/packages/System.Memory/)
* Provides functionality over pointers: [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/)
* Provides underlying type for tuples: [System.ValueTuple](https://www.nuget.org/packages/System.ValueTuple/)


.NETStandard 2.0:

* Provides HashCode types: [Microsoft.Bcl.HashCode](https://www.nuget.org/packages/Microsoft.Bcl.HashCode/)
* Resource pooling: [System.Buffers](https://www.nuget.org/packages/System.Buffers/)
* Efficient memory representation: [System.Memory](https://www.nuget.org/packages/System.Memory/)
* Provides functionality over pointers: [System.Runtime.CompilerServices.Unsafe](https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/)

.net6.0:

No dependencies.

.net7.0:

No dependencies.

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Formats.Cbor is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).