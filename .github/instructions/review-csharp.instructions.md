---
applyTo: "**/*.cs"
---

# Code Review -- C# (managed code)

Rules for reviewing C# changes across `src/`. Also apply `review-all-src` (all changes),
`review-all-tests` (test files), and any matching area file (`review-core-runtime`, `jit`,
`system-net-*`, `extensions-*`, `compression`, `cdac`). Native runtime code is covered by
`review-native`.

These are review criteria. During code authoring or local experimentation, treat PR-level gates
such as motivation, benchmark evidence, and issue prerequisites as preparation guidance for a
ready-for-review PR, not as reasons to block exploratory work unless the user asks for review.

## Correctness & Safety

### Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value is not null)` over the null-forgiving operator (`!`).
- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps.
- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
- **Initialize output parameters in all code paths.** When a method has `out` parameters or pointer outputs (`bytesWritten`, `numLocals`), ensure they are initialized to a defined value in all error paths.
- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, etc. instead of manual if-then-throw patterns.
- **Challenge exception swallowing that masks unexpected errors.** When a PR adds try/catch blocks that silently discard exceptions (`catch { continue; }`, `catch { return null; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem (race conditions, memory corruption, build environment issues). Silently catching exceptions that "shouldn't happen" hides root causes and makes debugging harder. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.

### Thread Safety

- **Use `Volatile` or `Interlocked` for cross-thread field access.** Fields written on one thread and read on another must use `Volatile<T>`, `Volatile.Read/Write`, or `Interlocked`. The `??=` operator is not thread-safe. `Nullable<T>` is not safe for caching (two-field struct tears). Do not use shared mutable arrays without synchronization.
- **Use `TickCount64` for timeout calculations.** Use `Environment.TickCount64` (long) instead of `Environment.TickCount` (int) to avoid integer overflow.

### Security

- **Guard integer arithmetic against overflow before mutating state.** Guard size computations involving multiplication (e.g., `newCapacity * sizeof(T)`) with checked arithmetic or an explicit bounds check. A `checked` expression is sufficient only when it throws before partial state mutation. When a guard is separated from the arithmetic it protects, add a brief comment connecting them.
- **Clean sensitive cryptographic data after use.** Always clear key material with `CryptographicOperations.ZeroMemory`. When using `PinAndClear` but copying to another buffer, clear the original too. Use non-short-circuit operators (`|`) in verification code to prevent timing leaks.
- **Don't proactively send credentials without opt-in.** Never send authentication credentials (especially Basic auth) before receiving a challenge.
- **Limit `stackalloc` to ~1KB total per method and validate size.** Don't stackalloc based on user-controlled or large input sizes. The total stackalloc budget across the entire method (not just the visible scope) must stay under ~1KB. If the method does a user callback, has unknown call depth, or potential for recursion, reduce the budget further or don't use stackalloc at all. Move stackalloc to just before usage, not before early returns. Use the bounded pattern `(length > Threshold) ? stackalloc[Threshold] : ArrayPool.Rent(length)` to safely cap user input.

### Correctness Patterns

- **Fix root cause, not symptoms or workarounds.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Revert broken commits before layering fixes.
- **Prefer safe code over unsafe micro-optimizations.** Do not introduce `Unsafe.As`, `Unsafe.AsRef`, or raw pointers without demonstrable performance need. Prefer Span-based APIs. If performance is the issue, prefer fixing the JIT.
- **Use `Unsafe.BitCast` for same-size type punning between blittable types.** Prefer `Unsafe.BitCast<TFrom, TTo>` over `Unsafe.As<TFrom, TTo>` for type punning between unmanaged value types of the same size. For common cases, prefer safe alternatives (e.g., `BitConverter.SingleToInt32Bits` for `float`→`int`).
- **Scope creep: don't bundle cleanup into unrelated changes.** When the focus is a functional change, don't also convert safe code to unsafe or refactor for micro-optimizations. Keep those in separate PRs.
- **Delete dead code and unnecessary wrappers.** Remove dead code, unnecessary wrappers, obsolete fields, and unused variables when encountered or when the only caller changes.
- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned SafeHandles. Get the exception before calling `Dispose`, since Dispose might clear the error state.
- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType()` comparison, flag this as a potential bug if the class is unsealed — the solution is usually to seal the class, but don't automatically recommend sealing as the fix. Raise it as a warning for the author to evaluate.
- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
- **File name casing must match csproj references exactly.** Linux is case-sensitive. New source files must be listed in the `.csproj` if other files in that folder are explicitly listed.
- **Backport targeted fixes, not refactorings.** When backporting to servicing branches, create small targeted fixes. Backporting large refactorings introduces unnecessary risk.

## Performance & Allocations

### Measurement & Evidence

- **Performance changes require benchmark evidence.** Include BenchmarkDotNet results before merging. Prefer local BenchmarkDotNet runs first, especially for experimental/iterative work. EgorBot runs on an individual's personal account and is not billed like Copilot usage — only recommend it when explicitly requested, or for a final cross-architecture (x64/arm64) confirmation that cannot be reproduced locally.
- **Justify binary size increases with real-world measurements.** Changes that increase binary size require measured wall-clock improvements on real-world apps, not just instruction counts.
- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Prefer making the underlying operation faster.

### Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations.
- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List` constructors when the expected count is available.
- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing allocations for equality comparison.
- **Avoid Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects surviving as long as the process.
- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow to avoid capturing unrelated `AsyncLocal`s that leak memory.

### Code Structure for Performance

- **Place cheap checks before expensive operations.** Order conditionals so cheapest/most-common checks come first. Move expensive work after early-exit checks.
- **Allocate resources lazily where possible.** Allocate expensive resources on first use, not during initialization. Avoid forcing type initialization during startup.
- **Extract throw helpers into `[DoesNotReturn]` methods.** Move throwing logic from error paths into separate static local functions or helper methods to allow the JIT to inline the success path.
- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring, or appropriate data structures.
- **Cache repeated accessor calls in locals.** Store the result of repeated property/getter calls in a local variable.
- **Consider scalability, not just throughput.** Evaluate whether data structures, caches, and locking strategies will hold up at high cardinality or under concurrent load. Watch for unbounded collection growth, lock contention that worsens with core count, and O(1) assumptions that break at scale.

### Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache AppContext switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
- **Do not cache `typeof` expressions in .NET Core.** `typeof(...)` is JITed into a constant; caching it is a de-optimization. Similarly, don't store `ArrayPool.Shared` in variables—it breaks devirtualization.
- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs. Use `ValueListBuilder` on hot paths.
- **Use `sizeof` consistently.** A pass removed calls to the equivalent `Unsafe` helper; do not reintroduce them. Use `sizeof` rather than `Marshal.SizeOf` for blittable structs; it is more correct and significantly faster when no marshalling is involved.
- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and optimizes it. Slice spans before iterating to avoid per-element bounds checks.
- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps. Output must be deterministic with Ordinal-sorted lists.
- **Use `ValueListBuilder` for dynamic array building in BCL.** Use `ValueListBuilder<T>` (with pooling) or `ArrayBuilder<T>`. Use stackalloc for small sizes, array pool when too large.

## API Design & Contracts

- **New public APIs require approved proposals before PR submission.** All new API surface must go through API review. PRs adding unapproved APIs will be closed. The implementation should match what was approved, though it is explicitly allowed to defer portions of an approved API to incremental follow-up PRs, and an implementor may opt to exclude specific API members for technical reasons without needing re-approval unless the exclusion significantly impacts the design. When new public API surface is detected, the API approval verification procedure (`.github/skills/code-review/api-approval-check.md`) is executed to enforce this rule.
- **Use `internal` for new APIs pending API review.** If the API is needed immediately for implementation, mark it `internal` and file a review request separately.
- **Parameter names must match between ref and src.** Renaming a public API parameter (including case changes) is a breaking change affecting named arguments and late-bound scenarios.
- **Align exception types and validation order across platforms.** Validate arguments first (`ArgumentNullException`, then `ArgumentException`), then `PNSE`, then `ObjectDisposedException`, then perform the operation. Throw the same exception types on all platforms.
- **`Try` APIs should return `false` only for the common expected failure.** Throw for everything else (corruption, permissions, invalid arguments). Try methods must always throw on invalid arguments.
- **Don't expose mutable options after construction.** If values are captured at construction time, don't expose a mutable options object. Don't reference private field names or internal types in user-facing error messages.
- **Use `PlatformNotSupportedException` for platform limitations.** When an operation can't complete in the current environment but could on a different platform, throw PNSE. Don't impose artificial limits beyond OS capabilities.
- **.NET APIs should compensate for platform quirks.** Public APIs should work consistently across platforms. When adding overloads, check F# compatibility for implicit conversion or type inference ambiguities.
- **Follow the obsoletion process for deprecated APIs.** Pick the next available SYSLIB diagnostic ID, add `[Obsolete]`, and use `[EditorBrowsable(Never)]` with `[OverloadResolutionPriority(-1)]` for overload fixes.
- **New virtual methods must work with unoverridden derived types.** The default implementation must behave identically to calling the pre-existing equivalent APIs.
- **Avoid non-CLS-compliant integer types in public APIs.** Preserve `byte`, `int`, or `long` according to the required range; `byte` is valid even though it is unsigned. Use named types instead of `ValueTuple` across file boundaries.

## Code Style & Formatting

- **Use well-named constants instead of magic numbers.** No raw hex or decimal constants without explanation. Don't duplicate magic constants across files.
- **Use `var` only when the type is apparent from the right-hand side.** "Apparent" means the type is visible as a literal, constructor (`new Foo()`), or explicit cast — not merely "obvious from context." For example, `var x = y.ToString()` is not considered apparent because it's neither a literal nor a constructor. Follow the `.editorconfig` rules for `var` usage. Never use `var` for numeric types.
- **Use PascalCase for constants; descriptive names for booleans.** All constant locals and fields use PascalCase (except interop constants matching external names). Boolean fields should be positive and descriptive (`_hasCurrent` not `valid`).
- **Name methods to accurately reflect their behavior.** Update names when behavior changes. `Get*` implies a return value; use `Print*/Display*` for void. `ThrowIf` not `ThrowExceptionIf`.
- **Prefer early return to reduce nesting.** Use early returns for short/error cases to avoid unnecessary nesting. Put the error case first, success return last.
- **Avoid `using static` and `#region` in new code.** `using static` is costly when reading code outside IDEs (e.g., GitHub review). `#region` gets out of date quickly.
- **Place local functions at method end, fields first in types.** Local functions go at the end of the containing method. Fields are the first members declared in a type.
- **Narrow warning suppression to smallest scope.** Avoid file-wide `#pragma` suppressions. Disable only around the specific line that triggers the warning.
- **Use pattern matching and `is`/`or`/`and` patterns.** Prefer `is` patterns and C# pattern matching over manual type checks and comparisons. Use named parameters for boolean arguments.
- **Do not initialize managed fields to default values (CA1805).** The CLR zero-initializes all fields in managed code. Explicit `= false`, `= 0`, `= null` is redundant. (This does not apply to native C/C++ code, where fields and locals must be explicitly initialized.)
- **Sealed classes do not need the full Dispose pattern.** A simple `Dispose()` is sufficient since no derived class can introduce a finalizer.

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts. Separate endianness-specific reads from target-endianness reads.
- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` and cross-platform APIs (`.Shuffle`, `.Min`) over `Avx512BW`, `SSE2`. Use the bit manipulation APIs exposed directly on numeric types (e.g., `int.PopCount`, `long.LeadingZeroCount`) rather than `BitOperations` for portable bit manipulation.
