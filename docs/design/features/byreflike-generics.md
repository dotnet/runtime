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

If any of the above instructions are attempted to be used with a ByRefLike type, the runtime will throw an `InvalidProgramException`.

The following instructions are already set up to support this feature since their behavior will fail as currently defined due to the inability to box a ByRefLike type.

- `throw` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `unbox` / `unbox.any` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `isinst` &ndash; Will always place `null` on stack.
- `castclass` &ndash; Will always throw `System.InvalidCastException`.

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

A new `GenericParameterAttributes` value will be defined which also represents metadata defined in the `CorGenericParamAttr` enumeration. Space is provided between the existing constraints group to permit constraint growth while keeping the bit-mask contiguous.

```diff
namespace System.Reflection
{
    [Flags]
    public enum GenericParameterAttributes
    {
+        AcceptByRefLike = 0x0100
    }
}
```

```diff
typedef enum CorGenericParamAttr
{
+   gpAcceptByRefLike = 0x0100 // type argument can be ByRefLike
} CorGenericParamAttr;
```

The expansion of metadata will impact at least the following:

- ILDasm/ILAsm &ndash; https://github.com/dotnet/runtime
- Cecil &ndash; https://github.com/jbevain/cecil
- IL Trimmer &ndash; https://github.com/dotnet/linker
- F# &ndash; https://github.com/fsharp/fsharp
- C++/CLI &ndash; The MSVC team

## Semantic Proposal

An API that is a JIT-time intrinsic will be needed to determine if a parameter is ByRefLike. This API would represent a check to occur at JIT time code-gen to avoid taking paths that would be invalid for some values of `T`. The existing `Type.IsByRefLike` property will be made an intrinsic (e.g., `typeof(T).IsByRefLike`).

For dispatch to object implemented methods and to default interface methods, the behavior shall be that an `InvalidProgramException` should be thrown. The JIT would insert the following IL at code-gen time.

```
newobj instance void System.InvalidProgramException::.ctor()
throw
```

The `Reflection.Emit` API will need to be updated to respect the behavior of this flag. The current Reflection API requires boxing of all types which makes usage of ByRefLike types impossible. Until the Reflection API can fully support ByRefLike types, this feature must be blocked when using Reflection. For example, API calls such as `MakeGenericType` / `MakeGenericMethod` are invalid.
