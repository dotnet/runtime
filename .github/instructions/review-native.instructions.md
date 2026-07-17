---
applyTo: "**/*.c,**/*.cc,**/*.cpp,**/*.cxx,**/*.h,**/*.hpp,**/*.inc,**/*.S,**/*.s,**/*.asm"
---

# Code Review -- Native code (C/C++/asm) & interop

Rules for reviewing native runtime code (CoreCLR VM, JIT, `src/native`, Mono native). Also apply
`review-all-src`. For JIT specifics see `jit`; for networking interop see `system-net-interop`.

## Correctness & Safety

### Error Handling & Assertions

- **Handle OOM with exceptions or fail-fast, never asserts.** Use `ThrowOutOfMemory` or `EEPOLICY_HANDLE_FATAL_ERROR`, not asserts. In interpreter loops, use `nothrow new` and check for null.
- **Use `_ASSERTE(!"message")` for unreachable native paths.** Keep native assertion guidance in native code rather than applying managed exception patterns.
- **Guard native size and offset arithmetic against overflow.** Validate multiplication and addition used for allocation sizes, buffer indexes, and pointer offsets before performing the operation. Prefer patterns and helpers that are correct by construction rather than checking an already-overflowed result.

### JIT-Specific Correctness

- **JIT lowering must not double-lower nodes.** Never call `LowerNode` on an already-lowered node. Return newly created nodes for the caller to lower. Constant folding belongs in import/morph, not lowering.
- **Mark collectible ALC test methods `NoInlining`.** Methods that touch collectible assembly load contexts must be `[MethodImpl(MethodImplOptions.NoInlining)]` to prevent the JIT from keeping references alive.

## Performance & Allocations

### Code Structure for Performance

- **Separate hot data from rarely-used data in runtime structures.** Keep frequently accessed data inline; move rarely-used data (GCInfo, DebugInfo) to separate structures.
- **Compute constant data at compile time, not execution time.** In interpreter and similar hot paths, pre-compute metadata lookups and type checks during the compilation phase.

## Code Style & Formatting

- **Prefer table-driven approaches over excessive case statements.** For hardware intrinsics and pattern-heavy code, use lookup tables (`AuxiliaryJitType`, `SpecialCodeGen` flags) instead of many explicit case entries.
- **Order struct fields to minimize padding.** In C/C++ struct definitions, order fields by size (pointers first) to reduce padding.
- **Run `jit-format` before pushing JIT changes.** JIT code must pass `jit-format` to avoid immediately failing CI. Run it with `python3 src/coreclr/scripts/jitformat.py -r . -o <os> -a <arch>` in the repo root. This should be done automatically when authoring JIT code and prior to pushing.

## Platform & Cross-Platform

- **Use correct platform/feature defines.** Use `TARGET_*`/`HOST_*` defines rather than compiler-provided defines (`__wasm__`). Use `HOST_*` for build machine code, `TARGET_*` for target platform. Use `PORTABILITY_ASSERT` for unimplemented platform code.

## Native Code & Interop

### C++ Style

- **Don't use `auto` in the runtime C++ codebase.** Use explicit types. Exception: unspeakable types like lambdas.
- **Use `nullptr`, `void*`, and native C++ types over legacy aliases.** Prefer `nullptr` over `NULL`, `void*` over `LPVOID`. Use `WCHAR` (not `wchar_t`) in Windows host code. Use `.inc` suffix for multiply-included files.
- **Match `#endif` comments to `#ifdef` exactly.** Add comments on `#else`/`#endif` for non-trivial blocks. Consistent brace placement and four-space indentation.
- **Prefer `static_cast` over C-style casts.** C-style casts are more permissive than needed and can silently degrade to `reinterpret_cast`.

### Runtime & VM Patterns

- **Use correct VM contracts and QCall patterns.** QCalls that may throw need `BEGIN_QCALL`/`END_QCALL`. Simple QCalls use `QCALL_CONTRACT_NO_GC_TRANSITION`. All VM methods need `STANDARD_VM_CONTRACT` or `WRAPPER_NO_CONTRACT`.
- **Append new GC-EE interface methods last.** Preserve vtable slot ordering by adding methods only at the end of the interface.
- **Keep GC protection correct around managed references.** Ensure all GC references are `GCPROTECT`-ed before GC-triggering calls. After GC-triggering calls, use `ObjectFromHandle(handle)` for a fresh reference.
- **Avoid dynamic allocation on fatal error paths.** Use stack-allocated buffers. Use simple synchronization (Interlocked with spin-wait) instead of Monitor/lock.
- **Avoid thread-local objects with destructors in CoreCLR.** Destruction order is arbitrary. Tie lifetime to the CoreCLR Thread object. Prefer `PLATFORM_THREAD_LOCAL` from minipal over C++ `thread_local` in perf-critical paths.
- **Use `SET_UNALIGNED` macros for potentially unaligned writes.** In code generation stubs, use `SET_UNALIGNED_32/64` rather than direct pointer dereferencing.
- **Zero-initialize arrays and buffers that may be partially used.** Zero-init allocated arrays whose elements have destructors. Zero-init EH tables, C arrays, and similar structures.
- **Add static asserts for hardcoded structural offsets.** When using hardcoded offsets to access struct fields (especially in assembly), add static asserts to verify them.
- **Use minipal for new platform abstractions.** Use minipal (new) instead of PAL (legacy) for platform abstraction in new CoreCLR code. Use `ALTERNATE_ENTRY` (not `LOCAL_LABEL`) for assembly labels called from outside their function.
- **Use `JITDUMP` and `LOG` macros, not `printf`.** In JIT code use `JITDUMP`. In CoreCLR VM use `LOG()`/`LOGGING` defines. Do not use `printf` or `Console.WriteLine` in production native code.

### P/Invoke & Marshalling

- **Prefer 4-byte `BOOL` for native interop marshalling.** Use `UnmanagedType.Bool`. Verify P/Invoke return types match native signatures exactly—mismatches may work on 64-bit but fail on 32-bit/WASM.
