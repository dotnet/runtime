---
name: vectorization
description: >
  Guidance for writing and reviewing SIMD / hardware-intrinsics code in
  dotnet/runtime. USE FOR: vectorizing a scalar algorithm, writing or reviewing
  code that uses Vector128/Vector256/Vector512, Vector<T>, or the platform
  intrinsics in System.Runtime.Intrinsics.X86/Arm/Wasm, and validating remainder
  handling, load/store safety, and hardware-acceleration fallbacks. DO NOT USE
  FOR: general performance work unrelated to SIMD (use performance-benchmark),
  or non-vectorized code review (use code-review).
---

# SIMD and vectorization in dotnet/runtime

The general, cross-cutting guidance for SIMD and hardware intrinsics lives in the official .NET
documentation. **Read it first** and defer to it for anything not specific to this repo:

- [Use SIMD and hardware intrinsics in .NET](https://learn.microsoft.com/dotnet/standard/simd)

The repo-specific nuance is in [`docs/coding-guidelines/vectorization-guidelines.md`](/docs/coding-guidelines/vectorization-guidelines.md).
This skill distills what to actually enforce when authoring or reviewing vectorized changes here.

## Core rules

1. **Reach for the highest-level API that already does the job.** `Span<T>`/`string` methods, LINQ,
   `TensorPrimitives`, and the tensor types already vectorize many operations. Don't hand-roll what's
   already optimized and tested.
2. **Start with `Vector128<T>`.** It's the common denominator accelerated on the broadest hardware, and
   you don't need `Vector256`/`Vector512` for a correct, portable implementation. Add wider widths and
   platform intrinsics only for a *measured* hot path.
3. **Keep platforms consistent.** Prefer the cross-platform APIs on `Vector128`/`Vector256`; they lower
   to the optimal instruction per target (for example `(vector & mask) == Vector128<byte>.Zero` becomes
   `ptest` on x86/x64). Only drop to `System.Runtime.Intrinsics.X86`/`Arm`/`Wasm` when a specific
   instruction measurably beats the portable form, and guard it with the class's `IsSupported`.
4. **Read `IsHardwareAccelerated` and `Count` directly, never cache them.** Both are JIT-time constants;
   caching defeats the constant-folding and dead-branch elimination.
5. **Prefer operators over named methods** (`+`, `&`, `<<`) to avoid operator-precedence bugs.

## Authoring checklist

- **Structure:** widest-supported width first, working down to a scalar fallback for small inputs and
  non-accelerated hardware. Guard each width with `Vector128.IsHardwareAccelerated` **and**
  `Vector128<T>.IsSupported` (the latter matters in generic code), then compare length against `Count`.
- **Loads and stores:** prefer the span-based `Vector128.Create(span)` / `CopyTo` — the JIT keeps them
  efficient and they need no pinning or reference arithmetic. The `unsafe` load/store variants are
  largely no longer needed; when you genuinely must walk a buffer by managed reference, use the
  `LoadUnsafe(ref T, nuint elementOffset)` / `StoreUnsafe` element-offset overloads rather than raw
  pointer or `ref` arithmetic.
- **Empty buffers:** get the starting reference from `MemoryMarshal.GetReference` (or
  `GetArrayDataReference` for arrays), not `ref span[0]`.
- **Reinterpreting unsupported types:** `Vector128<T>` supports the primitive numerics, not `char` or
  `bool`. Reinterpret via `MemoryMarshal.Cast` (a span) or the vector's `As<TFrom, TTo>` — for example
  `char` → `ushort`. Reinterpretation changes only the type, not the bits, so keeping the data
  well-formed is on you (a `bool` stays `0`/`1`, a `char` a valid UTF-16 code unit); normalize any
  out-of-range result before writing it back.
- **Offset arithmetic is unsigned (`nuint`).** Always check the buffer length before computing an offset
  like `buffer.Length - Vector128<int>.Count`; if the buffer is smaller than one vector that subtraction
  underflows to a huge value.
- **Always handle the remainder.** Reprocess the last full vector's worth of elements, overlapping what
  the loop already did. For an **idempotent** operation (a search) fold the overlap in directly; for a
  **non-idempotent** operation (a sum) mask the overlap to the operation's identity with
  `ConditionalSelect` first.
- **Watch backwards iteration.** Never let an intermediate `ref` point outside its buffer, even
  transiently — a GC that runs at that moment won't update it, producing a GC hole. See the
  `LastIndexOf` case study ([#73768](https://github.com/dotnet/runtime/pull/73768) /
  [fix](https://github.com/dotnet/runtime/pull/75857)).
- **Account for buffer overlap** when loading from one buffer and storing into another.

## Testing checklist

- **Cover every code path:** the `Vector256` path, the `Vector128` path, and the scalar path — each with
  inputs both large enough and too small to benefit.
- **Toggle acceleration via environment variables** (can't be done at the unit-test level): run the
  suite with no overrides, with `DOTNET_EnableAVX2=0` (disables `Vector256`), and with
  `DOTNET_EnableHWIntrinsic=0` (disables all intrinsics down to the software fallback). Build the
  affected library and run its test project per the build/test workflow in
  [`.github/copilot-instructions.md`](/.github/copilot-instructions.md), with the relevant
  `DOTNET_Enable*` variable set in the environment.
- **Guard against out-of-bounds reads with `BoundedMemory`.**
  [`BoundedMemory.Allocate<T>(count)`](/src/libraries/Common/tests/TestUtilities/System/Buffers/BoundedMemory.Creation.cs)
  places a no-access page immediately after the buffer (use `PoisonPagePlacement.Before` for
  backwards-iterating algorithms), so any read past the end throws `AccessViolationException` during
  testing instead of silently succeeding. Always include lengths that aren't an exact multiple of the
  vector width.

## Benchmarking

Vectorization adds complexity, so **measure that it pays off before keeping it.** Use BenchmarkDotNet
and the same `DOTNET_Enable*` variables to compare scalar / `Vector128` / `Vector256` in one run. Keep
in mind: larger inputs benefit more (small buffers can be *slower* due to setup), speedups are rarely
the theoretical multiple (memory throughput, alignment, and latency all factor in), and randomized
allocation alignment adds noise — allocate aligned memory or enable BenchmarkDotNet's randomization for
stable/observable results. For non-trivial changes, use the `performance-benchmark` skill.

## Review checklist

When reviewing a vectorized change, verify in priority order:

1. **Correctness first** — does it match the scalar contract, including signed-zero/NaN/overflow and
   endianness (`BitConverter.IsLittleEndian`) edge cases? Verify any claim about existing behavior.
2. **Reuse vs duplication** — should this use an existing higher-level API, helper, or shared loop
   instead of an Nth hand-rolled copy?
3. **Remainder handling** — is the tail covered, and is the idempotent-vs-masked choice correct?
4. **Memory safety** — no unguarded `nuint` underflow, no `ref` straying outside its buffer, empty
   buffers handled, overlap considered.
5. **Cross-platform consistency** — does it diverge from other architectures without justification?
   Prefer the portable API unless a per-platform intrinsic is justified by numbers.
6. **Tests** — all paths covered (including AV testing via `BoundedMemory`) and run under the
   acceleration-toggle env vars? Ask for the missing test rather than just rejecting.
7. **Perf claims** — backed by concrete numbers (codegen bytes, throughput, ns with noise context), not
   assertions. A wider vector is not automatically faster.
