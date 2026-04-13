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

## Issue 1: cDAC misses refs from explicit Frames

**Affected**: All debuggees (intermittent)

**Pattern**: The DAC reports refs from explicit Frame objects (e.g., InlinedCallFrame)
that the cDAC does not see. These are Frames on the Thread's frame chain that have
GcScanRoots implementations. The structured failure output now shows these clearly:

```
[FRAME_DAC_ONLY] Source=0x... (<frame 0x...>): DAC=1
```

## Issue 2: cDAC cannot unwind through native frames (EH first-pass)

**Affected**: ExceptionHandling (8-9 failures per run)

**Pattern**: The cDAC's GcInfoDecoder reports 1 extra live stack slot (`[rbp-8]`)
from ThrowHelper at the throw-site offset that the native GcInfoDecoder does not
report. All three walkers (cDAC, DAC, RT) visit ThrowHelper with `Report=true`,
but the native `EnumGcRefs` produces 0 refs while the cDAC produces 1. Root cause
is a GcInfo safe-point bitmap decoding difference — needs further investigation.

Note: The cDAC has `IsInterrupted`/`ExecutionAborted` tracking (matching native
`CrawlFrame::GetCodeManagerFlags`) but it doesn't help here because managed
throws don't push `FaultingExceptionFrame`. A hardware exception debuggee
(e.g., access violation) should be added to verify `ExecutionAborted` works
for that path.

## Future work

- Add a hardware exception debuggee (e.g., null dereference / access violation)
  to test the `IsInterrupted`/`ExecutionAborted` path through `FaultingExceptionFrame`.
- Investigate the GcInfo safe-point bitmap decoding difference for ThrowHelper.
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
