# EH ThrowHelper Extra Ref — Updated Investigation

## Root Cause (refined)

The cDAC's GcInfoDecoder reports 1 live stack slot from ThrowHelper at the
throw-site offset (e.g. offset=64) that the native GcInfoDecoder does not.

### What we've confirmed:
1. All three walkers (cDAC, DAC, RT) visit ThrowHelper during EH first-pass
2. The extra slot is: stack base=2 (RBP-relative), offset=-8, gcFlags=0x0
3. ExecutionAborted is NOT set by either side (FaultingExceptionFrame is not
   on the frame chain for managed throws)
4. The cDAC's scratch register/slot filtering matches the native code exactly
5. The native EnumGcRefs reports 0 refs from ThrowHelper at the same offset

### Remaining hypothesis:
The GcInfo safe-point live-state bitmap for ThrowHelper at offset 64 indicates
0 live slots in the native decoder but 1 live slot in the cDAC decoder. This
could be:
- A difference in safe-point index computation (FindSafePoint)
- A difference in the live-state bitmap reading
- The native decoder may not find a safe-point match and use the interruptible
  range path, while the cDAC finds a match at a different index

### Next steps:
1. Decode ThrowHelper's GcInfo manually to verify the safe-point table
2. Compare FindSafePoint results between native and cDAC
3. Check if the offset is at a safe point in both decoders
