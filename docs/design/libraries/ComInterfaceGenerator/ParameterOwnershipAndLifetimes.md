# Ownership and Lifetimes of parameters

The ComInterfaceGenerator generates both CCW and RCW methods, each of which could fail at many points in the generated stubs. It is important to make sure these failures do not leak memory or lead to double frees.
This document outlines the lifetimes and ownerships of the parameters as they are passed from managed stubs to COM and vice versa.

## Categories

The transfer of ownership depends on the following
- Indirection level and (native) parameter type [Blittable / ByValue, Single Pointer / NonBlittable, Double Pointer / Array of Blittable Elements, Array of Pointers (including arrays of arrays), Pointer to Array of Pointers]
- Ownership Semantics ['in', 'out', 'ref']

## Modifying parameters

The `in` RefKind adds an indirection level to the managed parameter type and means the parameter has 'in' ownership semantics
The `out` RefKind adds an indirection level to the managed parameter type and means the parameter has 'out' ownership semantics
The `ref` RefKind adds an indirection level to the managed parameter type and means the parameter has 'ref' ownership semantics
Having only `[InAttribute]` means the parameter has 'in' ownership semantics
Having only `[OutAttribute]` means the parameter has 'out' ownership semantics
Having both `[InAttribute]` and `[OutAttribute]` means the parameter has 'ref' ownership semantics

This means `[OutAttribute] ClassType paramName` has equivalent ownership rules to `out StructType paramName`.

Elements of the arrays will have ownership rules are as if they are Indirected. For example, elements of `[OutAttribute] ClassX[] arrayParamName` will have the same ownership rules as `out ClassX paramName`.

## Default Ownership semantics

All unmodified non-array parameters have 'in' ownership semantics by default.
Array types by default have 'ref' ownership semantics by default.

## Note on Arrays

Arrays have both the memory allocated for the array and (for arrays of non-blittable types) memory allocated for each element. These memory blocks may follow different ownership rules.

## In parameters

All parameters with 'in' ownership semantics are owned by the caller.

## Blittable

All blittable parameters do not have allocated memory associated with them and do not need to worry about ownership.

## Pointer / NonBlittable

All parameters with Pointer indirection level are owned by the caller.

### Array of blittable elements

An array of blittable elements is analogous to a pointer to a class and follows the same rules (owned by the caller).

## Double Pointer

Double pointers (e.g. `**StructType paramName`) may transfer ownership. As above, 'in' ownership is owned by the caller.

### Out
'Out' ownership semantics means the callee is expected to allocate a value and modify the singly indirected pointer (`*paramName`) to point to the new value. The owner of this new memory is the callee up until the callee returns a non-failing HResult. At this point the ownership transfers to the caller. If the callee returns a failure HResult, it should clean up the allocated memory. The value originally pointed to by `*paramName` is always owned by the caller.

### Ref

'Ref' ownerhsip semantics means the callee is expected to allocate a value and modify the singly indirected pointer (`*paramName`) to point to the new value. The owner of this new memory is the callee up until the callee returns a non-failing HResult. At this point the ownership transfers to the caller. If the callee returns a failure HResult, it should clean up the allocated memory. The ownership of the value originally pointed to by `*paramName` is owned by the caller until the callee returns a non-failing HResult. If the callee returns a failure HResult, the caller maintains ownership of the memory.

### Pointer to Arrays of blittable elements

A pointer to an array of blittable elements is analogous to a double pointer and follows the same rules.

## Array of Pointers

### Array

The memory of the array is always owned by the caller.

### Elements

The elements of the array follow the rules for Double Pointers outlined above.

## Pointer to Array of Pointers

### Array

The memory of the array follows the same rules as Double pointers.

### Elements

The elements of the array follow the same rules as Double pointers

# Examples


# Implementation

In implementation, this means that for all double pointers, the generated stubs need to clean up differently depending on if the call succeeded or not. One way is to set a flag `__invokeSuceeded` to `false` during the setup stage, and setting it to true at the end of the `try` block (after the marshal stage) for the ABI method / unmanaged to managed direction, and after the call to `ThrowForHR` in the InterfaceImplementation method / managed to unmanaged direction.

