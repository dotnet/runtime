# Correctness & Safety

_Rules for error handling, thread safety, security, general correctness, and JIT-specific correctness. Part of the [code-review skill](../SKILL.md)._

## Error Handling & Assertions

- **Use `Debug.Assert` for internal invariants, not exceptions.** For internal-only callers, assert assumptions rather than throwing `ArgumentException`. Prefer `Debug.Assert(value != null)` over the null-forgiving operator (`!`).
- **Use `throw` for reachable error paths, `UnreachableException` for exhaustive switches.** When a code path might be hit at runtime, throw an exception rather than asserting. Use `throw new UnreachableException()` for default cases in exhaustive switches. Use `PlatformNotSupportedException` (not `NotSupportedException`) for platform gaps. In native code, use `_ASSERTE(!"message")`.
- **Include actionable details in exception messages.** Use `nameof` for parameter names. Include the unsupported type or unexpected value. Never throw empty exceptions.
- **Initialize output parameters in all code paths.** When a method has `out` parameters or pointer outputs (`bytesWritten`, `numLocals`), ensure they are initialized to a defined value in all error paths.
- **Handle OOM with exceptions or fail-fast, never asserts.** Use `ThrowOutOfMemory` or `EEPOLICY_HANDLE_FATAL_ERROR`, not asserts. In interpreter loops, use `nothrow new` and check for null.
- **Use `ThrowIf` helpers over manual checks.** Use `ArgumentOutOfRangeException.ThrowIfNegative`, `ObjectDisposedException.ThrowIf`, etc. instead of manual if-then-throw patterns.
- **Challenge exception swallowing that masks unexpected errors.** When a PR adds try/catch blocks that silently discard exceptions (`catch { continue; }`, `catch { return null; }`), question whether the exception represents a truly expected, recoverable condition or an unexpected error signaling a deeper problem (race conditions, memory corruption, build environment issues). Silently catching exceptions that "shouldn't happen" hides root causes and makes debugging harder. The default disposition should be to let unexpected exceptions propagate or fail fast so the real issue gets investigated.

## Thread Safety

- **Use `Volatile` or `Interlocked` for cross-thread field access.** Fields written on one thread and read on another must use `Volatile<T>`, `Volatile.Read/Write`, or `Interlocked`. The `??=` operator is not thread-safe. `Nullable<T>` is not safe for caching (two-field struct tears). Do not use shared mutable arrays without synchronization.
- **Use `TickCount64` for timeout calculations.** Use `Environment.TickCount64` (long) instead of `Environment.TickCount` (int) to avoid integer overflow.

## Security

- **Guard integer arithmetic against overflow.** Guard size computations involving multiplication (e.g., `newCapacity * sizeof(T)`) against integer overflow. Use patterns correct by construction.
- **Clean sensitive cryptographic data after use.** Always clear key material with `CryptographicOperations.ZeroMemory`. When using `PinAndClear` but copying to another buffer, clear the original too. Use non-short-circuit operators (`|`) in verification code to prevent timing leaks.
- **Don't proactively send credentials without opt-in.** Never send authentication credentials (especially Basic auth) before receiving a challenge.
- **Limit `stackalloc` to ~1KB and validate size.** Don't stackalloc based on user-controlled or large input sizes. Move stackalloc to just before usage, not before early returns.

## Correctness Patterns

- **Fix root cause, not symptoms or workarounds.** Investigate and fix the root cause rather than adding workarounds or suppressing warnings. Revert broken commits before layering fixes.
- **Prefer safe code over unsafe micro-optimizations.** Do not introduce `Unsafe.As`, `Unsafe.AsRef`, or raw pointers without demonstrable performance need. Prefer Span-based APIs. If performance is the issue, prefer fixing the JIT.
- **Use `Unsafe.BitCast` for same-size type punning.** Prefer `Unsafe.BitCast<TFrom, TTo>` over `Unsafe.As<TFrom, TTo>` for type punning between value types of the same size.
- **Delete dead code and unnecessary wrappers.** Remove dead code, unnecessary wrappers, obsolete fields, and unused variables when encountered or when the only caller changes.
- **Handle `SafeHandle.IsInvalid` before `Dispose`.** Check `IsInvalid` (not null) on returned SafeHandles. Get the exception before calling `Dispose`, since Dispose might clear the error state.
- **Seal classes when `Equals` uses exact type matching.** If a class implements `Equals` with `GetType()` comparison, seal the class to prevent subtle inheritance bugs.
- **Use `Environment.ProcessPath` and `AppContext.BaseDirectory`.** Use these instead of `Process.GetCurrentProcess().MainModule?.FileName` and `Assembly.Location` for NativeAOT/single-file compatibility.
- **File name casing must match csproj references exactly.** Linux is case-sensitive. New source files must be listed in the `.csproj` if other files in that folder are explicitly listed.
- **Prefer correct-by-construction designs.** Prefer designs that are correct by construction (e.g., scanning IL) over manually maintained parallel data structures. A missed optimization is better than silent bad codegen.
- **Allocate on the correct loader allocator for collectibility.** When allocating runtime data structures for generic instantiations, use the correct loader allocator accounting for collectibility of type arguments.
- **Backport targeted fixes, not refactorings.** When backporting to servicing branches, create small targeted fixes. Backporting large refactorings introduces unnecessary risk.

## JIT-Specific Correctness

- **JIT lowering must not double-lower nodes.** Never call `LowerNode` on an already-lowered node. Return newly created nodes for the caller to lower. Constant folding belongs in import/morph, not lowering.
- **Mark collectible ALC test methods `NoInlining`.** Methods that touch collectible assembly load contexts must be `[MethodImpl(MethodImplOptions.NoInlining)]` to prevent the JIT from keeping references alive.
