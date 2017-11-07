# System.Memory Design Guidelines

`System.Memory` is a collection of types and features that make working with
buffers and raw memory more efficient while remaining type safe. The feature
specs can be found here:

* [`Span<T>`](https://github.com/dotnet/corefxlab/blob/master/docs/specs/span.md)
* [`Memory<T>`](https://github.com/dotnet/corefxlab/blob/master/docs/specs/memory.md)

## Overview

* `ReadOnlySpan<T>` is effectively the universal receiver, in that `T[]`, `T*`,
  `Memory<T>`, `ReadOnlyMemory<T>`, `Span<T>`, `ArraySegment<T>` can all be
  converted to it. So if you can declare your API to accept a `ReadOnlySpan<T>`
  and behave efficiently, that's best, as any of these inputs can be used with
  your method.
* Similarly for `Span<T>`, if you need write access in the implementation.
* It allows building safe public APIs that can operate on unmanaged memory
  without forcing all consumers to use pointers (and thus becoming unsafe). The
  implementation can still extract a raw pointer, therefore getting equivalent
  performance if necessary.
* It's generally best for a synchronous method to accept `Span<T>` or
  `ReadOnlySpan<T>`. However, since `ReadOnlySpan<T>`/`Span<T>` are stack-only
  [1], this may be too limiting for the implementation. In particular, if the
  implementation needs to be able to store the argument for later usage, such as
  with an asynchronous method or an iterator, `ReadOnlySpan<T>`/`Span<T>` is
  inappropriate. `ReadOnlyMemory<T>`/`Memory<T>` should be used in such
  situations.


[1] *stack-only* isn't the best way to put it. Strictly speaking, these types
    are called `ref`-like types. These types must be structs, cannot be fields
    in classes, cannot be boxed, and cannot be used to instantiate generic
    types. Value types containing fields of `ref`-like types must themselves be
    `ref`-like types.

## Guidance

* **DO NOT** use pointers for methods operating on buffers. Instead, use
  appropriate type from below. In performance critical code where bounds
  checking is unacceptable, the method's implementation can still pin the span
  and get the raw pointer if necessary. The key is that you don't spread the
  pointer through the public API.
    - Synchronous, read-only access needed: `ReadOnlySpan<T>`
    - Synchronous, writable access needed: `Span<T>`
    - Asynchronous, read-only access needed: `ReadOnlyMemory<T>`
    - Asynchronous, writable access needed: `Memory<T>`
* **CONSIDER** using `stackalloc` with `Span<T>` when you need small temporary
  storage but you need to avoid allocations and associated life-time management.
* **AVOID** providing overloads for both `ReadOnlySpan<T>` and `Span<T>` as `Span<T>`
  can be implicitly converted to `ReadOnlySpan<T>`.
* **AVOID** providing overloads for both `ReadOnlySpan<T>`/`Span<T>` as well as
  pointers and arrays as those can be implicitly converted to
  `ReadOnlySpan<T>`/`Span<T>`.