# Generics parameters of ByRefLike types

Using ByRefLike types in Generic parameters is possible by building upon support added for `ref` fields. Scenarios that would benefit most from this are those involving `Span<T>`. For example, consider the following examples:

- `Span<TypedReference>` &ndash; Represents the general case where a ByRefLike type is used as a Generic parameter. This specific case would be desirable for a more efficient Reflection API.
- `Span<Span<char>>` &ndash; Nested `Span<T>` types would be of benefit in the parsing result of strings.

## Runtime impact

Supporting ByRefLike types as Generic parameters will impact the following IL instructions.

Providing a ByRefLike type to the `box` instruction remains invalid and `InvalidProgramException` will be thrown when detected.

The `constrained.callvirt` sequence is valid if a ByRefLike type is provided. A `NotSupportedException` will be thrown at the callsite, if the target resolves to a method implemented on `object` or a default interface method.

Throws `TypeLoadException` when passed a ByRefLike type.
- `stsfld` / `ldsfld` &ndash; Type fields of a ByRefLike parameter cannot be marked `static`.
- `newarr` / `stelem` / `ldelem` / `ldelema` &ndash; Arrays are not able to contain ByRefLike types.
    - `newobj` &ndash; For multi-dimensional array construction.

The following instructions are already set up to support this feature since their behavior will fail as currently defined due to the inability to box a ByRefLike type.

- `throw`
- `unbox` / `unbox.any`
- `isinst`
- `castclass`

**NOTE** There are sequences involving some of the above instructions that may remain valid regardless of a `T` being ByRefLike&mdash;see ["Options for invalid IL" section](#invalid_il_options) below for details.

The expansion of ByRefLike types as Generic parameters does not relax restrictions on where ByRefLike types can be used. When `T` is ByRefLike, the use of `T` as a field will require the enclosing type to be ByRefLike.

## API Proposal

A new `GenericParameterAttributes` value will be defined which also represents metadata defined in the `CorGenericParamAttr` enumeration.

```diff
namespace System.Reflection
{
    [Flags]
    public enum GenericParameterAttributes
    {
+        AcceptByRefLike = 0x0020
    }
}
```

```diff
typedef enum CorGenericParamAttr
{
+   gpAcceptByRefLike = 0x0020 // type argument can be ByRefLike
} CorGenericParamAttr;
```

The expansion of metadata will impact at least the following:

- ILDasm/ILAsm/`System.Reflection.Metadata`/`System.Reflection.Emit` &ndash; https://github.com/dotnet/runtime
- Cecil &ndash; https://github.com/jbevain/cecil
- IL Trimmer &ndash; https://github.com/dotnet/runtime/tree/main/src/tools/illink
- F# &ndash; https://github.com/fsharp/fsharp
- C++/CLI &ndash; The MSVC team

### Troublesome API mitigation

If existing types are expected to add ByRefLike support, it is possible they contain previously valid APIs that will become invalid when ByRefLike types are permitted. A potential mitigation for this would be create an attribute to indicate to compilers that specific APIs are validated at run-time not compile-time. What follows is a potential solution.

The compiler will be imbued with knowledge of an API that tells it where ByRefLike types will be permissable and where the failure will be handled by the runtime. The compiler will only respect the attribute that is defined in the same assembly containing `System.Object`.

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates to the compiler the ByRefLike constraint check should be suppressed.
    /// </summary>
    /// <remarks>
    /// The checking will be suppressed for both the signature and method body. These
    /// checks are deferred and will be enforced at run-time.
    /// </remarks>
    /// <seealso href="https://github.com/dotnet/runtime/issues/99788">Design discussion</seealso>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    internal sealed class SuppressByRefLikeConstraintChecksAttribute : Attribute
    {
        /// <summary>Initializes the attribute.</summary>
        public SuppressByRefLikeConstraintChecksAttribute() { }
    }
}
```

Current examples of APIs that would need the attribute applied:

- [`Span<T>`](https://learn.microsoft.com/dotnet/api/system.span-1)
    - `public Span(T[]? array);`
    - `public Span(T[]? array, int start, int length);`
    - `public T[] ToArray();`
    - `public static implicit operator Span<T>(ArraySegment<T> segment);`
    - `public static implicit operator Span<T>(T[]? array);`
- [`ReadOnlySpan<T>`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)
    - `public ReadOnlySpan(T[]? array);`
    - `public ReadOnlySpan(T[]? array, int start, int length);`
    - `public T[] ToArray();`
    - `public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment);`
    - `public static implicit operator ReadOnlySpan<T>(T[]? array);`

## Semantic Proposal

An API that is a JIT-time intrinsic will be needed to determine if a parameter is ByRefLike. This API would represent a check to occur at JIT time to avoid taking paths that would be invalid for some values of `T`. The existing `Type.IsByRefLike` property will be made an intrinsic (e.g., `typeof(T).IsByRefLike`).

For dispatch to object implemented methods and to default interface methods, the behavior shall be that an `InvalidProgramException` should be thrown. The JIT will insert the following IL at code-gen time.

```
newobj instance void System.InvalidProgramException::.ctor()
throw
```

Adding `gpAcceptByRefLike` to the metadata of a Generic parameter will be considered a non-breaking binary change.

Enumerating of constructors/methods on `Span<T>` and `ReadOnlySpan<T>` may throw `TypeLoadException` if `T` is a ByRefLike type. See "Troublesome API mitigation" above for the list of APIs that cause this condition.

## <a name="invalid_il_options"></a> Options for invalid IL

There are two potential options below for how to address this issue.

The first indented IL sequences below represents the `is-type` sequence. Combining the first with the second indented section represents the "type pattern matching" scenario in C#. The below sequence performs a type check and then, if successful, consumes the unboxed instance.

```IL
// Type check
ldarg.0
    box <Source>
    isinst <Target>
    brfalse.s NOT_INST

