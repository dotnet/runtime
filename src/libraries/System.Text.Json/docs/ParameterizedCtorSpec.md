# Deserializing objects using parameterized constructors with `JsonSerializer`

## Motivation

`JsonSerializer` deserializes instances of objects (`class`es and `struct`s) using public parameterless
constructors. If none is present, and deserialization is attempted, the serializer throws a `NotSupportedException`
with a message stating that objects without public parameterless constructors, including `interfaces` and `abstract`
types, are not supported for deserialization. There is no way to deserialize an instance of an object using a
parameterized constructor.

A common pattern is to make data objects immutable for various reasons. For example, given `Point`:

```C#
public struct Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

It would be beneficial if `JsonSerializer` could deserialize `Point` instances using the parameterized constructor
above, given that mapping JSON properties into readonly members is not supported.

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

This feature also enables deserialization support for `Tuple<...>` instances, as `Tuple` types do not have parameterless constructors.

The implementation of this feature closes https://github.com/dotnet/runtime/issues/29895.

## New API Proposal

```C#
namespace System.Text.Json
{
    public sealed partial class JsonSerializerOptions
    {
        /// <summary>
        /// Determines whether a parameter's name uses a case-insensitive comparison during deserialization.
        /// The default value is false.
        /// </summary>
        /// <remarks>There is a performance cost associated when the value is true.</remarks>
        public bool ParameterNameCaseInsensitive { get { throw null; } set { } }

        /// <summary>
        /// Specifies the policy used to convert a parameter's name on an object to another format, such as camel-casing.
        /// The resulting parameter name is expected to match the JSON payload during deserialization.
        /// </summary>
        public System.Text.Json.JsonNamingPolicy? ParameterNamingPolicy { get { throw null; } set { } }
    }
}

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// When placed on a constructor, indicates that the constructor should be used to create
    /// instances of the type on deserialization.
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

These options don't map well to how `JsonSerializer` should behave. Non-`public` support will not be provided by
default, so configuring selection precedence involving non-`public` constructors is not applicable.


### `Utf8Json`

`Utf8Json` chooses the constructor with the most matched arguments by name (case insensitive). Such best-fit matching for may have a non-trivial performance impact, and may not always choose the constructor that the user wishes to be used.

The constructor to use can also be specified with a `[SerializationConstructor]` attribute.

`Utf8Json` does not support non-`public` constructors, even with the attribute. 

### `Jil` (.NET)

`Jil` only supports deserialization using a parameterless constructor (may be non-`public`), and doesn't provide options to configure the behavior.

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
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
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

A `NotSupportedException` is thrown because it is not clear which constructor to use. This may be resolved by using the `[JsonConstructor]`.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2,""Z"":3}"); // Throws `NotSupportedException`
```

This rule does not apply to `struct`s as there's always a public parameterless constructor.

#### Using [JsonConstructor]

##### Non-`public` constructors will not be used unless specified with a `[JsonConstructor]` attribute

The serializer doesn't offer non-`public` support by default. An attribute should be placed on any non-`public` constructor that is desired to be used during deserialization, even if they are
parameterless. Ideally, people should not be able to deserialize instances of types they don't own using non-`public` constructors.
This restriction of an attribute based opt-in, rather than a globally applied opt-in, dissuades the violation of constructor access modifiers.

Given `Point`:

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    [JsonConstructor]
    private Point(int x, int y) => (X, Y) = (x, y);
}
```

Deserialization works fine:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

Given another definition for `Point`:

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    private Point(int x, int y) => (X, Y) = (x, y);
}
```

A `NotSupportedException` is thrown because the serializer doesn't recognize non-`public` constructors without
`[JsonConstructor]`.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}"); // Throws `NotSupportedException`
```

Given another definition for `Point`:

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public int Z { get; }

    private Point(int x, int y) => (X, Y) = (x, y);

    public Point(int x, int y, int z) => (X, Y, Y) = (x, y, z);
}
```

The public parameterized constructor is used because the serializer doesn't recognize non-`public` constructors without `[JsonConstructor]`.

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2,""Z"":3}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
Console.WriteLine(point.Z); // 3
```

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

    public Point(int x, int y) => (X, Y) = (x, y);

    [JsonConstructor]
    private Point(int x, int y, int z = 3) => (X, Y, Z) = (x, y, z);
}
```

An `InvalidOperationException` is thrown:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2,""z"":3}"); // Throws `InvalidOperationException`
```

