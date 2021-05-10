Shared Generics Design
===

Author: Fadi Hanna - 2019

# Introduction

Shared generics is a runtime+JIT feature aimed at reducing the amount of code the runtime generates for generic methods of various instantiations (supports methods on generic types and generic methods). The idea is that for certain instantiations, the generated code will almost be identical with the exception of a few instructions, so in order to reduce the memory footprint, and the amount of time we spend jitting these generic methods, the runtime will generate a single special canonical version of the code, which can be used by all compatible instantiations of the method.

### Canonical Codegen and Generic Dictionaries

Consider the following C# code sample:

``` c#
string Method<T>()
{
    return typeof(List<T>).ToString();
}
```

Without shared generics, the code for instantiations like `Method<object>` or `Method<string>` would look identical except for one single instruction: the one that loads the correct TypeHandle of type `List<T>`:
``` asm
    mov rcx, type handle of List<string> or List<object>
    call ToString()
    ret
```

With shared generics, the canonical code will not have any hard-coded versions of the type handle of List<T>, but instead looks up the exact type handle either through a call to a runtime helper API, or by loading it up from the *generic dictionary* of the instantiation of Method<T> that is executing. The code would look more like the following:
``` asm
    mov rcx, generic context                                                // MethodDesc of Method<string> or Method<object>
    mov rcx, [rcx + offset of InstantiatedMethodDesc::m_pPerInstInfo]       // This is the generic dictionary
    mov rcx, [rcx + dictionary slot containing type handle of List<T>]
    call ToString()
    ret
```

The generic context in this example is the InstantiatedMethodDesc of `Method<object>` or `Method<string>`. The generic dictionary is a data structure used by shared generic code to fetch instantiation-specific information. It is basically an array where the entries are instantiation-specific type handles, method handles, field handles, method entry points, etc... The "PerInstInfo" fields on MethodTable and InstantiatedMethodDesc structures point at the generic dictionary structure for a generic type and method respectively.

In this example, the generic dictionary for Method<object> will contain a slot with the type handle for type List<object>, and the generic dictionary for Method<string> will contain a slot with the type handle for type List<string>.

This feature is currently only supported for instantiations over reference types because they all have the same size/properties/layout/etc... For instantiations over primitive types or value types, the runtime will generate separate code bodies for each instantiation.


# Layouts and Algorithms

### Dictionaries Pointers on Types and Methods

The dictionary used by any given generic method is pointed at by the `m_pPerInstInfo` field on the `InstantiatedMethodDesc` structure of that method. It's a direct pointer to the contents of the generic dictionary data.

On generic types, there's an extra level of indirection: the `m_pPerInstInfo` field on the `MethodTable` structure is a pointer to a table of dictionaries, and each entry in that table is a pointer to the actual generic dictionary data. This is because types have inheritance, and derived generic types inherit the dictionaries of their base types.

Here's an example:
```c#
class BaseClass<T> { }

class DerivedClass<U> : BaseClass<U> { }

class AnotherDerivedClass : DerivedClass<string> { }
```

The MethodTables of each of these types will look like the following:

| **BaseClass[T]'s MethodTable** |
|--------------------------|
| ...      |
| `m_PerInstInfo`: points at dictionary table below     |
| ...      |
| `dictionaryTable[0]`: points at dictionary data below      |
| `BaseClass's dictionary data here`  |

| **DerivedClass[U]'s MethodTable ** |
|--------------------------|
| ...      |
| `m_PerInstInfo`: points at dictionary table below     |
| ...      |
| `dictionaryTable[0]`: points at dictionary data of `BaseClass`      |
| `dictionaryTable[1]`: points at dictionary data below      |
| `DerivedClass's dictionary data here`  |

| **AnotherDerivedClass's MethodTable** |
|--------------------------|
| ...      |
| `m_PerInstInfo`: points at dictionary table below     |
| ...      |
| `dictionaryTable[0]`: points at dictionary data of `BaseClass`      |
| `dictionaryTable[1]`: points at dictionary data of `DerivedClass`      |

Note that `AnotherDerivedClass` doesn't have a dictionary of its own given that it is not a generic type, but inherits the dictionary pointers of its base types.

### Dictionary Slots

As described earlier, a generic dictionary is an array of multiple slots containing instantiation-specific information. When a dictionary is initially allocated for a certain generic type or method, all of its slots are initialized to NULL, and are lazily populated on demand as code executes (see: `Dictionary::PopulateEntry(...)`).

