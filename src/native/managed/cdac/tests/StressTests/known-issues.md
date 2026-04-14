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

## Issue 2: CheckForSkippedFrames causes false IsActiveFrame=false (instruction-level stress only)

**Affected**: Topmost managed frame at specific instruction offsets
**Frequency**: ~4 per 25K verifications (0.02%)
**Root cause**: IDENTIFIED — `CheckForSkippedFrames` boundary condition

**Where it happens**: `StackWalk_1.WalkStackReferences()` in
`src/native/managed/cdac/.../Contracts/StackWalk/StackWalk_1.cs` (lines 173-174)
and `CreateStackWalk()` (lines 141-143).

**Root cause**: When the initial managed frame's caller SP is at the exact boundary
of an explicit Frame's address, `CheckForSkippedFrames` returns `true` at some
instruction offsets (IP) but `false` at adjacent offsets (IP-4). When it returns
`true`, the state is set to `SW_SKIPPED_FRAME` and the first yielded frame has
`IsActiveFrame=false` (because `IsActiveFrame = IsFirst && SW_FRAMELESS` and the
state is SW_SKIPPED_FRAME). This causes `reportScratchSlots=false`, and scratch
register GC refs (RAX, R8) are not reported.

**Evidence (IsActiveFrame trace)**:
- PASS at IP=0x7FFB44F824F9: `IsActive=True flags=ActiveStackFrame` → 41 refs
- FAIL at IP=0x7FFB44F824FD: `IsActive=False flags=0` → 40 refs (1 scratch reg missing)

The missing ref is always a scratch register (RAX=0 or R8=8) that would be reported
if `ActiveStackFrame` were set. The GcInfoDecoder bitmap is correct and identical
between native and cDAC — the issue is purely in the `IsActiveFrame` flag.

**GcInfoDecoder**: VERIFIED CORRECT. Parallel tracing confirmed:
- `numTracked=5, numUntracked=0, liveStateBitOffset=262` — identical
- Per-slot liveness bits match exactly for every (offset, safePointIndex) pair
- Slot table decoding and bit positions are identical

**Follow-up fix**: The `CheckForSkippedFrames` comparison at initialization needs
to match the native `ProcessCurrentFrame` behavior more precisely, particularly
for the case where the explicit Frame address is at the exact boundary of the
caller SP. The native walker may use `<=` vs `<` or may adjust the comparison
to account for the Frame being pushed during the GC stress mechanism itself.

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
