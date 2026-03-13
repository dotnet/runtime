# PR #124770 Analysis: Enable R2R precompilation of objc_msgSend P/Invoke stubs

## PR Overview

- **Title**: Enable R2R precompilation of objc_msgSend P/Invoke stubs
- **Author**: davidnguyen-tech
- **Branch**: `feature/r2r-objc-pinvoke-stubs` → `main`
- **Status**: Draft, open
- **Files changed**: 6 (+41/-21)
- **Head commit**: 8226f744d0ff53032abee5786729fef021e05eaa

## Problem Statement

On iOS with CoreCLR, the JIT is unavailable. ReadyToRun (R2R) precompiles code ahead of time, but ObjC `objc_msgSend` P/Invoke stubs were explicitly blocked from R2R compilation — the `PInvokeILEmitter` threw `NotSupportedException` for any P/Invoke that needed a pending exception check. This forced these calls to fall back to runtime JIT stub generation, which on JIT-less iOS means falling back to the interpreter, causing a performance penalty.

## Key Concepts

### ReadyToRun (R2R) vs NativeAOT
- **R2R**: Hybrid precompilation. Produces native code bundled alongside IL. Can fall back to JIT at runtime for things it couldn't precompile. Used for CoreCLR on iOS.
- **NativeAOT**: Full ahead-of-time compilation. No IL, no JIT, no fallback. Standalone native binary. Already handles ObjC P/Invokes correctly.

### Blittable Types
Types with identical memory layout in managed and unmanaged memory (e.g., `int`, `double`, `IntPtr`, flat structs of blittable fields). Non-blittable types (e.g., `string`, `bool`, arrays, classes with references) require marshalling — data conversion between managed and unmanaged representations.

### `IsMarshallingRequired`
`Marshaller.IsMarshallingRequired(MethodDesc)` in `Marshaller.ReadyToRun.cs` determines whether a P/Invoke method needs an IL stub for parameter/return value marshalling. Returns `true` if any parameter is non-blittable, or if flags like `SetLastError`, `!PreserveSig`, `IsUnmanagedCallersOnly` are set.

### `GeneratesPInvoke`
`ReadyToRunCompilationModuleGroupBase.GeneratesPInvoke(MethodDesc)` decides whether R2R should precompile a P/Invoke. Currently: `return !Marshaller.IsMarshallingRequired(method)` — i.e., only precompile if no marshalling is needed (blittable params, no special flags).

### ObjC Pending Exception Check
After calling `objc_msgSend`, the runtime must check if the ObjC runtime set a pending exception and rethrow it on the managed side. This is done by calling `ObjectiveCMarshal.ThrowPendingExceptionObject()`.

## Files Changed in the PR

### 1. `src/coreclr/System.Private.CoreLib/src/System/Runtime/InteropServices/ObjectiveCMarshal.CoreCLR.cs`
- Added `ThrowPendingExceptionObject()` — a new `[StackTraceHidden] internal static` method that calls `StubHelpers.GetPendingExceptionObject()` and rethrows via `ExceptionDispatchInfo.Throw(ex)`.

### 2. `src/coreclr/vm/corelib.h`
- Registered `ThrowPendingExceptionObject` in the VM's managed method table under `#ifdef FEATURE_OBJCMARSHAL`.

### 3. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/IL/Stubs/PInvokeILEmitter.cs`
- In `EmitPInvokeCall`: After the native call, now emits `call ObjectiveCMarshal.ThrowPendingExceptionObject()` when `ShouldCheckForPendingException` is true.
- In `EmitIL()`: Removed the `NotSupportedException` throw that previously blocked ObjC P/Invokes entirely.

### 4. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCompilationModuleGroupBase.cs`
- `GeneratesPInvoke()` expanded: now also returns `true` when `ShouldCheckForPendingException` is true, as an escape hatch to let ObjC P/Invokes through even if `IsMarshallingRequired` returns true.

### 5. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCodegenCompilation.cs`
- Wrapped `PInvokeILEmitter.EmitIL(key)` in a try/catch for `RequiresRuntimeJitException`, returning null on failure (graceful fallback).

