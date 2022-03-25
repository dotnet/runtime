# Generics parameters of ByRefLike types

Using ByRefLike types in Generic parameters is possible by building upon support added for `ref` fields. Scenarios that would benefit most from this are those involving `Span<T>`. For example, consider the following examples:

- `Span<TypedReference>` &ndash; Represents the general case where a ByRefLike type is used as a Generic parameter. This specific case would be desirable for a more efficient Reflection API.
- `Span<Span<char>>` &ndash; Nested `Span<T>` types would be of benefit in the parsing result of strings.

## Runtime impact

Supporting ByRefLike type as Generic parameters will impact the following IL instructions:

- `box` &ndash; Types with ByRefLike parameters used in fields cannot be boxed.
- `throw` &ndash; Requires an object reference on the stack so not directly impacted since boxing is not permitted.
- `stsfld` / `ldsfld` &ndash; Type fields of a ByRefLike parameter cannot be marked `static`.
- `newarr` / `stelem` / `ldelem` / `ldelema` &ndash; Arrays are not able to contain ByRefLike types.
    - `newobj` &ndash; For multi-dimensional array construction.
- `constrained.callvirt` &ndash; This IL sequence must resolve to a method implemented on `object`, or a default interface method.
- Use of any Generic which doesn’t expect a ByRefLike parameter as a Generic parameter.

## Proposal

A new Attribute API will be defined to indicate which Generic parameters are permissible to be of any type&mdash;including ByRefLike types. The Attribute could be used by the compiler to implement a generic non-constraint.

```csharp
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class GenericParameterSupportsAnyTypeAttribute : Attribute
    {
        /// </summary>
        /// Constructs an instance with a Generic parameter index of 0.
        /// </summary>
        public GenericParameterSupportsAnyTypeAttribute()
            => ParameterIndex = 0;

        /// </summary>
        /// Constructs an instance with a Generic parameter index.
        /// </summary>
        /// <param name="parameterIndex">Non-negative index of Generic parameter to apply to.</param>
        public GenericParameterSupportsAnyTypeAttribute(int parameterIndex)
            => ParameterIndex = parameterIndex;

        /// <summary>
        /// Generic Parameter Index.
        /// </summary>
        public int ParameterIndex { get; set; }
    }
}
```

A new API will be implemented as a JIT intrinsic for determining if a parameter is ByRefLike. This API would represent a check to occur at JIT time code-gen to avoid taking paths that would be invalid for some values of `T`.

```diff
namespace System
{
    public abstract partial class Type
    {
+        [Intrinsic]
+        public static bool IsByRefLike<T>();
    }
}
```

For dispatch to object implemented methods and to default interface methods, the behavior shall be that an `InvalidProgramException` should be thrown. The JIT would insert the following IL at code-gen time.

```
newobj instance void System.InvalidProgramException::.ctor()
throw
```

When boxing due to a constrained call that cannot be made, instead of allocating a normal boxed object, an object of `InvalidBoxedObject` type will be created. It will have implementations of the various overridable object methods which throw `InvalidProgramException`, and interface dispatch shall have a special case for attempting to invoke an interface method on such an object, that will also throw an `InvalidProgramException`. 

## Open questions

- This scenario is the inverse of [Generic constraints](https://docs.microsoft.com/dotnet/csharp/programming-guide/generics/constraints-on-type-parameters) as it is an "allow". What does this look like as a general case as to potentially support pointers in the future? See https://github.com/dotnet/runtime/issues/13627.
