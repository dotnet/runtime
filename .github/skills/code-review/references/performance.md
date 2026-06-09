# Performance & Allocations

_Rules for performance measurement, allocation avoidance, code structure for performance, and performance-sensitive API choices. Part of the [code-review skill](../SKILL.md)._

## Measurement & Evidence

- **Performance changes require benchmark evidence.** Include BenchmarkDotNet or EgorBot numbers before merging. Validate with real-world scenarios, not just microbenchmarks.
- **Justify binary size increases with real-world measurements.** Changes that increase binary size require measured wall-clock improvements on real-world apps, not just instruction counts.
- **Avoid premature optimization with object pools and caches.** Do not introduce global caches or object pools without evidence they are needed. Prefer making the underlying operation faster.

## Allocation Avoidance

- **Avoid closures and allocations in hot paths.** When a lambda captures locals creating a closure, consider using a static delegate with a state parameter (value tuple). Avoid string concatenation; use span-based operations.
- **Pre-allocate collections when size is known.** Pass capacity to `Dictionary`, `HashSet`, `List` constructors when the expected count is available.
- **Structs in dictionaries need `IEquatable<T>` and `GetHashCode`.** Without these, the runtime falls back to boxing allocations for equality comparison.
- **Avoid Pinned Object Heap for non-permanent objects.** POH is never compacted and effectively gen2. Only use for objects surviving as long as the process.
- **Suppress `ExecutionContext` flow for infrastructure timers.** When allocating `Timer` or similar background infrastructure, suppress EC flow to avoid capturing unrelated `AsyncLocal`s that leak memory.

## Code Structure for Performance

- **Place cheap checks before expensive operations.** Order conditionals so cheapest/most-common checks come first. Move expensive work after early-exit checks.
- **Allocate resources lazily where possible.** Allocate expensive resources on first use, not during initialization. Avoid forcing type initialization during startup.
- **Extract throw helpers into `[DoesNotReturn]` methods.** Move throwing logic from error paths into separate static local functions or helper methods to allow the JIT to inline the success path.
- **Avoid O(n²) patterns in collections and hot paths.** Watch for linear scans inside loops, repeated `RemoveAt` in loops. Use `RemoveAll`, single-pass restructuring, or appropriate data structures.
- **Cache repeated accessor calls in locals.** Store the result of repeated property/getter calls in a local variable.
- **Separate hot data from rarely-used data in runtime structures.** Keep frequently accessed data inline; move rarely-used data (GCInfo, DebugInfo) to separate structures.
- **Compute constant data at compile time, not execution time.** In interpreter and similar hot paths, pre-compute metadata lookups and type checks during the compilation phase.
- **Consider scalability, not just throughput.** Evaluate whether data structures, caches, and locking strategies will hold up at high cardinality or under concurrent load. Watch for unbounded collection growth, lock contention that worsens with core count, and O(1) assumptions that break at scale.

## Specific API Choices

- **Use `AppContext.TryGetSwitch` with a static readonly property.** Cache AppContext switches in `static bool Prop { get; } = AppContext.TryGetSwitch(...)` so the JIT can dead-code-eliminate unreachable paths.
- **Do not cache `typeof` expressions in .NET Core.** `typeof(...)` is JITed into a constant; caching it is a de-optimization. Similarly, don't store `ArrayPool.Shared` in variables—it breaks devirtualization.
- **Use `CollectionsMarshal` for large value-type dictionary lookups.** Use `GetValueRefOrAddDefault` or `GetValueRefOrNullRef` to avoid copying large structs. Use `ValueListBuilder` on hot paths.
- **Use `sizeof` instead of `Marshal.SizeOf` for blittable structs.** `sizeof` is more correct and significantly faster when no marshalling is involved.
- **Use the idiomatic `(uint)index >= (uint)length` bounds check.** The JIT recognizes this pattern and optimizes it. Slice spans before iterating to avoid per-element bounds checks.
- **Source generators must be properly incremental.** Do not store Roslyn symbols (`ISymbol`, `Compilation`) in incremental pipeline steps. Output must be deterministic with Ordinal-sorted lists.
- **Avoid LINQ and records in low-level compiler codebases.** In CG2/ILC and AOT tools, use direct loops instead of LINQ and readonly structs instead of records. Use concrete types over interfaces in private code.
- **Use `ValueListBuilder` for dynamic array building in BCL.** Use `ValueListBuilder<T>` (with pooling) or `ArrayBuilder<T>`. Use stackalloc for small sizes, array pool when too large.
