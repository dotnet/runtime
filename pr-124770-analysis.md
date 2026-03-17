# PR #124770 Analysis: Enable R2R precompilation of objc_msgSend P/Invoke stubs

## PR Overview

- **Title**: Enable R2R precompilation of objc_msgSend P/Invoke stubs
- **Author**: davidnguyen-tech
- **Branch**: `feature/r2r-objc-pinvoke-stubs` → `main`
- **Status**: Draft, open
- **March 17 update**: The PR was force-pushed and restructured into 3 cleaner commits:
  1. `01a7fe190ad` — add `ThrowPendingExceptionObject()` plus the `corelib.h` binder entry
  2. `b41aa09deb6` — emit `ThrowPendingExceptionObject` in `PInvokeILEmitter.EmitPInvokeCall()` and remove the old ObjC blocker
  3. `e04bc858067` — remove `ShouldCheckForPendingException` from `Marshaller.IsMarshallingRequired` and move the direct-call suppression to `PInvokeILStubMethodIL.IsMarshallingRequired`
- **Historical note**: The earlier 6-file snapshot below is still useful context, but sections 4-6 are now historical because the force-push removed that part of the design.
- **Scope note**: The March 17 updates in this document describe the PR as force-pushed on GitHub, even if the local checkout you are reading alongside this note still reflects the older pre-force-push revision.

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
`ReadyToRunCompilationModuleGroupBase.GeneratesPInvoke(MethodDesc)` decides whether R2R should precompile a P/Invoke. In the force-pushed PR, it is back to the original `return !Marshaller.IsMarshallingRequired(method)` shape — i.e., only precompile if parameter marshalling itself is not required.

### `PInvokeILStubMethodIL.IsMarshallingRequired` (class in `PInvokeILEmitter.cs`)
`PInvokeILStubMethodIL.IsMarshallingRequired` is what `CorInfoImpl.ReadyToRun.cs` ultimately reports back to the JIT for R2R direct-call decisions once a stub has been built. The March 17 force-push moves the ObjC pending-exception special case here so R2R can still precompile a stub for blittable ObjC signatures while preventing the JIT from bypassing that stub as a raw native direct-call.

### ObjC Pending Exception Check
After calling `objc_msgSend`, the runtime must check if the ObjC runtime set a pending exception and rethrow it on the managed side. This is done by calling `ObjectiveCMarshal.ThrowPendingExceptionObject()`.

## Files Changed in the PR

### 1. `src/coreclr/System.Private.CoreLib/src/System/Runtime/InteropServices/ObjectiveCMarshal.CoreCLR.cs`
- Added `ThrowPendingExceptionObject()` — a new `[StackTraceHidden] internal static` method that calls `StubHelpers.GetPendingExceptionObject()` and rethrows via `ExceptionDispatchInfo.Throw(ex)`.

### 2. `src/coreclr/vm/corelib.h`
- Registered `ThrowPendingExceptionObject` in the VM's managed method table under `#ifdef FEATURE_OBJCMARSHAL`.

### 3. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/IL/Stubs/PInvokeILEmitter.cs`
- In `EmitPInvokeCall`: After the native call, the force-pushed PR now emits `call ObjectiveCMarshal.ThrowPendingExceptionObject()` when `ShouldCheckForPendingException` is true.
- In `EmitIL()`: Removed the old `NotSupportedException` throw that previously blocked ObjC P/Invokes entirely.
- The intended stub shape is now: `marshal args → call objc_msgSend → call ThrowPendingExceptionObject → ret`.
- **Open concern**: that shape is trivial for `void` returns, but non-void returns must preserve the native return value across the helper call. A Copilot review comment flagged possible IL stack corruption if the helper is emitted while the return value is still on the evaluation stack.

### 4. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCompilationModuleGroupBase.cs`
- **Update (March 17)**: The temporary `GeneratesPInvoke` escape hatch was removed from the PR.
- `GeneratesPInvoke()` is back to `return !Marshaller.IsMarshallingRequired(method)`.
- The old escape-hatch design is still important historical context because it explains Vlad's review comment, but it is no longer in the current PR.

### 5. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCodegenCompilation.cs`
- **Update (March 17)**: The temporary try/catch for `RequiresRuntimeJitException` was removed from the PR.
- The new design relies on better up-front gating rather than speculative emission plus catch-and-fallback.

### 6. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/JitInterface/CorInfoImpl.ReadyToRun.cs`
- **Update (March 17)**: This file is no longer part of the PR.
- The earlier simplification became unnecessary once the design moved the ObjC special case into `PInvokeILStubMethodIL.IsMarshallingRequired` instead.

