# Extend support for TKey in Dictionaries to non-string types

# Motivation
Most of our users that serialize dictionary use `Dictionary<string, TKey>`; however there is a significant amount that relies on `Dictionary<TKey, TValue>` where `TKey` is a primitive other than `string`, e.g: `int` or `Guid`, most of them came form `Newtonsoft.Json` which offers a plenty amount of support for using several types as the `TKey`, other popular .Net serializers also offer support for other types, the most common are integers (`int`, `uint`, `long`, etc.), and `enum`s (including Flags `enum`s).

# Goals
* 80%+ of dictionaries with non-string keys work out of the box, especially if they can round-trip.
* Remain high performance.

# Non-goals
* Complete parity with `Newtonsoft.Json` capabilities, especially in how string support is extended; any extension point can be through `JsonConverter<MyDictionary<non-string, TValue>>`.

# Sample
```cs
// (De)serialize into a dictionary with a non-string key.
Dictionary<int, string> root = new Dictionary<int, string>();
root.Add(1, "value");

string json = JsonSerializer.Serialize(root);
// JSON
// {
//   "1":"value"
// }

Dictionary<int, string> rootCopy = JsonSerializer.Deserialize<Dictionary<int, string>>(json);
Console.WriteLine(rootCopy[1]);
 // Prints
 // value
```

# Strawman proposal

## `KeyConverter`
Implement an internal custom mechanism that is in charge of converting a defined set of types to be supported as the dictionary `TKey`; more or less like internal `JsonConverter`s work but for dictionary keys to JSON property names and viceversa.

* The alternative that offers the best performance.

* We need to define a criteria to choose what types we should support, I suggest to do as Utf8JsonReader/Writer and support the types supported by the Utf8Parser/Formatter.

* Supported types (Types supported by [`Utf8Formatter/Parser`](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.text.utf8formatter?view=netcore-3.1) + a few others that are popular):
  * Boolean
  * Byte
  * DateTime
  * DateTimeOffset
  * Decimal
  * Double
  * Enum
  * Guid
  * Int16
  * Int32
  * Int64
  * Object (Only on Serialization and if the runtime type is one of the supported types in this list)
  * SByte
  * Single
  * String
  * UInt16
  * UInt32
  * UInt64

* https://github.com/Jozkee/runtime/tree/TKey_CustomConverter

# Alternative considered

## `TypeConverter`
Use `TypeConverter` to parse and write the string representation of the type and use that as the JSON property name.
* Many .NET types count with `TypeConverter` support that provides a string interpretation of the type.
* Users can hook up `TypeConverter`s for their own types.
* Not a high-performance alternative, involves unnecessary boxing.
* https://github.com/Jozkee/runtime/tree/TKeySupport_TypeConverter

# Benchmark results
Using a dictionary that contains 100 elements.

## Serialize/Write

The custom `KeyConverter` that calls Utf8Parser underneath performs slightly faster than calling `TypeConverter`, keep in mind that `KeyConverter` is a naive implementation, it also calls `Encoding.UTF8.GetString` since `JsonNamingPolicy.ConvertName` only takes strings, this could be fixed if we add an internal method that can take a `ROS<byte>`, also the allocations are currently super high; this might be alleviated by moving the `KeyConverter` store to the `JsonSerializerOptions`.

`Dictionary<String, TValue>` results show the same numbers across branches since that still uses `DictionaryOfStringTValueConverter`.

**main:**
|                                       Type |                      Method |     Mean |     Error |    StdDev |   Median |      Min |       Max |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------- |---------------------------- |---------:|----------:|----------:|---------:|---------:|----------:|-------:|------:|------:|----------:|
| WriteDictionary<Dictionary<String, Int32>> |        SerializeToUtf8Bytes | 8.737 us | 0.1760 us | 0.1883 us | 8.743 us | 8.487 us |  9.030 us | 0.8867 |     - |     - |    3760 B |
| WriteDictionary<Dictionary<String, Int32>> | SerializeUtf8ObjectProperty | 9.908 us | 0.6205 us | 0.6897 us | 9.800 us | 8.994 us | 11.343 us | 0.9392 |     - |     - |    4048 B |

