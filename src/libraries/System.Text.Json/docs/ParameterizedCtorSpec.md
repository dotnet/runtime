# Deserializing objects using parameterized constructors with `JsonSerializer`

## Motivation

`JsonSerializer` deserializes instances of objects (`class`es and `struct`s) using public parameterless
constructors. If none is present, and deserialization is attempted, the serializer throws a `NotSupportedException`
with a message stating that objects without public parameterless constructors, including `interface`s and `abstract`
types, are not supported for deserialization. There is no way to deserialize an instance of an object using a parameterized constructor.

A common pattern is to make data objects immutable for various reasons. For example, given `Point`:

```C#
public struct Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

It would be beneficial if `JsonSerializer` could deserialize `Point` instances using the parameterized constructor above, given that mapping JSON properties into readonly members is not supported.

Also consider `User`:

```C#
public class User
{
  public string UserName { get; private set; }

  public bool Enabled { get; private set; }

  public User() { }

  public User(string userName, bool enabled)
  {
    UserName = userName;
    Enabled = enabled;
  }
}
```

`User` instances will be deserialized using the parameterless constructor above, and the `UserName` and `Enabled` properties will be ignored, even if there is JSON that maps to them in the payload.

Although there is work scheduled to support deserializing JSON directly into properties with private setters
(https://github.com/dotnet/runtime/issues/29743), providing parameterized constructor support as an option
increases the scope of support for customers with varying design needs.

Deserializing with parameterized constructors also gives the opportunity to do JSON "argument" validation once on the creation of the instance.

This feature enables deserialization support for `Tuple<...>` instances, using their parameterized constructors.

This feature is designed to support round-tripping of such "immutable" types.

There are no easy workarounds for the scenarios this feature enables:

- Support for immutable classes and structs (https://github.com/dotnet/runtime/issues/29895)
- Choosing which constructor to use
- Support for `Tuple<...>` types

Only public constructors are supported.

## New API Proposal

```C#
namespace System.Text.Json.Serialization
{
  /// <summary>
  /// When placed on a constructor, indicates that the constructor should be used to create
  /// instances of the type on deserialization.
  /// <remarks>The constructor must be public. The attribute cannot be placed on multiple constructors.</remarks>
  /// </summary>
  [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
  public sealed partial class JsonConstructorAttribute : JsonAttribute
  {
    public JsonConstructorAttribute() { }
  }
}
```

### Example usage

Given an immutable class `Point`,

```C#
public class Point
{
  public int X { get; }

  public int Y { get; }

  public Point() {}

  [JsonConstructor]
  public Point(int x, int y) => (X, Y) = (x, y);
}
```

We can deserialize JSON into an instance of `Point` using `JsonSerializer`:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

## Solutions by other libraries

### `Newtonsoft.Json` (.NET)

`Newtonsoft.Json` provides a `[JsonConstructor]` attribute that allows users to specify which constructor to use. The attribute can be applied to only one constructor, which may be non-`public`.

`Newtonsoft.Json` also provides a globally applied
[`ConstructorHandling`](https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_ConstructorHandling.htm) which determines which constructor is used if none is specified with the attribute. The options are:

`Default`: First attempt to use the public default constructor, then fall back to a single parameterized constructor,
then to the non-`public` default constructor.

`AllowNonPublicDefaultConstructor`: Newtonsoft.NET will use a non-`public` default constructor before falling back to a
parameterized constructor.

Non-`public` support is not provided by `JsonSerializer` by default, so configuring selection precedence involving non-`public` constructors is not applicable.


### `Utf8Json`

`Utf8Json` chooses the constructor with the most matched arguments by name (case insensitive). This best-fit matching approach can be considered by `JsonSerializer` in the future.

The constructor to use can also be specified with a `[SerializationConstructor]` attribute.

`Utf8Json` does not support non-`public` constructors, even with the attribute.

### `Jil` (.NET)

`Jil` supports deserialization exclusively by using a parameterless constructor (may be non-`public`), and doesn't provide options to configure the behavior.

### `Jackson` (Java)

`Jackson` provides an annotation type called
[`JsonCreator`](https://fasterxml.github.io/jackson-annotations/javadoc/2.7/com/fasterxml/jackson/annotation/JsonCreator.html)
which is very similar in functionality to the `JsonConstructor` attributes in `Newtonsoft.Json`
and proposed in this spec.

```Java
@JsonCreator
public BeanWithCreator(
    @JsonProperty("id") int id,
    @JsonProperty("theName") String name) {
    this.id = id;
    this.name = name;
}
```

As shown, a `@JsonProperty` annotation can be placed on a parameter to indicate the JSON name. Adding
`JsonParameterName` attribute to be placed on constructor parameters was considered, but `Newtonsoft.Json` does not have an equivalent for this. There's probably not a big customer need for this behavior.

In addition to constructors, the `JsonCreator` can be applied to factory creator methods. There
hasn't been any demand for this from the .NET community. Support for object deserialization with factory
creation methods can be considered in the future.

## Feature behavior

### Attribute presence

#### Without `[JsonConstructor]`

##### A public parameterless constructor will always be used if present

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point() {}

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The public parameterless constructor is used:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 0
Console.WriteLine(point.Y); // 0
```

##### `struct`s will always be deserialized using the default constructor if `[JsonConstructor]` is not used

Given `Point`,

```C#
public struct Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The default constructor is used:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 0
Console.WriteLine(point.Y); // 0
```

##### A single public parameterized constructor will always be used if there's no public parameterless constructor

Given `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

The singular parameterized constructor is used:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

This rule does not apply to `struct`s as there's always a public parameterless constructor.

##### `NotSupportedException` is thrown when there are multiple parameterized ctors, but no public parameterless ctor

Given another definition for `Point`,

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    public Point(int x, int y) => (X, Y) = (x, y);

    public Point(int x, int y, int z = 3) => (X, Y, Z) = (x, y, z);
}
```

A `NotSupportedException` is thrown because it is not clear which constructor to use. This may be resolved by using the `[JsonConstructor]`, or by adding a
public parameterless constructor.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2,""Z"":3}"); // Throws `NotSupportedException`
```

This rule does not apply to `struct`s as there's always a public parameterless constructor.

#### Using [JsonConstructor]

##### `[JsonConstructor]` can only be used on one constructor

Given `Point`,

```C#
public class Point
{
  public int X { get; }