### 7. `src/coreclr/tools/aot/ILCompiler.ReadyToRun/IL/Stubs/PInvokeILEmitter.cs` (`PInvokeILStubMethodIL.IsMarshallingRequired`)
- **Key March 17 change**: `ShouldCheckForPendingException` was removed from `Marshaller.IsMarshallingRequired` and instead reflected in `PInvokeILStubMethodIL.IsMarshallingRequired`.
- That keeps blittable ObjC P/Invokes eligible for stub generation while still telling the JIT that a raw direct-call would be incorrect because it would skip `ThrowPendingExceptionObject()`.

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

### Update (March 17)

The force-pushed PR adopted Vlad's core suggestion:
- `ShouldCheckForPendingException` was removed from `Marshaller.IsMarshallingRequired`
- the `GeneratesPInvoke` escape hatch was removed

It also added an extra safeguard that was not part of the earlier review thread: `PInvokeILStubMethodIL.IsMarshallingRequired` now carries the ObjC pending-exception requirement so the JIT still avoids the raw direct-call path for blittable ObjC P/Invokes.

## Git History Investigation

### Original Commit: 4a782d58ac4 (Aaron Robinson, May 2021, PR #52849)

**"Objective-C msgSend* support for pending exceptions in Release"**

Aaron added `ShouldCheckForPendingException` to `IsMarshallingRequired` as a **two-layer safety net**:

1. **Layer 1 — `IsMarshallingRequired` returns `true`**: Prevents `GeneratesPInvoke` from returning `true`, so R2R won't try to inline ObjC P/Invokes as raw native calls (bypassing the stub entirely).

2. **Layer 2 — `PInvokeILEmitter.EmitIL()` throws `NotSupportedException`**: Even if R2R tries the stub path, it fails gracefully and falls back to runtime JIT.

**Why it was designed this way**: At the time (2021), CrossGen2/R2R had **no ability** to emit the pending exception check. The check in `IsMarshallingRequired` was an expedient way to keep ObjC methods off the R2R fast path entirely. It wasn't saying "this needs parameter marshalling" — it was saying "this needs a stub that R2R can't produce."

The VM-level equivalent in `dllimport.cpp` has the same check inside `NDirect::MarshalingRequired()`, guarded by `#ifndef CROSSGEN_COMPILE` (meaning it was already excluded from the old crossgen path).

## Recommended Plan

### Update (March 17): what is already implemented vs what is still open

1. **✅ Implemented: remove `ShouldCheckForPendingException` from `Marshaller.IsMarshallingRequired`**
   - This restores `Marshaller.IsMarshallingRequired` to being about actual parameter/return marshalling.
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Interop/IL/Marshaller.ReadyToRun.cs`

2. **✅ Implemented: remove the `GeneratesPInvoke` escape hatch**
   - `GeneratesPInvoke` is back to `return !Marshaller.IsMarshallingRequired(method)`.
   - Blittable ObjC P/Invokes now get through naturally; non-blittable ObjC P/Invokes are excluded naturally.
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/Compiler/ReadyToRunCompilationModuleGroupBase.cs`

3. **✅ Implemented: move the ObjC direct-call suppression to `PInvokeILStubMethodIL.IsMarshallingRequired`**
   - This is the key new mechanism from the force-push.
   - It preserves stub generation for blittable ObjC signatures while still preventing the JIT from treating them as raw direct-call candidates.
   - File: `src/coreclr/tools/aot/ILCompiler.ReadyToRun/IL/Stubs/PInvokeILEmitter.cs`

4. **✅ Implemented: keep the `PInvokeILEmitter.cs` changes that emit `ThrowPendingExceptionObject()`**
   - This remains the core value of the PR.
   - The generated stub is intended to be `marshal args → call objc_msgSend → call ThrowPendingExceptionObject → ret`.

5. **✅ Implemented: keep the CoreLib/VM changes** (`ObjectiveCMarshal.CoreCLR.cs`, `corelib.h`)
   - These provide the managed helper the emitted stub calls.

6. **❌ Still open: add regression coverage and resolve the new design questions**
   - Add an R2R `--map` validation test proving ObjC stubs are precompiled.
   - Validate non-void return handling, image-size impact, and P/Invoke frame correctness.
   - Keep the JIT-helper alternative on the table if the emitted-IL approach becomes too awkward.

### Expected behavior after the force-push

| Scenario | `Marshaller.IsMarshallingRequired` | `GeneratesPInvoke` | `PInvokeILStubMethodIL.IsMarshallingRequired` | Result |
|----------|------------------------------------|-------------------|-----------------------------------------------|--------|
| ObjC P/Invoke, blittable params | `false` | `true` | `true` | R2R emits a stub and the JIT stays on the stub path so the pending-exception helper runs |
| ObjC P/Invoke, non-blittable params | `true` (due to params) | `false` | n/a | Falls back to runtime/interpreter (correct) |
| Regular P/Invoke, blittable params | `false` | `true` | `false` | Existing direct-call behavior stays unchanged |
| Regular P/Invoke, non-blittable params | `true` | `false` | n/a | Falls back to runtime (unchanged) |