### 6. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/JitInterface/CorInfoImpl.ReadyToRun.cs`
- Simplified `pInvokeMarshalingRequired` by removing a redundant try/catch; now just checks if `GetMethodIL` returns null.

## Reviewer Feedback: Vlad's Comment (r2845070125)

Vlad commented on the change to `GeneratesPInvoke()` in `ReadyToRunCompilationModuleGroupBase.cs`:

> "this check here looks wrong. This check is already done as part of `Marshaller.IsMarshallingRequired` and I believe it should be removed from there. In your case, this method is returning true for pinvokes with non-blittable types that require check for pending exception. It should be returning false instead and I believe this is the reason you are forcefully catching the requires jit exception above."

### Analysis: Vlad is Correct

The root issue is that `ShouldCheckForPendingException` inside `IsMarshallingRequired` **conflates two different concerns**:
1. "Does this P/Invoke need a pending exception check?" (simple — R2R can now emit this)
2. "Does this P/Invoke need complex parameter marshalling?" (complex — R2R may not handle this)

Because `IsMarshallingRequired` returns `true` for ObjC P/Invokes (due to the pending exception check), the PR had to add:
- An escape hatch in `GeneratesPInvoke` (the `ShouldCheckForPendingException` override)
- A try/catch in `ReadyToRunCodegenCompilation.cs` for when the emitter still can't handle non-blittable ObjC P/Invokes

This is a workaround for a problem that shouldn't exist.

## Git History Investigation

### Original Commit: 4a782d58ac4 (Aaron Robinson, May 2021, PR #52849)

**"Objective-C msgSend* support for pending exceptions in Release"**

Aaron added `ShouldCheckForPendingException` to `IsMarshallingRequired` as a **two-layer safety net**:

1. **Layer 1 — `IsMarshallingRequired` returns `true`**: Prevents `GeneratesPInvoke` from returning `true`, so R2R won't try to inline ObjC P/Invokes as raw native calls (bypassing the stub entirely).

2. **Layer 2 — `PInvokeILEmitter.EmitIL()` throws `NotSupportedException`**: Even if R2R tries the stub path, it fails gracefully and falls back to runtime JIT.

**Why it was designed this way**: At the time (2021), CrossGen2/R2R had **no ability** to emit the pending exception check. The check in `IsMarshallingRequired` was an expedient way to keep ObjC methods off the R2R fast path entirely. It wasn't saying "this needs parameter marshalling" — it was saying "this needs a stub that R2R can't produce."

The VM-level equivalent in `dllimport.cpp` has the same check inside `NDirect::MarshalingRequired()`, guarded by `#ifndef CROSSGEN_COMPILE` (meaning it was already excluded from the old crossgen path).

## Recommended Plan

### What should change (addressing Vlad's feedback)

1. **Remove `ShouldCheckForPendingException` from `IsMarshallingRequired`** in `Marshaller.ReadyToRun.cs` (lines 120-121)
   - This makes `IsMarshallingRequired` purely about parameter marshalling again.
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Interop/IL/Marshaller.ReadyToRun.cs`

2. **Remove the escape hatch from `GeneratesPInvoke`** in `ReadyToRunCompilationModuleGroupBase.cs`
   - Revert `GeneratesPInvoke` back to just `return !Marshaller.IsMarshallingRequired(method)`
   - The ObjC P/Invokes with blittable params will now naturally pass through (IsMarshallingRequired=false → GeneratesPInvoke=true)
   - ObjC P/Invokes with non-blittable params will correctly be excluded (IsMarshallingRequired=true → GeneratesPInvoke=false → runtime fallback)
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCompilationModuleGroupBase.cs`

