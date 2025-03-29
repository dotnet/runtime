## About

Provides the `CustomReflectionContext` class to enable adding or removing custom attributes from reflection objects, or adding dummy properties to those objects, without re-implementing the complete reflection model.

## Key Features

<!-- The key features of this package -->

* Create a custom reflection context to control how types and members are presented during reflection.
* Easily extend or customize the reflection model to fit specialized application needs.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Defining a custom `Attribute` and implement a `CustomReflectionContext` to add the attribute to specic methods.

```csharp
using System.Reflection;
using System.Reflection.Context;

[AttributeUsage(AttributeTargets.Method)]
class CustomAttribute : Attribute
{
}

class CustomContext : CustomReflectionContext
{
    // Called whenever the reflection context checks for custom attributes.
    protected override IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
    {
        // Add custom attribute to "ToString" members
        if (member.Name == "ToString")
        {
            yield return new CustomAttribute();
        }

        // Keep existing attributes as well
        foreach (var attr in declaredAttributes)
        {
            yield return attr;
        }
    }
}
```

Inspecting the `string` type with both the default and custom reflection context.

```csharp
Type type = typeof(string);

// Representation of the type in the default reflection context
TypeInfo typeInfo = type.GetTypeInfo();

Console.WriteLine("\"To*\" members and their attributes in the default reflection context");
foreach (MemberInfo memberInfo in typeInfo.DeclaredMembers)
{
    if (memberInfo.Name.StartsWith("To"))
    {
        Console.WriteLine(memberInfo.Name);
        foreach (Attribute attribute in memberInfo.GetCustomAttributes())
        {
            Console.WriteLine($" - {attribute.GetType()}");
        }
    }
}
Console.WriteLine();

// Output:
// "To*" members and their attributes in the default reflection context
// ToCharArray
// ToCharArray
// ToString
// ToString
// ToLower
// ToLower
// ToLowerInvariant
// ToUpper
// ToUpper
// ToUpperInvariant

// Representation of the type in the customized reflection context
CustomContext customContext = new();
TypeInfo customTypeInfo = customContext.MapType(typeInfo);

Console.WriteLine("\"To*\" members and their attributes in the customized reflection context");
foreach (MemberInfo memberInfo in customTypeInfo.DeclaredMembers)
{
    if (memberInfo.Name.StartsWith("To"))
    {
        Console.WriteLine(memberInfo.Name);
        foreach (Attribute attribute in memberInfo.GetCustomAttributes())
        {
            Console.WriteLine($" - {attribute.GetType()}");
        }
    }
}

// Output:
// "To*" members and their attributes in the customized reflection context
// ToCharArray
// ToCharArray
// ToString
//  - CustomAttribute
// ToString
//  - CustomAttribute
// ToLower
// ToLower
// ToLowerInvariant
// ToUpper
// ToUpper
// ToUpperInvariant
```
## Main Types

<!-- The main types provided in this library -->

The main type provided by this library is:

* `System.Reflection.Context.CustomReflectionContext`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.reflection.context)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.Reflection.Context is released as open source under the [MIT license](https://licenses.nuget.org/MIT).
Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
