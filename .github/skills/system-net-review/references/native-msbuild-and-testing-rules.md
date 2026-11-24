# Native Interop, MSBuild, and Testing Rules

Rules for P/Invoke and handle lifetime, the C PAL and CMake, project/ref/trimming/WASM wiring, and networking tests; load when `Interop/**`, `.c`/`.h`/`CMakeLists.txt`, `.csproj`/`.props`/`.targets`/`ref/**`, or `**/tests/**` change.

---

## Interop & Handle Lifetime

| Check | What to look for |
|-------|-----------------|
| **No exceptions escape `[UnmanagedCallersOnly]`** | Any code in an `[UnmanagedCallersOnly]` callback (OpenSSL/Network.framework/MsQuic/DNS completion) that can throw outside a `try`/`catch`: handle lookups, `GCHandle.Target` casts, logging. An escaping exception is an unhandled crash of the process on Unix. Wrap the whole body in `try`/`catch`, stash the exception for the managed caller, and return an error code to native. (`#117950`, `#115996`) |
| **Pin before taking a pointer** | `Unsafe.AsPointer(ref array[0])`, `&MemoryMarshal.GetReference(span)` without `fixed`, or a struct holding a pointer into a managed array. The GC can move unpinned memory while native code uses it: a silent GC hole that corrupts memory. Use `fixed`/`GCHandle.Alloc(Pinned)`/native memory, size the pointer from the same span it came from, and avoid `Unsafe.Add` with negative or mixed-sign offsets. (`#63674`, `#69098`, `#89841`) |
| **SafeHandle, never raw `IntPtr` or fd** | Passing `handle.DangerousGetHandle()`, a cached `IntPtr`, or an int fd to native code while another thread can dispose the owner. The OS recycles the value and the call hits an unrelated handle; finalization can also close it mid-call. Pass the `SafeHandle` to `LibraryImport` (it ref-counts), keep owners alive with `GC.KeepAlive`, and close only after native work completes. (`#109052`, `#72000`, `#103364`) |
| **Balance `DangerousAddRef`/`DangerousRelease`** | `DangerousAddRef` outside `try`, `DangerousRelease` not in `finally`, release without checking the `ref bool success` flag, or a finalizer that releases a handle whose add-ref may have thrown. Leaks keep handles open forever; extra releases free in-use native objects. Use `bool added = false; try { h.DangerousAddRef(ref added); ... } finally { if (added) h.DangerousRelease(); }`. (`#97348`, `#69527`, `#130758`) |
| **GCHandle lifetime matches the native callback** | `GCHandle.Free` while a native callback can still fire, weak handles never freed, `Target` used after `Free`, or untyped `GCHandle` where `GCHandle<T>`/`WeakGCHandle<T>` fits. A freed or recycled handle dispatches to the wrong object. Free only after native signals completion (typically in `ReleaseHandle`), and prefer the typed handle APIs. (`#104835`, `#99357`, `#129302`) |
| **Non-null pointer for empty buffers** | `fixed (byte* p = span)` yields `null` for an empty span; many native APIs (OpenSSL, MsQuic, CNG) treat `NULL` as an error or as "absent" rather than zero-length. Use `MemoryMarshal.GetNonNullPinnableReference`/the shared helper when native requires a valid pointer, or relax the check in our own shim; never assume `0`-length and `NULL` mean the same. (`#98958`, `#110864`) |
| **`LibraryImport` signatures match native** | Non-blittable parameters forcing marshalling, custom marshallers where `IntPtr`/`SafeHandle` suffices, `Marshal.SizeOf` on blittable structs instead of `sizeof`, `SetLastError = true` without reading `Marshal.GetLastPInvokeError()` immediately, or chatty one-call-per-native-function shims. Keep signatures blittable and typed (`byte*`, `nint`), and batch logical operations into one shim call. (`#74695`, `#73191`, `#128245`) |

---

## C PAL & CMake

| Check | What to look for |
|-------|-----------------|
| **Check every syscall result and retry `EINTR`** | Unchecked `socket`/`setsockopt`/`pthread_*` returns, `errno` read after another call clobbered it, hard-coded errno values, busy-wait init loops instead of `pthread_once`, `int` used for `size_t`/`socklen_t`, and out parameters left uninitialized on error or empty paths. Loop on `EINTR`, map errors through the PAL `Error_*` codes, use fixed-width types, set every out parameter, and never rely on `assert` as a release NULL guard. (`#63963`, `#62780`, `#117417`, `#117016`) |
| **Probe features and keep vendored code upstream** | `#ifdef __linux__`/target checks instead of CMake `check_symbol_exists`/`HAVE_*` probes, misspelled `HAVE_*` macros, `find_library` in the wrong layer, or edits to vendored/generated files (`msquic.cs`, `*.xml.cs`, external C sources). Probes keep ports working; local edits are lost on the next sync. Fix the generator or upstream, bump the `*-version.txt`, and update `THIRD-PARTY-NOTICES.TXT` when vendoring new code. (`#105317`, `#106977`, `#93125`, `#78591`) |
| **Keep OS behavior in parity** | Logic implemented for Linux but not macOS/BSD/Android/iOS, or Windows semantics assumed on Unix (socket options, cert stores, address families), without a guard or `PlatformNotSupportedException`. Users get silently different behavior per OS. Implement or explicitly reject on every supported platform and test each. (`#115254`, `#107351`) |

