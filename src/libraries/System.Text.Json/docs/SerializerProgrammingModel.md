This document describes the current serializer API (both committed and forward-looking) and provides information on associated features.

The APIs shown here reflection Preview 5.

Design points:
- Due to time constraints, and to gather feedback, the feature set is intended to a minimum viable product for 3.0.
  - An expectation is that a significant percent of Json.NET consumers would be able to use this, especially for ASP.NET scenarios. However, with the 3.0 release being a minimum viable product, that percent is not known. However, the percent will never be 100%, and that is neither a goal nor a long-term goal, because Json.NET has [many features](https://www.newtonsoft.com/json/help/html/JsonNetVsDotNetSerializers.htm) and some are not target requirements.
- Simple POCO object scenarios are targeted. These are typically used for DTO scenarios.
- The API designed to be extensible for new features in subsequent releases and by the community.
- Design-time attributes for defining the various options, but still support modifications at run-time.
- High performance - Minimal CPU and memory allocation overhead on top of the lower-level reader\writer.

# API
## JsonSerializer
This static class is the main entry point.

Let's start with coding examples before the formal API is provided.

Using a simple POCO class:
```cs
    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDay { get; set; }
    }
```

To deserialize a JSON string into a POCO instance:
```cs
    string json = ...
    Person person = JsonSerializer.Parse<Person>(json);
```

To serialize an object to a JSON string:
```cs
    Person person = ...
    string json = JsonSerializer.ToString(person);
```

Note there are also byte[]-based methods of these which are faster than using the string-based methods because the bytes (as UTF8) do not need to be converted to\from string (UTF16).

```cs
namespace System.Text.Json.Serialization
{
    public static class JsonSerializer
    {
        public static object Parse(ReadOnlySpan<byte> utf8Json, Type returnType, JsonSerializerOptions options = null);
        public static TValue Parse<TValue>(ReadOnlySpan<byte> utf8Json, JsonSerializerOptions options = null);

        public static object Parse(string json, Type returnType, JsonSerializerOptions options = null);
        public static TValue Parse<TValue>(string json, JsonSerializerOptions options = null);

        public static ValueTask<object> ReadAsync(Stream utf8Json, Type returnType, JsonSerializerOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
        public static ValueTask<TValue> ReadAsync<TValue>(Stream utf8Json, JsonSerializerOptions options = null, CancellationToken cancellationToken = default(CancellationToken));

        // Naming of `ToBytes` TBD based on API review; may want to expose a char8[] in addition to byte[].
        public static byte[] ToBytes(object value, Type type, JsonSerializerOptions options = null);
        public static byte[] ToBytes<TValue>(TValue value, JsonSerializerOptions options = null);

        public static string ToString(object value, Type type, JsonSerializerOptions options = null);
        public static string ToString<TValue>(TValue value, JsonSerializerOptions options = null);

        public static Task WriteAsync(object value, Type type, Stream utf8Json, JsonSerializerOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
        public static Task WriteAsync<TValue>(TValue value, Stream utf8Json, JsonSerializerOptions options = null, CancellationToken cancellationToken = default(CancellationToken));
    }
}
```
## JsonSerializerOptions
This class contains the options that are used during (de)serialization and optionally passed as the last argument into methods in `JsonSerializer`. If not specified, a global version is used (which is not accessible).

For performance, when a `JsonSerializerOptions` instance is created, it should be cached or re-used because caches are held by it.

```cs
namespace System.Text.Json.Serialization
{
    public class JsonSerializerOptions
    {
        public JsonSerializerOptions();

        // Note that all bool-based properties are false by default.

        public bool AllowTrailingCommas { get; set; }
        public int DefaultBufferSize { get; set; }
        public JsonNamingPolicy DictionaryKeyPolicy { get; set; }
        public bool IgnoreNullValues { get; set; }
        public bool IgnoreReadOnlyProperties { get; set; }
        public int MaxDepth { get; set; }
        public bool PropertyNameCaseInsensitive { get; set; }
        public JsonNamingPolicy PropertyNamingPolicy { get; set; }
        public JsonCommentHandling ReadCommentHandling { get; set; }
        public bool WriteIndented { get; set; }
    }
}

```
## Property Name feature
This feature determine how a property name is (de)serialized. Functionality includes:
- An attribute used to specify an explicit name (`JsonPropertyNameAttribute`).
- The `JsonSerializerOptions.PropertyNamingPolicy` property specifies a name converter for properties, such as a converter for camel-casing.
- A similar `DictionaryKeyPolicy` property is used to specify a name converter for dictionary keys.

To change a property name using the attribute (JSON will contain "birthdate" instead of "BirthDay")
```cs
    public class Person
    {
...
        [JsonPropertyName("birthdate")] public DateTime BirthDay { get; set; }
...
    }
```

To specify camel-casing:
```cs
    var options = new JsonSerializerOptions();
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

    // Be sure to specify the options class on calls to the serializer:
    string json = JsonSerializer.ToString(person, options);
```

It is possible to author a new converter by deriving from `JsonNamingPolicy` and overriding `ConvertName`.

### API
```cs
namespace System.Text.Json.Serialization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JsonPropertyNameAttribute : System.Text.Json.Serialization.JsonAttribute
    {
        public JsonPropertyNameAttribute(string propertyName) { }
        public string Name { get; set; }
    }

    public abstract partial class JsonNamingPolicy
    {
        public static System.Text.Json.Serialization.JsonNamingPolicy CamelCase { get; }

        protected JsonNamingPolicy() { }
        public abstract string ConvertName(string name);
    }    
}
```

## Ignore feature
The `JsonIgnore` attribute specifies that the property is not serialized or deserialized.

```cs
    [JsonIgnore] public DateTime? BirthDay { get; set; }
```

### API
```cs
   [System.AttributeUsage(AttributeTargets.Property, AllowMultiple=false)]
   public sealed partial class JsonIgnoreAttribute : System.Text.Json.Serialization.JsonAttribute
   {
       public JsonIgnoreAttribute() { }
   }
```

## Case insensitivity feature
The `JsonSerializerOptions.PropertyNameCaseInsensitive` property specifies that property names in JSON should be treated as being case-insensitive when finding the corresponding property name on an object. The default is case-sensitive.

Note that Json.NET is case-insensitive by default.

## Date Support
Dates are not part of JSON, so an internal converter is supplied. Dates are typically JSON strings. Currently there is an internal `DateTime` and `DateTimeOffset` converter that currently supports ISO 8601 formats. See https://github.com/dotnet/corefx/issues/34690 for more information.

If the internal converter is not sufficient, such as when the format has a custom format or is not ISO 8601 compatible, then a developer will be able to add a new value converter.

## Enum Support
By default, Enums are treated as longs in the JSON. This is most efficient. There will be future support to be based on strings; likely through an attribute to change the default for a given property and an option on `JsonSerializerOptions` to change the default globally.

## ICollection and Array Converter feature
By default there is an internal converter that supports any concrete class that implements IList<T> and an Array converter that supports jagged arrays (`foo[][]`) but not multidimensional (`foo[,]`).

Work is underway to support collections in the System.Collections.Immutable namespace and others automatically.

## Extensibility and manual (de)serialization
This is a pending feature and design work and requirement gathering is underway. There will likely be two types of converters: a "value" converter for primitive types that have a single value in JSON (such as a number or string) and an "object" converter that allows access to the reader and writer and will have before- and after- callbacks. These converters will be registered with `JsonSerializerOptions`.

# API comparison to Json.NET
## Simple scenario
Json.NET:
```cs
    Person person = ...;
    string json = JsonConvert.SerializeObject(person);
    person = JsonConvert.DeserializeObject(json);
```

JsonSerializer:
```cs
    Person person = ...;
    string json = JsonSerializer.ToBytes(person);
    person = JsonSerializer.Parse(json);
```
## Simple scenario with run-time settings
Json.NET:
```cs
    var settings = new JsonSerializerSettings();
    settings.NullValueHandling = NullValueHandling.Ignore;
    string json = JsonConvert.SerializeObject(person, settings);
```

JsonSerializer:
```cs
    var options = new JsonSerializerOptions();
    options.IgnoreNullValues = true;
    string json = JsonSerializer.ToString(person, options);
```

Note that Json.NET also has a `JsonSerializer` class with instance methods for advanced scenarios. See also Json.NET [code samples](https://www.newtonsoft.com/json/help/html/Samples.htm) and [documentation](https://www.newtonsoft.com/json/help/html/R_Project_Documentation.htm).

# Design notes
## JsonSerializerOptions
If an instance of `JsonSerializerOptions` is not specified when calling read\write then a default instance is used which is immutable and private. Having a global\static instance is not a viable feature because of unintended side effects when more than one area of code changes the same settings. Having a instance specified per thread\context mechanism is possible, but will only be added pending feedback. It is expected that ASP.NET and other consumers that have non-default settings maintain their own global, thread or stack variable and pass that in on every call. ASP.NET and others may also want to read a .config file at startup in order to initialize the options instance.

An instance of this class and exposed objects will be immutable once (de)serialization has occurred. This allows the instance to be shared globally with the same settings without the worry of side effects. The immutability is also desired with a future code-generation feature. Due to the immutable nature and fine-grain control over options through Attributes, it is expected that the instance is shared across users and applications. We may provide a Clone() method pending feedback.

## Static typing \ polymorphic behavior
The Read\Write methods specify statically (at compile time) the POCO type through `<TValue>` which below is `<Person>`:
```cs
    Person person = JsonSerializer.Parse<Person>(utf8);
    JsonSerializer.ToBytes<Person>(person);

    // Due to generic inference, Write can be simplified as:
    JsonSerializer.ToBytes(person);
```

For Read, this means that if the `utf8` data has additional properties that originally came from a derived class (e.g. a Customer class that derived from Person), only a Person object is instantiated. This is fairly obvious, since there is no hint to say a Customer was originally serialized.

For Write, this means that even if the actual `person` object is a derived type (e.g. a Customer instance), only the data from Person (and base classes, but not derived classes) are serialized. This is as not as obvious as Read, since the Write method could have internally called `object.GetType()` to obtain the type.

Because serialization of an object tree in an opt-out model (all properties are serialized by default), this static typing helps prevent misuses such as accidental data exposure of a derived runtime-created type.

However, static typing also limits polymorphic scenarios. This overload can be used with `GetType()` to address this:
```cs
    JsonSerializer.ToBytes(person, person.GetType());
```

In this case, if `person` is actually a Customer object, the Customer will be serialized.

When POCO objects are returned from other properties or collection, they follow the same static typing based on the property's type or the collection's generic type. There is not a mechanism to support polymorphic behavior here, although that could be added in the future as an opt-in model.

## Async support for Stream and Pipe (Pipe support pending active discussions)
The design supports async methods that can stream with a buffer size around the size of the largest property value which means the capability to asynchronously (de)serialize a very large object tree with a minimal memory footprint. This is unlike other serializers which may "drain" the Stream upfront during deserialization and pass around a potentially very large buffer which can cause the allocations to be placed on the Large Object Heap and cause other performance issues or simply run out of memory.

Currently the async `await` calls on Stream and Pipe is based on a byte threshold determined by the current buffer size.

For the Stream-based async methods, the Stream's `ReadAsync()` \ `WriteAsync()` are awaited. There is no call to `FlushAsync()` - it is expected the consumer does this or uses a Stream or an adapter that can auto-flush.

## Performance
The goal is to have a super fast (de)serializer given the feature set with minimal overhead on top of the reader and writer.

Design notes:
- Uses IL Emit by default for object creation. Property set\get are handled through direct delegates. There is potential here for additional emit support (including build-time or code-gen support) and\or new reflection features.
- Avoid boxing when feasible. POCO properties that are value types should not be boxed (and unboxed in IL).
- No objects created on the heap during (de)serialization after warm-up, with the exception of the POCO instance and its related state during serialization.
- No expensive reflection or other calls after warm-up (all items cached).
- Optimized lookup for property names using an array of long integers as the key (containing the first 6 bytes of the name plus 2 bytes for size) with fallback to comparing to byte[] when the key is a match (no conversion from byte to string necessary for the name). For POCO types with a high count of properties, we may (TBD) use a secondary algorithm such as a hashtable.

# Pending features not covered here

## Pending features that will affect the public API

### Loosely-typed arrays and objects
Support for the equivalent of `JArray.Load(serializer)` and `JObject.Load(serializer)` to process out-of-order properties or loosely-typed objects that have to be manually (de)serialized. This is being done
### Underflow \ overflow feature
These are important for version resiliency as they can be used to be backwards- or forward- compatible with changes to JSON schemas. It also allows the POCO object to have minimal deserialized properties for the local logic but enables preserving the other loosely-typed properties for later serialization \ round-trip support.
#### Default values \ underflow
Support default values for underflow (missing JSON data when deserializing), and trimming of default values when serializing.
#### Overflow
Ability to remember overflow (JSON data with no matching POCO property). Must support round-tripping meaning the ability to pass back this state (obtained during deserialize) back to a serialize method.
### Required Fields
Ability to specify which fields are required.

## Pending features that do not affect the public API

### IDictionary support
### Default creation of types for collections of type IEnumerable<T>, IList<T> or ICollection<T>.

# Currently supported Types
- Array (see the feature notes)
- Boolean
- Byte
- Char (as a JSON string of length 1)
- DateTime (see the feature notes)
- DateTimeOffset (see the feature notes)
- Dictionary<string, TValue> (currently just primitives in Preview 5)
- Double
- Enum (as integer for now)
- Int16
- Int32
- Int64
- IEnumerable (see the feature notes)
- IList (see the feature notes)
- Object (polymorhic mode for serialization only)
- Nullable < T >
- SByte
- Single
- String
- UInt16
- UInt32
- UInt64
