# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference enumeration
and the runtime's GC stack scanning.

## Current Test Results

Using `DOTNET_CdacStress=0x11` (ALLOC+REFS):

| Debuggee | Result |
|----------|--------|
| BasicAlloc | 0-2 failures |
| Comprehensive | 0 failures |
| DeepStack | 0 failures |
| ExceptionHandling | 4-12 failures |
| Generics | 0 failures |
| MultiThread | 0 failures |
| PInvoke | 0 failures |

## Issue 1: Intermittent content mismatches (snapshot timing)

**Affected debuggees**: BasicAlloc (intermittent, 0-2 per run)

**Pattern**: `cDAC=33 DAC=33 RT=33` — same count but different content.
The cDAC reports register-held refs (`Address=0x0`) while the runtime
reports all refs with stack addresses (registers are spilled to the stack
before the stress hook fires). The two-phase `CompareRefSets` matching
tries exact `(Address, Object, Flags)` for stack refs then fuzzy
`(Object, Flags)` for register refs, but timing differences in object
values between the cDAC snapshot and the runtime scan cause the fuzzy
phase to fail.

**Root cause**: The cDAC reads the thread's saved context and walks the
stack from that snapshot. The runtime's GC scan happens at a slightly
different execution point where all registers have been spilled to the
stack. This is inherent to comparing a diagnostic snapshot against a
live internal scan — the cDAC itself is correct (cDAC matches DAC).

## Issue 2: cDAC cannot unwind through native frames (EH first-pass)

**Affected debuggees**: ExceptionHandling (4-12 failures per run)

**Severity**: Low — only affects live-process stress testing during active
exception first-pass dispatch. Does not affect dump analysis where the
thread is suspended with a consistent Frame chain.

**Pattern**: `cDAC < RT` — the cDAC reports fewer refs than the runtime
(e.g., cDAC=7 RT=16). Occurs when `m_pFrame` is `FRAME_TOP` during EH
first-pass dispatch.

**Root cause**: The cDAC's `AMD64Unwinder.Unwind` (and equivalents for
other architectures) can only unwind **managed** frames — it checks
`ExecutionManager.GetCodeBlockHandle(IP)` first and returns false if the
IP is not in a managed code range. This means it cannot unwind through
native runtime frames (allocation helpers, EH dispatch code, etc.).

When the allocation stress point fires during exception first-pass dispatch:

1. The thread's `m_pFrame` is `FRAME_TOP` (no explicit Frames in the
   chain because they have been popped during EH dispatch)
2. The initial IP is in native code (allocation helper)
3. The cDAC cannot unwind past native frames → walk stops early
4. The runtime uses OS-level unwind (`RtlVirtualUnwind`) which handles
   native frames, so it walks more of the stack

**Possible fixes**:
1. **Ensure Frames are always available** — change the runtime to keep
   an explicit Frame pushed during allocation points within EH dispatch.
   The Frame chain is the only mechanism the cDAC has for transitioning
   through native code to reach managed frames.
2. **Accept as known limitation** — these failures only occur during
   live-process stress testing at a narrow window. In dumps, the
   exception state is frozen and the Frame chain is consistent.