  public int Y { get; }

  public int Z { get; }

  [JsonConstructor]
  public Point() {}

  [JsonConstructor]
  public Point(int x, int y) => (X, Y) = (x, y);
}
```

An `InvalidOperationException` is thrown:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2,""Z"":3}"); // Throws `InvalidOperationException`
```

### Parameter name matching

#### Constructor parameters that bind to object properties will use their Json property names for deserialization.

Each property's CLR name will be converted with the `camelCase` naming policy to find its matching constructor parameter. The constructor parameter will use the cached JSON property name to find a match on deserialization, and the object property will be ignored on deserialization.

This matching mechanism is optimized for object definitions that follow the
C# guidelines for naming properties and method parameters.

This proposal does not include an extension point for users to specify a policy to determine the match, but it can be considered in the future.

Consider `Point`:

```C#
public class Point
{
  public int X { get; }

  public int Y { get; }

  public Point(int x, int y) => (X, Y) = (x, y);
}
```

The `int X` property matches with the `int x` parameter, and the `int Y` property matches with the `int y` property.
With default serializer options, the properties would normally match with their exact pascal case representations. The
constructor arguments will be configured to bind with the JSON instead:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");

Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

This means that the JSON property name(s) specified for each property will be properly applied to each
matching parameter constructor. ASP.NET default settings of `camelCase` casing (and case-insensitivity) will work fine without needing extra configuration.

The benefit of this approach is that a `JsonPropertyName` attribute placed on a property would be honored by its matching constructor parameter on deserialziation,
enabling roundtripping scenarios:

```C#
public class Point
{
  [JsonPropertyName("XValue")]
  public int X { get; }

  [JsonPropertyName("YValue")]
  public int Y { get; }

  public Point(int x, int y) => (X, Y) = (x, y);
}
```

```C#
Point point = new Point(1,2);

string json = JsonSerializer.Serialize(point);
Console.WriteLine(json); // {"XValue":1,"YValue":2}

point = JsonSerializer.Deserialize<Point>(json);
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

**If a constructor parameter does not match with a property, `InvalidOperationException` will be thrown if deserialization is attempted.**

**Parameter naming matching is case sensitive by default**. This can be toggled by users with the `options.PropertyNameCaseInsensitive` option.

**Constructor argument deserialization will honor the `[JsonPropertyName]`, `[JsonIgnore]`, and `[JsonConverter]` attributes placed on the matching object property.**

#### If no JSON maps to a constructor parameter, then default values are used.

This is consistent with `Newtonsoft.Json`. If no JSON maps to a constructor parameter, the following fallbacks are used in order:

- default value on constructor parameter
- CLR `default` value for the parameter type

Given `Person`,

```C#
public struct Person
{

    public string Name { get; }

    public int Age { get; }

    public Point Point { get; }

    public Person(string name, int age, Point point = new Point(1, 2))
    {
        Name = name;
        Age = age;
        Point = point;
    }
}
```

When there are no matches for a constructor parameter, a default value is used:

```C#
Person person = JsonSerializer.Deserialize<Person>("{}");
Console.WriteLine(person.Name); // null
Console.WriteLine(person.Age); // 0
Console.WriteLine(person.Point.X); 1
Console.WriteLine(person.Point.Y); 2
```

#### Members are never set with JSON properties that matched with constructor parameters

Doing this can override modifications done in constructor. `Newtonsoft.Json` has the same behavior.

Given `Point`,

```C#
public struct Point
{
    public int X { get; set; }

    public int Y { get; set; }

