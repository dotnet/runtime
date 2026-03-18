# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps and differences between the cDAC's stack reference
enumeration (`ISOSDacInterface::GetStackReferences`) and the runtime's GC root scanning.

## GC Stress Test Results

With `DOTNET_GCStress=0x24` (instruction-level JIT stress + cDAC verification):
- ~25,200 PASS / ~55 FAIL out of ~25,300 stress points (99.8% pass rate)
- All 55 failures have delta=1 (RT reports 1 more ref than cDAC)

## Known Issues

### 1. One GC Slot Missing Per Dynamic Method Stack Walk

**Severity**: Low
**Pattern**: `cDAC < RT` (diff=-1), RT has one extra stack-based copy of a GC ref

The remaining 55 failures each show the RT reporting one GC object at both a register
location (Address=0) and a stack spill address, while the cDAC only reports the register
copy. This is NOT caused by `FindMethodCode` failing for RangeList sections — investigation
confirmed that JIT'd dynamic method code (InvokeStub_*) lives in CODEHEAP sections with
nibble maps, and the cDAC resolves them successfully.

The root cause is a subtle difference in GcInfo slot decoding. The runtime reports one
additional stack-spilled copy of a GC ref that the cDAC misses, likely due to:
- Different handling of callee-saved register spill slots
- Or a funclet parent frame flag (known issue #4) causing the runtime to report
  an extra slot that the cDAC skips

**Follow-up**: Add per-frame GC slot logging to identify which specific frame and
GcInfo slot produces the extra ref, then compare cDAC vs runtime GcInfo decoding
for that frame.

### 2. Frame Context Restoration Causes Duplicate Walks

**Severity**: Low — mitigated by dedup in stress tool
**Pattern**: `cDAC > RT` (diff=+1 to +3), same Address/Object from two Source IPs

When a non-leaf Frame's `UpdateContextFromFrame` restores a managed IP that was
already walked from the initial context (or will be walked via normal unwinding),
the same managed frame gets walked twice at different offsets. This produces
duplicate GC slot reports.

The stress tool's `DeduplicateRefs` filter removes stack-based duplicates
(same Address/Object/Flags), but register-based duplicates (Address=0) with
different Source IPs are not caught.

**Mitigations in place**:
- `callerSP` Frame skip in `CreateStackWalk` (prevents most leaf-level duplicates)
- `SkipCurrentFrameInCheck` for active `InlinedCallFrame` (prevents ICF re-encounter)
- `DeduplicateRefs` in stress tool (removes stack-based duplicates)

**Follow-up**: Track walked method address ranges in the cDAC's stack walker and
suppress duplicate `SW_FRAMELESS` yields for methods already visited.

### 3. PromoteCallerStack — Implemented

**Status**: Implemented — GCRefMap path + MetaSig fallback + DynamicHelperFrame scanning
**Affected frames**: `StubDispatchFrame`, `ExternalMethodFrame`, `CallCountingHelperFrame`,
`PrestubMethodFrame`, `DynamicHelperFrame`

These Frame types call `PromoteCallerStack` / `PromoteCallerStackUsingGCRefMap`
to report method arguments from the transition block. The cDAC now implements:

1. **GCRefMap-based scanning** for StubDispatchFrame (when cached) and ExternalMethodFrame
2. **MetaSig-based scanning** for PrestubMethodFrame, CallCountingHelperFrame, and
   StubDispatchFrame (when GCRefMap is null — dynamic/LCG methods)
3. **DynamicHelperFrame flag-based scanning** for argument registers

The MetaSig path parses ECMA-335 MethodDefSig format (including ELEMENT_TYPE_INTERNAL
for runtime-internal types in dynamic method signatures) and maps parameter positions
to transition block offsets using the GCRefMap position scheme.

This reduced the per-failure delta from 3 to 1 for all 55 failures. The remaining
delta is from issue #1 (RangeList code heap resolution).

**Not yet implemented**:
- CLRToCOMMethodFrame (COM interop, requires return value promotion)
- PInvokeCalliFrame (requires VASigCookie-based signature reading)
- Value type GCDesc scanning in MetaSig path (ELEMENT_TYPE_VALUETYPE with embedded refs)
- x86-specific register ordering in OffsetFromGCRefMapPos

### 4. Funclet Parent Frame Flags Not Consumed

**Severity**: Low — only affects exception handling scenarios
**Flags**: `ShouldParentToFuncletSkipReportingGCReferences`,
`ShouldParentFrameUseUnwindTargetPCforGCReporting`,
`ShouldParentToFuncletReportSavedFuncletSlots`

The `Filter` method computes these flags for funclet parent frames, but
`WalkStackReferences` does not act on them. This could cause:
- Double-reporting of slots already reported by a funclet
- Using the wrong IP for GC liveness lookup on catch/finally parent frames
- Missing callee-saved register slots from unwound funclets

**Follow-up**: Wire up `ParentOfFuncletStackFrame` flag to `EnumGcRefs`.
Requires careful validation — an initial attempt caused 253 regressions
because `Filter` sets the flag too aggressively.

### 5. Interior Stack Pointers

**Severity**: Informational — handled in stress tool
**Pattern**: cDAC reports interior pointers whose Object is a stack address

The runtime's `PromoteCarefully` (siginfo.cpp) filters out interior pointers
whose object value is a stack address. These are callee-saved register values
(RSP/RBP) that GcInfo marks as live interior slots but don't point to managed
heap objects. The cDAC reports all GcInfo slots faithfully.

**Mitigation**: The stress tool's `FilterInteriorStackRefs` removes these
before comparison, matching the runtime's behavior.

### 6. forceReportingWhileSkipping State Machine Incomplete

**Severity**: Low — theoretical gap
**Location**: `StackWalk_1.cs` Filter method

The `ForceGcReportingStage` state machine transitions `Off → LookForManagedFrame
→ LookForMarkerFrame` but never transitions back to `Off`. The native code checks
if the caller IP is within `DispatchManagedException` / `RhThrowEx` to deactivate.

**Follow-up**: Implement marker frame detection.
