# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference enumeration
and the legacy DAC / runtime's GC stack scanning.

## Current Test Results

### Unit tests: 1374/1374 pass

### ALLOC+WALK+USE_DAC (0x61) — Stack walk frame comparison
**7/7 debuggees: 100% clean (zero WALK_FAIL) when tested**

### ALLOC+REFS+USE_DAC (0x51) — Three-way GC ref comparison

| Debuggee | Typical Result | Notes |
|----------|---------------|-------|
| BasicAlloc | Clean or 1 fail | |
| Comprehensive | Clean or 1 fail | |
| DeepStack | Clean or 1 fail | |
| Generics | Clean or 1 fail | |
| MultiThread | Clean or 1 fail | |
| PInvoke | Clean | |
| DynamicMethods | Clean or 2 mismatch | |
| StructScenarios | Clean or 1 fail | |
| ExceptionHandling | 8-9 fail | EH known issue |

## Issue 1: cDAC misses refs from explicit Frames (instruction-level stress only)

**Affected**: All debuggees at instruction-level GC stress (`DOTNET_GCStress=0x4`)
**Frequency**: ~3 per 25K verifications (0.01%)

**Pattern**: The DAC reports refs from explicit Frame objects (e.g., ExternalMethodFrame)
that the cDAC does not see. The cDAC's `FindGCRefMap` resolves most cases by reading
the R2R import section `AuxiliaryData`, but at instruction-level stress the timing
window is wider — the GCRefMap may not be resolved and the Frame's MethodDescPtr
may also be null (preventing the `PromoteCallerStack` fallback).

```
[FRAME_DAC_ONLY] Source=0x... (<frame 0x...>): DAC=1
```

The DAC succeeds because its `GetGCRefMap()` calls `FindGCRefMap(m_pZapModule,
m_pIndirection)` which independently reads the module's import section tables.
At allocation-level stress (default), this issue is fully resolved by our
`FindGCRefMap` implementation.

## Issue 2: GcInfo register ref mismatch (instruction-level stress only)

**Affected**: All debuggees at instruction-level GC stress
**Frequency**: ~4 per 25K verifications (0.02%)

**Pattern**: The cDAC reports 1 fewer register-based GC ref than the DAC for
non-active frames inside QCall/PInvoke boundary methods. The missing ref is
always in a callee-saved register (non-scratch), with the DAC reporting it as
live but the cDAC's GcInfoDecoder not finding it in the live-state bitmap.

```
[FRAME_DIFF] Source=0x... (RuntimeAssembly.GetTypeCore(...)): cDAC=4 DAC=5
  [DAC_ONLY] Addr=0x0 Obj=0x0 Flags=0x1  ← register-based interior pointer
```

This appears only at instruction-level stress because:
1. The GC breakpoint fires at IPs inside QCall transition stubs
2. At these offsets, the GcInfo may have partially-interruptible safe points
   where liveness differs between the cDAC's decoded bitmap and the native
   decoder's
3. The cDAC and native decoders may compute a different safe-point index
   for the same code offset, leading to different liveness results

Root cause is likely a subtle difference in how the cDAC's `FindSafePoint`
or live-state bitmap decoding handles these edge-case offsets.

## EH ThrowHelper (FIXED)

Previously 8-9 failures per run. Fixed by detecting `SoftwareExceptionFrame`
and `FaultingExceptionFrame` as interrupted frames and setting `ExecutionAborted`
flag, matching native `CrawlFrame::GetCodeManagerFlags`.

## Allocation-level stress results

At allocation-level stress (`DOTNET_CdacStress=0x51`, the default):
- All 9 debuggees pass 100% (0 failures across ~45K total verifications)

## Instruction-level stress results

At instruction-level stress (`DOTNET_GCStress=0x4 + DOTNET_CdacStress=0x54`):
- Comprehensive: 25,522 pass / 7 fail (99.97%)

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
