# Guide to Porting from Newtonsoft.Json to System.Text.Json

## Overview

* The `System.Text.Json` APIs that are included in .NET Core 3.0 are designed primarily for performance critical scenarios and hence emphasize runtime performance and reducing allocations over high usability and convenience. Furthermore, the in-box JSON stack does not aim to provide feature parity with `Newtonsoft.Json`, especially since it is in its infancy. That said, we still want to make it easier for those whose needs can be fulfilled by the new APIs and those who are looking for better performance. This guide captures some of the common pitfalls and programming patterns that are useful to note when trying to use the new in-box APIs.
  - Please help contribute to this porting guide to help others onboard to the new APIs.
  - For an overview on the objectives, constraints, and design requirements of the new APIs, please see the [roadmap](https://github.com/dotnet/corefx/blob/master/src/System.Text.Json/roadmap/README.md).
  - If you have any feedback or feature requests, feel free to submit issues: https://github.com/dotnet/corefx/issues

## Common Pitfalls, Patterns, and Tips

### Using Ref Structs

* The low-level `Utf8JsonReader` and `Utf8JsonWriter` are `ref structs` which means they have certain limitations (see https://aka.ms/span-safety). For example, they cannot be stored as a field on a class or struct (other than ref structs). To achieve the desired performance, these types must be `ref structs` since they need to cache the input or output `Span<byte>` (which itself is a ref struct). Furthermore, these types are mutable since they hold state. Hence, you should **pass them by ref** rather than by value. Passing them by value would result in a struct copy and the state changes would not be visible to the caller. This differs from `Newtonsoft.Json` since the `JsonTextReader`/`JsonTextWriter` you might be familiar with are classes.

### Reading from a String / Stream

* The `Utf8JsonReader` supports reading from a UTF-8 encoded `ReadOnlySpan<byte>` and `ReadOnlySequence<byte>` (which is the result of reading from a `PipeReader`). We currently (as of .NET Core 3.0 preview 2) do not have a convenient API to read JSON from a stream directly (either synchronously or asynchronously). For synchronous reading (especially of small payloads), you could read the JSON payload till the end of the stream into a byte array and pass that into the reader. For reading from a string (which is encoded as UTF-16), you should use the `Encoding.UTF8.GetBytes` API to first transcode the string to a UTF-8 encoded byte array, and pass that to the `Utf8JsonReader`.
  - Code sample for async reading from a `Stream` / `PipeReader` - TBD

### ReadOnlySequence, HasValueSequence, and ValueSequence

* The `Utf8JsonReader` supports reading from a UTF-8 encoded `ReadOnlySpan<byte>` and `ReadOnlySequence<byte>`. If your JSON input is a **span**, each JSON element can be accessed from the `ValueSpan` property on the reader as you go through the "read loop". However, if your input is a **sequence**, some JSON elements might straddle multiple segments of the `ReadOnlySequence<byte>` and hence would not be accessible from `ValueSpan` in a contiguous memory block. Instead, whenever you have a multi-segment `ReadOnlySequence<byte>` as input, you should always poll the `HasValueSequence` property on the reader to figure out how to access the current JSON element. Here is a recommended pattern that you can follow:

```C#
while (reader.Read())
{
  switch (reader.TokenType)
  {
    // ...
    ReadOnlySpan<byte> jsonElement = reader.HasValueSequence ?
                reader.ValueSequence.ToArray() :
                reader.ValueSpan;
    // ...
  }
}
```

### Writing to an Array / Stream

* The `Utf8JsonWriter` only supports writing JSON data synchronously to an `IBufferWriter<byte>`. There is one implementation of this interface today, and that's `PipeWriter`. If, however, you need to write to an array, or a stream (either synchronously or asynchronously), you would need to provide an implementation of this interface. You can take inspiration from this [sample implementation](https://gist.github.com/ahsonkhan/c76a1cc4dc7107537c3fdc0079a68b35) of an `ArrayPool` backed `IBufferWriter<byte>` to help write to an array or stream. Make sure to periodically call `Flush()`/`FlushAsync()` (or equivalent) on your actual output location after you have synchronously written some amount of JSON (or at least once at the end for small payloads). Here is a recommended pattern that you can follow for writing large JSON payloads:
  - Code sample for async writing to a `Stream` / `PipeWriter` - TBD

```C#
// Assume ArrayBufferWriter is a class that implements IBufferWriter<byte> and has a CopyTo(Stream) method
static void WriteJson(ArrayBufferWriter output, Stream stream)
{
    const int SyncWriteThreshold = 1_000; // Copy to stream after writing this many bytes

    var json = new Utf8JsonWriter(output, state: default);

    long prevBytesWritten = 0;

    // Write some JSON, let's say an array of JSON objects in a loop
    json.WriteStartArray();

    for (int j = 0; j < numberOfElements; j++)
    {
        // json.Write...

        prevBytesWritten = json.BytesWritten - prevBytesWritten;
        if (prevBytesWritten > SyncWriteThreshold)
        {
            json.Flush(isFinalBlock: false);
            output.CopyTo(stream);
        }
    }

    json.WriteEndArray();
    json.Flush(isFinalBlock: true);
    output.CopyTo(stream);
}
```

### Use Text Encoded as UTF-8 for Best Performance

* As the type names suggest, if you are looking for the best possible performance while using the `Utf8JsonReader` and `Utf8JsonWriter`, try to read/write JSON payloads already encoded as UTF-8 text (rather than UTF-16 strings). For example, if you are writing string literals, consider caching them as static byte arrays, and write those instead.

```C#
private const string PropertyName = "name";
private static readonly byte[] PropertyNameBytes = Encoding.UTF8.GetBytes(PropertyName);
// ...
jsonWriter.WriteNumber(PropertyNameBytes, 42, escape: false);
// ...
```

### Using a Read-only JsonDocument

* `JsonDocument` provides the ability to parse JSON data and build a **read-only** Document Object Model (DOM) with low allocations for common payload sizes (i.e. < 1 MB). It does this by building an in-memory view of the data into a pooled buffer. Therefore, unlike `JObject`/`JArray`, this type is `IDisposable` and needs to be used inside a using block. Additionally, since the DOM is read-only, it doesn't provide the ability to add/remove JSON elements. If your scenario requires a writable DOM or if you need to build up a JSON payload from scratch, continue to use `Newtonsoft.Json` and `JObject`/`JArray` since that is currently an unsupported feature in the new stack.
  - `JsonDocument` exposes the `RootElement` as a property of type `JsonElement` which is the type that encompasses any JSON element (this concept is represented by dedicated types like `JObject`/`JArray`/`JToken`/etc. in `Newtonsoft.Json`). `JsonElement` is what you can search and enumerate over, and you can use the found `JsonElement` to materialize JSON elements into .NET types.
  - Unlike searching and accessing JSON elements on `JObject`/`JArray`, which tend to be relatively fast lookups in some dictionary, searching and accessing JSON elements on `JsonElement` requires sequential lookup (since we trade-off initial parse time for look-up time). Therefore, it is recommended that you try to avoid searching the whole `JsonDocument` for every property, but rather search on nested JSON objects that you already have as `JsonElement`. For the same reason, we encourage you to use the built-in enumerators (`EnumerateArray` and `EnumerateObject`) rather than doing your own indexing/loops.

```C#
static double ParseJson()
{
    const string json = " [ { \"name\": \"John\" }, [ \"425-000-1212\", 15 ], { \"grades\": [ 90, 80, 100, 75 ] } ]";

    double average = -1;

    using (JsonDocument doc = JsonDocument.Parse(json))
    {
        JsonElement root = doc.RootElement;
        JsonElement info = root[1];

        string phoneNumber = info[0].GetString();
        int age = info[1].GetInt32();

        JsonElement grades = root[2].GetProperty("grades");

        double sum = 0;
        foreach (JsonElement grade in grades.EnumerateArray())
        {
            sum += grade.GetInt32();
        }

        int numberOfCourses = grades.GetArrayLength();
        average = sum / numberOfCourses;
    }

    return average;
}
```

### Multi-Targeting Various TFMs

* If possible, you should target .NET Core 3.0 and get the in-box `System.Text.Json` APIs. However, if you need to support netstandard2.0 (for example, if you are a library developer), you can use our NuGet package which is netstandard2.0 compatible. If, however, you need to target an older platform or standard, or for some other reason would like to continue to use `Newtonsoft.Json` on certain platforms, you can try to multi-target and have two implementations. However, this is not trivial and would require some `#ifdefs` and source duplication especially if you heavily rely on features that only exist in `Newtonsoft.Json`. One pattern to try to share as much code as possible is to create a `ref struct` wrapper around types like `Utf8JsonReader`/`JsonTextReader` and `Utf8JsonWriter`/`JsonTextWriter` to unify the public surface area used while isolating the behavioral differences. This way you can isolate the changes mainly to the construction of the type (along with passing the new type around by ref). In fact, that is the pattern we currently follow in [core-setup](https://github.com/dotnet/core-setup):
  - [UnifiedJsonReader.Utf8JsonReader.cs](https://github.com/dotnet/core-setup/blob/45f9401bf62faf0d3446cfd8681d35cc3487367a/src/managed/Microsoft.Extensions.DependencyModel/UnifiedJsonReader.Utf8JsonReader.cs)
  - [UnifiedJsonReader.JsonTextReader.cs](https://github.com/dotnet/core-setup/blob/45f9401bf62faf0d3446cfd8681d35cc3487367a/src/managed/Microsoft.Extensions.DependencyModel/UnifiedJsonReader.JsonTextReader.cs)
  - [UnifiedJsonWriter.Utf8JsonWriter.cs](https://github.com/dotnet/core-setup/blob/45f9401bf62faf0d3446cfd8681d35cc3487367a/src/managed/Microsoft.Extensions.DependencyModel/UnifiedJsonWriter.Utf8JsonWriter.cs)
  - [UnifiedJsonWriter.JsonTextWriter.cs](https://github.com/dotnet/core-setup/blob/45f9401bf62faf0d3446cfd8681d35cc3487367a/src/managed/Microsoft.Extensions.DependencyModel/UnifiedJsonWriter.JsonTextWriter.cs)

## Discrepancies Between Newtonsoft.Json and System.Text.Json

### Support for JSON RFC

* By default, the new JSON stack follows the [JSON spec](https://tools.ietf.org/html/rfc8259). This means that things like comments, trailing commas, or any other invalid JSON syntax would be rejected by the `Utf8JsonReader`/`JsonDocument`, by default. However, we provide some knobs to deviate from the spec (for example allowing comments) that you can opt into. Similarly, the `Utf8JsonWriter` does not allow you to write JSON that is syntactically invalid unless you opt-out of validation explicitly.

### Reading/Writing Null Values

* We consider the `null` literal as its own token type. Therefore, when reading, make sure to check against `JsonTokenType.Null` for validating your JSON schema. Comparing a `null` token against `JsonTokenType.String` will not work so you should handle it explicitly. Similarly, for writing `nulls`, you should explicitly call the `WriteNull(...)`/`WriteNullValue()` methods where feasible. Here is a before/after sample:

```C#
// BEFORE - Newtonsoft.Json
public static string ReadAsString(this JsonTextReader reader)
{
    reader.Read();

    if (reader.TokenType != JsonToken.String)
    {
        throw new InvalidDataException();
    }

    return reader.Value?.ToString();
}

// AFTER - System.Text.Json
public static string ReadAsString(this ref Utf8JsonReader reader)
{
    reader.Read();

    if (reader.TokenType == JsonTokenType.Null)
    {
        return null;
    }

    if (reader.TokenType != JsonTokenType.String)
    {
        throw new InvalidDataException();
    }

    return reader.GetString();
}
```