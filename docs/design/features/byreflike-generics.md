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
- `constrained.callvirt` &ndash; This IL sequence must resolve to a method implemented on `object`, or a default interface method.

If any of the above instructions are attempted to be used with a ByRefLike type, the runtime will throw an `InvalidProgramException`.

The following instructions are already set up to support this feature since their behavior will fail as currently defined due to the inability to box a ByRefLike type.

- `throw` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `unbox` / `unbox.any` &ndash; Requires an object reference to be on stack, which can never be a ByRefLike type.
- `isinst` &ndash; Will always place `null` on stack.
- `castclass` &ndash; Will always throw `System.InvalidCastException`.

## Proposal

Support for the following would be indicated by the existing `RuntimeFeature.ByRefFields` mechanism.

A new `GenericParameterAttributes` value will be defined which also represents metadata defined in the `CorGenericParamAttr` enumeration. Space is provided between the existing constraints group to permit constraint growth.

```diff
namespace System.Reflection
{
    [Flags]
    public enum GenericParameterAttributes
    {
+        SupportsByRefLike = 0x0100
    }
}
```

```diff
typedef enum CorGenericParamAttr
{
+   gpSupportsByRefLike = 0x0100 // type argument can be ByRefLike
} CorGenericParamAttr;
```

The expansion of metadata will impact at least the following:

- ILDasm/ILAsm &ndash; https://github.com/dotnet/runtime
- Cecil &ndash; https://github.com/jbevain/cecil
- IL Trimmer &ndash; https://github.com/dotnet/linker
- C++/CLI &ndash; The MSVC team

An API that is a JIT-time intrinsic will be needed to determine if a parameter is ByRefLike. This API would represent a check to occur at JIT time code-gen to avoid taking paths that would be invalid for some values of `T`. The existing `Type.IsByRefLike` property will be made an intrinsic (e.g., `typeof(T).IsByRefLike`).

For dispatch to object implemented methods and to default interface methods, the behavior shall be that an `InvalidProgramException` should be thrown. The JIT would insert the following IL at code-gen time.

```
newobj instance void System.InvalidProgramException::.ctor()
throw
```

When boxing due to a constrained call that cannot be made, instead of allocating a normal boxed object, an object of `InvalidBoxedObject` type will be created. It will have implementations of the various overridable object methods which throw `InvalidProgramException`, and interface dispatch shall have a special case for attempting to invoke an interface method on such an object, that will also throw an `InvalidProgramException`.

The `Reflection.Emit` API will need to be updated to respect the behavior of this flag. How it will handle support is an open question.

## Open questions

- This scenario is the inverse of [Generic constraints](https://docs.microsoft.com/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters) as it is an "allow". What does this look like as a general case as to potentially support pointers in the future? See https://github.com/dotnet/runtime/issues/13627.

- Should `Reflection` support this scenario initially? This includes calling and using API calls such as `MakeGenericType` / `MakeGenericMethod`.