// Unbox and store unboxed instance
ldarg.0
    box <Source>
    isinst <Target>
    unbox.any <Target>
stloc.X

NOT_INST:
ret
```

With the above IL composition implemented, the following C# describes the following "type pattern matching" scenarios and what one might expect given current C# semantics.

```csharp
struct S {}
struct S<T> {}
ref struct RS {}
ref struct RS<T> {}
interface I {}
class C {}
class C<T> {}

// Not currently valid C#
void M<T, U>(T t) where T: allows ref struct
{
    // Valid
    if (t is int i)

    if (t is S s)
    if (t is S<char> sc)
    if (t is S<U> su)

    if (t is RS rs)
    if (t is RS<char> rsc)
    if (t is RS<U> rsu)

    if (t is string str)
    if (t is C c)
    if (t is C<I> ci)
    if (t is C<U> cu)

    // Can be made to work in IL.
    if (t is I itf) // A new local "I" would not be used for ByRefLike scenarios.
                    // The local would be the ByRefLike type, not "I".

    // Invalid
    if (t is object o)  // ByRefLike types evaluate "true" for object.
    if (t is U u)
}
```

### Option 1) Compiler helpers

The following two helper functions could be introduced and would replace currently invalid `is-type` IL sequences when ByRefLike types are involved. Their behavior would broadly be defined to operate as if the ByRefLike aspect of either the `TFrom` and `TTo` is not present. An alternative approach would be consult with the Roslyn team and define the semantics of these functions to adhere to C# language rules.

```csharp
namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        // Replacement for the [box; isinst; brfalse/true] sequence.
        public static bool IsInstanceOf<TFrom, TTo>(TFrom source)
            where TFrom: allows ref struct
            where TTo: allows ref struct;

        // Replacement for the [box; isinst; unbox.any] sequence.
        // Would throw InvalidCastException for invalid use at run-time.
        // For example:
        //  TFrom: RS, TTo: object      => always throws
        //  TFrom: RS, TTo: <interface> => always throws
        public static TTo CastTo<TFrom, TTo>(TFrom source)
            where TFrom: allows ref struct
            where TTo: allows ref struct;
    }
}
```

Example usage of the above methods.

```csharp
TTo result;
if (RuntimeHelpers.IsInstanceOf<TFrom, TTo>(source))
{
    result = RuntimeHelpers.CastTo<TFrom, TTo>(source);
}
```

### Option 2) Special IL sequences

The following are IL sequences involving the `box` instruction. They are used for common C# language constructs and would continue to be valid, even with ByRefLike types. These sequences would be **required** to be valid when the target type is ByRefLike. Each sequence would be added to the ECMA-335 addendum.

`box` ; `isinst` ; `br_true/false` &ndash; Passing a ByRefLike type as the argument to the `box` instruction is permitted to accomplish a type check, in C# `x is Y`. **Note** ByRefLike types would evaluate to `true` when compared against `System.Object`.

`box` ; `isinst` ; `unbox.any` &ndash; In order to permit "type pattern matching", in C# `x is Y y`, this sequence will permit use of a ByRefLike type on any instruction, but does not permit the use of generic parameters being exposed to `isinst` or `unbox.any`.

`box` ; `unbox.any` &ndash; Valid to use ByRefLike types.

`box` ; `br_true/false` &ndash; Valid to use ByRefLike types.

## Examples

Below are currently (.NET 9) valid and invalid examples of ByRefLike as Generic parameters.

**1) Valid**
```csharp
class A<T1> where T1: allows ref struct
{
    public void M();
}

// The derived class is okay to lack the 'allows'
// because the base permits non-ByRefLike (default)
// _and_ ByRefLike types.
class B<T2> : A<T2>
{
    public void N()
        => M(); // Any T2 satisfies the constraints from A<>
}
```

**2) Invalid**
```csharp
class A<T1>
{
    public void M();
}

// The derived class cannot push up the allows
// constraint for ByRefLike types.
class B<T2> : A<T2> where T2: allows ref struct
{
    public void N()
        => M(); // A<> may not permit a T2
}
```

**3) Valid**
```csharp
interface IA
{
    void M();
}

ref struct A : IA
{
    public void M() { }
}

class B
{
    // This call is permitted because no boxing is needed
    // to dispatch to the method - it is implemented on A.
    public static void C<T>(T t) where T: IA, allows ref struct
        => t.M();
}
```

**4) Invalid**
```csharp
interface IA
{
    public void M() { }
}

ref struct A : IA
{
    // Relies on IA::M() implementation.
}

class B
{
    // Reliance on a DIM forces the generic parameter
    // to be boxed, which is invalid for ByRefLike types.
    public static void C<T>(T t) where T: IA, allows ref struct
        => t.M();
}
```

**5) Valid**
```csharp
class A<T1> where T1: allows ref struct
{
}

class B<T2>
{
    // The type parameter is okay to lack the 'allows'
    // because the field permits non-ByRefLike (default)
    // _and_ ByRefLike types.
    A<T2> Field;
}
```

**6) Invalid**
```csharp
class A<T1>
{
}

class B<T2> where T2: allows ref struct
{
    // The type parameter can be passed to
    // the field type, but will fail if
    // T2 is a ByRefLike type.
    A<T2> Field;
}
```

**7) Invalid**
```csharp
class A
{
    virtual void M<T1>() where T1: allows ref struct;
}

class B : A
{
    // Override methods need to match be at least
    // as restrictive with respect to constraints.
    // If a user has an instance of A, they are
    // not aware they could be calling B.
    override void M<T2>();
}
```