**KeyConverter:**
|                                       Type |                      Method |      Mean |     Error |    StdDev |    Median |      Min |       Max |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------- |---------------------------- |----------:|----------:|----------:|----------:|---------:|----------:|-------:|------:|------:|----------:|
|   WriteDictionary<Dictionary<Guid, Int32>> |        SerializeToUtf8Bytes |  9.874 us | 0.1383 us | 0.1226 us |  9.891 us | 9.660 us | 10.057 us | 1.1874 |     - |     - |      5 KB |
|  WriteDictionary<Dictionary<Int32, Int32>> |        SerializeToUtf8Bytes |  8.877 us | 0.2902 us | 0.3105 us |  8.770 us | 8.534 us |  9.554 us | 0.5553 |     - |     - |   2.41 KB |
| WriteDictionary<Dictionary<String, Int32>> |        SerializeToUtf8Bytes |  8.859 us | 0.2583 us | 0.2871 us |  8.828 us | 8.456 us |  9.484 us | 0.8803 |     - |     - |   3.67 KB |
|   WriteDictionary<Dictionary<Guid, Int32>> | SerializeUtf8ObjectProperty | 10.155 us | 0.2284 us | 0.2136 us | 10.124 us | 9.818 us | 10.647 us | 1.2779 |     - |     - |   5.28 KB |
|  WriteDictionary<Dictionary<Int32, Int32>> | SerializeUtf8ObjectProperty |  8.633 us | 0.2301 us | 0.2558 us |  8.640 us | 8.275 us |  9.143 us | 0.6482 |     - |     - |   2.69 KB |
| WriteDictionary<Dictionary<String, Int32>> | SerializeUtf8ObjectProperty |  8.845 us | 0.1065 us | 0.0831 us |  8.864 us | 8.666 us |  8.949 us | 0.9470 |     - |     - |   3.95 KB |

**TypeConverter:**
|                                       Type |                      Method |      Mean |     Error |    StdDev |    Median |       Min |       Max |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------- |---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|-------:|------:|------:|----------:|
|   WriteDictionary<Dictionary<Guid, Int32>> |        SerializeToUtf8Bytes | 19.067 us | 0.6886 us | 0.7930 us | 18.971 us | 18.249 us | 20.809 us | 4.2226 |     - |     - |   17.5 KB |
|  WriteDictionary<Dictionary<Int32, Int32>> |        SerializeToUtf8Bytes | 16.022 us | 0.2106 us | 0.1970 us | 16.021 us | 15.726 us | 16.331 us | 2.1591 |     - |     - |   9.01 KB |
| WriteDictionary<Dictionary<String, Int32>> |        SerializeToUtf8Bytes |  8.236 us | 0.1465 us | 0.1370 us |  8.232 us |  8.020 us |  8.477 us | 0.8754 |     - |     - |   3.67 KB |
|   WriteDictionary<Dictionary<Guid, Int32>> | SerializeUtf8ObjectProperty | 18.688 us | 0.3887 us | 0.4476 us | 18.726 us | 18.111 us | 19.540 us | 4.3006 |     - |     - |  17.78 KB |
|  WriteDictionary<Dictionary<Int32, Int32>> | SerializeUtf8ObjectProperty | 15.688 us | 0.2953 us | 0.3032 us | 15.658 us | 15.225 us | 16.271 us | 2.2737 |     - |     - |   9.29 KB |
| WriteDictionary<Dictionary<String, Int32>> | SerializeUtf8ObjectProperty |  8.690 us | 0.1620 us | 0.1591 us |  8.688 us |  8.435 us |  8.969 us | 0.9363 |     - |     - |   3.95 KB |


## Deserialize/Read

**main:**
|                                      Type |                   Method |     Mean |    Error |   StdDev |   Median |      Min |      Max |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------ |------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|-------:|------:|------:|----------:|
| ReadDictionary<Dictionary<String, Int32>> | DeserializeFromUtf8Bytes | 22.05 us | 0.439 us | 0.470 us | 22.10 us | 21.23 us | 23.16 us | 4.0872 |     - |     - |   17176 B |