    [JsonConstructor]
    public Point(int x, int y)
    {
        X = 40;
        Y = 60;
    }
}
```

We can expect the following behavior:

```C#
Point obj = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
```

This behavior also applies to property name matches (from JSON properties to object properties) due
to naming policy.

#### JSON properties that don't map to constructor parameters or object properties go to extension data, if present

This is in keeping with the established serializer handling of extension data.

#### Serializer uses "last one wins" semantics for constructor parameter names

This is consistent with how object properties are deserialized.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2,""X"":4}");
Assert.Equal(4, point.X); // Note, the value isn't 1.
Assert.Equal(2, point.Y);
```

The serializer will not store any extra JSON values that map to constructor arguments in extension data.

Given `Person:

```C#
public class Person
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid Id { get; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> ExtensionData { get; set; }

    public Person(Guid id) => Id = id;
}
```

We can expect the following behavior with `JsonSerializer`:

```C#
string json = @"{
    ""FirstName"":""Jet"",
    ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
    ""EmailAddress"":""jetdoe@outlook.com"",
    ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
    ""LastName"":""Doe"",
    ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
    }";

Person person = JsonSerializer.Deserialize<Person>(json);
Console.WriteLine(person.FirstName); // Jet
Console.WriteLine(person.LastName); // Doe
Console.WriteLine(person.Id); // 63cf821d-fd47-4782-8345-576d9228a534 (note that the first matching JSON property "won")
Console.WriteLine(person.ExtensionData["EmailAddress"].GetString()); // jetdoe@outlook.com
Console.WriteLine(person.ExtensionData.ContainsKey("Id")); // False
```

This is consistent with `Newtonsoft.Json` behavior.

#### `options.IgnoreNullValues` is honored when deserializing constructor arguments

This is helpful to avoid a `JsonException` when null is applied to value types.

Given `PointWrapper` and `Point_3D`:

```C#
public class PointWrapper
{
	public Point_3D Point { get; }

	public PointWrapper(Point_3D point) {}
}

public struct Point_3D
{
	public int X { get; }

	public int Y { get; }

	public int Z { get; }
}
```

We can ignore `null` tokens and not pass them as arguments to a non-nullable parameter. A default value will be passed instead.
 The behavior if the serializer does not honor the `IgnoreNullValue` option would be to preemptively throw a `JsonException`, rather than leaking an `InvalidCastException` thrown by the CLR.

```C#
JsonSerializerOptions options = new JsonSerializerOptions
{
    IgnoreNullValues = true
};
PointWrapper obj = JsonSerializer.Deserialize<PointWrapper>(@"{""Point"":null}"); // obj.Point is `default`
```

In the same scenario, `Newtonsoft.Json` fails with error:

```C#
JsonSerializerSettings settings = new JsonSerializerSettings { NullValueHandling = true };
PointWrapper obj = JsonConvert.DeserializeObject<PointWrapper>(@"{""Point"":null}");

// Unhandled exception. Newtonsoft.Json.JsonSerializationException: Error converting value {null} to type 'Program+Point_3D'. Path 'Point', line 1, position 21.
```

#### Specified constructors cannot have more than 64 parameters

This is an implementation detail. If deserialization is attempted with a constructor that has more than
64 parameters, a `NotSupportedException` will be thrown.

We expect most users to have significantly less than 64 parameters, but we can respond to user feedback.

#### [`ReferenceHandling` semantics](https://github.com/dotnet/runtime/blob/13c1e65a9f7aab201fe77e3daba11946aeb7cbaa/src/libraries/System.Text.Json/docs/ReferenceHandling_spec.md) will not be applied to objects deserialized with parameterized constructors

`NotSupportedException` will be thrown if any properties named "$id", "$ref", or "$values" are found in the payload, and `options.ReferenceHandling` is set to
`ReferenceHandling.Preserve`. If the feature is off, these properties will be treated like any other (likely end up in extension data property).
This behavior prevents us from breaking people if we implement this feature in the future.

`Newtonsoft.Json` does not not honor reference metadata within objects deserialized with parameterized constructors. They are ignored and treated like any other property.

Consider an `Employee` class:

```C#
public class Employee
{
    public string FullName { get; set; }

    public Employee Manager { get; internal set; }

    public Employee(Employee manager = null)
    {
        Manager = manager;
    }
}
```

Serializing an `Employee` instance with `ReferenceHandling.Preserve` semantics may look like this:

```C#
Employee employee = new Employee();
employee.FullName = "Jet Doe";
employee.Manager = employee;

JsonSerializerOptions options = new JsonSerializerOptions
{
    ReferenceHandling = ReferenceHandling.Preserve
};

string json = JsonSerializer.Serialize(employee, options);
Console.WriteLine(json); // {"$id":"1","Manager":{"$ref":"1"},"FullName":"Jet Doe"}
```

It might be non-trivial work to resolve such scenarios on deserialization and handle error cases.

### Deserialization with parameterized constructor does not apply to enumerables

The semantics described in this document does not apply to any type that implements `IEnumerable`.
This may change in the future, particularly if an
[option to treat enumerables as objects](https://github.com/dotnet/runtime/issues/1808) with members is provided.