The first N slots in an instantiation of N arguments are always going to be the type handles of the instantiation type arguments (this is kind of an optimization as well). The slots that follow contain instantiation-based information.

For instance, here is an example of the contents of the generic dictionary for our `Method<string>` example:

| `Method<string>'s dicionary` |
|--------------------------|
| slot[0]: TypeHandle(`string`)      |
| slot[1]: Total dictionary size  |
| slot[2]: TypeHandle(`List<string>`)  |
| slot[3]: NULL (not used)  |
| slot[4]: NULL (not used)  |

*Note: the size slot is never used by generic code, and is part of the dynamic dictionary expansion feature. More on that below.*

When this dictionary is first allocated, only slot[0] is initialized because it contains the instantiation type arguments (and of course the size slot is also initialized with the dictionary expansion feature), but the rest of the slots (example slot[2]) are NULL, and get lazily populated with values if we ever hit a code path that attempts to use them.

When loading information from a slot that is still NULL, the generic code will call one of these runtime helper functions to populate the dictionary slot with a value:
- `JIT_GenericHandleClass`: Used to lookup a value in a generic type dictionary. This helper is used by all instance methods on generic types.
- `JIT_GenericHandleMethod`: Used to lookup a value in a generic method dictionary. This helper used by all generic methods, or non-generic static methods on generic types.

When generating shared generic code, the JIT knows which slots to use for the various lookups, and the kind of information contained in each slot using the help of the `DictionaryLayout` implementation ([genericdict.cpp](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/genericdict.cpp)).

### Dictionary Layouts

The `DictionaryLayout` structure is what tells the JIT which slot to use when performing a dictionary lookup. This `DictionaryLayout` structure has a couple of important properties:
- It is shared across all compatible instantiations of a certain type of method. In other words, a dictionary layout is associated with the canonical instantiation of a type or a method. For instance, in our example above, `Method<object>` and `Method<string>` are compatible instantiations, each with their own **separate dictionaries**, however they all share the **same dictionary layout**, which is associated with the canonical instantiation `Method<__Canon>`.
- The dictionaries of generic types or methods have the same number of slots as their dictionary layouts. Note: historically before the introduction of the dynamic dictionary expansion feature, the generic dictionaries could be smaller than their layouts, meaning that for certain lookups, we had to use invoke some runtime helper APIs (slow path).

When a generic type or method is first created, its dictionary layout contains 'unassigned' slots. Assignments happen as part of code generation, whenever the JIT needs to emit a dictionary lookup sequence. This assignment happens during the calls to the `DictionaryLayout::FindToken(...)` APIs. Once a slot has been assigned, it becomes associated with a certain signature, which describes the kind of value that will go in every instantiated dictionary at that slot index.

Given an input signature, slot assignment is performed with the following algorithm:

```
Begin with slot = 0
Foreach entry in dictionary layout
    If entry.signature != NULL
        If entry.signature == inputSignature
            return slot
        EndIf
    Else
        entry.signature = inputSignature
        return slot
    EndIf
    slot++
EndForeach
```

So what happens when the above algorithm runs, but no existing slot with the same signature is found, and we're out of 'unassigned' slots? This is where the dynamic dictionary expansion kicks in to resize the layout by adding more slots to it, and resizing all dictionaries associated with this layout.

# Dynamic Dictionary Expansion

### History

Before the dynamic dictionary expansion feature, dictionary layouts were organized into buckets (a linked list of fixed-size `DictionaryLayout` structures). The size of the initial layout bucket was always fixed to some number which was computed based on some heuristics for generic types, and always fixed to 4 slots for generic methods. The generic types and methods also had fixed-size generic dictionaries which could be used for lookups (also known as "fast lookup slots").

When a bucket gets filled with entries, we would just allocate a new `DictionaryLayout` bucket, and add it to the list. The problem however is that we couldn't resize the generic dictionaries of types or methods, because they are already allocated with a fixed size, and the JIT does not support generating instructions that could indirect into a linked-list of dictionaries. Given that limitation, we could only lookup a generic dictionary for a fixed number of values (the ones associated with the entries of the first `DictionaryLayout` bucket), and were forced to go through a slower runtime helper for additional lookups.