3. **Keep the try/catch safety net** in `ReadyToRunCodegenCompilation.cs` (but update the comment)
   - `PInvokeILEmitter.EmitIL()` throws `NotSupportedException` for `LCIDConversionAttribute`, which is NOT checked by `IsMarshallingRequired`. A blittable P/Invoke with `[LCIDConversion]` would pass `GeneratesPInvoke` but fail in `EmitIL`. The try/catch is the only thing preventing crossgen2 from crashing.
   - Update the comment to explain this is a general safety net for any `EmitIL` failure, not ObjC-specific.
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCodegenCompilation.cs`

4. **Keep the `PInvokeILEmitter.cs` changes** that emit `ThrowPendingExceptionObject()`
   - This is the core value of the PR — teaching R2R to emit the pending exception check
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/IL/Stubs/PInvokeILEmitter.cs`

5. **Keep the `CorInfoImpl.ReadyToRun.cs` simplification**
   - The simplified null check is cleaner regardless
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/JitInterface/CorInfoImpl.ReadyToRun.cs`

6. **Keep the CoreLib/VM changes** (`ObjectiveCMarshal.CoreCLR.cs`, `corelib.h`)
   - These provide the managed method that the emitted stub calls
   - Files: `src/coreclr/System.Private.CoreLib/src/System/Runtime/InteropServices/ObjectiveCMarshal.CoreCLR.cs`, `src/coreclr/vm/corelib.h`

### Expected behavior after the fix

| Scenario | `IsMarshallingRequired` | `GeneratesPInvoke` | Result |
|----------|------------------------|-------------------|--------|
| ObjC P/Invoke, blittable params | `false` | `true` | R2R precompiles with pending exception check |
| ObjC P/Invoke, non-blittable params | `true` (due to params) | `false` | Falls back to runtime (correct) |
| Regular P/Invoke, blittable params | `false` | `true` | R2R precompiles (unchanged) |
| Regular P/Invoke, non-blittable params | `true` | `false` | Falls back to runtime (unchanged) |

---

## LibraryImport vs DllImport Analysis

### Question: Would switching macios to `[LibraryImport]` fix this?

**Answer: No.** `[LibraryImport]` does **not** solve the problem and would cause significant breakage.

### How LibraryImport Works Under the Hood

`[LibraryImport]` uses a Roslyn source generator to emit a managed wrapper that handles marshalling at compile time. But the **inner** P/Invoke that the generator emits is still a `[DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]` targeting blittable types.

That inner call:
1. Still targets `objc_msgSend` in `libobjc.dylib`
2. Still triggers `ShouldCheckForPendingException` in both the VM and R2R
3. Still gets blocked from R2R precompilation by the same `IsMarshallingRequired` gate

**Verification:** `ShouldCheckForPendingException` (in `MarshalHelpers.cs:935-955`) matches on `metadata.Module` (the library path) and `metadata.Name` (the entry point). With `[LibraryImport]`, the source generator preserves both on the inner `[DllImport]`, so the check still triggers.

### Why LibraryImport Would Break Users

1. **Binary breaking change** — `[DllImport] extern` methods have a fundamentally different calling convention than `[LibraryImport]` generated wrappers. Existing compiled assemblies referencing these methods would fail at runtime.
2. **Massive scope** — macios has hundreds/thousands of `objc_msgSend` overloads. These are the backbone of all iOS/macOS ObjC interop.
3. **Source generator doesn't know about pending exceptions** — `[LibraryImport]`'s generator has no concept of `ObjectiveCMarshal.ThrowPendingExceptionObject()`. You'd need custom logic in the generated wrapper to call it, which the source generator doesn't support.

### After This PR's Fix

After removing `ShouldCheckForPendingException` from `IsMarshallingRequired`, the inner `[DllImport]` generated by `[LibraryImport]` (which has blittable-only parameters) *would* pass `IsMarshallingRequired` = `false` → `GeneratesPInvoke` = `true` → R2R precompiles it with the pending exception check. So the fix works equally well regardless of whether the consumer uses `[DllImport]` or `[LibraryImport]`.

### Conclusion

Switching macios to `[LibraryImport]` wouldn't solve the problem *without* this runtime fix, and would be a massive breaking change for no benefit. The fix belongs in the runtime (this PR).

---

## P/Invoke Inlining: Which `IsMarshallingRequired` Gets Called?

### The Question

When the JIT considers "inlining" a P/Invoke (embedding the native call directly instead of going through an IL stub), which marshalling check does it consult?

### Answer: R2R uses `CorInfoImpl.ReadyToRun.cs`, NOT the VM `dllimport.cpp`

**Full call chain for R2R P/Invoke direct-call decisions:**

```
JIT: impCheckForPInvokeCall (importercalls.cpp:6849)
  → CorInfoImpl.pInvokeMarshalingRequired (CorInfoImpl.ReadyToRun.cs:3111-3142)
    → _compilation.GetMethodIL(method)
      → GeneratesPInvoke gate (ReadyToRunCompilationModuleGroupBase.cs:712-725)
        → PInvokeILEmitter.EmitIL (PInvokeILEmitter.cs)
          → Marshaller.IsMarshallingRequired (Marshaller.ReadyToRun.cs:104-131)
    → PInvokeILStubMethodIL.IsMarshallingRequired property
