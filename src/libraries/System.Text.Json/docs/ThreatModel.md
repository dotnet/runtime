# `System.Text.Json` Threat Model

## Summary

`System.Text.Json` is a .NET library providing a serializer (`JsonSerializer`), reader (`Utf8JsonReader`), and writer (`Utf8JsonWriter`) implementation for the JavaScript Object Notation (JSON) Data Interchange Format, as specified in [RFC 8259](https://tools.ietf.org/html/rfc8259). The `JsonDocument` and `JsonElement` types are provided as immutable JSON document representations, and `JsonElement` instances are deserialized when JSON payloads are intended to map with `typeof(object)` properties or collection elements, for example with non-generic collections.

The emphasis of this document is to describe security threats that were considered when designing `JsonSerializer`, and how they can be mitigated.

## Serialization

The notes in this section apply to the following serialization methods:

- [`JsonSerializer.Serialize`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer.serialize?view=net-5.0)
- [`JsonSerializer.SerializeAsync`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer.serializeasync?view=net-5.0)
- [`JsonSerializer.SerializeToUtf8Bytes`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer.serializetoutf8bytes?view=net-5.0)

### Threat: Stack overflow due to circular references

.NET object graphs existing at runtime are generally trusted as inputs to `Utf8JsonWriter` and `JsonSerializer.Serialize` when writing. However, circular references in the instantiated input object graphs may cause a [`StackOverflowException`](https://docs.microsoft.com/dotnet/api/system.stackoverflowexception?view=net-5.0) if not guarded against, and possibly cause the application process to crash.

#### Mitigation

Circular references are detected when writing due to a maximum depth setting (64 by default), and a clear [`JsonException`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonexception?view=net-5.0) is thrown when this depth is exceeded. Note that the reference handling feature implemented by the serializer provides a way to preserve object references on serialization and deserialization.

### Threat: Unintentional information disclosure

`JsonSerializer` provides an opt-in only feature to allow the serialization of non-public properties and fields. If utilized, this may allow the exposure of private and internal data outside of the type or assembly during serialization. Additionally, the shape of a type can be exposed during serialization.

#### Mitigation

- By default, `JsonSerializer` does not allow the serialization of non-public members of input types and will not call any non-public surface area. An explicit opt-in is required to include these members. If this feature is utilized, be sure to understand your type's serialization projection and what data may be exposed.

- Be aware that the same type may have multiple serialization projections, depending on the serializer in use. The same type may expose one set of data when used with `System.Text.Json.JsonSerializer` and another set of data when used with `Newtonsoft.Json.JsonConvert`. Accidentally using the wrong serializer may lead to information disclosure. For example, a property that is annotated with `Newtonsoft.Json.JsonIgnoreAttribute` will be skipped when serializing and deserializing with `JsonConvert`, but will potentially be included when processed with `JsonSerializer`, as `System.Text.Json`'s serializer does not honor `Newtonsoft.Json` attributes.

## Deserialization

The notes in this section apply to the following deserialization methods:

- [`JsonSerializer.Deserialize`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer.deserialize?view=net-5.0)
- [`JsonSerializer.DeserializeAsync`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializer.deserializeasync?view=net-5.0)

The `Utf8JsonReader` class, leveraged by `JsonSerializer` for deserialization, accepts arbitrary UTF-8 text or binary data as input, and is therefore expected to handle data from potentially untrusted sources.

### Threat: Denial-of-Service (DoS) attacks due to deeply nested JSON

Unreasonably deeply nested JSON may be constructed to exhaust stack space on the machine performing deserialization. If unguarded against, this may cause the application process to crash, possibly resulting in a denial of service.

#### Mitigation

The reader has a `MaxDepth` property with a default value of 64 to avoid recursing too deep in JSON. The deserializer also has a `JsonSerializerOptions.MaxDepth` property (same default of 64) that is passed to the reader to configure the reader’s `MaxDepth`. The serializer assumes the reader will throw and thus does not check the value itself. The serializer uses the same `MaxDepth` for both serialization and deserialization. During serialization, the serializer does need to check against the `MaxDepth` since the writer does not have a configurable max. However, the writer does have a fail-safe limit of 1,000 depth which means a `MaxDepth` setting of > 1000 is not honored during serialization but is honored during deserialization.

### Threat: Algorithmic complexity attacks

Algorithmic complexity attacks have been considered as part of the design of `JsonSerializer`.

#### Mitigations

There is no known way to craft JSON that can cause the deserializer to do more work than the adversary had to do. That is, given a payload of length _n_ bytes, the total amount of work performed and memory allocated by `Utf8JsonReader` and `JsonSerializer.Deserialize` will be bounded by `O(n)`. There are no unmitigated `O(n^2)` or higher complexity algorithms in the deserialization routines.

### Threat: Loading `System.Type`s based on untrusted user data

Loading unintended types may have significant consequences, whether the type is malicious or just has security-sensitive side effects. A type may contain an exploitable security vulnerability, perform security-sensitive actions in its instance or `static` constructor, have a large memory footprint that facilitates DoS attacks, or may throw non-recoverable exceptions. Types may have `static` constructors that run as soon as the type is loaded and before any instances are created. For these reasons, it is important to control the set of types that the deserializer may load.

From the perspective of the serializer, this means that deserializing `System.Type` instances from JSON input is dangerous. Similarly, unrestricted polymorphic deserialization via type metadata in JSON, where a string representing the specific type to create is passed to an API such as `Type.GetType(string)`, is dangerous.

#### Mitigation

- Serialization and deserialization of `System.Type` instances is not supported by `JsonSerializer` by default. A custom converter can be implemented to handle `System.Type` instances, but care should be taken to avoid processing untrusted data.

- If a polymorphic deserialization feature is implemented in the future, it will **not** utilize a mechanism where the type to create is specified via passing an arbitrary JSON string to `Type.GetType(string)` and instantiating the indicated instance.

### Threat: Deserialized instances being in an unintended state

An instance of a type or an instantiated object graph may have internal consistency constraints that must be enforced. Care must be taken to avoid breaking these constraints during deserialization.

#### Mitigation

This situation can be avoided by being aware of the following points:

- Constructors run when `JsonSerializer.Deserialize` deserializes POCOs. Therefore, logic in constructors may participate in state management of the deserialized instance.

- If necessitated by your deserialization scenario, use callbacks to ensure that the object is in a valid state. This is not yet a first-class feature in the serializer, but a workaround is described in the [How to migrate from Newtonsoft.Json to System.Text.Json document](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-migrate-from-newtonsoft-how-to#callbacks). Be aware that this workaround potentially degrades performance.

- Where applicable, consider validating object graphs deserialized from untrusted data sources before processing them. Each individual object may be in a consistent state, but an object graph as a whole may not be.

### Threat: Hash collision vulnerability with dictionaries whose keys are not strings

A vulnerability exists when serializable types contain dictionaries whose keys are not strings; for example `Dictionary<int, ...>`, `Dictionary<double, ...>`, and `Dictionary<Employee, ...>`. The problem occurs if a large number of values are inserted into a hashtable where a large number of those values generate the same hash code. This can be used as a DoS attack. This applies broadly to collection types which bucket entries based on a hash code. For example, object graphs which contain properties typed as `HashSet<T>` (where `T` != `string`) may be subject to the same attack.

#### Mitigation

- `JsonSerializer` does not instantiate and populate any dictionaries with non-string keys in an unbounded manner in its internal implementation.

- When deserializing, dictionaries with non-string keys existing in the object graphs of input serializable types will be arbitrarily populated as dictated by the input payloads. Be sure to understand the nature of possible payloads that may be deserialized to dictionary members or types and if they present any security vulnerabilities.

## Serialization and deserialization

These notes apply to both serialization and deserialization.

### Threat: User-provided code execution with adverse effects

A number of routines in the `JsonSerializer` implementation run code that is provided by the user. For example, when deserializing, `JsonSerializer.Deserialize` may call user-provided constructors and property/field set accessors. Similarly when serializing, `JsonSerializer.Serialize` and property/field get accessors may be called.

The `JsonConverter` type provides a way for users to author logic that handles the serialization of a type, property, or field. This presents an extensibility point which will be invoked by the serializer.

These surface areas present security vulnerabilities in the event that they contain code that is malicious or just has security-sensitive side effects.

#### Mitigation

It is the responsibility of the code author to ensure that no security vulnerabilities exist. For example, if you create a serializable type with a property of type integer, and in the set accessor implementation allocate an array based on the property value, you expose the possibility of a DoS attack if a malicious message contains an extremely large value for this data member. In general, avoid any allocations based on incoming data or long-running processing in user-provided code (especially if long-running processing can be caused by a small amount of incoming data, i.e. not bounded linearly).

Also, you should ensure that there is no malicious code in any members within an input object graph, or custom converters which could be called by the serializer, including for types you do not own.

### Threat: Information disclosure due to exception messages

The message for an exception thrown by the serializer may contain the value or partial value of the current JSON property (on deserialization) or CLR property (on serialization) being processed. This is direct “user data” exposed; without this information it would be more difficult to determine what the errors are. It is expected that any application that displays or saves this information treats it as raw text.

#### Mitigation

- When using the serializer, consider what information might be included if an exception is thrown when handling your input data.

- Be cautious about how the raw data from the exception message is processed or forwarded by your application.

### Threat: Errors due to badly constructed, malicious, or non-RFC-compliant JSON payloads

Malicious payloads may contain badly formatted JSON in an attempt to cause unexpected exceptions, put the serializer or reader in an invalid state, or cause other security-sensitive problems.

The `JsonWriterOptions.SkipValidation` property specifies whether to allow a caller to write invalid JSON using `Utf8JsonWriter`. For better performance when writing, `JsonSerializer` sets this property to `true`. This setting is used even when a user-provided converter is used to serialize a type. This means that the only safeguard the `JsonSerializer.Serialize` offers against writing invalid JSON is ensuring that the nesting level of the written JSON is the same when the converter is finished as when it started. This limited validation opens the possiblility of writing invalid JSON that may be accidently (or intentionally) misused and presents a threat when passed as an input to parsers such as `JsonSerializer.Deserialize`.

By default, the `JsonSerializer` and `Utf8JsonReader` strictly honor the [RFC 8259 specification](https://tools.ietf.org/html/rfc8259). When enabling non-RFC-compliant behaviors (such as permitting single or multi-line comments when reading JSON), different deserializers may interpret incoming payloads differently. This could result in two different deserializers reading the exact same payload but populating an object's members with different values. This may have security implications for distributed systems. For example, if a validating frontend uses one deserializer but the backend system uses a different deserializer, this may present an opportunity for an adversary to craft a payload which appears safe when seen by the frontend but which is malicious when deserialized by the backend.

#### Mitigation

- The `Utf8JsonReader`, `Utf8JsonWriter`, and `JsonSerializer` types adhere to the [RFC 8259 specification](https://tools.ietf.org/html/rfc8259) by default, requiring the input and output payloads follow the standard formatting for strings, numbers, lists, and objects. Payloads that don't conform will cause a clear `JsonException` to be thrown when reading.

- Understand how different JSON processing routines across a distributed system handle inputs for both serialization and deserialization, and how any differences may affect the security of the entire system.

- Avoid writing serialization converters that produce non-standard output and thus require the receiver to opt in to potentially dangerous behaviors.

- When advancing through JSON payloads, the `Utf8JsonReader` validates that the next token does not violate RFC 8529. However, a deserialization converter may decide to validate that the next token is compatible with the target type they wish to obtain. For instance, if you are expecting a `JsonTokenType.Number` the reader will not validate that the next token is not a `JsonTokenType.StartObject`. For such cases, the converter could check the payload and throw a `JsonException` if it is invalid, similar to what the serializer would do. Otherwise, the reader will throw an `InvalidOperationException`.

    If a `JsonException` or `NotSupportedException` is thrown from a custom converter without a message, the serializer will append a message that includes the path to the part of the JSON that caused the error. For more information on error handling in converters, see [How to write custom converters for JSON serialization (marshalling) in .NET](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-converters-how-to?pivots=dotnet-5-0#error-handling).

## `JsonSerializerOptions`

The notes in this section apply to usages of the [`JsonSerializerOptions`](https://docs.microsoft.com/dotnet/api/system.text.json.jsonserializeroptions?view=net-5.0) type.

### Threat: Poor CPU throughput due to not caching custom `JsonSerializerOptions` instances

`JsonSerializerOptions` can be used to specify custom options when using the serializer. When a new options instance is passed to the serializer, it undergoes a warm-up phase during the first serialization of every type (for which data is present) in the object graph of the input type. This warm-up includes creating a cache of metadata it needs to perform serialization: funcs to property getters, setters, ctor arguments, specified attributes etc. This metadata cache is stored in the options instance. This process is not cheap, and can lead to very bad CPU time performance if done repeatedly.

#### Mitigation

It is recommended to cache options instances for reuse on subsequent calls to the serializer to avoid unnecessarily undergoing the warm-up repeatedly. Not caching the options instance can lead to performance issues.

If an options instance is not passed to the serializer, a default options instance is internally created and cached as a static variable. This means the warm-up phase is only performed once for every type when using the default options instance.

### Threat: Memory exhaustion due to (de)serializing numerous types

`JsonSerializer` creates and caches metadata for each unique type that is passed to it as an input (along with serializable types in their object graphs). This metadata is cached on the default `JsonSerializerOptions` instance, or in custom instances that are passed to the serializer. Serializing and deserializing numerous types, for example, multiple anonymous types or types created with reflection, can cause memory to be exhausted due to all the metadata being cached.

#### Mitigation

Be sure to maintain an upper bound to the number of types being passed to `JsonSerializer`, for both serialization and deserialization. For example, if implementing an API backend, do not accidentally create throwaway (dynamically generated) types per request to represent response data to be serialized and returned to the client.

### Threat: Elevation of privilege

`JsonSerializer` makes no trust decisions, so there's no potential risk due to accidental elevation of privilege. Systems built on top of JSON serialization such as ASP.NET Core's type serialization and custom user libraries need to determine security considerations for input and output data processed by the `JsonSerializer`.

## Implementation considerations

- When deserializing, `JsonSerializer.Deserialize` utilizes `Utf8JsonReader`'s `Read()` method to parse, validate, and advance through JSON payloads. The reader also provides methods for data type conversions, for example parsing `DateTime` instances from JSON strings. This functionality is utilized by the serializer when mapping JSON to types and properties. This process is typically a forward-only operation.

    However, the deserializer will "rewind" if all of the following conditions are met:

    - The `DeserializeAsync` method is used.
    - A custom converter is used for the given property type.
    - The JSON value starts with a StartArray or StartObject. If the JSON value is a primitive (e.g. string, number, bool, null) then the logic assumes no "read-ahead" is necessary for the converter.

    When these conditions are met, the deserializer will "read-ahead" or "skip" to the end of the current JSON scope (not all of the JSON) so that the converter won't encounter a situation where `reader.Read()` returns `false` due to lack of data in the current buffer held by the reader. Then the deserializer "rewinds" the offset back to the beginning so the custom converter sees the first token (which will be a `StartArray` or `StartObject`).

    This special "rewind" mode is **not** `O(n^2)`; instead it is a two-pass algorithm (so `O(2n)`).

    Another scenario where the serializer will go through the input a second time is during synchronous deserialization of objects with parameterized constructors (added in 5.0). The algorithm for this scenario consists of an optimized two-pass read of the JSON payload. The first pass parses the payload for constructor arguments needed to construct the object, while keeping track of the positions and metadata for JSON properties that map to regular CLR object properties. This first read is through the entire subset of the payload that corresponds to the particular object being deserialized to ensure “last one wins” semantics. On the second pass, only JSON mapping directly to properties on POCOs (and not via constructor) is processed.

- All exceptions that can be thrown directly from the `System.Text.Json` APIs have been documented. Exceptions thrown by lower level APIs which are not part of the normal execution flow of the various types are not documented. An example is `OutOfMemoryException` which can be thrown by `ArrayBufferWriter<T>` when used by `Utf8JsonWriter` and there is no more available memory to write data to.

    Exceptions thrown by the `System.Text.Json` APIs may contain the following:
    - Size of the buffer
    - Length of committed and uncommitted bytes
    - Current token type
    - The value or partial value of the current JSON property (on deserialization) or CLR property (on serialization) being processed.

- The serializer, reader, and writer strictly honor the [RFC 8259 specification](https://tools.ietf.org/html/rfc8259) by default, but more permissive settings such as allowing trailing commas, allowing quoted numbers, and allowing, skipping, or disallowing both single-line and multi-line comments can be opted-into with `JsonSerializerOptions` and `JsonReaderOptions`.

- The serializer has a built-in converter for `DateTime` and `DateTimeOffset` types which parses and formats according to the ISO 8601:1-2019 extended profile. The specification and information about workarounds using custom converters is documented in the docs website: https://docs.microsoft.com/en-us/dotnet/standard/datetime/system-text-json-support.

- The reference handling feature utilizes a non-standard JSON schema adopted from `Newtonsoft.Json` in which "$id", "$ref", and "$values" metadata properties are interspersed in JSON to allow keeping track of object references. When deserializing, this allows objects to point to each other in the resulting instance. When serializing, the resulting JSON is formatted using the previously mentioned representation. This feature is opt-in only (not enabled by default). When the feature is enabled, any deviation from the expected metadata in the input payloads when deserializing will cause a `JsonException` to be thrown.

- There are no unmitigated `O(n^2)` or higher complexity algorithms in the `System.Text.Json` APIs.

- Various UTF-8 and UTF-16 encode and decode APIs in `System.Text.Encoding`, as well as APIs in `System.Text.Web.Encodings` are used extensively to handle transcoding and JSON escaping logic. These APIs are safe for untrusted input. No JSON values are passed to other APIs as input (for example obtaining a `System.Type` instance from the string). No other transformations are applied on the input or output payloads of the serializer.

- When deserializing, `JsonSerializer` creates a `JsonElement` for JSON that is applied to a member type of `System.Object`. This has potential usability and performance issues in cases such as when a non-generic `IList` property will create a `JsonElement` for each member even if the JSON array only contains primitive values such a `string`s or `bool`s. This is the default behavior since there is no universal, consistent algorithm to map arbitrary JSON data to primitives like `string`, `double` etc. Treating the data as a property bag with `JsonElement` works for all cases and is consistent since it is based upon the CLR type and not the JSON contents. An alternative implementation which creates boxed primitives rather than `JsonElement`s is ambiguous, such as sometimes treating a JSON string as a `DateTime` (detected by parsing), or by creating a `double` for a JSON number if a decimal point is present (which can cause accuracy issues if `decimal` is expected) or creating a `long` for a JSON number if there is no decimal point (which can cause range issues if a `ulong` or `BigInteger` is expected). Thus the default is consistent and safe instead of convenient. It is possible, however, to change the `System.Object` behavior by using a custom converter.

 - When writing, `JsonSerializer` and `Utf8JsonWriter` escape all non-ASCII characters by default to provide defense-in-depth protections against cross-site scripting (XSS) or information-disclosure attacks. HTML-sensitive characters, such as `<`, `>`, `&`, and `'` are escaped as well. We provide an option to override the default behavior by specifying a custom `System.Text.Encodings.Web.JavaScriptEncoder`.

     `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, a built-in, more permissive encoder is provided. It doesn't escape HTML-sensitive characters or offer additional defense-in-depth protections against XSS or information-disclosure attacks. It should only be used when it is known that the client will be interpreting the resulting payload as UTF-8 encoded JSON. Never allow the output of `UnsafeRelaxedJsonEscaping` to be emitted into an HTML page or a `<script>` element.

- When deserializing, properties are processed in the order specified by the input payload. When serializing, properties are written according to the non-deterministic order returned when fetching the properties of a POCO with reflection. Do not rely on a specific property ordering of JSON when deserializing, or on a specific order when serializing. If a feature to specify serialization order is implemented in the serializer, assumptions based on ordering may then be utilized safely.
