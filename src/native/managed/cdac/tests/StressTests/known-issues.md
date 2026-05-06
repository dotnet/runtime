# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps between the cDAC's stack reference enumeration
and the legacy DAC's `GetStackReferences`.

## Current Test Results

Using `DOTNET_CdacStress` with cDAC-vs-DAC comparison:

| Mode | Non-EH debuggees (6) | ExceptionHandling |
|------|-----------------------|-------------------|
| INSTR (0x4 + GCStress=0x4, step=10) | 0 failures | 0-2 failures |
| ALLOC+UNIQUE (0x101) | 0 failures | 4 failures |
| Walk comparison (0x20, IP+SP) | 0 mismatches | N/A |

## Known Issue: cDAC Cannot Unwind Through Native Frames

**Severity**: Low — only affects live-process stress testing during active
exception first-pass dispatch. Does not affect dump analysis where the thread
is suspended with a consistent Frame chain.

**Pattern**: `cDAC < DAC` (cDAC reports 4 refs, DAC reports 10-13).
ExceptionHandling debuggee only, 4 deterministic occurrences per run.

**Root cause**: The cDAC's `AMD64Unwinder.Unwind` (and equivalents for other
architectures) can only unwind **managed** frames — it checks
`ExecutionManager.GetCodeBlockHandle(IP)` first and returns false if the IP
is not in a managed code range. This means it cannot unwind through native
runtime frames (allocation helpers, EH dispatch code, etc.).

When the allocation stress point fires during exception first-pass dispatch:

1. The thread's `m_pFrame` is `FRAME_TOP` (no explicit Frames in the chain
   because the InlinedCallFrame/SoftwareExceptionFrame have been popped or
   not yet pushed at that point in the EH dispatch sequence)
2. The initial IP is in native code (allocation helper)
3. The cDAC attempts to unwind through native frames but
   `GetCodeBlockHandle` returns null for native IPs → unwind fails
4. With no Frames and no ability to unwind, the walk stops early

The legacy DAC's `DacStackReferenceWalker::WalkStack` succeeds because
`StackWalkFrames` calls `VirtualUnwindToFirstManagedCallFrame` which uses
OS-level unwind (`RtlVirtualUnwind` on Windows, `PAL_VirtualUnwind` on Unix)
that can unwind ANY native frame using PE `.pdata`/`.xdata` sections.

**Possible fixes**:
1. **Ensure Frames are always available** — change the runtime to keep
   an explicit Frame pushed during allocation points within EH dispatch.
   The cDAC cannot do OS-level native unwind (it operates on dumps where
   `RtlVirtualUnwind` is not available). The Frame chain is the only
   mechanism the cDAC has for transitioning through native code to reach
   managed frames. If `m_pFrame = FRAME_TOP` when the IP is native, the
   cDAC cannot proceed.
2. **Accept as known limitation** — these failures only occur during
   live-process stress testing at a narrow window during EH first-pass
   dispatch. In dumps, the exception state is frozen and the Frame chain
   is consistent.
