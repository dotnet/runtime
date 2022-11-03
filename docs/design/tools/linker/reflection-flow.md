# Reflection Handling in ILLink

Unconstrained reflection in .NET presents a challenge for discovering parts of .NET apps that are (or are not) used. We had multiple attempts to solve the problem – probably the most complex one was in .NET Native. .NET Native was trying to make arbitrary reflection "just work" by using a combination of:

* cross-method dataflow analysis framework (that could e.g. model the `dynamic` keyword in C#, or reason about cascading reflection patterns like `Type.GetType("Foo").GetMethod("Bar").ReturnType.GetMethod("Baz")`)
* expressive annotation format (RD.XML) that could express things like "keep all properties and constructors on types passed as a parameter to this method, recursively". The idea was that library developers would annotate their libraries and things would "just work" for the user.

Even with the level of investment in .NET Native it wasn't possible to make arbitrary reflection "just work" – before shipping, we had to make a decision to not do any tree-shaking on user assemblies by default because the reflection patterns were often (~20% of the Store app catalog) arbitrary enough that it wasn't possible to describe them in generic ways or detect them. The .NET Native compiler did not warn the user about presence of unrecognized patterns either because we expected there would be too much noise due to the disconnect between RD.XML (that simply described what to keep) and the dataflow analysis (that focused on reflection API usage).

## Linker-friendly reflection

Note: While this document mostly talks about reflection, this includes reflection-like APIs with impact on linker's analysis such as `RuntimeHelpers.GetUninitializedObject`, `Marshal.PtrToStructure`, or `RuntimeHelpers.RunClassConstructor`.

In .NET 5, we would like to carve out a _subset_ of reflection patterns that can be made compatible in the presence of ILLink. Since trimming is optional, users do not have to adhere to this subset. They can still use ILLink if they do not adhere to this subset, but we will not provide guarantees that trimming won't change the semantics of their app. Linker will warn if a reflection pattern within the app is not compatible.

To achieve compatibility, we'll logically classify methods into following categories:

* Linker-friendly: most code will fall into this category. Trimming can be done safely based on information in the static callgraph.
* Potentially unfriendly: call to the method is unsafe if the linker cannot reason about a parameter value (e.g. `Type.GetType` with a type name string that could be unknown)
* Always unfriendly: calls to these methods are never safe in the presence of linker (e.g. `Assembly.ExportedTypes`).

Explicit non-goals of this proposal:

* Provide a mechanism to solve reflection based serializer (and similar patterns)
* Provide a mechanism to solve dependency injection patterns

It is our belief that _linker-friendly_ serialization and dependency injection would be better solved by source generators.

## Analyzing calls to potentially unfriendly methods

The most interesting category to discuss are the "potentially unfriendly" methods: reasoning about a parameter to a method call requires being able to trace the value of the parameter through the method body. ILLink is currently capable of doing this in a limited way. We'll be expanding this functionality further so that it can cover patterns like:

```csharp
Type t;
if (isNullable)
{
    t = typeof(NullableAccessor);
}
else
{
    t = typeof(ObjectAccessor);
}

// Linker should infer we need the default constructor for the 2 types above
Activator.CreateInstance(t);
// Linker should infer we need method Foo on the 2 types above
var mi = t.GetMethod("Foo");
// Linker should infer we need field Bar on the 2 types above
var fi = t.GetField("Bar");
```

In an ideal world, this would be the extent of the reflection that can be made safe by the linker. Such small subset would not be practical because reflection often happens across method bodies. Instead of introducing cross-method dataflow analysis, we'll help linker reason about reflection happening across method bodies with annotations.

## Cross-method annotations

To document reflection use across methods, we'll introduce a new attribute `DynamicallyAccessedMembersAttribute` that can be attached to method parameters, the method return parameter, fields, and properties (whose type is `System.Type`, or `System.String`). The attribute will provide additional metadata related to linker-friendliness of the parameter or field.

(When the attribute is applied to a location of type `System.String`, the assumption is that the string represents a fully qualified type name.)

```csharp
public sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberKinds)
    {
        MemberTypes = memberTypes;
    }

    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

[Flags]
public enum DynamicallyAccessedMemberTypes
{
    DefaultConstructor = 0x0001,
    PublicConstructors = 0x0002 | DefaultConstructor,
    NonPublicConstructors = 0x0004,
    // ...
}
```

When a method or field is annotated with this attribute, two things will happen:
* The method/field becomes potentially linker-friendly. Linker will ensure that the values logically written to the annotated location (i.e. passed as a parameter, returned from the method, written to the field) can be statically reasoned about, or a warning will be generated.
* The analysis of the value read from the annotated location will have richer information available and the linker can assume that linker-unfriendly operations with an otherwise unknown value could still be safe.

Example:

```csharp
class Program
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static readonly Type _otherType;

    static Program()
    {
        // _otherType is marked DynamicallyAccessedMembers - the linker
        // ensures that the value written to this can be statically reasoned about
        // and meets the MemberKinds restriction. So it will keep all methods on Foo.
        _otherType = Type.GetType("Foo, FooAssembly");
    }

    static void Main()
    {
        // SAFE: _otherType was annotated as "some type that has all methods kept"
        _otherType.GetMethod("RandomMethod");
    }
}
```

(The above pattern exists in CoreLib and is currently unfriendly.)

More details discussed in https://github.com/dotnet/runtime/issues/33861.

TODO:  Creating a delegate to a potentially linker unfriendly method could be solvable. Reflection invoking a potentially linker unfriendly method is hard. Both out of scope?
TODO: It might be possible to apply similar pattern to generic parameters. The DynamizallAccessedMembers could be added to the generic parameter declaration and linker could make sure that when it's "assigned to" the requirements are met.

## Intrinsic recognition of reflection APIs

While it would be possible to annotate reflection primitives with the proposed DynamicallyAccessedMembers attribute, linker is going to intrinsically recongnize some of the reflection primitives so that it can do a better job at preserving just the pieces that are really needed. For example:

* `Type.GetMethod`
* `Type.GetProperty`
* `Type.GetField`
* `Type.GetMember`
* `Type.GetNestedType`

are going to be special cased so that if the type and name is exactly known at trimming time, only the specific member will be preserved. If the name is not known, all matching members are going to be preserved instead. Linker may look at other parameters to these methods, such as the binding flags and parameter counts to further restrict the set of members preserved.

The special casing will also help in situations such as when the type is not statically known and we only have an annotated value - e.g. calling `GetMethod(...BindingFlags.Public)` on a `System.Type` instance annotated as `DynamicallyAccessedMemberTypes.PublicMethods` should be considered valid.

## Linker unfriendly annotations

Another annotation will be used to mark methods that are never linker friendly:

```csharp
public sealed class LinkerUnfriendlyAttribute : Attribute
{
    public LinkerUnfriendlyAttribute(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
```

All calls to methods annotated with LinkerUnfriendly will result in a warning and the linker will not analyze linker-friendliness within the method body or the parts of the static callgraph that are only reachable through this method.

TODO: Do we care about localization issues connected with the message string? Do we need an enum with possible messages ("this is never friendly", "use different overload", "use different method", etc.) This is probably the same bucket as `ObsoleteAttribute`.

## Escape hatch: Warning supression

We will provide a way to supress reflection flow related warnings within linker. This is meant to be used in cases where we know that a pattern is safe, but the linker is not smart enough to reason about it.

A good example of such pattern could be caches and maps using System.Type. If the cache only stores values that have a certain annotation, all values read from the cache should be annotated the same way, but linker won't be able to see this though.

```csharp
private Dictionary<Type, Type> _interfaceToImpl;

public void RegisterInterface(Type intface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)]Type impl)
{
    _interfaceToImpl.Add(intface, impl);
}

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2006:UnrecognizedReflectionPattern", MessageId = "CreateInstance")]
public object Activate(Type intface)
{
    // This would normally warn because value retrieved from the dictionary is not annotated, but since
    // all values placed into the dictionary were annotated, this is safe.
    return Activator.CreateInstance(_interfaceToImpl[intface]);
}

```

More details discussed in https://github.com/dotnet/runtime/issues/35339.

## Escape hatch: DynamicDependencyAttribute annotation

This is an existing custom attribute (known as `DynamicDependencyAttribute`) understood by the linker. This attribute allows the user to declare the type name, method/field name, and signature (all as a string) of a method or field that the method dynamically depends on.

When the linker sees a method/constructor/field annotated with this attribute as necessary, it also marks the referenced member as necessary. It also suppresses all analysis within the method.
See issue https://github.com/dotnet/runtime/issues/30902 for details.

## Case study: Custom attributes

Custom attributes are going to be analyzed same way as method calls – the act of applying a custom attribute is a method call to the attribute constructor. The act of setting a property is a call to the property setter.

Linker currently special cases the `TypeConverterAttribute` to make sure we always keep the default constructor of the type referenced by the attribute. With the proposed annotations, it's possible to express what's necessary without the special casing. It will also make it possible for the consuming code to safely pass the value of `TypeConverterAttribute.ConverterTypeName` to `Type.GetType`/`Activator.CreateInstance`, since the location is annotated as known to keep the type and the default constructor.

```csharp
public sealed class TypeConverterAttribute : Attribute
{
    public TypeConverterAttribute()
    {
        ConverterTypeName = string.Empty;
    }

    public TypeConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)] Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        // SAFE: assigning AssemblyQualifiedName of a type that is known to have default ctor available
        ConverterTypeName = type.AssemblyQualifiedName!;
    }

    public TypeConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)] string typeName)
    {
        if (typeName == null)
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        // SAFE: assigning a known type name string
        ConverterTypeName = typeName;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.DefaultConstructor)]
    public string ConverterTypeName { get; }
}
```