```

**Key details of `CorInfoImpl.pInvokeMarshalingRequired` (lines 3111-3142):**

1. If `method.IsRawPInvoke()` → return `false` (this is the synthetic raw native target *inside* an IL stub)
2. If method is outside the version bubble → return `true`
3. Call `_compilation.GetMethodIL(method)` — if null → return `true` (don't inline; runtime fallback)
4. Otherwise return `((PInvokeILStubMethodIL)stubIL).IsMarshallingRequired`

The `IsMarshallingRequired` property is set during `PInvokeILEmitter.EmitIL()` from `Marshaller.IsMarshallingRequired(MethodDesc)`.

**The VM path** (`CEEInfo::pInvokeMarshalingRequired` → `NDirect::MarshalingRequired` in `dllimport.cpp`) is only used by **normal runtime JIT**, not R2R.

### Is Inlining a P/Invoke That Requires Marshalling Safe?

**No — but the system prevents it with graceful fallback.**

R2R's marshaller factory (`Marshaller.ReadyToRun.cs:12-25`) only supports trivially blittable cases:
- Supported: `Enum`, `BlittableValue`, `BlittableStruct`, `UnicodeChar`, `VoidReturn`
- Everything else: `new NotSupportedMarshaller()`

If unsupported marshalling is encountered:
1. `NotSupportedMarshaller.EmitMarshallingIL()` throws `NotSupportedException`
2. `PInvokeILEmitter.EmitIL()` catches it, rethrows as `RequiresRuntimeJitException`
3. `ReadyToRunCodegenCompilation.cs` catches that, returns `null` for `methodIL`
4. `CorInfoImpl.pInvokeMarshalingRequired` sees `stubIL == null` → returns `true`
5. JIT sees marshalling required → does NOT direct-call → falls back to runtime stub

**It never silently generates incorrect direct-call code.**

### Implications for This PR

The current PR adds a `ShouldCheckForPendingException` escape hatch in `GeneratesPInvoke` that returns `true` for ALL ObjC P/Invokes, including non-blittable ones. For non-blittable ObjC P/Invokes, this causes:
- `GeneratesPInvoke` = true → R2R attempts to emit IL stub
- `NotSupportedMarshaller` throws → `RequiresRuntimeJitException` → caught by try-catch → `stubIL = null`
- Fallback to runtime (correct but wasteful)

**With BrzVlad's fix** (removing the check from `IsMarshallingRequired`):
- Non-blittable ObjC: `IsMarshallingRequired` = true (due to params) → `GeneratesPInvoke` = false → never attempts emission → no waste
- Blittable ObjC: `IsMarshallingRequired` = false → `GeneratesPInvoke` = true → R2R emits stub with pending exception check → works perfectly

This confirms BrzVlad's approach is both cleaner and more efficient.

---

### Build & Test

- Component: CoreCLR (files under `src/coreclr/`)
- Baseline build: `./build.sh clr+libs+host` (from main branch first)
- After changes: rebuild tools and run tests
- The ObjC interop tests are in `src/libraries/System.Runtime.InteropServices/tests/` — look for ObjectiveC-related test files