## Open Concerns After the March 17 Force-Push

1. **Code size / stub count impact**
   - More ObjC callsites now intentionally take the precompiled stub path.
   - That is probably the right trade-off, but it should be measured rather than assumed.

2. **P/Invoke frame correctness**
   - Forcing the stub path changes where the pending-exception logic runs.
   - It is worth validating that the resulting frame/transition behavior matches what the runtime expects for ObjC interop on iOS.

3. **Non-void return IL shape**
   - The simple `call native → call ThrowPendingExceptionObject → ret` sketch is only obviously correct for `void` returns.
   - For non-void P/Invokes, the return value likely needs to be stored and reloaded around the helper call.

4. **JIT helper alternative**
   - jkoritzinsky suggested a JIT-helper implementation instead of emitting the helper call directly in IL.
   - David and jkoritzinsky exploring that option is reasonable if it simplifies stack handling, frame correctness, or code size.

---

## LibraryImport vs DllImport Analysis

### Question: Would switching macios to `[LibraryImport]` fix this?

**Answer: No.** `[LibraryImport]` does **not** solve the problem and would cause significant breakage.

### How LibraryImport Works Under the Hood

`[LibraryImport]` uses a Roslyn source generator to emit a managed wrapper that handles marshalling at compile time. But the **inner** P/Invoke that the generator emits is still a `[DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]` targeting blittable types.

That inner call:
1. Still targets `objc_msgSend` in `libobjc.dylib`
2. Still triggers `ShouldCheckForPendingException` in both the VM and R2R
3. In the pre-force-push design, it was blocked by the same `IsMarshallingRequired` gate; in the March 17 design, that gate is removed and the direct-call suppression happens later via `PInvokeILStubMethodIL.IsMarshallingRequired`

**Verification:** `ShouldCheckForPendingException` (in `MarshalHelpers.cs:935-955`) matches on `metadata.Module` (the library path) and `metadata.Name` (the entry point). With `[LibraryImport]`, the source generator preserves both on the inner `[DllImport]`, so the check still triggers.

### Why LibraryImport Would Break Users

1. **Binary breaking change** — `[DllImport] extern` methods have a fundamentally different calling convention than `[LibraryImport]` generated wrappers. Existing compiled assemblies referencing these methods would fail at runtime.
2. **Massive scope** — macios has hundreds/thousands of `objc_msgSend` overloads. These are the backbone of all iOS/macOS ObjC interop.
3. **Source generator doesn't know about pending exceptions** — `[LibraryImport]`'s generator has no concept of `ObjectiveCMarshal.ThrowPendingExceptionObject()`. You'd need custom logic in the generated wrapper to call it, which the source generator doesn't support.

### After This PR's Force-Pushed Design

After the March 17 force-push, the inner `[DllImport]` generated by `[LibraryImport]` (which has blittable-only parameters) can pass `Marshaller.IsMarshallingRequired = false` and `GeneratesPInvoke = true`, while `PInvokeILStubMethodIL.IsMarshallingRequired = true` still keeps the JIT on the stub path so `ThrowPendingExceptionObject()` runs. So the fix still benefits both `[DllImport]` and `[LibraryImport]` consumers automatically — the mechanism just moved.

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

In the force-pushed PR, `PInvokeILStubMethodIL.IsMarshallingRequired` is no longer just a mirror of `Marshaller.IsMarshallingRequired(MethodDesc)`. It also carries the ObjC pending-exception requirement so the JIT will not raw-inline/direct-call a blittable ObjC P/Invoke and accidentally skip `ThrowPendingExceptionObject()`.

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

The force-pushed PR no longer relies on a `GeneratesPInvoke` escape hatch. Instead, it splits the two concerns cleanly:
- **Stub generation** still uses `Marshaller.IsMarshallingRequired`
- **JIT direct-call suppression** now uses `PInvokeILStubMethodIL.IsMarshallingRequired`

That means:
- **Non-blittable ObjC**: `Marshaller.IsMarshallingRequired = true` (because of real marshalling needs) → `GeneratesPInvoke = false` → no speculative emission
- **Blittable ObjC**: `Marshaller.IsMarshallingRequired = false` → `GeneratesPInvoke = true` → R2R emits a stub, but `PInvokeILStubMethodIL.IsMarshallingRequired = true` keeps the JIT from bypassing that stub
- **Regular blittable P/Invokes**: both stay false, so existing direct-call behavior is preserved

This is cleaner than the old escape-hatch design. The remaining question is no longer the layering — it is whether the emitted stub shape is correct and worth the code-size/runtime trade-off.

---

### Build & Test

- Component: CoreCLR (files under `src/coreclr/`)
- Baseline build: `./build.sh clr+libs+host` (from main branch first)
- After changes: rebuild tools and run tests
- The ObjC interop tests are in `src/libraries/System.Runtime.InteropServices/tests/` — look for ObjectiveC-related test files