---

## MSBuild, ref/, Trimming, AOT & WASM

| Check | What to look for |
|-------|-----------------|
| **Idiomatic MSBuild and project wiring** | Hard-coded package versions instead of central versions, per-project `NoWarn`, redundant or wrong `TargetOS`/RID conditions, string compares instead of `$([MSBuild]::GetTargetFrameworkIdentifier())`, shared sources not linked from `Common`, CI YAML path globs that miss the change, or Helix/Docker images out of sync. Follow existing project patterns and run the affected leg locally. (`#78354`, `#64500`, `#80149`) |
| **Builds on every TFM and configuration** | APIs, `using`s, or fields outside `#if NET`/`#if DEBUG`/OS `#if` — including shared test files vs. `*.Net.cs` splits in multi-targeted test projects (`$(NetFrameworkCurrent)` / `netstandard*`) — that break downlevel or Release builds; dropped downlevel `TargetFramework`s; analyzer/ILLink warnings suppressed instead of fixed; orphaned `Strings.resx` entries or `#pragma`s. Build all TFMs without `-f` (both `src/` and multi-TFM `tests/`) before finishing. (`#124374`, `#104165`, `#93405`) |
| **`ref/` matches the approved API** | `ref/*.cs` hand-edited out of order, signatures, parameter names, or nullability that differ from the `api-approved` shape, or new public members/`[Obsolete]` with no approved proposal. Generate ref source with the documented tooling, mirror the approved shape exactly, and update `PACKAGE.md`/README samples for shipped packages. (`#64307`, `#83713`, `#63690`) |
| **Trim, AOT, and WASM safe** | Reflection without `[DynamicallyAccessedMembers]`/`[RequiresUnreferencedCode]`, `IlcTrimMetadata` or ILLink descriptors masking real warnings, generic instantiations that bloat AOT size, missing `PlatformManifest`/NativeAOT native-library entries, or Browser/WASI code that assumes threads without `FEATURE_WASM_MANAGED_THREADS`/`WasmEnableThreads` guards. Annotate instead of suppressing and test trimmed/AOT/WASM legs. (`#90013`, `#71440`, `#66333`, `#124959`) |

---

## Testing

| Check | What to look for |
|-------|-----------------|
| **Cover the fix and its edges** | Bug fixes without a regression test reproducing the exact failure; new public overloads/options untested; single-direction boolean/state tests (`false -> true` without `true -> false`, or single-connection pool checks missing `low -> high -> new connection`); parser/validator crash tests that assert only "did not throw" without checking positive parsed outputs; missing sync/async, HTTP/1.1/2/3, proxy, disposal-mid-operation, malformed-input, empty/zero-length, and split-read variants. Add the failing scenario first, bidirectional state/boundary cases, and positive output assertions. Build test inputs (certificates, frames, payloads) in code rather than adding opaque binary assets. (`#97911`, `#116098`, `#62074`) |
| **Assertions that can actually fail** | Setup that bypasses the validation under test, ignored `Try*`/`OperationStatus` results, `Assert.True(x == y)` instead of `Assert.Equal`, `as`+`NotNull` instead of `Assert.IsType`, exceptions swallowed or tasks left unobserved, missing `ParamName`/`SocketErrorCode` checks, or asserts on English `Exception.Message`. Use specific xUnit helpers and compare against the `SR` resource when the message is contractual. (`#67859`, `#72095`, `#108670`) |
| **Skip with attributes, isolate with `RemoteExecutor`** | `if (!IsSupported) return;` inside the body (test reports pass), overly broad `[PlatformSpecific]`/`[SkipOnPlatform]`, `[ActiveIssue]` without an issue URL, misapplied `[OuterLoop]`, or tests mutating `AppContext` switches, env vars, or static caches in-process. Use `[ConditionalFact]`/`[ConditionalTheory]`, narrow guards, and `RemoteExecutor` for process-wide state. (`#71385`, `#65737`, `#104352`) |
| **No sleeps, fixed ports, or races** | `Task.Delay`/`Thread.Sleep` as synchronization, tight timeouts that flake on slow CI machines, hard-coded ports, barriers that hang when a peer fails, or fire-and-forget tasks. Synchronize with `TaskCompletionSource`/loopback-server signals, bind to port `0`, and await every background task with a generous timeout. (`#89258`, `#62935`, `#94664`) |
| **Theories, shared helpers, scenario names** | Monolithic `[Fact]`s exercising many scenarios, copy-pasted tests that differ only in inputs, duplicated loopback-server boilerplate, or vague names (`BehavesCorrectly`). Use `[Theory]` with simple-typed `[InlineData]`/`[MemberData]`, reuse `Common/tests` helpers, and name tests `Operation_Condition_Expected`. (`#74953`, `#126669`, `#88773`) |
