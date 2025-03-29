# `UnsafeAccessorAttribute`

## Background and motivation

Number of existing .NET serializers depend on skipping member visibility checks for data serialization. Examples include System.Text.Json or EF Core. In order to skip the visibility checks, the serializers typically use dynamically emitted code (Reflection.Emit or Linq.Expressions) and classic reflection APIs as slow fallback. Neither of these two options are great for source generated serializers and native AOT compilation. This API proposal introduces a first class zero-overhead mechanism for skipping visibility checks.

## Semantics

This attribute will be applied to an `extern static` method. The implementation of the `extern static` method annotated with this attribute will be provided by the runtime based on the information in the attribute and the signature of the method that the attribute is applied to. The runtime will try to find the matching method or field and forward the call to it. If the matching method or field is not found, the body of the `extern static` method will throw `MissingFieldException` or `MissingMethodException`.

For `Method`, `StaticMethod`, `Field`, and `StaticField`, the type of the first argument of the annotated `extern static` method identifies the owning type. Only the specific type defined will be examined for inaccessible members. The type hierarchy is not walked looking for a match.

The value of the first argument is treated as `this` pointer for instance fields and methods.

The first argument must be passed as `ref` for instance fields and methods on structs.

The value of the first argument is not used by the implementation for static fields and methods.

The return value for an accessor to a field can be `ref` if setting of the field is desired.

Constructors can be accessed using Constructor or Method.

The return type is considered for the signature match. Modreqs and modopts are initially not considered for the signature match. However, if an ambiguity exists ignoring modreqs and modopts, a precise match is attempted. If an ambiguity still exists, `AmbiguousMatchException` is thrown.

By default, the attributed method's name dictates the name of the method/field. This can cause confusion in some cases since language abstractions, like C# local functions, generate mangled IL names. The solution to this is to use the `nameof` mechanism and define the `Name` property.

Scenarios involving generics may require creating new generic types to contain the `extern static` method definition. The decision was made to require all `ELEMENT_TYPE_VAR` and `ELEMENT_TYPE_MVAR` instances to match identically type and generic parameter index. This means if the target method for access uses an `ELEMENT_TYPE_VAR`, the `extern static` method must also use an `ELEMENT_TYPE_VAR`. For example:

```csharp
class C<T>
{
    T M<U>(U u) => default;
}

class Accessor<V>
{
    // Correct - V is an ELEMENT_TYPE_VAR and W is ELEMENT_TYPE_VAR,
    //           respectively the same as T and U in the definition of C<T>::M<U>().
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
    extern static void CallM<W>(C<V> c, W w);

    // Incorrect - Since Y must be an ELEMENT_TYPE_VAR, but is ELEMENT_TYPE_MVAR below.
    // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
    // extern static void CallM<Y, Z>(C<Y> c, Z z);
}
```

Methods with the `UnsafeAccessorAttribute` that access members with generic parameters are expected to have the same declared constraints with the target member. Failure to do so results in unspecified behavior. For example:

```csharp
class C<T>
{
    T M<U>(U u) where U: Base => default;
}

class Accessor<V>
{
    // Correct - Constraints match the target member.
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
    extern static void CallM<W>(C<V> c, W w) where W: Base;

    // Incorrect - Constraints do not match target member.
    // [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "M")]
    // extern static void CallM<W>(C<V> c, W w);
}
```

## API

```csharp
namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public class UnsafeAccessorAttribute : Attribute
{
    public UnsafeAccessorAttribute(UnsafeAccessorKind kind);

    public UnsafeAccessorKind Kind { get; }

    // The name defaults to the annotated method name if not specified.
    // The name must be null for constructors
    public string? Name { get; set; }
}

public enum UnsafeAccessorKind
{
    Constructor, // call instance constructor (`newobj` in IL)
    Method, // call instance method (`callvirt` in IL)
    StaticMethod, // call static method (`call` in IL)
    Field, // address of instance field (`ldflda` in IL)
    StaticField // address of static field (`ldsflda` in IL)
};
```

## API Usage

```csharp
class UserData
{
    private UserData() { }
    public string Name { get; set; }
}

[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
extern static UserData CallPrivateConstructor();

// This API allows accessing backing fields for auto-implemented properties with unspeakable names.
[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Name>k__BackingField")]
extern static ref string GetName(UserData userData);

UserData ud = CallPrivateConstructor();
GetName(ud) = "Joe";
```

Using generics

```csharp
class UserData<T>
{
    private T _field;
    private UserData(T t) { _field = t; }
    private U ConvertFieldToT<U>() => (U)_field;
}

// The Accessors class provides the generic Type parameter for the method definitions.
class Accessors<V>
{
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    extern static UserData<V> CallPrivateConstructor(V v);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ConvertFieldToT")]
    extern static U CallConvertFieldToT<U>(UserData<V> userData);
}

UserData<string> ud = Accessors<string>.CallPrivateConstructor("Joe");
Accessors<string>.CallPrivateConstructor<object>(ud);
```