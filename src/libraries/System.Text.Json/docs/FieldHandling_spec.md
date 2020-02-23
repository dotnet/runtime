# Table of contet

- [Motivation](#motivation)
- [Proposal](#proposal)
  - [In depth](#in-depth)
  - [Compatibility](#compatibility)
- [Other implementations](#other-implementations)
    - [Jil](#jil)
    - [Utf8Json](#utf8json)
    - [Newtonsoft.Json](#newtonsoftjson)
    - [System.Runtime.Serialization.Json](#system-runtime-serialization-json)

# Motivation

Currently, there is no way to serialize and deserialize fields while they are an essetial part of type system which can be exposed to public like properties. While public properties are no recommended, they are used in .NET itself (see value tuples) and by users.

The feature stated to be requested even before .NET Core 3.0 was released, but wasn't included into it due to a lack of time.

# Proposal

Originally proposed by Layomi Akinrinade.

```csharp
namespace System.Text.Json
{
    public partial class JsonSerializerOptions
    {
        public bool IncludeFields { get; set; } = false;
    }
}
```

## In depth

By default the feature is turned off to prevent a leak for users which use `System.Text.Json` to serialize types with public fields because it could break contracts.

When the feature is turned on fields goes to be serialized the same way as properties without any difference and no additional attributes, keeping `System.Text.Json` clean and tiny, but powerful tool.

## Compatibility

The proposed implementation is incompatible with other popular JSON serializers if opt-in strategy is used. Legacy `System.Runtime.Serialization.Json` suppoorts fields oout of the box too.

```csharp
public class ObjectWithFields
{
    public string Value = "Some value";
}
```

### Jil

```csharp
var json = Jil.JSON.Serialize(new ObjectWithFields { Value = "Jil" });
// {"Value":"Jil"}
```

### Utf8Json

```csharp
var json = Utf8Json.JsonSerializer.ToJsonString(new ObjectWithFields { Value = "Utf8Json" });
// {"Value":"Utf8Json"}
```

### Newtonsoft.Json

```csharp
using var legacyJson = new MemoryStream();
new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(ObjectWithFields))
    .WriteObject(legacyJson, new ObjectWithFields { Value = "System.Runtime.Serialization.Json" });
var json = Encoding.UTF8.GetString(legacyJson.ToArray())
// {"Value":"System.Runtime.Serialization.Json"}
```
