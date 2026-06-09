# Platform, Cross-Platform & Native Interop

_Rules for cross-platform portability, native C++ style, runtime/VM patterns, and P/Invoke marshalling. Part of the [code-review skill](../SKILL.md)._

## Platform & Cross-Platform

- **Use `BinaryPrimitives` for endianness-safe reads.** Use `ReadInt32LittleEndian`/`BigEndian` rather than pointer casts. Separate endianness-specific reads from target-endianness reads.
- **Use cross-platform vector APIs over ISA-specific intrinsics.** Prefer `Vector128/256/512.IsHardwareAccelerated` and cross-platform APIs (`.Shuffle`, `.Min`) over `Avx512BW`, `SSE2`. Use `BitOperations` for portable bit manipulation.
- **Use correct platform/feature defines.** Use `TARGET_*`/`HOST_*` defines rather than compiler-provided defines (`__wasm__`). Use `HOST_*` for build machine code, `TARGET_*` for target platform. Use `PORTABILITY_ASSERT` for unimplemented platform code.

## Native C++ Style

- **Don't use `auto` in the runtime C++ codebase.** Use explicit types. Exception: unspeakable types like lambdas.
- **Use `nullptr`, `void*`, and native C++ types over legacy aliases.** Prefer `nullptr` over `NULL`, `void*` over `LPVOID`. Use `WCHAR` (not `wchar_t`) in Windows host code. Use `.inc` suffix for multiply-included files.
- **Match `#endif` comments to `#ifdef` exactly.** Add comments on `#else`/`#endif` for non-trivial blocks. Consistent brace placement and four-space indentation.
- **Prefer `static_cast` over C-style casts.** C-style casts are more permissive than needed and can silently degrade to `reinterpret_cast`.

## Runtime & VM Patterns

- **Use correct VM contracts and QCall patterns.** QCalls that may throw need `BEGIN_QCALL`/`END_QCALL`. Simple QCalls use `QCALL_CONTRACT_NO_GC_TRANSITION`. All VM methods need `STANDARD_VM_CONTRACT` or `WRAPPER_NO_CONTRACT`.
- **Keep GC protection correct around managed references.** Ensure all GC references are `GCPROTECT`-ed before GC-triggering calls. After GC-triggering calls, use `ObjectFromHandle(handle)` for a fresh reference.
- **Avoid dynamic allocation on fatal error paths.** Use stack-allocated buffers. Use simple synchronization (Interlocked with spin-wait) instead of Monitor/lock.
- **Avoid thread-local objects with destructors in CoreCLR.** Destruction order is arbitrary. Tie lifetime to the CoreCLR Thread object. Prefer `PLATFORM_THREAD_LOCAL` from minipal over C++ `thread_local` in perf-critical paths.
- **Use `SET_UNALIGNED` macros for potentially unaligned writes.** In code generation stubs, use `SET_UNALIGNED_32/64` rather than direct pointer dereferencing.
- **Zero-initialize arrays and buffers that may be partially used.** Zero-init allocated arrays whose elements have destructors. Zero-init EH tables, C arrays, and similar structures.
- **Add static asserts for hardcoded structural offsets.** When using hardcoded offsets to access struct fields (especially in assembly), add static asserts to verify them.
- **Use minipal for new platform abstractions.** Use minipal (new) instead of PAL (legacy) for platform abstraction in new CoreCLR code. Use `ALTERNATE_ENTRY` (not `LOCAL_LABEL`) for assembly labels called from outside their function.
- **Use `JITDUMP` and `LOG` macros, not `printf`.** In JIT code use `JITDUMP`. In CoreCLR VM use `LOG()`/`LOGGING` defines. Do not use `printf` or `Console.WriteLine` in production native code.

## P/Invoke & Marshalling

- **Prefer 4-byte `BOOL` for native interop marshalling.** Use `UnmanagedType.Bool`. Verify P/Invoke return types match native signatures exactly—mismatches may work on 64-bit but fail on 32-bit/WASM.
