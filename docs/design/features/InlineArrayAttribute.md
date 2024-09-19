### Background and motivation

There were multiple proposals and ideas in the past asking for a no-indirection primitive for inline data. [Example1(inline strings)](https://github.com/dotnet/csharplang/issues/2099),  [Example2(inline arrays)](https://github.com/dotnet/runtime/issues/12320), [Example3(generalized fixed-buffers)](https://github.com/dotnet/csharplang/blob/main/proposals/low-level-struct-improvements.md#safe-fixed-size-buffers)
Our existing offering in this area – [unsafe fixed-sized buffers](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#fixed-size-buffers) has multiple constraints, in particular it works only with blittable value types and provides no overrun/type safety, which considerably limits its use. 

The InlineArrayAttribute is a building block to allow efficient, type-safe, overrun-safe indexable/sliceable inline data.

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

Unlike "filed0; field1; field2;..." approach, the resulting layout would be guaranteed to have the same order and packing details as elements of an array with element `[0]` matching the location of the single specified field. 
That will allow the whole aggregate to be safely indexable/sliceable.

`Length` must be greater than 0.

struct must not have explicit layout.

In cases when the attribute cannot have effect, it is an error case handled in the same way as the given platform handles cases when a type layout cannot be constructed.
Generally, it would be a `TypeLoadException` thrown at the time of layout construction. 

### API Usage

```C#
// runtime replicates the layout of the struct 42 times 
[InlineArray(Length = 42)] 
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

TBD
     
### Special note on scenario when the element is readonly.

TBD

### FAQ: 

**Why do we put the attribute on the struct and not on the field?** 

Allowing the attribute on individual fields introduces numerous additional scenarios and combinations that make the feature considerably more complex.
- we would need to rationalize or somehow forbid the attribute usage on static, threadstatic, RVA fields
- allowing replicated storage in classes would need to account for base classes that also may have instance fields, and perhaps with replicated storage as well.
- allowing multiple replicated fields in the same struct could make the overall layout computation a fairly complex routine when field ordering, packing, ref-ness and alignment of the fields are considered. (i.e alignment would need to consider pre-replicated sizes, but packing post-replicated)

All the above issues can be solved, but at a cost to the implementation complexity while most additional scenarios appear to be less common and easily solvable by providing a wrapper struct.
