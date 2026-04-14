# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference enumeration
and the legacy DAC / runtime's GC stack scanning.

## Current Test Results

### Unit tests: 1374/1374 pass

### ALLOC+WALK+USE_DAC (0x61) — Stack walk frame comparison
**7/7 debuggees: 100% clean (zero WALK_FAIL) when tested**

### ALLOC+REFS+USE_DAC (0x51) — Three-way GC ref comparison

| Debuggee | Result | Notes |
|----------|--------|-------|
| BasicAlloc | 0 failures | |
| Comprehensive | 0 failures | |
| DeepStack | 0 failures | |
| Generics | 0 failures | |
| MultiThread | 0 failures | |
| PInvoke | 0 failures | Windows only |
| DynamicMethods | 0 failures | |
| StructScenarios | 0 failures | |
| ExceptionHandling | 0 failures | Fixed via ExecutionAborted |

## Issue 1: ELEMENT_TYPE_INTERNAL in PromoteCallerStack (instruction-level stress only)

**Affected**: Explicit Frames whose method signature contains `ELEMENT_TYPE_INTERNAL` (0x21)
**Frequency**: ~3 per 25K verifications (0.01%)
**Root cause**: IDENTIFIED — follow-up fix needed

**Where it happens**: `FrameIterator.PromoteCallerStack()` in
`src/native/managed/cdac/.../Contracts/StackWalk/FrameHandling/FrameIterator.cs`
(around line 604). This is the fallback path used when a Frame's GCRefMap is
unavailable and we must decode the method signature to determine which caller
arguments are GC references.

**Pattern**: The DAC reports 1 ref from an explicit Frame that the cDAC fails to scan.
The `PromoteCallerStack` fallback decodes the method signature using
`System.Reflection.Metadata.SignatureDecoder`, which only handles standard ECMA-335
type codes. Runtime-internal signatures (generated for IL stubs, marshalling stubs,
unsafe accessors, etc.) may contain `ELEMENT_TYPE_INTERNAL` (0x21), which encodes a
raw pointer-sized `TypeHandle` directly in the signature blob. The SRM decoder doesn't
recognize this type code and throws `BadImageFormatException`.

```
System.BadImageFormatException: Unexpected SignatureTypeCode: (0x21).
   at SignatureDecoder`2.DecodeType(BlobReader&, Boolean, Int32)
   at SignatureDecoder`2.DecodeGenericTypeInstance(BlobReader&)
   at FrameIterator.PromoteCallerStack(...)
   at FrameIterator.GcScanRoots(...)
```

The exception is caught by the per-frame exception handler in `WalkStackReferences()`
(`StackWalk_1.cs`, around line 245), which silently swallows it and continues the
walk — causing the Frame's GC refs to be unreported.

**How the DAC handles it**: The native DAC uses `MetaSig` + `ArgIterator`
(`frames.cpp:1520-1596`) instead of the SRM decoder. `MetaSig` natively understands
`ELEMENT_TYPE_INTERNAL` — it reads the embedded TypeHandle pointer and follows it to
determine the actual type for GC classification.

**How the Legacy cDAC handles it**: `SigFormat.cs` (line 157-175) already handles
`ELEMENT_TYPE_INTERNAL` by reading the pointer-sized TypeHandle, resolving it via
`RuntimeTypeSystem.GetTypeHandle()`, and checking `GetSignatureCorElementType()`.

**Current workaround**: A `catch (BadImageFormatException)` in `PromoteCallerStack`
returns without reporting refs for the frame.

**Follow-up fix**: Replace the SRM `SignatureDecoder` usage with a custom signature
walker that:
1. Pre-processes the signature bytes, handling `ELEMENT_TYPE_INTERNAL` (0x21) by
   reading the pointer-sized TypeHandle and resolving through `RuntimeTypeSystem`
   (following the pattern in `SigFormat.cs:157-175`)
2. Delegates standard ECMA-335 type codes to the existing `GcSignatureTypeProvider`
3. Handles `ELEMENT_TYPE_CMOD_INTERNAL` (0x22) similarly if encountered

## Issue 2: GcInfo register ref mismatch (instruction-level stress only)

**Affected**: QCall boundary methods during EventSource initialization
**Frequency**: ~4 per 25K verifications (0.02%)

**Pattern**: The cDAC reports 1 fewer register-based GC ref than the DAC for
methods at specific instruction offsets. The preceding instruction offset (IP-4)
always passes with identical counts.

Affected methods (consistent across runs):
- `EventSource.AddProviderEnumKind` — `Obj=0x..., Flags=0x0` (normal object ref)
- `RuntimeAssembly.GetTypeCore` (QCall) — `Obj=0x0, Flags=0x1` (null interior pointer)
- `ModuleHandle.GetDynamicMethod` (QCall) — `Obj=0x..., Flags=0x1` (interior pointer)

```
[FRAME_DIFF] Source=0x... (GetTypeCore(QCall...)): cDAC=4 DAC=5
  [DAC_ONLY] Addr=0x0 Obj=0x0 Flags=0x1  ← register-based interior pointer
```

Root cause is a subtle difference in how the cDAC's `GcInfoDecoder.EnumerateLiveSlots`
handles partially-interruptible safe point liveness at these specific code offsets.
The native decoder finds the register slot live at this IP but the cDAC does not.
Further investigation requires side-by-side GcInfo tracing with slot-level output.

## EH ThrowHelper (FIXED)

Previously 8-9 failures per run. Fixed by detecting `SoftwareExceptionFrame`
and `FaultingExceptionFrame` as interrupted frames and setting `ExecutionAborted`
flag, matching native `CrawlFrame::GetCodeManagerFlags`.

## Allocation-level stress results

At allocation-level stress (`DOTNET_CdacStress=0x51`, the default):
- All 9 debuggees pass 100% (0 failures across ~45K total verifications)

## Instruction-level stress results

At instruction-level stress (`DOTNET_GCStress=0x4 + DOTNET_CdacStress=0x54`):
- Comprehensive: 25,461 pass / 7 fail (99.97%)
  - 4 FRAME_DIFF (GcInfo register mismatch at QCall boundaries)
  - 3 FRAME_DAC_ONLY (missing explicit Frame in cDAC walk)

## Future work

- Investigate the GcInfo safe-point bitmap decoding difference for QCall frames
- Replace `fprintf`-based stress logging in `cdacstress.cpp` with a more
  structured mechanism (e.g., ETW events or StressLog) for better tooling
  integration and reduced I/O overhead during stress runs.

## Log Format

The stress log uses structured per-frame output with method name resolution:

```
[PASS] Thread=0x... IP=0x... cDAC=N DAC=N RT=M
[FAIL] Thread=0x... IP=0x... cDAC=N DAC=M RT=M
  [COMPARE cDAC-vs-DAC]
    [FRAME_DIFF] Source=0x... (MethodName): cDAC=X DAC=Y
      [cDAC_ONLY] Addr=0x... Obj=0x... Flags=0x...
      [DAC_ONLY] Addr=0x... Obj=0x... Flags=0x...
    [FRAME_cDAC_ONLY] Source=0x... (MethodName): cDAC=X
    [FRAME_DAC_ONLY] Source=0x... (<frame 0x...>): DAC=Y
  [RT_DIFF] cDAC=N RT=M (cDAC matches DAC but differs from RT)
  [STACK_TRACE] (cDAC=N DAC=M RT=M)
    #i MethodName (cDAC=X DAC=Y) [<-- MISMATCH]
```
