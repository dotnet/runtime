# cDAC Stack Reference Walking — Known Issues

This document tracks known gaps and differences between the cDAC's stack reference
enumeration (`ISOSDacInterface::GetStackReferences`) and the runtime's GC root scanning.

## GC Stress Test Results

With `DOTNET_GCStress=0x24` (instruction-level JIT stress + cDAC verification):
- ~25,000 PASS / ~125 FAIL out of ~25,100 stress points (99.5% pass rate)

## Known Issues

### 1. Dynamic Method / IL Stub GC Refs Not Enumerated

**Severity**: Low — matches legacy DAC behavior
**Affected methods**: `dynamicclass::InvokeStub_*` (reflection invoke stubs), LCG methods
**Pattern**: `cDAC < RT` (diff=-1), always missing `RT[0]` register ref

The cDAC (and legacy DAC) cannot resolve code blocks for methods in RangeList-based
code heaps (HostCodeHeap). Both `EEJitManager::JitCodeToMethodInfo` and the cDAC's
`FindMethodCode` return failure for `RANGE_SECTION_RANGELIST` sections. This means
GcInfo cannot be decoded for these methods, and their GC refs are not reported.

The runtime's `GcStackCrawlCallBack` reports additional refs from these methods
because it processes them through the Frame chain (`ResumableFrame`, `InlinedCallFrame`)
which has access to the register state.

This is a pre-existing gap in the DAC's diagnostic API, not a cDAC regression.

**Follow-up**: Implement RangeList-based code lookup in the cDAC's ExecutionManager.
This requires reading the `HostCodeHeap` linked list and matching IPs to code headers
within dynamic code heaps.

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

### 3. PromoteCallerStack Not Implemented for Stub Frames

**Severity**: Low — not currently manifesting in GC stress tests
**Affected frames**: `StubDispatchFrame`, `ExternalMethodFrame`, `CallCountingHelperFrame`,
`DynamicHelperFrame`, `CLRToCOMMethodFrame`

These Frame types call `PromoteCallerStack` / `PromoteCallerStackUsingGCRefMap`
to report method arguments from the transition block. The cDAC's `ScanFrameRoots`
is a no-op for these frame types.

This gap doesn't manifest in GC stress testing because stub frame arguments are
not the source of the current count differences. However, it IS a DAC parity gap —
the legacy DAC reports these refs via `Frame::GcScanRoots`.

**Follow-up**: Port `GCRefMapDecoder` to managed code and implement
`PromoteCallerStackUsingGCRefMap` in `ScanFrameRoots`. Prototype implementation
exists (stashed as "PromoteCallerStack implementation + GCRefMapDecoder").

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