### Parameter name matching

#### Parameter name matching is case sensitive by default

C# guidance recommends that property names should be in `PascalCase`, and parameter names in `camelCase`.

With default options, the matching behavior is to case-sensitively map constructor parameter names to JSON property names equal to the result of applying a `ToUpperInvariant` operation on the first character of the parameter name.

This table shows some examples:

| Parameter name | Expected JSON property name |
|----------------|-----------------------------|
| x              | X                           |
| X              | X                           |
| firstName      | FirstName                   |
| first_name     | First_name                  |
| 最初           | 最初                         |

This behavior is optimized for the default C# scenario, and assumes that parameters are named with camel case
notation, and map to JSON properties representing object members that were serialized with pascal case notation.

For flexibility, and accounting for scenarios not covered with the default case, this proposal includes a
new `ParameterNameCaseInsensitive` `bool` on `JsonSerializerOptions`. With this option turned on, the
serializer is more flexible with constructor parameter-JSON property matching.

This table shows some examples:

| Parameter name | Possible JSON property name matches      |
|----------------|------------------------------------------|
| x              | X, x                                     |
| X              | X, x                                     |
| firstName      | FirstName, firstName, FiRsTNaMe, etc.    |
| first_name     | First_name, first_name, FiRsT_NaMe, etc. |
| 最初           | 最初                                      |

Given class `Point`:

```C#
public class Point
{
    public int X { get; }

    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
```

Here are some possible scenarios:

```C#
Point point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""Y"":2}");
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2

point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}");
Console.WriteLine(point.X); // 0
Console.WriteLine(point.Y); // 0

JsonSerializerOptions options = new JsonSerializerOptions
{
    ParameterNameCaseInsensitive = true
};

point = JsonSerializer.Deserialize<Point>(@"{""x"":1,""y"":2}", options);
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2

point = JsonSerializer.Deserialize<Point>(@"{""X"":1,""y"":2}", options);
Console.WriteLine(point.X); // 1
Console.WriteLine(point.Y); // 2
```