**KeyConverter:**
|                                      Type |                   Method |     Mean |    Error |   StdDev |   Median |      Min |      Max |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------------ |------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|-------:|------:|------:|----------:|
|   ReadDictionary<Dictionary<Guid, Int32>> | DeserializeFromUtf8Bytes | 29.35 us | 0.724 us | 0.805 us | 29.23 us | 28.50 us | 31.51 us | 5.0274 |     - |     - |  20.72 KB |
|  ReadDictionary<Dictionary<Int32, Int32>> | DeserializeFromUtf8Bytes | 20.11 us | 0.313 us | 0.278 us | 20.08 us | 19.77 us | 20.50 us | 2.7725 |     - |     - |  11.48 KB |
| ReadDictionary<Dictionary<String, Int32>> | DeserializeFromUtf8Bytes | 21.68 us | 0.453 us | 0.522 us | 21.73 us | 20.97 us | 22.79 us | 4.0213 |     - |     - |  16.77 KB |

**TypeConverter:**
|                                      Type |                   Method |     Mean |    Error |   StdDev |   Median |      Min |      Max |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------------------------ |------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|-------:|-------:|------:|----------:|
|   ReadDictionary<Dictionary<Guid, Int32>> | DeserializeFromUtf8Bytes | 34.83 us | 0.669 us | 0.593 us | 34.83 us | 33.84 us | 36.17 us | 5.8045 |      - |     - |  23.84 KB |
|  ReadDictionary<Dictionary<Int32, Int32>> | DeserializeFromUtf8Bytes | 26.39 us | 0.448 us | 0.419 us | 26.33 us | 25.79 us | 27.43 us | 3.3389 |      - |     - |  13.82 KB |
| ReadDictionary<Dictionary<String, Int32>> | DeserializeFromUtf8Bytes | 21.73 us | 0.378 us | 0.336 us | 21.77 us | 21.20 us | 22.29 us | 4.0248 | 0.1750 |     - |  16.77 KB |



# Prior-art
## Newtonsoft.Json

### On write:

* if the `TKey` is a concrete primitive type*:
  * it calls `Convert.ToString()`
  except for the next types:
    * DateTime (uses `DateFormatHandling` specified in options)
    * DateTimOffset
    * Double (uses `double.ToString("R")`) // 'R' stands for round-trip
    * Single
    * Enum (uses an internal helper method)

* If the `TKey` is `object` or non-primitive.
  * it calls the `TypeConverter` of the `TKey` runtime type.
  Except for :
    * `Type`, which returns the `AssemblyQualifiedName`.
  * If the type does not have a `TypeConverter`, it calls `ToString()` on the `TKey` instance.


\* A *primitive type* is a value cataloged as such by Json.Net from [this list](https://github.com/JamesNK/Newtonsoft.Json/blob/a31156e90a14038872f54eb60ff0e9676ca4a0d8/Src/Newtonsoft.Json/Utilities/ConvertUtils.cs#L119-L168).

### On read:

* If the `TKey` is a concrete type.
  * If is a primitive that implements `IConvertible`:
    * Calls `Convert.ChangeType(propertyName, concreteType)` But first tries to manually convert on these types:
      * Enum
      * DateTime
      * BigInteger
    * If the type does not implement `IConvertible`:
      * It tries to manually convert or cast to the concrete type using a few custom helper methods.
* If the `TKey` is `object`, the entries' keys will be of type `string` if they are quoted (Newtonsoft supports unquoted properties).

## Utf8Json
Supported types:
* String
* Numerics (int, float, etc.)
* Enum
* Guid
* Boolean
* Nullables of all the above types

## Jil
Supported types:
* String
* Numerics (int, long, etc. Note: does not support floating point types)
* Enum

# Notes
1. `DictionaryKeyPolicy` will apply to the resulting string of the non-string types.
1. Should we provide a way to allow users to customize the `EnumKeyConverter` behavior, as it is done in `JsonStringEnumConverter`?
As of now `KeyConverter`s are meant to be internal types, to enable the previously described behavior we either pass the options through `JsonSerializerOptions` or through an attribute.
1. Discuss support for `object` as the `TKey` type on deserialization, should we support it in this enhancement? `object` is treated as a `JsonElement` on deserialization and is not part of the supported types on the `Utf8Parser/Formatter`.
Consider to defer it when we add support for intuitive types (parse keys as string, etc. instead of JsonElement).
