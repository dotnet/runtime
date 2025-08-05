### Background and motivation

There were multiple proposals and ideas in the past asking for a no-indirection primitive for inline data. [Example1(inline strings)](https://github.com/dotnet/csharplang/issues/2099),  [Example2(inline arrays)](https://github.com/dotnet/runtime/issues/12320), [Example3(generalized fixed-buffers)](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-11.0/low-level-struct-improvements.md#safe-fixed-size-buffers)
Our preexisting offering in this area – [unsafe fixed-sized buffers](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#fixed-size-buffers) has multiple constraints, in particular it works only with blittable value types and provides no overrun/type safety, which considerably limits its use.

*The InlineArrayAttribute is a building block to allow efficient, type-safe, overrun-safe indexable/sliceable inline data.*

An example of one such feature is [`Inline Arrays` in C# 12.0](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-12.0/inline-arrays.md).

### API

```C#
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InlineArrayAttribute : Attribute
    {
        public InlineArrayAttribute (int length)
        {
            Length = length;
        }

        public int Length { get; }
    }
}
```

When `InlineArray` attribute is applied to a struct with one instance field, it is interpreted by the runtime as a directive to replicate the layout of the struct `Length` times. That includes replicating GC tracking information if the struct happens to contain managed pointers.

Unlike "filed0; field1; field2;..." approach, the resulting layout would be guaranteed to have the same order and packing details as elements of an array with element `[0]` matching the location of the single specified field. That will allow the whole aggregate to be safely indexable/sliceable.

`Length` must be greater than 0.

struct must not have explicit layout.

In cases when the attribute cannot have effect, it is an error case handled in the same way as the given platform handles cases when a type layout cannot be constructed.
Generally, it would be a `TypeLoadException` thrown at the time of layout construction.

### API Usage

```C#
// runtime replicates the layout of the struct 42 times 
[InlineArray(length: 42)] 
struct MyArray<T> 
{ 
    private T _element0; 
    public Span<T> SliceExample() 
    { 
        return MemoryMarshal.CreateSpan(ref _element0, 42); 
    } 
} 
```
### Memory layout of an inline array instance.

The memory layout of a struct instance decorated with `InlineArray` attribute closely matches the layout of the element sequence of an array `T[]` with length == `Length`.
In particular (using the `MyArray<T>` example defined above):
* In unboxed form there is no object header or any other data before the first element.

Example: assuming the instance is not GC-movable, the following holds: `(byte*)&inst == (byte*)&inst._element0`

* There is no additional padding between elements.

Example: assuming the instance is not GC-movable and `Length > 1`, the following will yield a pointer to the second element: `(byte*)&inst._element0 + sizeof(T)`

* The size of the entire instance is the size of its element type multiplied by the `Length`

Example: the following holds: `sizeof(MyArray<T>) == Length * sizeof(T)`

* Just like with any other struct, the boxed form will contain the regular object header followed by an entire unboxed instance.

Example: boxing/unboxing will result in exact copy on an entire instance: `object o = inst; MyArray<T> inst1copy = (MyArray<T>)o`

Type T can be a reference type and can contain managed references. The runtime will ensure that objects reachable through elements of an inline array instance can be accessed in a type-safe manner.

### Size limits for inline array instances.

The size limits for inline array instances will match the size limits of structs on a given runtime implementation.
Generally this is a very large size imposed by the type system implementation and is rarely reachable in actual applications due to other limitations such as max stack size, max size of an object, and similar.

### Default behavior of of `Equals()` and `GetHashCode()`

In .NET 9 and later, the default implementations of `Equals()` and `GetHashCode()` for types marked with `InlineArray` attribute throw `NotSupportedException`.
Prior to .NET 9 the default behavior of these members was undefined.

User is expected to override both `Equals()` and `GetHashCode` if they will be used.

For more details see: https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.inlinearrayattribute

### Special note on scenario when the element is readonly.

There is a scenario where the element field in a struct decorated with `InlineArrayAttribute` is `readonly`.

The `readonly` part in such scenario has no special semantics and as such the scenario is unsupported and is not recommended.

### FAQ:

**Why do we put the attribute on the struct and not on the field?**

Allowing the attribute on individual fields introduces numerous additional scenarios and combinations that make the feature considerably more complex.
- we would need to rationalize or somehow forbid the attribute usage on static, threadstatic, RVA fields
- allowing replicated storage in classes would need to account for base classes that also may have instance fields, and perhaps with replicated storage as well.
- allowing multiple replicated fields in the same struct could make the overall layout computation a fairly complex routine when field ordering, packing, ref-ness and alignment of the fields are considered. (i.e alignment would need to consider pre-replicated sizes, but packing post-replicated)

All the above issues can be solved, but at a cost to the implementation complexity while most additional scenarios appear to be less common and easily solvable by providing a wrapper struct.