This was acceptable, until we introduced the [ReadyToRun](https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/readytorun-overview.md) and the Tiered Compilation technologies. Slots were getting assigned quickly when used by ReadyToRun code, and when the runtime decided re-jitted certain methods for better performance, it could not in some cases find any remaining "fast lookup slots", and was forced to generate code that goes through the slower runtime helpers. This ended up hurting performance in some scenarios, and a decision was made to not use the fast lookup slots for ReadyToRun code, and instead keep them reserved for re-jitted code. This decision however hurt the ReadyToRun performance, but it was a necessary compromise since we cared more about re-jitted code throughput over R2R throughput.

For this reason, the dynamic dictionary expansion feature was introduced.

### Description and Algorithms

The feature is simple in concept: change dictionary layouts from a linked list of buckets into dynamically expandable arrays instead. Sounds simple, but great care had to be taken when implementing it, because:
- We can't just resize `DictionaryLayout` structures alone. If the size of the layout is larger than the size of the actual generic dictionary, this would cause the JIT to generate indirection instructions that do not match the size of the dictionary data, leading to access violations.
- We can't just resize generic dictionaries on types and methods:
    - For types, the generic dictionary is part of the `MethodTable` structure, which can't be reallocated (already in use by managed code)
    - For methods, the generic dictionary is not part of the `MethodDesc` structure, but can still be in use by some generic code.
    - We can't have multiple MethodTables or MethodDescs for the same type or method anyways, so reallocations are not an option.
- We can't just resize the generic dictionary for a single instantiation. For instance, in our example above, let's say we wanted to expand the dictionary for `Method<string>`. The resizing of the layout would have an impact on the shared canonical code that the JIT generates for `Method<__Canon>`. If we only resized the dictionary of `Method<string>`, the shared generic code would work for that instantiation only, but when we attempt to use it with another instantiation like `Method<object>`, the jitted instructions would no longer match the size of the dictionary structure, and would cause access violations.
- The runtime is multi-threaded, which adds to the complexity.

The current implementation expands the dictionary layout and the actual dictionaries separately to keep things simple:

 - Dictionary layouts are expanded when we are out of empty slots. See implementations of `DictionaryLayout::FindToken()` in [genericdict.cpp](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/genericdict.cpp).
 - Instantiated type and method dictionaries are expanded lazily on demand whenever any code is attempting to read the value of a slot beyond the size of the dictionary of that type or method. This is done through a call to the helper functions mentioned previously (`JIT_GenericHandleClass` and `JIT_GenericHandleMethod`).

The dictionary access codegen is equivalent to the following (both in JITted code and ReadyToRun code):
``` c++
void* pMethodDesc = <some value>;		           // Input MethodDesc for the instantiated generic method
int requiredOffset = <some value>;                 // Offset we need to access

void* pDictionary = pMethodDesc->m_pPerInstInfo;

// Note how we check for the dictionary size first before indirecting at 'requiredOffset'
if (pDictionary[sizeOffset] <= requiredOffset || pDictionary[requiredOffset] == NULL)
    pResult = JIT_GenericHandleMethod(pMethodDesc, <signature>);
else
    pResult = pDictionary[requiredOffset];
```

This size check is **not** done unconditionally every time we need to read a value from the dictionary, otherwise this would cause a noticeable performance regression. When a dictionary layout is first allocated, we keep track of the initial number of slots that were allocated, and **only** perform the size checks if we are attempting to read the value of a slot beyond those initial number of slots.

Dictionaries on types and methods are expanded by the `Dictionary::GetTypeDictionaryWithSizeCheck()` and `Dictionary::GetMethodDictionaryWithSizeCheck()` helper functions in [genericdict.cpp](https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/genericdict.cpp).

One thing to note regarding types is that they can inherit dictionary pointers from their base types. This means that if we resize the generic dictionary on any given generic type, we will need to propagate the new dictionary pointer to all of its derived types. This propagation is also done in a lazy way whenever the code calls into the `JIT_GenericHandleWorker` helper function with a derived type MethodTable pointer. In that helper, if we find that the dictionary pointer on the base type has been updated, we copy it to the derived type.

Old dictionaries are not deallocated after resizing, but once a new dictionary gets published on a MethodTable or MethodDesc, any subsequent dictionary lookup by generic code will make use of that newly allocated dictionary. Deallocating old dictionaries would be extremely complicated, especially in a multi-threaded environment, and won't give any useful benefit.
