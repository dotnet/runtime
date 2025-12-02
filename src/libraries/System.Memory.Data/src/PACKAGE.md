## About

<!-- A description of the package and where one can find more documentation -->

System.Memory.Data introduces the `BinaryData` type, a lightweight abstraction for a byte payload.
It makes it easy to convert between string, bytes, and stream.

This abstraction can simplify the API surface by exposing a single type instead of numerous overloads or properties.
The `BinaryData` type handles data ownership efficiently, wrapping passed-in bytes when using `byte[]` or `ReadOnlyMemory<byte>` constructors or methods, and managing data as bytes when dealing with streams, strings, or rich model types serialized as JSON.


## Key Features

<!-- The key features of this package -->

* Lightweight abstraction for byte payload via `BinaryData` type.
* Convenient helper methods for common conversions among string, bytes, and stream.
* Efficient data ownership handling.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

To/From String:

```csharp
var data = new BinaryData("some data");

// ToString will decode the bytes using UTF-8
Console.WriteLine(data.ToString()); // prints "some data"
```

To/From Bytes:

```csharp
byte[] bytes = Encoding.UTF8.GetBytes("some data");

// Create BinaryData using a constructor ...
BinaryData data = new BinaryData(bytes);

// Or using a static factory method.
data = BinaryData.FromBytes(bytes);

// There is an implicit cast defined for ReadOnlyMemory<byte>
ReadOnlyMemory<byte> rom = data;

// There is also an implicit cast defined for ReadOnlySpan<byte>
ReadOnlySpan<byte> ros = data;

// there is also a ToMemory method that gives access to the ReadOnlyMemory.
rom = data.ToMemory();

// and a ToArray method that converts into a byte array.
byte[] array = data.ToArray();
```

To/From stream:

```csharp
var bytes = Encoding.UTF8.GetBytes("some data");
Stream stream = new MemoryStream(bytes);
var data = BinaryData.FromStream(stream);

// Calling ToStream will give back a stream that is backed by ReadOnlyMemory, so it is not writable.
stream = data.ToStream();
Console.WriteLine(stream.CanWrite); // prints false
```

`BinaryData` also can be used to integrate with `ObjectSerializer`.
By default, the `JsonObjectSerializer` will be used, but any serializer deriving from `ObjectSerializer` can be used.

```csharp
var model = new CustomModel
{
    A = "some text",
    B = 5,
    C = true
};

var data = BinaryData.FromObjectAsJson(model);
model = data.ToObjectFromJson<CustomModel>();
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.BinaryData`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.binarydata)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Memory.Data is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
