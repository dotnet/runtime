# Ownership and Lifetimes of parameters

## Motivation
The ComInterfaceGenerator generates both CCW and RCW, and the generated methods on each could fail at many points in the generated stubs. It is important to make sure these failures do not leak memory or lead to double frees.
This document outlines the lifetimes and ownerships of the parameters as they are passed from managed stubs to COM and vice versa.

## Characteristics

The transfer of ownership depends on the following characteristics of the parameter types

Indirection level
- ByValue: A value that is not indirected (e.g. `int`, `float`, blittable types in C#)
- Single Indirection: A reference to a value (e.g. pointers / `int*`, reference types in C#)
- Double Indirection: A reference to a reference to a value (e.g. pointers to pointers / `int**`, `ref` parameters in C#)

Mutability (Only relevant for indirect values)
- Immutable - The caller is not allowed to modify the value pointed at (e.g. `in` in C#, `const` in C++)
- Mutable - The callee may modify the value pointed at (e.g. ref value types and reference types in C#)
- Requires Mutation - The callee must modify the value pointed at (e.g. `out` in C#)

## Defaults

The following are the default characteristics of C# parameter types:

#### Indirection

Blittable types and value types are ByValue.

Classes are SingleIndirection.

No type is Double Indirection by default, but a SingleIndirection type can become multiple indirection with the RefKind parameter modifiers.

#### Mutability

All unmodified non-array parameters have Immutable mutability by default.

Array types by default have Mutable mutability by default.

## Parameter modifiers / attributes

In the C# interface definitions, the RefKind parameter modifiers and [InAttribute] and [OutAttribute] modify the default characteristics.

The `in` RefKind adds an indirection level to the managed parameter type and means the parameter has Immutable mutability
The `out` RefKind adds an indirection level to the managed parameter type and means the parameter has Requires Mutation mutability
The `ref` RefKind adds an indirection level to the managed parameter type and means the parameter has Mutable mutability
Having only `[InAttribute]` means the parameter has Immutable mutability
Having only `[OutAttribute]` means the parameter has Requires Mutation mutability
Having both `[InAttribute]` and `[OutAttribute]` means the parameter has Mutable mutability

This means `[OutAttribute] ClassType param` has equivalent ownership rules to `out StructType param`.

## Arrays

Arrays of Single Indirection types have both the memory allocated for the container of the array as any memory allocated for each element.
These types of memory follow different ownership rules, so it is necessary to consider the array container memory and element memory separately.

Since an array of ByValue elements do not have memory allocated for the elements, it can be treated as a normal reference type.

#### Container

The array container follows rules like any normal reference type, and by default has Single Indirection.
For example the container of `[OutAttribute] ClassX[] arrayParamName` will have the same ownership rules as `[OutAttribute] ClassX paramName` and `out StructX paramName`.

`out`, `ref` and `in` add an indirection level to the array container.
For example, the array container for `ref ClassX[] arrayParamName` follows the same rules as `ref ClassX paramName`.

When ownership of an array container is transfered, the elements of the array are all transfered as well.
For jagged arrays of multiple dimensions, this works transitively
(e.g. when the container for `ClassX[][][][]` is transferred, all the elements of type `ClassX[][][]` are transfered, and the elements of each of those are transfered and so on).

#### Elements

Elements of the arrays will have ownership rules are as if they have an additional indirection.
For example, elements of `[OutAttribute] ClassX[] arrayParamName` will have the same ownership rules as `out ClassX paramName`.
As a result of the above note that an array container that transfers ownership will always transfer the ownership of its elements, any array that has Double Indirection will follow the ownership of the array container.
For example, elements of `ref ClassX[] arrayParam` will follow the same ownership rules as the container.

# Ownership transfer rules

## Immutable parameters

All parameters with Immutable mutability are owned by the caller.

## By Value

All By Value parameters do not have allocated memory associated with them and do not need to worry about ownership.

## Single Indirection

All parameters with Pointer indirection level are owned by the caller.

### Array of blittable elements

An array of blittable elements follows the same rules as Single Indirection (owned by the caller).

## Double Indirection

Double Indirection (e.g. `ClassX** paramName` in C) may transfer ownership of heap memory.
For clarity, the parameter value (a reference to a reference to memory on the heap) will be refered to as "the double reference".
The reference to the memory on the heap will be called "the single reference".
The memory on the heap that the single reference points to will be called "the heap memory".
The double reference points to the single reference, which points to the heap memory.

### Immutable

As mentioned above, Immutable parameters are owned by the caller and will never transfer ownership.

### Requires Mutation

Requires Mutation mutability means the caller passes a double reference to a single reference that may be invalid.
This means the callee is not expected to use the value in the single reference.
If the single reference is a valid reference to heap memory, the caller is responsible for deallocating the memory.
When called, the callee is expected to allocate new heap memory and change the value of the single reference to point to the newly allocated heap memory.
The callee is the owner of this new memory until the callee returns a sucessful return value, at which point the ownership transfers to the caller.
If the callee returns a failure return value, the callee is expected to have cleaned up any memory allocated, even if it has set the single reference to point to the newly allocated heap memory.

// Should the single reference be restored to the original value if the callee fails?

For example, a caller may pass `ClassX** param` in C.
`param` must point to a valid pointer, but `*param` is not required to be a valid pointer to a `ClassX` on the heap.
The callee is expected to change the value of `*param` to point to a newly allocated `ClassX` on the heap.
Once the callee has returned a successful return value, ownership is transfered to the caller,
and the caller will be responsible for freeing the memory containing the new `ClassX`
If the callee returns a failure return value, it is expected to have deallocated the memory allocated for the new `ClassX` instance,
and upon receiving a failing return value, the `*param` is not expected to not point to a valid `ClassX`.

// Is *param supposed to be restored to the original?


### Mutable

Mutable mutability means the caller passes a double reference to a single reference to valid heap memory.
The callee may choose to modify the single reference, or the the heap memory values.
When the callee returns a successful return value, the memory pointed to by the original value of the single reference is transfered to the callee,
and the memory pointed to by the single reference at the end of the call is transferred to the caller.
If after a successful return, the single reference does not change where it points to, the caller keeps ownership of the memory pointed to.
If after a successful return, the single reference changes where it points to, the callee is expected to have deallocated the memory that the original single reference pointed to (the original heap memory).
If the callee returns a failure return value, the callee is expected to deallocate any memory it allocated and restore the single pointer to the original value.

// Should the single reference be restored to the original value if the callee fails?

For example, a caller may pass `ClassX** param` in C.
`param` must point to a valid pointer, and `*param` is required to be a valid pointer to a `ClassX` on the heap.
The callee may or may not change the value at `*param` to point to a newly allocated `ClassX` on the heap.
Once the callee has returned a successful return value, it has taken ownership of the heap memory pointed to by the original value of `*param`,
and the caller is expected to take ownership of the heap memory that `*param` points after the call.
If the callee returns a failure return value, it is expected to deallocate any memory allocated.
Upon receiving a failing return value, the caller is expected to take ownership of the location that `param*` originally pointed to.

# Examples
// TODO:

# Implementation

In implementation, this means that for all double indirection parameters, the generated stubs need to clean up differently depending on if the call succeeded or not. One way is to set a flag `__invokeSuceeded` to `false` during the setup stage, and setting it to true at the end of the `try` block (after the marshal stage) for the ABI method / unmanaged to managed direction, and after the call to `ThrowForHR` in the InterfaceImplementation method / managed to unmanaged direction.
