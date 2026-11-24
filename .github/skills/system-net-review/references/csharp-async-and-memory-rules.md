# C# Async, Concurrency, Lifetime & Memory Rules

Checks for any changed `.cs` file in networking code: async and cancellation, races, resource lifetime, stream contracts, allocations, integer bounds, and untrusted-input limits.

---

## Async, Cancellation & Task Completion

| Check | What to look for |
|-------|-----------------|
| **Flow the CancellationToken end to end** | A public async method accepts a token but inner `ReadAsync`/`WriteAsync`/`ConnectAsync`/`WaitAsync` calls drop it, or a pre-canceled token still starts I/O. `WaitAsync(token)` only stops waiting; the underlying operation keeps running and must be canceled or observed. Pass the token to every inner call, check `ThrowIfCancellationRequested` before side effects, use `UnsafeRegister` for internal registrations and dispose the `CancellationTokenRegistration` once the operation completes. (`#67954`, `#113409`, `#72338`) |
| **No sync-over-async or gratuitous Task.Run** | `.GetAwaiter().GetResult()`/`.Wait()` on an async path, blocking sync I/O inside an `async` method, or `Task.Run` wrapping work that is already async. These starve the thread pool and deadlock under sync contexts; sync and async paths that diverge get bug fixes in only one place. Share one core with a `bool async` parameter, call the sync API on the sync path, and keep validation identical in both paths. (`#134260`, `#65854`, `#87108`) |
| **TaskCompletionSource runs continuations asynchronously** | `new TaskCompletionSource(...)` without `TaskCreationOptions.RunContinuationsAsynchronously` completed from a MsQuic/native callback, a timer, or while holding a lock. `SetResult` then runs arbitrary awaiter code inline on that thread, re-entering locks or blocking the callback. Always pass `RunContinuationsAsynchronously` for TCSs completed by infrastructure code, and never replace a TCS that callers may already be awaiting (use `??=` or a final-state check). (`#104835`, `#129302`, `#101531`) |
| **Suppress ExecutionContext for long-lived timers and threads** | A `Timer`, `Thread`, or long-lived background loop created lazily inside a user request captures that request's `ExecutionContext` and `AsyncLocal`s, leaking them (and whatever they root) for the lifetime of the timer and attributing work to the wrong caller. Create it under `ExecutionContext.SuppressFlow()` (restore in `finally`, guarding with `IsFlowSuppressed()`), or use `UnsafeRegister`/`UnsafeStart`-style APIs. (`#111791`) |
| **Consume ValueTask once; observe every fault** | Awaiting a `ValueTask` twice, storing it, or calling `.Result` before completion; fire-and-forget `Task`s or `async void` whose exceptions go unobserved; returning `default(ValueTask)` where a real result is required. Await a `ValueTask` exactly once (or `.AsTask()` it), attach a fault-logging continuation to deliberate fire-and-forget work, and avoid adding async state machines for trivial pass-through methods. (`#105841`, `#107096`, `#69272`) |

---

## Concurrency & Shared State

| Check | What to look for |
|-------|-----------------|
| **One synchronization story per shared field** | A field written by `Read`/`Write`/`Dispose`, a timer callback, or a native callback on another thread with no lock, `volatile`, or `Interlocked`; `bool?`/packed bit flags updated non-atomically; `volatile` removed from double-checked locking. Torn or stale reads corrupt connection state or double-send. Document the owning thread, or use `Interlocked.CompareExchange`/`Volatile.Read` consistently on every access. (`#124200`, `#100513`, `#76361`) |
| **Check-then-act must be atomic** | `if (!dict.ContainsKey(k)) dict[k] = ...`, "is disposed? then use" sequences, or returning a pooled work item/handle before the operation that uses it finishes. Another thread can interleave between check and act, causing double-free or use-after-return. Use `TryAdd`/`GetOrAdd`/`TryRemove` results, a single `Interlocked` state transition, or refcounted handles, and return pooled objects only after completion. (`#99371`, `#124374`, `#117950`) |
| **Lock scope matches the invariant** | Some reads/writes of a guarded field happen outside the lock, the lock covers less than the multi-field invariant, user callbacks or exception construction run under the lock, or a `SemaphoreSlim`/lock is added where single-owner or `Interlocked` suffices. Hold the lock for the whole invariant, never call out to user code while holding it, and remove locks that protect nothing. (`#111791`, `#67651`, `#62037`) |

---

## Lifetime & Disposal

| Check | What to look for |
|-------|-----------------|
| **Dispose every owned IDisposable** | Undisposed `CancellationTokenSource`, `HttpResponseMessage`, `Stream`, `Socket`, `X509Certificate2`, hash object, or WASI pollable at scope exit or in the owner's `Dispose`. Leaked timers/handles pin native resources and connections. Use `using`/`await using` declarations for locals and dispose owned fields in `Dispose`. (`#94211`, `#84327`, `#107096`) |
| **Release partial state when initialization throws** | A constructor or multi-step setup allocates native memory, a handle, or a rented array and then a later step throws before ownership is handed off; or output handles are assigned before all error checks pass. Wrap setup in `try`/`catch` that disposes what was acquired and rethrows, and publish results only after success. (`#125220`, `#68288`, `#69301`) |
| **Dispose pattern is idempotent and complete** | `Dispose(bool)` that releases unmanaged state only when `disposing` is true (leaking on finalization), throws on second call, schedules cleanup that can run after `Dispose` returns, or adds a finalizer that is not needed. Release native state on both paths, make `Dispose` idempotent with an `Interlocked.Exchange` flag, and avoid finalizers unless the type directly owns an unmanaged resource. (`#69527`, `#87966`, `#90503`) |
| **Honor leaveOpen and check disposed state** | A wrapper closes a caller-owned inner stream when `leaveOpen` is true, double-disposes it, assumes ownership of a user-supplied certificate context, or keeps operating after disposal. State ownership explicitly, dispose only what you own, and guard public members with `ObjectDisposedException.ThrowIf`. (`#117667`, `#115826`, `#95001`) |

