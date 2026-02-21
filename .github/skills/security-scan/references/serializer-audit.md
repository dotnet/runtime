# Serializer Audit Guide

Deep audit guide for every serializer in the .NET ecosystem. Read this when the triage script flags `serialization` signals.

## Contents

- [Audit checklist (all serializers)](#universal-audit-checklist)
- [Per-serializer guides](#per-serializer-guides)
- [Anti-patterns to flag](#anti-patterns-to-flag)

## Risk Summary

| Serializer | Risk | Primary concern |
|---|---|---|
| `BinaryFormatter` | 🔴 Critical | Arbitrary type instantiation, known gadget chains |
| `SoapFormatter` | 🔴 Critical | Same as BinaryFormatter |
| `Newtonsoft.Json` | 🔴 High | `TypeNameHandling` enables type injection |
| Custom `ISerializable` | 🔴 High | Arbitrary constructor calls, no type restrictions |
| `System.Text.Json` | 🟡 Medium-High | Custom converters, polymorphic handling, `JsonDocument` DOS |
| `XmlSerializer` | 🟡 Medium | XXE by default, `[XmlInclude]` type expansion |
| `DataContractSerializer` | 🟡 Medium | `[KnownType]` expansion, reference preservation |
| `MessagePack` / `protobuf` | 🟢 Low-Medium | Schema mismatches, version confusion |

## Universal Audit Checklist

For **every** serializer instance found, answer these questions:

1. **Where is it instantiated?** What configuration options are set?
2. **Is type discrimination enabled?** (`TypeNameHandling`, `$type`, polymorphic type metadata, `[JsonDerivedType]`)
   - If yes: is the allowed type list restricted?
3. **Are there size/depth limits?**
   - `JsonSerializerOptions.MaxDepth` (default: 64)
   - `XmlReaderSettings.MaxCharactersInDocument` / `MaxCharactersFromEntities`
   - Custom limits on `Stream.Read` before deserialization
4. **Is the input source trusted or untrusted?**
   - Network input, file from user, database field → untrusted
   - Compile-time constant, resource embedded in assembly → trusted
5. **Can an attacker control the type being deserialized?**
   - Via `$type` property, XML element name, `__type` hint, etc.
6. **Are custom converters/resolvers present?**
   - Do they validate input before constructing objects?
   - Can they be tricked into instantiating unexpected types?
7. **What happens with malformed input?**
   - Does it throw, return default, or silently produce corrupt data?
   - Are exceptions caught and swallowed somewhere upstream?

## Per-Serializer Guides

### BinaryFormatter / SoapFormatter

**Status**: Obsolete and banned. Any usage is a finding.

**What to look for**:
- Direct instantiation (`new BinaryFormatter()`)
- Indirect use via `[Serializable]` types that implement `ISerializable`
- Legacy compat shims that wrap BinaryFormatter
- `BinaryFormatter.Deserialize(stream)` — this is arbitrary code execution

**Action**: Flag any usage. There are no safe configurations.

### System.Text.Json

**Key areas**:

1. **Custom `JsonConverter<T>`**: Does the `Read` method validate the JSON structure before constructing the object? Can malformed JSON cause it to enter an unexpected state?

2. **Polymorphic serialization** (`[JsonDerivedType]`, `JsonTypeInfo`): Are the allowed types explicitly listed? Can an attacker inject a type discriminator that resolves to an unexpected type?

3. **`JsonDocument` / `JsonElement`**: These hold the entire document in memory. Parsing an untrusted multi-GB stream → OOM. Check:
   - Is there a `MaxDepth` set?
   - Is the source stream bounded?
   - Is `JsonDocument.Parse` called on untrusted input without size limits?

4. **`Utf8JsonReader`**: Lower-level, generally safer, but check for:
   - `reader.GetString()` on untrusted input without length limits
   - Missing validation of `reader.TokenType` before accessing values

5. **Source generators** (`JsonSerializerContext`): Generally trusted — generated code is deterministic. Lower audit priority.

### XmlSerializer

**Key areas**:

1. **XXE (XML External Entities)**: `XmlSerializer` itself doesn't process DTDs, but the underlying `XmlReader` might if created without `DtdProcessing.Prohibit`.
   ```csharp
   // UNSAFE — default XmlReader processes DTDs
   var reader = XmlReader.Create(stream);
   // SAFE
   var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
   ```

2. **`[XmlInclude]` type expansion**: Can an attacker trigger deserialization of an included type they shouldn't have access to?

3. **`XmlReaderSettings` limits**: Are `MaxCharactersInDocument` and `MaxCharactersFromEntities` set for untrusted input?

### DataContractSerializer

**Key areas**:

1. **`[KnownType]` lists**: Are they minimal? Each known type expands the attack surface.

2. **Reference preservation** (`PreserveObjectReferences`): Can an attacker create circular references that cause stack overflow?

3. **`DataContractResolver`**: Custom resolvers that map type names to types — are the mappings restricted?

### Newtonsoft.Json (Json.NET)

**Key areas**:

1. **`TypeNameHandling`**: ANY value other than `None` is dangerous without a custom `SerializationBinder`:
   - `TypeNameHandling.All` — attacker controls all deserialized types
   - `TypeNameHandling.Auto` — attacker can inject `$type` on any polymorphic property
   - `TypeNameHandling.Objects` / `Arrays` — partial control, still exploitable

2. **`SerializationBinder`**: If present, does it whitelist types or just log? A binder that returns `null` for unknown types still allows `null` injection.

3. **`JsonConverter` implementations**: Same concerns as STJ custom converters.

### ISerializable

**Key areas**:

1. **Constructor**: `ISerializable` types must have a `(SerializationInfo, StreamingContext)` constructor. This constructor is called during deserialization and receives attacker-controlled data via `SerializationInfo`.

2. **`GetObjectData`**: Can expose internal state to serialized output if not carefully implemented.

3. **Trust of `SerializationInfo` values**: Does the constructor validate types and ranges of values from `info.GetValue()`? Trusting these values = trusting attacker input.

## Anti-Patterns to Flag

These are always reportable findings regardless of context:

| Pattern | Severity | Description |
|---|---|---|
| Any `BinaryFormatter` / `SoapFormatter` usage | 🔴 HIGH | Known arbitrary code execution |
| `TypeNameHandling != None` without restricted `SerializationBinder` | 🔴 HIGH | Type injection |
| `JsonSerializer.Deserialize<object>()` on untrusted input | 🔴 HIGH | No type restriction |
| `XmlReader.Create(stream)` without `DtdProcessing.Prohibit` | 🟡 MEDIUM | XXE if DTD present |
| `JsonDocument.Parse(stream)` without size limit on untrusted input | 🟡 MEDIUM | DOS via memory exhaustion |
| `ISerializable` constructor trusting `SerializationInfo` values | 🟡 MEDIUM | Attacker-controlled construction |
| Custom `JsonConverter.Read` without `TokenType` validation | 🟡 MEDIUM | Unexpected JSON structure |
| `DataContractSerializer` with 10+ `[KnownType]` entries | 🟡 MEDIUM | Excessive type surface |
