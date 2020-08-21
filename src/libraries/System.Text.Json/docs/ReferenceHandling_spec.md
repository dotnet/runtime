# Table of Contents

- [Terminology](#terminology)
- [Motivation](#motivation)
- [Proposal](#proposal)
  - [In depth](#in-depth)
  - [Compatibility](#compatibility)
- [Examples](#examples)
  - [Using Default on Deserialize](#using-default-on-deserialize)
  - [Using Default on Serialize](#using-default-on-serialize)
  - [Using Ignore on Serialize](#using-ignore-on-serialize)
  - [Using Preserve on Serialize](#using-preserve-on-serialize)
  - [Using Preserve on Deserialize](#using-preserve-on-deserialize)
- [Other languages](#other-languages)
  - [Newtonsoft.Json](#newtonsoftjson)
  - [dojo toolkit (JavaScript framework)](#dojo-toolkit-javascript-framework)
  - [flatted (JavaScript module) (probably not worth it)](#flatted-javascript-module-probably-not-worth-it)
  - [Jackson (Java)](#jackson-java)
  - [golang](#golang)
- [Ground rules](#ground-rules)
  - [Reference objects ($ref)](#reference-objects-ref)
  - [Preserved objects ($id)](#preserved-objects-id)
  - [Preserved arrays](#preserved-arrays)
  - [JSON Objects if not Collection (Class | Struct | Dictionary) - On Deserialize (and Serialize?)](#json-objects-if-not-collection-class--struct--dictionary---on-deserialize-and-serialize)
  - [JSON Object if Collection - On Deserialize](#json-object-if-collection---on-deserialize)
  - [Immutable types](#immutable-types)
  - [Value types](#value-types)
  - [Interaction with JsonPropertyNameAttribute](#interaction-with-jsonpropertynameattribute)
- [Future](#future)
- [Notes](#notes)


# Terminology

**Reference loops**: Also referred as circular references, loops occur when a property of a .NET object refers to the object itself, either directly (a -> a) or indirectly (a -> b -> a). They also occur when the element of an array refers to the array itself (arr[0] -> arr). Multiple occurrences of the same reference do not imply a cycle.

**Preserve duplicated references**: Semantically represent objects and/or arrays that have been previously written, with a reference to them when found again in the object graph (using reference equality for comparison).

**Metadata**: Extra properties on JSON objects and/or arrays (that may change their schema) to enable reference preservation when round-tripping. These additional properties are only meant to be understood by the `JsonSerializer`.

# Motivation

Currently, there is no mechanism to avoid infinite loops while serializing .NET object instances that contain cycles nor to preserve references that round-trip when using `System.Text.Json`. The `JsonSerializer` throws a `JsonException` when a loop is found within the object graph.

This is a heavily requested feature since it is considered by many as a very common scenario, especially when serializing POCOs that came from an ORM Framework, such as Entity Framework; even though the JSON specification does not support reference loops by default. Therefore, this will be implemented as an opt-in feature (for both serialization and deserialization).

The current solution to deal with cycles in the object graph while serializing is to rely on `MaxDepth` and throw a `JsonException` after it is exceeded. This was done to avoid perf overhead for cycle detection in the common case. The goal is to enable the new opt-in feature with minimal impact to existing performance.


# Proposal

```cs
namespace System.Text.Json
{
    public partial class JsonSerializerOptions
    {
        public ReferenceHandling ReferenceHandling { get; set; } = ReferenceHandling.Default;
    }
}

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines the various ways the <see cref="JsonSerializer"/>
    /// can deal with references on Serialization and Deserialization.
    /// </summary>
    public sealed class ReferenceHandling
    {
        public static ReferenceHandling Default { get; }
        public static ReferenceHandling Preserve { get; }
        // TODO: decide if we keep or remove this option.
        public static ReferenceHandling Ignore { get; }
    }
}
```
See also the [internal implementation details](https://gist.github.com/Jozkee/b0922ef609f7a942f00ac2c93a976ff1).

## In depth
* **Default**:
   * **On Serialize**: Throw a `JsonException` when `MaxDepth` is exceeded. This may occur by either a reference loop or by passing a very deep object. This option will not affect the performance of the serializer.
   * **On Deserialize**: Metadata properties will not be consumed, therefore they will be treated as regular properties that can map to a real property using `JsonPropertyName` or be added to the `JsonExtensionData` overflow dictionary.

* **Preserve**:
  * **On Serialize**: When writing complex types (e.g. POCOs/non-primitive types), the serializer also writes the metadata (`$id`, `$values` and `$ref`) properties in order to reference them later by writing a reference to the previously written JSON object or array.
  * **On Deserialize**: While the other options have no effect on deserialization, `Preserve` does affect its behavior, as follows: Metadata will be expected (although is not mandatory) and the deserializer will try to understand it.

* **Ignore**:
  * **On Serialize**: Ignores (skips writing) the property/element where the reference loop is detected.
  * **On Deserialize**: Metadata properties will not be consumed, therefore they will be treated as regular properties that can map to a real property using `JsonPropertyName` or be added to the `JsonExtensionData` dictionary.

For `System.Text.Json`, the goal is to stick to the same *metadata* syntax used when preserving references using `Newtonsoft.Json` and provide a similar usage in `JsonSerializerOptions` that encompasses the needed options (e.g. provide reference preservation). This way, JSON output produced by `Newtonsoft.Json` can be deserialized by `System.Text.Json` and vice versa.

This API is exposing the `ReferenceHandling` property as a class, to be extensible in the future; and provide built-in static instances of `Default` and `Preserve` that are useful to enable the most common behaviors by just setting those in `JsonSerializerOptions.ReferenceHandling`.

With `ReferenceHandling` being a class, we can exclude things that, as of now, we are not sure are required and add them later based on customer feedback. For example, the `Object` and `Array` granularity of `Newtonsoft.Json's` `PreserveReferencesHandling` feature or the `ReferenceLoopHandling.Ignore` option.

## Compatibility

The next table show the combination of Newtonsoft's **ReferenceLoopHandling** (RLH) and **PreserveReferencesHandling** (PRH) and how to get its equivalent on System.Text.Json's *ReferenceHandling*:

|       RLH\PRH |      None |              All |          Objects |           Arrays |
|--------------:|----------:|-----------------:|-----------------:|-----------------:|
|     **Error** | *Default* | future (overlap) | future (overlap) | future (overlap) |
|    **Ignore** |  *Ignore* | future (overlap) | future (overlap) | future (overlap) |
| **Serialize** |    future |       *Preserve* |           future |           future |

Notes:
* We are deferring adding support for Newtonsoft's `MetadataPropertyHandling.ReadAhead` for now.
* `Objects` and `Arrays` granularity may apply to both, serialization and deserialization.
* (overlap) means that preserve references co-exists along with reference loop handling and we will need to define how to resolve that (On `Newtonsoft.Json`, `PreserveReferencesHandling` takes precedence); see [example](#using-a-custom-referencehandling-to-show-possible-future-usage).


# Examples

## Using Default on Deserialize
```cs
class Employee
{
    [JsonPropertyName("$id")]
    public string Identifier { get; set; }
    public Employee Manager { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> ExtensionData { get; set; }
}

private const string json =
    @"{
        ""$id"": ""1"",
        ""Name"": ""Angela"",
        ""Manager"": {
            ""$id"": ""2"",
            ""Name"": ""Bob"",
            ""Manager"": {
                ""$ref"": ""2""
            }
        }
    }";
```

```cs
public static void ReadObject()
{
    Employee angela = JsonSerializer.Deserialize<Employee>(json);
    Console.WriteLine(angela.Identifier); //prints: "1".
    Console.WriteLine(angela.Manager.Identifier); //prints: "2".
    Console.WriteLine(angela.Manager.Manager.ExtensionData["$ref"]); //prints: "2".
}
```

Note how you can annotate .Net properties to use properties that are meant for metadata and are added to the `JsonExtensionData` overflow dictionary, in case there is any, when opting-out of the `ReferenceHanding.Preserve` feature.

For the next samples, let's assume you have the following class:
```cs
class Employee
{
    public string Name { get; set; }
    public Employee Manager { get; set; }
    public List<Employee> Subordinates { get; set; }
}
```

## Using Default on Serialize
```cs
private Employee bob = new Employee { Name = "Bob" };
private Employee angela = new Employee { Name = "Angela" };

angela.Manager = bob;
bob.Subordinates = new List<Employee>{ angela };

public static void WriteObject()
{
    string json = JsonSerializer.Serialize(angela, options);
    // Throws JsonException -
    // "A possible object cycle was detected which is not supported.
    // This can either be due to a cycle or if the object depth is larger than the maximum allowed depth of 64."
}
```

## Using Ignore on Serialize
```cs
private Employee bob = new Employee { Name = "Bob" };
private Employee angela = new Employee { Name = "Angela" };

angela.Manager = bob;
bob.Subordinates = new List<Employee>{ angela };
```

On `System.Text.Json`:
```cs
public static void WriteIgnoringReferenceLoops()
{
    var options = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Ignore
        WriteIndented = true,
    };

    string json = JsonSerializer.Serialize(angela, options);
    Console.Write(json);
}
```

On `Newtonsoft.Json`:
```cs
public static void WriteIgnoringReferenceLoops()
{
    var settings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        Formatting = Formatting.Indented
    };

    string json = JsonConvert.SerializeObject(angela, settings);
    Console.Write(json);
}
```

Output:
```jsonc
{
    "Name": "Angela",
    "Manager": {
        "Name": "Bob",
        // Note how subordinates is empty because Angela is being ignored.
        "Subordinates": []
    }
}
```

## Using Preserve on Serialize
```cs
private Employee bob = new Employee { Name = "Bob" };
private Employee angela = new Employee { Name = "Angela" };

angela.Manager = bob;
bob.Subordinates = new List<Employee>{ angela };
```

On `System.Text.Json`:
```cs
public static void WritePreservingReference()
{
    var options = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Preserve
        WriteIndented = true,
    };

    string json = JsonSerializer.Serialize(angela, options);
    Console.Write(json);
}
```

On `Newtonsoft.Json`:
```cs
public static void WritePreservingReference()
{
    var settings = new JsonSerializerSettings
    {
        PreserveReferencesHandling = PreserveReferencesHandling.All
        Formatting = Formatting.Indented
    };

    string json = JsonConvert.SerializeObject(angela, settings);
    Console.Write(json);
}
```

Output:
```jsonc
{
    "$id": "1",
    "Name": "Angela",
    "Manager": {
        "$id": "2",
        "Name": "Bob",
        "Subordinates": {
            // Note how the Subordinates' square braces are replaced with curly braces
            // in order to include $id and $values properties,
            // $values will now hold whatever value was meant for the Subordinates list.
            "$id": "3",
            "$values": [
                {  // Note how this object denotes reference to Angela that was previously serialized.
                    "$ref": "1"
                }
            ]
        }
    }
}
```

## Using Preserve on Deserialize
```cs
private const string json =
    @"{
        ""$id"": ""1"",
        ""Name"": ""Angela"",
        ""Manager"": {
            ""$id"": ""2"",
            ""Name"": ""Bob"",
            ""Subordinates"": {
                ""$id"": ""3"",
                ""$values"": [
                    {
                        ""$ref"": ""1""
                    }
                ]
            }
        }
    }";
```
On `System.Text.Json`:
```cs
public static void ReadJsonWithPreservedReferences(){
    var options = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Preserve
    };

    Employee angela = JsonSerializer.Deserialize<Employee>(json, options);
    Console.WriteLine(object.ReferenceEquals(angela, angela.Manager.Subordinates[0])); //prints: true.
}
```

On `Newtonsoft.Json`:
```cs
public static void ReadJsonWithPreservedReferences(){
    var options = new JsonSerializerSettings
    {
        //Newtonsoft.Json reads metadata by default, just setting the option for illustrative purposes.
        MetadataPropertyHanding = MetadataPropertyHandling.Default
    };

    Employee angela = JsonConvert.DeserializeObject<Employee>(json, settings);
    Console.WriteLine(object.ReferenceEquals(angela, angela.Manager.Subordinates[0])); //prints: true.
}
```

# Other languages

## Newtonsoft.Json
`Newtonsoft.Json` contains settings that you can enable to deal with such problems.
* For Serialization:
  * [`ReferenceLoopHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_ReferenceLoopHandling.htm)
  * [`PreserveReferencesHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_PreserveReferencesHandling.htm)
* For Deserialization:
  * [`MetadataPropertyHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_MetadataPropertyHandling.htm)

When using `ReferenceLoopHandling.Ignore`, other objects that were already seen on the current graph branch will be ignored on serialization.

When using `PreserveReferencesHandling.All` you are signaling that your resulting JSON will contain *metadata* properties `$ref`, `$id` and `$values` which are going to act as reference identifiers (`$id`) and pointers (`$ref`).
Now, to read back those references, you have to use `MetadataPropertyHandling.Default` to indicate that *metadata* is expected in the payload passed to the `Deserialize` method.

* Pros
  * If we opt-in for this we could provide compatibility with Newtonsoft which is always desired by the community.
* Cons
  * Quite invasive, (it affects `JsonException.Path`, `JsonSerializerOptions.IgnoreNullValues`, `JsonPropertyNameAttribute`, and Converters).
  * This could break existing converters. For example, an array converter may expect the first token to be "[" but a preserved array starts with "{".
    * perhaps converters are more feasible with the JSON path implementation.
  * We will now accept that an array comes in valid format when starts with a curly brace "{"; below issue is related to guard against NRE when this happens:
    * https://github.com/dotnet/runtime/issues/31192

## dojo toolkit (JavaScript framework)
https://dojotoolkit.org/reference-guide/1.10/dojox/json/ref.html

Similar: https://www.npmjs.com/package/json-cyclic

* id-based (ignore this approach since it is the same as `Newtonsoft.Json`)
* path-based
  * "\#" denotes the root of the object and then uses semantics inspired by JSONPath.
  * It does not uses `$id` nor `$values` metadata, therefore, everything can be referenced.
  * Pros
    * It looks cleaner.
    * Only disruptive (weird) edge case would be a reference to an array e.g: { "MyArray": { "$ref": "#manager.subordinates" } }.
  * Cons
    * Path value will become too long on very deep objects.
    * Storing all the complex types could become very expensive, are we going to store also primitive types?
    * This would break existing converters when handling reference to an array.
    * Not compatible with `Newtonsoft.Json`.

## flatted (JavaScript module) (probably not worth it)
https://github.com/WebReflection/flatted

* While stringifying, all Objects, including Arrays and strings, are flattened out and replaced with a unique index.
* Once parsed, all indexes will be replaced through the flattened collection.
* It has 23M downloads per month.
* Every single value (primitive and complex) is preserved.
* Cons:
  * It does not look like JSON anymore.


## Jackson (Java)
https://www.baeldung.com/jackson-bidirectional-relationships-and-infinite-recursion

* Let you annotate your class with `@JsonIdentityInfo` where you can define a class property that will be used to further represent the object.

## golang
* Circularity detection will start to occur after a fixed threshold of 1,000 depth.
  * [This fix](https://go-review.googlesource.com/c/go/+/187920/) is about detecting circular references after a threshold of 1,000 and throw when found in order to prevent a non-recoverable stack overflow.

# Ground rules

As a rule of thumb, we throw on all cases where the JSON payload being read contains any metadata that is impossible to create with the `JsonSerializer` (e.g. it was hand modified). Since `System.Text.Json` is more strict, it means that certain payloads that `Newtonsoft.Json` could process, will fail with `System.Text.Json`. Specific example scenarios where that could happen are described below.

## Reference objects ($ref)

* Regular property **before** `$ref`.
  * **Newtonsoft.Json**: `$ref` is ignored if a regular property is previously found in the object.
  * **S.T.Json**: Throw - Reference objects cannot contain other properties.

```json
{
    "$id": "1",
    "Name": "Angela",
    "Manager": {
        "Name": "Bob",
        "$ref": "1"
    }
}
```

* Regular property **after** `$ref`.
  * **Newtonsoft.Json**: Throw - Additional content found in JSON reference object.
  * **S.T.Json**: Throw - Reference objects cannot contain other properties.

```json
{
    "$id": "1",
    "Name": "Angela",
    "Manager":{
        "$ref": "1",
        "Name": "Angela"
    }
}
```

* Metadata property **before** `$ref`:
  * **Newtonsoft.Json**: `$id` is disregarded, and the reference is set.
  * **S.T.Json**: Throw - Reference objects cannot contain other properties.
```json
{
    "$id": "1",
    "Name": "Angela",
    "Manager": {
        "$id": "2",
        "$ref": "1"
    }
}
```

* Metadata property **after** `$ref`:
  * **Newtonsoft.Json**: Throw with the next message: 'Additional content found in JSON reference object'.
  * **S.T.Json**: Throw - Reference objects cannot contain other properties.
```json
{
    "$id": "1",
    "Name": "Angela",
    "Manager": {
        "$ref": "1",
        "$id": "2"
    }
}
```

* Reference object is before preserved object (or preserved object was never spotted):
  * **Newtonsoft.Json**: Reference object evaluates as `null`.
  * **S.T.Json**: Throw - Reference not found.
```json
[
    {
        "$ref": "1"
    },
    {
        "$id": "1",
        "Name": "Angela"
    }
]
```

## Preserved objects ($id)

* Having more than one `$id` in the same object:
  * **Newtonsoft.Json**: last one wins, in the example, the reference object evaluates to `null` (if `$ref` would be `"2"`, it would evaluate to itself).
  * **S.T.Json**: Throw - $id must be the first property.
```json
{
    "$id": "1",
    "$id": "2",
    "Name": "Angela",
    "Manager": {
        "$ref": "1"
    }
}
```

* `$id` is not the first property:
  * **Newtonsoft.Json**: Object is not preserved and cannot be referenced, therefore any reference to it would evaluate as null.

  * **S.T.Json**: Throw - Object $id is not the first property.
  Note: In case we would want to switch, we can handle the `$id` not being the first property since we store the reference at the moment we spot the `$id` property, we throw to honor the rule of thumb.
```json
{
    "Name": "Angela",
    "$id": "1",
    "Manager": {
        "$ref": "1"
    }
}
```

* `$id` is duplicated (not necessarily nested):
  * **Newtonsoft.Json**: Throws - Error reading object reference '1'- Inner Exception: ArgumentException: A different value already has the Id '1'.
  * **S.T.Json**: Throws - Duplicated id found while preserving reference.
```json
[
    {
        "$id": "1",
        "Name": "Angela"
    },
    {
        "$id": "1",
        "Name": "Bob"
    }
]
```

## Preserved arrays
A regular array is `[ elem1, elem2 ]`.
A preserved array is written in the next format `{ "$id": "1", "$values": [ elem1, elem2 ] }`

* Preserved array does not contain any metadata:
  * **Newtonsoft.Json**: Throws - Cannot deserialize the current JSON object into type 'System.Collections.Generic.List`1
  * **S.T.Json**: Throw - Preserved array $values property was not present or its value is not an array.

  ```json
  {}
  ```

* Preserved array only contains $id:
  * **Newtonsoft.Json**: Throws - Cannot deserialize the current JSON object into type 'System.Collections.Generic.List`1
  * **S.T.Json**: Throw - Preserved array $values property was not present or its value is not an array.

  ```json
  {
      "$id": "1"
  }
  ```

* Preserved array only contains `$values`:
  * **Newtonsoft.Json**: Does not throw and the payload evaluates to the array in the property.
  * **S.T.Json**: Throw - Preserved arrays cannot lack an identifier.

  ```json
  {
      "$values": []
  }
  ```

* Preserved array `$values` property is null
  * **Newtonsoft.Json**: Throw - Unexpected token while deserializing object: EndObject. Path ''.
  * **S.T.Json**: Throw - Invalid token after $values metadata property.

  ```json
  {
      "$id": "1",
      "$values": null
  }
  ```

* Preserved array `$values` property is a primitive value
  * **Newtonsoft.Json**: Unexpected token while deserializing object: EndObject. Path ''.
  * **S.T.Json**: Throw - Invalid token after $values metadata property.

  ```json
  {
      "$id": "1",
      "$values": 1
  }
  ```

* Preserved array `$values` property contains object
  * **Newtonsoft.Json**: Unexpected token while deserializing object: EndObject. Path ''.
  * **S.T.Json**: Throw - Invalid token after $values metadata property.

  ```json
  {
      "$id": "1",
      "$values": {}
  }
  ```

* Preserved array contains a property other than `$id` and `$values`
  * **Newtonsoft.Json**: Ignores other properties.
  * **S.T.Json**: Throw - Invalid property in preserved array.

  ```json
  {
      "$id": "1",
      "$values": [1, 2, 3],
      "TrailingProperty": "Hello world"
  }
  ```

## JSON Objects if not Enumerable (Class | Struct | Dictionary) - On Deserialize (and Serialize?)

* `$ref` **Valid** under conditions:
  * must be the only property in the object.

* `$id` **Valid** under conditions:
  * must be the first property in the object.

* `$values` **Not valid**

* `$.*` **Not valid**

* `\u0024.*` **valid**

* `\u0024id*` **valid** but not considered metadata.

Note: For Dictionary keys on serialize, should we allow serializing keys `$id`, `$ref` and `$values`? If we allow it, then there is a potential round-tripping issue.
Sample of similar issue with `DictionaryKeyPolicy`:
```cs
public static void TestDictionary_Collision()
{
    var root = new Dictionary<string, int>();
    root["helloWorld"] = 100;
    root["HelloWorld"] = 200;

    var opts = new JsonSerializerOptions
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    string json = JsonSerializer.Serialize(root, opts);
    Console.WriteLine(json);
    /* Output:
    {"helloWorld":100,"helloWorld":200} */

    // Round tripping issue
    root = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
}
```

Resolution for above issue:
On serialization, when a JSON property name, that is either a dictionary key or a CLR class property, starts with a '$' character, we must write the escaped character "\u0024" instead.

On deserialization, metadata will be digested by using only the raw bytes, so no encoded characters are allowed in metadata; to read JSON properties that start with a '$' you will need to pass it with the escaped '$' (\u0024) or turn the feature off.


## JSON Object if Enumerable - On Deserialize

* `$ref` **Valid** under conditions:
  * must be the only property in the object.

* `$id` **Valid** under conditions:
  * must be the first property in the object.

* `$values` **Valid** under conditions:
  * must be after `$id`

* `.*` **Not Valid** any property other than above metadata will not be valid.


## Immutable types
Since these types are created with the help of an internal converter, and they are not parsed until the entire block of JSON finishes; nested reference to these types is impossible to identify, unless you re-scan the resulting object, which is too expensive.

With that said, the deserializer will throw when it reads `$id` on any of these types. When serializing (e.g. writing) those types, however, they are going to be preserved as any other collection type (`{ "$id": "1", "$values": [...] }`) since those types can still be parsed into a collection type that is supported.

Note: By the same principle, `Newtonsoft.Json` does not support parsing JSON arrays into immutables as well.
Note 2: When using immutable types and `ReferenceHandling.Preserve`, you will not be able to generate payloads that are capables of round-tripping.

* **Immutable types**: e.g: `ImmutableList` and `ImmutableDictionary`
* **System.Array**: e.g: `Array<T>` and `T[]`

## Value types

* **Serialization**:
The serializer emits an `$id` for every JSON complex type. However, to reduce bandwidth, structs will not be written with metadata, since it would be meaningless due `ReferenceEquals` is used when comparing the objects and no backpointer reference would be ever written to an struct.

```cs
public static void SerializeStructs()
{
    EmployeeStruct angela = new EmployeeStruct
    {
        Name = "Angela"
    };

    List<EmployeeStruct> employees = new List<EmployeeStruct>
    {
        angela,
        angela
    };

    var options = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Preserve
    };

    string json = JsonSerializer.Serialize(employees, options);
    Console.WriteLine(json);
}

Output:
```json
{
    "$id": "1",
    "$values": [
        {
            "Name": "Angela"
        },
        {
            "Name": "Angela"
        }
    ]
}
```

* **Deserialization**:
The deserializer will throw when it reads `$ref` within a property that matches to a value type (such as a struct) and `ReferenceHandling.Preserve` is set.

Example:
```cs
public static void DeserializeStructs()
{
    string json = @"
    {
        ""$id"": ""1"",
        ""$values"": [
            {
                ""$id"": ""2"",
                ""Name"": ""Angela""
            },
            {
                ""$ref"": ""2""
            }
        ]
    }";

    var options = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Preserve
    };

    List<EmployeeStruct> root = JsonSerializer.Deserialize<List<EmployeeStruct>>(json, options);
    // Throws JsonException.
}
```

In other words, having a `$ref` property in a struct, is never emitted by the serializer and reading such a payload (for instance, if the payload was hand-crafted) is not supported by the deserializer. However, since `Newtonsoft.Json` does emit `$id` for value-type objects `System.Text.Json` will allow reading struct objects that contain `$id`, regardless of not being able to create such payloads.

## Interaction with JsonPropertyNameAttribute
Let's say you have the following class:

```cs
private class EmployeeAnnotated
{
    [JsonPropertyName("$id")]
    public string Identifier { get; set; }
    [JsonPropertyName("$ref")]
    public string Reference { get; set; }
    [JsonPropertyName("$values")]
    public List<EmployeeAnnotated> Values { get; set; }

    public string Name { get; set; }
}
```

Both on serialization and deserialization:

```cs
public static void DeSerializeWithPreserve()
{
    var root = new EmployeeAnnotated();
    var opts = new JsonSerializerOptions
    {
        ReferenceHandling = ReferenceHandling.Preserve
    };

    // The property will be emitted with the '$' encoded.
    string json = JsonSerializer.Serialize(root, opts);
    Console.WriteLine(json);
}
```

```json
{
    "\u0024id": null,
    "\u0024ref": null,
    "\u0024values": null
}
```

If the name of your property starts with '$', either by using `JsonPropertyNameAttribute`, by using F#, or by any other reason, that leading '$' (and that one only), will be replaced with its encoded equivalent `\u0024`.

# Future
Things that we may want to consider building on top based on customer feedback:

* (De)Serialize can define its own, independently configurable reference handling behavior (for example, you could opt-out from preserve reference on serialization but opt-in for reading them on deserialization).

* Expose a `ReferenceResolver` to override the logic that preserves references (the caller creates their own implementation of a reference resolver).

* Expose the `ReferenceResolver` in `Converters` to have access to the map of references.

* Create `JsonReferenceHandlingAttribute` to enable annotating properties and classes with their own isolated `ReferenceHandling` behavior (I am deferring this feature because the constant checking for attributes was causing too much perf overhead on the main path, but maybe we can try moving the attribute check to the warm-up method to reduce the runtime increase).
```cs
// Example of a class annotated with JsonReferenceHandling attributes.
[JsonReferenceHandling(ReferenceHandling.Preserve)]
public class Employee {
    public string Name { get; set; }

    [JsonReferenceHandling(ReferenceHandling.Ignore)]
    public Employee Manager { get; set; }

    public List<Employee> Subordinates { get; set; }
}
```

## Using a custom `ReferenceHandling` (shows potential API evolution and usage).
```cs
public static void WriteIgnoringReferenceLoopsAndReadPreservedReferences()
{
    var bob = new Employee { Name = "Bob" };
    var angela = new Employee { Name = "Angela" };

    angela.Manager = bob;
    bob.Subordinates = new List<Employee>{ angela };

    var allEmployees = new List<Employee>
    {
        angela,
        bob
    };

    var options = new JsonSerializerOptions
    {
        ReferenceHandling = new ReferenceHandling(
            PreserveReferencesHandling.All, // Preserve References Handling on serialization.
            PreserveReferencesHandling.All, // Preserve References Handling on deserialization.
            ReferenceLoopHandling.Ignore) // Reference Loop Handling on serialization.
        WriteIndented = true,
    };

    string json = JsonSerializer.Serialize(allEmployees, options);
    Console.Write(json);

    /* Output:
    [
        {
            "$id": "1",
            "Name": "Angela",
            "Manager": {
                "$id": "2",
                "Name": "Bob",
                "Subordinates": {
                    "$id": "3",
                    // Note how subordinates is empty because Angela is being ignored.
                    // Alternatively: we may let PreserveReferenceHandling take precedence and write the reference instead?
                    "$values": []
                }
            }
        },
        {
            // Note how element 2 is written as a reference
            // since was previously seen in allEmployees[0].Manager
            "$ref": "2"
        }
    ]
    */

    allEmployees = JsonSerializer.Deserialize<List<Employee>>(json, options);
    Console.WriteLine(allEmployees[0].Manager == allEmployees[1]);
    /* Output: true */
}
```

# Notes

1. `MaxDepth` validation will not be affected by `ReferenceHandling.Preserve`.
2. We are merging the `Newtonsoft.Json` types [`ReferenceLoopHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_ReferenceLoopHandling.htm), [`MetadataPropertyHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_MetadataPropertyHandling.htm) (without `ReadAhead`), and [`PreserveReferencesHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_PreserveReferencesHandling.htm) (without the granularity of `Objects` and `Arrays`) into one single class; `ReferenceHandling`.
3. While immutable types and `System.Arrays` can be serialized with preserve semantics, they will not be supported when trying to deserialize them as a reference. Those types are created with the help of an internal converter and they are not parsed until the entire block of JSON finishes. Nested reference to these types is impossible to identify, unless you re-scan the resulting object, which is too expensive.
4. Value types, such as structs that contain preserve semantics, will not be supported when deserializing as well. This is because the serializer will never emit a reference object to those types and doing so implies boxing of value types.
5. Additional features, such as converter support, `ReferenceResolver`, `JsonPropertyAttribute.IsReference` and `JsonPropertyAttribute.ReferenceLoopHandling`,  that build on top of `ReferenceLoopHandling` and `PreserveReferencesHandling` were considered but they can be added in the future based on customer requests.
6. We are still looking for evidence that backs up supporting `ReferenceHandling.Ignore`. This option will not ship if said evidence is not found.
7. Round-tripping support for preserved references into the `JsonExtensionData` is currently not supported (we emit the metadata on serialization and we create a JsonElement on deserialization instead), while in `Newtonsoft.Json` they are supported. This may change in a future based on customer feedback.