---

## Streams

| Check | What to look for |
|-------|-----------------|
| **Respect partial, zero-byte, and seek contracts** | Code that assumes `Read` fills the buffer, treats a 0-byte read as EOF when the caller passed an empty buffer, routes 0-byte reads through buffering, assumes `Length`/`Position` work without `CanSeek`, or overrides only some read/write overloads so others fall back to slow or wrong base behavior. Loop with `ReadAtLeast`/`ReadExactly`, return 0 only at EOF with no partial frame, check `CanSeek`, and override `ReadByte`, `Read(Span)`, and `ReadAsync(Memory)` together. (`#63184`, `#126669`, `#61913`) |

---

## Allocation & Span Usage

| Check | What to look for |
|-------|-----------------|
| **Reuse vectorized BCL text and span APIs** | Hand-written char/byte loops, ASCII/UTF-8 checks, or search loops that duplicate `IndexOfAny`, `SearchValues<T>`, `Ascii.*`, `Utf8.IsValid`, `char.IsAsciiDigit`, or `string.Concat`; culture-sensitive comparisons on protocol tokens. Use the BCL helper (vectorized and tested) and `StringComparison.Ordinal`/`OrdinalIgnoreCase` for headers, schemes, hosts, and RIDs. (`#121297`, `#102805`, `#82095`) |
| **No allocation on hot or disabled-telemetry paths** | Eager allocation of options, `SR.Format` messages, lists, closures, boxed enums/structs, LINQ, interface enumerators, or tag arrays on every request even when the fast path or disabled `ILogger`/`EventSource`/`Meter` does not need them. Allocate lazily, guard with `IsEnabled()`, use `static` lambdas with state arguments, presize collections, and use `Array.Empty<T>()`. (`#65249`, `#63012`, `#61530`) |
| **Slice spans instead of copying** | `ToArray()`, `Substring`, `Split`, `Trim`, `GetBytes()` into a temporary array, or `(array, offset, count)`/`Buffer.BlockCopy` plumbing where downstream code accepts `ReadOnlySpan<T>`. Each copy allocates and duplicates bounds logic. Pass `AsSpan()`/`Slice()` and `CopyTo`, and parse directly from spans. (`#102917`, `#63641`, `#102144`) |
| **Scratch buffers: stackalloc small, pool large, return once** | `new byte[N]` for tiny fixed scratch, `StringBuilder` where `ValueStringBuilder`/`string.Create` fits, a `ValueStringBuilder` never disposed, or `ArrayPool` rentals that assume a zeroed or exact-size array, are returned twice, or leak on throw. `stackalloc` a constant-sized buffer under a threshold with a pooled fallback, slice rented arrays to the requested length, clear sensitive or handle-bearing rentals, and return in `finally`. (`#68288`, `#87874`, `#82779`) |
| **Keep span code JIT-friendly** | `in ReadOnlySpan<T>` parameters, caching `span.Length` in locals (defeats bounds-check elimination), collection expressions capturing a local by value instead of by ref, or separate heap arrays where `[InlineArray]`/`MemoryMarshal` fits. Pass spans by value, index against `span.Length`, check codegen for byref tricks, and order fields to avoid padding. (`#102805`, `#116111`, `#110864`) |
| **Avoid quadratic scans and whole-payload buffering** | Repeated linear lookups inside loops, double passes over header values, re-enumerating dictionaries into arrays, or buffering an entire stream when it could be processed incrementally. Use single-pass algorithms, `CollectionsMarshal.GetValueRefOrAddDefault`, and streaming APIs. (`#105647`, `#62981`, `#64369`) |

---

## Integer Arithmetic, Bounds & Untrusted Input

| Check | What to look for |
|-------|-----------------|
| **Bounds checks cannot overflow or truncate** | `offset + count > length` with signed ints, `length << 3`/`count * size` wrapping, 64-bit lengths cast to `int`, `index == -1` instead of `< 0`, or `size_t` narrowed to 32 bits in a native shim. Use `(uint)index >= (uint)length` and `(ulong)offset + (uint)count > (uint)length` idioms, `checked` arithmetic that surfaces as `ArgumentOutOfRangeException`, and `NativeMemory.Alloc(count, size)`. (`#62074`, `#88970`, `#68288`) |
| **Validate a private copy, not caller-mutable state** | Validating the length of a caller's array, list, `IPEndPoint`, or file and later passing the same mutable object to a native call or I/O. The caller (or another thread) can change it between check and use, bypassing the check. Copy into a local/span or snapshot the value (e.g. read file length after opening the handle), validate the copy, and use only the copy. (`#120634`, `#100898`, `#129302`) |
| **Cap peer-controlled growth** | Queues, caches, line buffers, write buffers, or failed-handshake exceptions that grow with untrusted peer input without a limit. A malicious peer amplifies small input into unbounded memory or CPU. Enforce explicit maximums (bounded channels, max line/frame length, cache size) and drop or log instead of throwing per bad attempt. (`#82261`, `#95001`, `#102238`) |
