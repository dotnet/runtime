# Generics parameters of ByRefLike types

Using ByRefLike types in Generic parameters is possible by building upon support added for `ref` fields. Scenarios that would benefit most from this are those involving `Span<T>`. For example, consider the following examples:

- `Span<TypedReference>` &ndash; Represents the general case where a ByRefLike type is used as a Generic parameter. This specific case would be desirable for a more efficient Reflection API.
- `Span<Span<char>>` &ndash; Nested `Span<T>` types would be of benefit in the parsing result of strings.

## Runtime impact

Supporting ByRefLike type as Generic parameters will impact the following IL instructions:

- `box` &ndash; Types with ByRefLike parameters used in fields cannot be boxed.
- `stsfld` / `ldsfld` &ndash; Type fields of a ByRefLike parameter cannot be marked `static`.
- `newarr` / `stelem` / `ldelem` / `ldelema` &ndash; Arrays are not able to contain ByRefLike types.
    - `newobj` &ndash; For multi-dimensional array construction.
- `constrained.callvirt` &ndash; If this IL sequence resolves to a method implemented on `object` or default interface method, an error will occur during the attempt to box the instance.

If any of the above instructions are attempted to be used with a ByRefLike type, the runtime will throw an `InvalidProgramException`. Sequences involving some of the above instructions are considered optimizations and represent cases that will remain valid regardless of a `T` being ByRefLike. See "Special IL Sequences" section below for details.

The following instructions are already set up to support this feature since their behavior will fail as currently defined due to the inability to box a ByRefLike type.

- `throw` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `unbox` / `unbox.any` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `isinst` &ndash; Will always place `null` on stack.
- `castclass` &ndash; Will always throw `InvalidCastException`.

The expansion of ByRefLike types as Generic parameters does not relax restrictions on where ByRefLike types can be used. When `T` is ByRefLike, the use of `T` as a field will require the enclosing type to be ByRefLike.

## API Proposal

Support for the following will be indicated by a new property. For .NET 7, the feature will be marked with `RequiresPreviewFeaturesAttribute` to indicate it is in [preview](https://github.com/dotnet/designs/blob/main/accepted/2021/preview-features/preview-features.md).

```diff
namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
+        /// <summary>
+        /// Represents a runtime feature where byref-like types can be used in Generic parameters.
+        /// </summary>
+        public const string GenericsAcceptByRefLike = nameof(GenericsAcceptByRefLike);
    }
}
```

The compiler will need an indication for existing troublesome APIs where ByRefLike types will be permissable, but where the failure will be handled at runtime. An attribute will be created and added to these APIs.

```csharp
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates to the compiler that constraint checks should be suppressed
    /// and will instead be enforced at run-time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property)]
    internal sealed class SuppressConstraintChecksAttribute : Attribute
    { }
}
```

Troublesome APIs:

- [`Span<T>`](https://docs.microsoft.com/dotnet/api/system.span-1)
    - `public Span(T[]? array);`
    - `public Span(T[]? array, int start, int length);`
    - `public T[] ToArray();`
    - `public static implicit operator Span<T>(ArraySegment<T> segment);`
    - `public static implicit operator Span<T>(T[]? array);`
- [`ReadOnlySpan<T>`](https://docs.microsoft.com/dotnet/api/system.readonlyspan-1)
    - `public ReadOnlySpan(T[]? array);`
    - `public ReadOnlySpan(T[]? array, int start, int length);`
    - `public T[] ToArray();`
    - `public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment);`
    - `public static implicit operator ReadOnlySpan<T>(T[]? array);`

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
- IL Trimmer &ndash; https://github.com/dotnet/linker
- F# &ndash; https://github.com/fsharp/fsharp
- C++/CLI &ndash; The MSVC team

## Semantic Proposal

An API that is a JIT-time intrinsic will be needed to determine if a parameter is ByRefLike. This API would represent a check to occur at JIT time to avoid taking paths that would be invalid for some values of `T`. The existing `Type.IsByRefLike` property will be made an intrinsic (e.g., `typeof(T).IsByRefLike`).

For dispatch to object implemented methods and to default interface methods, the behavior shall be that an `InvalidProgramException` should be thrown. The JIT will insert the following IL at code-gen time.

```
newobj instance void System.InvalidProgramException::.ctor()
throw
```

Adding `gpAcceptByRefLike` to the metadata of a Generic parameter will be considered a non-breaking binary change.

Enumerating of constructors/methods on `Span<T>` and `ReadOnlySpan<T>` may throw `TypeLoadException` if `T` is a ByRefLike type. See "Troublesome APIs" above for the list of APIs that cause this condition.

## Special IL Sequences

The following are IL sequences involving the `box` instruction. They are used for common C# language constructs and shall continue to be valid, even with ByRefLike types, in cases where the result can be computed at JIT time and elided safely. These sequences must now be elided when the target type is ByRefLike. The conditions where each sequence is elided are described below and each condition will be added to the ECMA-335 addendum.

`box` ; `unbox.any` &ndash; The box target type is equal to the unboxed target type.

`box` ; `br_true/false` &ndash; The box target type is non-`Nullable<T>`.

`box` ; `isinst` ; `unbox.any` &ndash; The box, `isint`, and unbox target types are all equal.

`box` ; `isinst` ; `br_true/false` &ndash; The box target type is equal to the unboxed target type or the box target type is `Nullable<T>` and target type equalities can be computed.
