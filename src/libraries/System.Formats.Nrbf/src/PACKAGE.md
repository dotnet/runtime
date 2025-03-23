## About

<!-- A description of the package and where one can find more documentation -->

`System.Formats.Nrbf` exposes only one component: [NrbfDecoder](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.nrbfdecoder): a stateless, forward-only decoder class that can decode .NET Remoting Binary Format (NRBF) binary data from a **stream**.

You can think of [NrbfDecoder](https://learn.microsoft.com/en-us/dotnet/api/system.formats.nrbf.nrbfdecoder) as being the equivalent of using a JSON/XML reader without the deserializer.

## How to Use

The NRBF payload consists of serialization records that represent the serialized objects and their metadata. To read the whole payload and get the root record, you need to call one of the [NrbfDecoder.Decode](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.nrbfdecoder.decode) methods.

The `Decode` method returns a [SerializationRecord](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.serializationrecord) instance. `SerializationRecord` is an abstract class that represents the serialization record and provides three properties: [Id](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.serializationrecord.id), [RecordType](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.serializationrecord.recordtype), and [TypeName](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.serializationrecord.typename). It exposes one method, [TypeNameMatches](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.serializationrecord.typenamematches), which compares the type name read from the payload (and exposed via `TypeName` property) against the specified type. This method ignores assembly names, so you don't need to worry about type forwarding and assembly versioning. It also does not consider member names or their types (because getting this information would require type loading).

```csharp
using System.Formats.Nrbf;

public class Sample
{
    public int Integer;
    public string? Text;
    public byte[]? ArrayOfBytes;
    public Sample? ClassInstance;
}

ClassRecord rootRecord = NrbfDecoder.DecodeClassRecord(payload);
Sample output = new()
{
    // using the dedicated methods to read primitive values
    Integer = rootRecord.GetInt32(nameof(Sample.Integer)),
    Text = rootRecord.GetString(nameof(Sample.Text)),
    // using dedicated method to read an array of bytes
    ArrayOfBytes = ((SZArrayRecord<byte>)rootRecord.GetArrayRecord(nameof(Sample.ArrayOfBytes))).GetArray(),
    // using GetClassRecord to read a class record
    ClassInstance = new()
    {
        Text = rootRecord
            .GetClassRecord(nameof(Sample.ClassInstance))!
            .GetString(nameof(Sample.Text))
    }
};
```

## Main Types

<!-- The main types provided in this library -->

There are more than a dozen different serialization [record types](https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf). This library provides a set of abstractions, so you only need to learn a few of them:

- [`PrimitiveTypeRecord<T>`](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.primitivetyperecord-1): describes all primitive types natively supported by the NRBF (`string`, `bool`, `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `decimal`, `TimeSpan`, and `DateTime`).
  - Exposes the value via the `Value` property.
  - `PrimitiveTypeRecord<T>` derives from the non-generic [PrimitiveTypeRecord](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.primitivetyperecord), which also exposes a [Value](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.primitivetyperecord.value) property. But on the base class, the value is returned as `object` (which introduces boxing for value types).
- [ClassRecord](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.classrecord): describes all `class` and `struct` besides the aforementioned  primitive types.
- [ArrayRecord](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.arrayrecord): describes all array records, including jagged and multi-dimensional arrays.
- [`SZArrayRecord<T>`](https://learn.microsoft.com/dotnet/api/system.formats.nrbf.szarrayrecord-1): describes single-dimensional, zero-indexed array records, where `T` can be either a primitive type or a `SerializationRecord`.

```csharp
SerializationRecord rootObject = NrbfDecoder.Decode(payload); // payload is a Stream

if (rootObject is PrimitiveTypeRecord primitiveRecord)
{
    Console.WriteLine($"It was a primitive value: '{primitiveRecord.Value}'");
}
else if (rootObject is ClassRecord classRecord)
{
    Console.WriteLine($"It was a class record of '{classRecord.TypeName.AssemblyQualifiedName}' type name.");
}
else if (rootObject is SZArrayRecord<byte> arrayOfBytes)
{
    Console.WriteLine($"It was an array of `{arrayOfBytes.Length}`-many bytes.");
}
```

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.formats.nrbf)
* [BinaryFormatter migration guide: Read BinaryFormatter (NRBF) payloads](https://learn.microsoft.com/dotnet/standard/serialization/binaryformatter-migration-guide/read-nrbf-payloads)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Formats.Nrbf is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