These semantics permit allow the deserialization of
[`Tuple`](https://docs.microsoft.com/dotnet/api/system.tuple-1.-ctor?view=netcore-3.1#System_Tuple_1__ctor__0_)
instances where the `itemN` constructor arguments are camel case, but the `ItemN` properties are pascal case.

`Newtonsoft.Json` uses case-insensitive parameter comparison. There is a performance cost associated with this.
The proposed semantics (case sensitivity and the transformation operation described above)
provides the best serializer performance in the default case, while accounting for C# naming guidelines.

The `ParameterNameCaseInsensitive` options does not affect object property to JSON property matching with
`PropertyNameCaseInsensitive`. For instance, one could perform performant case-sensitive matching for object properties, and case-insensitive matching for constructor parameters, as needed.

##### Alternatives

1. Do case insensitive matching and don't provide an opt-in for case sensitive matching.
    
    - More flexible.
    - Unnecessary perf hit in the major scenario (`camelCase` parameters, `PascalCase` properties).

2. Do case insensitive matching and provide an opt-in for case sensitive matching.

    - More flexible.
    - Unnecessary perf hit in the major scenario if option is not specified.

3. Instead of adding the `ParameterCaseInsensitive` option, share the `PropertyCaseInsensitive` option

    - Overloads the term `Property` which is understood to mean object `Property`, not JSON property.
    - Because the default value is `false`, ioes not work for the major scenario, including for built in types like `Tuple<...>`, without setting the option.
    - Could degrade performance significantly if turned on in the common scenario, where there are more object properties than constructor parameters.

#### `ParameterNamingPolicy` can be used to facilitate matching

A major scenario enabled by this feature is the round-trippability of "immutable" types that have read-only properties.

For example, given `Point`:

```C#
public class Point
{
    public int XValue { get; }

    public int YValue { get; }

    public Point(int xValue, int yValue) => (XValue, YValue) = (xValue, yValue);
}
```

Serializing a new instance of `Point` using a `SnakeCase` policy would yield something like the following:

```C#
JsonSerializerOptions options = new JsonSerializerOptions
{
    PropertyNamingPolicy = new JsonSnakeCaseNamingPolicy()
};

string serialized = JsonSerializer.Serialize(new Point(1, 2), options);
Console.WriteLine(serialized); // {"x_value":1,"y_value":2}

Point point = JsonSerializer.Deserialize<Point>(serialized, options);
Console.WriteLine(point.XValue); // 0
Console.WriteLine(point.YValue); // 0
```

If there is no way to specify a `JsonNamingPolicy` for constructor parameters, matching on deserialization
will be impossible.

This proposal introduces a new `ParameterNamingPolicy` which can be used to facilitate such matching:

```C#
JsonNamingPolicy policy = new JsonSnakeCaseNamingPolicy(); 

JsonSerializerOptions options = new JsonSerializerOptions
{
    ParameterNamingPolicy = policy,
    PropertyNamingPolicy = policy
};

string serialized = JsonSerializer.Serialize(new Point(1, 2), options);
Console.WriteLine(serialized); // {"x_value":1,"y_value":2}

Point point = JsonSerializer.Deserialize<Point>(serialized, options);
Console.WriteLine(point.XValue); // 1
Console.WriteLine(point.YValue); // 2
```

An alternative solution is to share the `PropertyNamingPolicy` used by object properties.

Upside to using a new `JsonNamingPolicy`

1. Keep concepts consistently separate i.e. we shouldn't have different case sensitivity options,
but a shared naming policy option between constructor parameters and object properties.
2. Keep the use of the word `Property` specific to object properties.

Downside

1. Perhaps som extra friction wrt. user experience.
2. Redundancy as the two option values would almost never be different.
3. Means we would likely have to add new `FieldNameCaseInSensitivity` and `FieldNamingPolicy` options.
Maybe this is not a bad thing.

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

Another option is to throw an exception (perhaps `ArgumentException`), but this behavior would
be unecessarily prohibitive for users.

#### Members are never set with JSON properties that matched with constructor parameters

Doing this can override modifications done in constructor. `Newtonsoft.Json` has the same behavior.

Given `Point`,

```C#
public struct Point
{
    [JsonPropertyName("A")]
    public int X { get; set; }

    [JsonPropertyName("B")]
    public int Y { get; set; }

    [JsonConstructor]
    public Point_PropertiesHavePropertyNames(int a, int b)
    {
        X = 40;
        Y = 60;
    }
}
```

We can expect the following behavior:

```C#
Point obj = JsonSerializer.Deserialize<Point>(@"{""A"":1,""B"":2}");
Assert.Equal(40, obj.X); // Would be 1 if property were set directly after object construction.
Assert.Equal(60, obj.Y); // Would be 2 if property were set directly after object construction.
```

This behavior also applies to property name matches (from JSON properties to object properties) due
to naming policy.

#### JSON properties that don't map to constructor parameters or object properties, go to extension data, if present

This is in keeping with the established serializer handling of extension data.

#### Serializer uses "first one wins" semantics for constructor parameter names

For performance, the serializer constructs objects with paramaterized constructors using the first arguments
parsed from a guven JSON payload.

```C#
// u0058 is "X"
Point point = JsonSerializer.Deserialize<Point>(@"{""\u0058"":1,""\u0059"":2,""X"":4}");
Assert.Equal(1, point.X); // Note, the value isn't 4.
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
Console.WriteLine(person.Id); // 270bb22b-4816-4bd9-9acd-8ec5b1a896d3 (note that the first matching JSON property "won")
Console.WriteLine(person.ExtensionData["EmailAddress"].GetString()); // jetdoe@outlook.com
Console.WriteLine(person.ExtensionData.ContainsKey("Id")); // False
```

In contrast, `Newtonsoft.Json` has last-one-wins behavior, but will also not store extra constructor
arguments in extension. We can expect the following behavior with `Newtonsoft.Json`'s `JsonConvert`:

```C#
string json = @"{
    ""FirstName"":""Jet"",
    ""Id"":""270bb22b-4816-4bd9-9acd-8ec5b1a896d3"",
    ""EmailAddress"":""jetdoe@outlook.com"",
    ""Id"":""0b3aa420-2e98-47f7-8a49-fea233b89416"",
    ""LastName"":""Doe"",
    ""Id"":""63cf821d-fd47-4782-8345-576d9228a534""
}";

Person person = JsonConvert.DeserializeObject<Person>(json);
Console.WriteLine(person.FirstName); // Jet
Console.WriteLine(person.LastName); // Doe
Console.WriteLine(person.Id); // 63cf821d-fd47-4782-8345-576d9228a534 (note that the last matching JSON property "won")
Console.WriteLine(person.ExtensionData["EmailAddress"]); // jetdoe@outlook.com
Console.WriteLine(person.ExtensionData.ContainsKey("Id")); // False
```

Having duplicate properties in JSON seems to be an edge case scenario. User feedback can be taken into
account to provide opt-in last-one-wins extension point in the futre.

### JSON property name collisions (JSON properties mapping to multiple constructor parameters) are not allowed

With this design, the only way for there to be collisions between constructor parameters is if
a `ParameterNamingPolicy` yields the same name.

Given `Point` and a dubious naming policy:

```C#
public class ManyToOneNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        return "JsonName";
    }
}
```

Deserialization scenarios which would lead to collisions will throw an `InvalidOperationException`:

```C#
var options = new JsonSerializerOptions
{
    ParameterNamingPolicy: new ManyToOneNamingPolicy()
};

Point point = JsonSerializer.Deserialize<Point>(@"{}", options); // throws `InvalidOperationException`
```

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

This feature work will not include honoring references within objects deserialized with parameterized constructors. It may be considered in the future.

`Nestonsoft.Json` does not not honor reference metadata within objects deserialized with
parameterized constructors.

### For performance, properties that may be deserialized through constructor arguments will be written first on deserialization.

Given `ClassWithPrimitives`:

```C#
public class ClassWithPrimitives
{
    public int FirstInt { get; set; }
    public int SecondInt { get; set; }
    public string FirstString { get; set; }
    public string SecondString { get; set; }
    public DateTime FirstDateTime { get; set; }
    public DateTime SecondDateTime { get; set; }
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public int ThirdInt { get; set; }
    public int FourthInt { get; set; }
    public string ThirdString { get; set; }
    public string FourthString { get; set; }
    public DateTime ThirdDateTime { get; set; }
    public DateTime FourthDateTime { get; set; }

    public ClassWithPrimitives(int x, int y, int z) => (X, Y, Z) = (x, y, z);
}
```

On serialization, the `X`, `Y`, and `Z` properties will be written first. For example:

```C#
ClassWithPrimitives @class = new ClassWithPrimitives(1, 2, 3);

JsonSerializerOptions options = new JsonSerializerOptions
{
    WriteIndented = true
};

string json = JsonSerializer.Serialize(@class, options);
Console.WriteLine(json);
// {
// "X": 1,
// "Y": 2,
// "Z": 3,
// "FirstInt": 0,
// "SecondInt": 0,
// "FirstString": null,
// "SecondString": null,
// "FirstDateTime": "0001-01-01T00:00:00",
// "SecondDateTime": "0001-01-01T00:00:00",
// "ThirdInt": 0,
// "FourthInt": 0,
// "ThirdString": null,
// "FourthString": null,
// "ThirdDateTime": "0001-01-01T00:00:00",
// "FourthDateTime": "0001-01-01T00:00:00"
// }
```

### Deserialization with parameterized constructor does not apply to enumerables

The semantics described in this document does not apply to any type that implements `IEnumerable`. This may
change in the future, particularly if an
[option to treat enumerables as objects](https://github.com/dotnet/runtime/issues/1808) with members is provided.