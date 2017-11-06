# System.Memory Design Guidelines

`System.Memory` is a collection of types and features that make working with
buffers and raw memory more efficient while remaining type safe. The feature
specs can be found here:

* [`Span<T>`](https://github.com/dotnet/corefxlab/blob/master/docs/specs/span.md)
* [`Memory<T>`](https://github.com/dotnet/corefxlab/blob/master/docs/specs/memory.md)

## Overview

* `ReadOnlySpan<T>` is effectively the universal receiver, in that `T[]`, `T*`,
  `Memory<T>`, `ReadOnlyMemory<T>`, `Span<T>`, `ArraySegment<T>` can all be
  converted to it.  So if you can declare your API to accept a `ReadOnlySpan<T>`
  and behave efficiently, that's best, as any of these inputs can be used with
  your method.
* Similarly for `Span<T>`, if you need write access in the implementation.
* It allows building safe public APIs that can operate on unmanaged memory
  without forcing all consumers to use pointers (and thus becoming unsafe) while
  still getting equivalent performance.
* It's generally best for a synchronous method to accept `Span<T>` or
  `ReadOnlySpan<T>`. However, due to `ReadOnlySpan<T>`/`Span<T>` limitations,
  this may be limiting for the implementation. In particular, if the
  implementation needs to be able to store the argument for later usage, such as
  with an async method or an iterator, `ReadOnlySpan<T>`/`Span<T>` is
  inappropriate. `ReadOnlyMemory<T>`/`Memory<T>` should be used in such
  situations.

## Guidance

* **DO NOT** use pointers for methods operating on buffers. Instead, use
  appropriate type form below. In performance critical code where bounds
  checking is unacceptable, the method's implementation can still pin the span
  and get the raw pointer if necessary. The key is that you don't spread the
  pointer through the public API.
    - Synchronous, read-only access needed: `ReadOnlySpan<T>`
    - Synchronous, writable access needed: `Span<T>`
    - Asynchronous, read-only access needed: `ReadOnlyMemory<T>`
    - Asynchronous, writable access needed: `Memory<T>`
* **CONSIDER** using `stackalloc` with `Span<T>` when you need temporary storage
  but you need to avoid allocations and associated life-time management.
* **AVOID** providing overloads for both `ReadOnlySpan<T>` and `Span<T>` as `Span<T>`
  can be implicitly converted to `ReadOnlySpan<T>`.
* **AVOID** providing overloads for both `ReadOnlySpan<T>`/`Span<T>` as well as
  pointers and arrays as those can be implicitly converted to
  `ReadOnlySpan<T>`/`Span<T>`.