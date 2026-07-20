# Vectorization guidelines

The general guidance for writing SIMD and hardware-intrinsics code in .NET now lives in the official
documentation:

- [Use SIMD and hardware intrinsics in .NET](https://learn.microsoft.com/dotnet/standard/simd)

That article covers the material this document used to duplicate: the layering from `System.Numerics`
through `Vector64/128/256/512<T>` to the platform-specific intrinsics and `TensorPrimitives`, checking
for hardware acceleration, structuring a vectorized method, handling the loop remainder (idempotent vs.
non-idempotent), loading and storing safely, the full API tool-chain, and how to test and benchmark.
**Read it first.** The rest of this document only calls out the nuance that is specific to working in
dotnet/runtime.

## Testing for access violations

Mishandling the remainder is the most common source of bugs in vectorized code; a loop that reads past
the end of a buffer produces non-deterministic results and can crash. To catch this in tests, use the
[`BoundedMemory`](/src/libraries/Common/tests/TestUtilities/System/Buffers/BoundedMemory.Creation.cs)
helper. On most targets it allocates a memory region immediately followed (or preceded) by a poison
(`MEM_NOACCESS`) page, so an out-of-bounds read faults with an access violation during testing rather
than silently succeeding. (On a few targets — Browser/WASI and .NET Framework — it falls back to an
unprotected allocation that won't fault, so don't rely on the guard being present everywhere.)

`BoundedMemory.Allocate<T>(elementCount)` places the poison page immediately *after* the buffer (the
default `PoisonPagePlacement.After`) and fills it with random data, so running the method under test
against its `Span` faults immediately on any read past the end:

```csharp
[Theory]
[InlineData(3)]  // smaller than one Vector128<int>
[InlineData(6)]  // length not a multiple of the vector width
[InlineData(16)]
public void Sum_DoesNotReadOutOfBounds(int length)
{
    using BoundedMemory<int> bounded = BoundedMemory.Allocate<int>(length);

    // If Sum's remainder handling reads past the buffer, this faults instead of
    // silently succeeding against adjacent memory.
    int actual = Sum(bounded.Span);

    Assert.Equal(Reference(bounded.Span), actual);
}
```

Pass `PoisonPagePlacement.Before` instead to catch reads *before* the start of the buffer, which is the
failure mode for algorithms that iterate backwards.

Always add coverage for buffers whose length is not an exact multiple of the vector width, and run the
relevant tests under each hardware-acceleration configuration (for example `DOTNET_EnableAVX2=0` and
`DOTNET_EnableHWIntrinsic=0`, as described in the official article).

## Managed references can introduce GC holes

Prefer the span-based overloads. As the official article notes, `Vector128.Create(span)` and
`CopyTo` are the simplest way to move data between a span and a vector, the JIT keeps them efficient,
and they need no pinning or reference arithmetic. With those, and the improvements to bounds-check
elision and codegen since this guidance was first written, the `unsafe` load/store variants are largely
no longer needed — reach for them only when you genuinely must walk a buffer by managed reference on a
measured hot path.

When you do need the lower-level path, use the `LoadUnsafe(ref T, nuint elementOffset)` / `StoreUnsafe`
overloads rather than raw pointer or `ref` arithmetic. The element-offset form requires no pinning and
no manual `ref` advancing, which is exactly what makes raw managed-reference arithmetic easy to get
wrong. This is not hypothetical — a GC hole was introduced in dotnet/runtime this way. In
[#73768](https://github.com/dotnet/runtime/pull/73768) a `LastIndexOf` implementation walked the buffer
backwards:

```csharp
ref TValue currentSearchSpace = ref Unsafe.Add(ref searchSpace, length - Vector128<TValue>.Count);

do
{
    equals = Vector128.Equals(values, Vector128.LoadUnsafe(ref currentSearchSpace));
    if (equals == Vector128<TValue>.Zero)
    {
        currentSearchSpace = ref Unsafe.Subtract(ref currentSearchSpace, Vector128<TValue>.Count);
        continue;
    }

    return ...;
}
while (Unsafe.IsAddressGreaterThanOrEqualTo(ref currentSearchSpace, ref searchSpace));
```

On the final iteration `currentSearchSpace` could point before the start of the buffer. That was fine
until the GC ran right after the `Unsafe.Subtract`: it moved objects, updated every *valid* managed
reference, then resumed execution — but `currentSearchSpace` was invalid and therefore not updated, so
the loop condition could read stale memory. See the [issue](https://github.com/dotnet/runtime/issues/75792#issuecomment-1249973858)
and the [fix](https://github.com/dotnet/runtime/pull/75857) for details.

The takeaway: when you do drop to the element-offset overloads, keep every intermediate `ref` pointing
within its buffer, and be especially careful with backwards iteration.

## Real-world examples in this repo

[#64451](https://github.com/dotnet/runtime/issues/64451) tracks places in dotnet/runtime that have been
(or can be) vectorized with the cross-platform APIs. The linked PRs are a good source of real,
reviewed implementations to learn from when vectorizing a new algorithm here.
