# Design Document: Stub Target Debug Data

**Status:** Draft  
**Author:** GitHub Copilot  
**Date:** 2026-02-05

## 1. Executive Summary

This document proposes a rough design for adding debug data to runtime stubs, with an initial focus on stubs compiled from IL. The goal is to enable a simplified stub stepping mechanism in the debugger, reducing complexity and maintenance burden of the current bespoke StubManager prediction logic, while supporting future inlining scenarios where stubs can be inlined into user methods or other stubs.

This is an incomplete sketch I drafted quickly with copilot. Nothing here has been implemented in the runtime. I wanted to save the work-in-progress assuming that it will be useful to resume at some point, but it isn't urgent to implement right now and doing a good job on the design needs additional time. Folks are welcome to comment on the PR with suggestions but I'm not planning to spend much time revising the doc further for now. Instead what I'd like to do is check it in as-is with the expectation that anyone who resumes this work in the future should review the PR comments before moving forward with it.

## 2. Background

### 2.1 Current State

The .NET runtime uses various "stubs" - small adapter methods that perform setup work before branching/calling to a target method. During managed debugging, these stubs should be automatically stepped through without exposing implementation details to developers.

Currently, stub stepping is implemented through:
- **StubManagers:** Specialized managers for different stub types that recognize and predict stub behavior
- **DoTraceStub:** Methods that analyze stub code and predict the next execution location
- **TraceManager:** More complex prediction logic that may execute inside locks or GC no-trigger regions

### 2.2 Problems with Current Approach

1. **Complexity:** Each StubManager needs particularized understanding of its stub type
2. **Maintenance Burden:** Stub generators and StubManagers must be kept in sync
3. **Execution Context Issues:** Prediction logic may not be legal to run in certain contexts (locks, GC no-trigger regions)
4. **Out-of-Process Compatibility:** Current approach doesn't align well with declarative stepping patterns needed for out-of-process debuggers

## 3. Proposed Solution

### 3.1 Core Concept

Extend our existing debug data for IL-compiled code to annotate stub native call instruction addresses that should be stepped into. In the future other non IL based stubs could generate the same debug data. A new general purpose StubManager would be created to step through stubs that have this debug data.

At a high level, the stub stepping flow becomes:
1. At stub code generation time extra debug data is produced and stored in a similar manner as our existing method debug data.
2. At debugging time the stepping code encounters an IP that might belong to one of these stubs.
3. The new StubManager looks up the debug data corresponding that IP. The StubManager claims the stub if the appropriate debug data exists and the debug data indicates the IP is inside the range of a stub. For non-inlined cases the range is the whole method.
4. The debug data will indicate which native call instruction(s) go to the stub target method(s).
5. Within a stub code region, step through the code one instruction at a time. Call instructions that were enumerated in the debug data are stepped into, other call instructions are stepped over.

**Access and compatibility:** No new debugger APIs are in the initial scope; only StubManager logic needs to read the data. The debug data format should be describable via a data contract for future scenarios.

### 3.2 Inlining Considerations

We must assume stubs can be inlined into user methods and stubs can be inlined into other stubs. The debug data must therefore:

- Disambiguate which code regions originate from a stub versus user code.
- Preserve the stub callout annotations even when a stub is inlined.
- Allow stepping logic to treat stub regions as “step-through” segments without misattributing user code.

### 3.3 Stub Annotation Inputs (IL Generation)

IL-compiled stub generators need a simple API to mark IL instructions that represent control transfers to the target method(s). The API should operate on IL offsets, since generators operate at the IL level.

**Proposed API (conceptual):**
```cpp
// On IL stub generation helpers (e.g., ILStubResolver or ILStubLinker)
class ILStubDebugDataBuilder
{
public:
  // Record that the IL instruction at ilOffset calls the stub target method.
  void RecordStubTargetCallILOffset(uint32_t ilOffset);
};
```

**Example usage (conceptual):**
```cpp
// During IL stub generation for a P/Invoke stub
ILCodeStream* il = pILStubLinker->GetILStream();
ILStubDebugDataBuilder* debugData = pILStubResolver->GetDebugDataBuilder();

// ... emit marshaling and setup IL ...

uint32_t callILOffset = il->GetCurrentOffset();
il->EmitCALLI(/* target signature */);
debugData->RecordStubTargetCallILOffset(callILOffset);

// ... emit cleanup IL ...
```

This yields a list of IL offsets that identify target-call instructions in the IL stream. The list may contain multiple entries for stubs with multiple call sites.

### 3.4 Stub Debug Data Format

The stub debug data should be encoded as a compact extension to existing debug info and use the same conventions (nibble-encoded integers, delta encoding). The data stores **native offsets only**.

**Proposed chunk: `StubDebugInfo`**

*Layout (nibble-encoded U32 values):*
1. `Version` (initially 1)
2. `CallCount`
3. `CallOffsets[CallCount]` (native offsets, strictly increasing)

*Encoding conventions:*
- `CallOffsets` are stored as delta-encoded unsigned integers. The first offset is encoded relative to the method’s native code start address and subsequent entries are deltas from the previous offset.
- Offsets are relative to the method’s native code start address (matching existing debug info conventions).

This format mirrors existing debug-info encoding patterns and is compact for typical stubs (few call sites).

### 3.5 Serialization and IL-to-Native Resolution

When serializing debug info, the IL offsets recorded by stub generators must be resolved to the native call-instruction offsets already emitted by the JIT.

**Proposed resolution point:** `CompressDebugInfo::Compress` in [src/coreclr/vm/debuginfostore.cpp](src/coreclr/vm/debuginfostore.cpp).

**Proposed data flow for IL offsets:**
1. The IL stub generator records target-call IL offsets into an `ILStubDebugDataBuilder` owned by the IL stub resolver (e.g., `ILStubResolver`).
2. When the stub `MethodDesc` is created, the resolver persists the IL offset list alongside other IL stub metadata.
3. During JIT compilation, the `MethodDesc` exposes the stored IL offset list to the JIT interface so it can be carried through to debug info serialization.
4. `CompressDebugInfo::Compress` receives the IL offset list as an additional input and resolves it against the JIT-produced call-instruction mappings.

This method already has access to:
- `pOffsetMapping`/`iOffsetMapping` which include `SourceTypes.CallInstruction` entries
- `pRichOffsetMappings`/`iRichOffsetMappings` for inline-aware mappings when enabled

**Resolution logic (conceptual):**
1. For each IL offset from `RecordStubTargetCallILOffset`, scan `pOffsetMapping` and select mappings where `SourceTypes.CallInstruction` is set and the IL offset matches.
2. Emit the matching native offsets into `CallOffsets`.
3. Serialize the resulting `StubDebugInfo` blob alongside other debug info chunks.

This keeps all IL-to-native resolution at serialization time, using the already-produced JIT debug mappings.

### 3.6 Debug Data Store Access APIs

Add APIs to retrieve stub debug info from the debug data store, mirroring existing rich debug info access patterns.

**Proposed APIs (conceptual):**
```cpp
struct StubTargetCallList
{
  uint32_t Count;
  uint32_t* NativeOffsets; // fpNew-allocated
};

BOOL IJitManager::GetStubDebugInfo(
  const DebugInfoRequest& request,
  FP_IDS_NEW fpNew, void* pNewData,
  StubTargetCallList* callList);
```

An API to access the inlining data for stub ranges is also needed once stub inlining is supported but no design for it is included here.

**Implementation notes:**
- Add `CompressDebugInfo::RestoreStubDebugInfo` to decode the `StubDebugInfo` chunk.
- Add `EECodeGenManager::GetStubDebugInfoWorker` and forwarders in `EEJitManager` (and `ReadyToRunJitManager` if needed) analogous to `GetRichDebugInfo`.

## 4. Design Questions

1. **Multiple Call Sites:** Record all control-transfer sites as an array of native offsets.
2. **Cross-Platform Consistency:** Prefer a platform-agnostic data format.
3. **Debug Data Availability:** Always produce the data at runtime; if it is not produced, debuggers will be unable to step through stubs.

---

## Appendix A: Background — Survey of Existing Stubs

### A.1 Stub Generation Mechanisms

Based on analysis of the codebase, stubs are generated through three primary mechanisms. This design focuses first on IL-compiled stubs; other stub types can be included in future phases. In this document, “IL-compiled stubs” refers to all stubs compiled from IL, while “ILStub” is reserved for the DynamicMethodDesc-based subset where `IsILStub()` reports `TRUE`.

#### A.1.1 IL-Compiled Stubs (IL-Based Generation) — Primary Focus
- **Location:** `src/coreclr/vm/ilstubcache.cpp`, `src/coreclr/vm/ilstubresolver.cpp`
- **Generated For:**
  - P/Invoke stubs
  - COM interop stubs
  - Delegate invoke stubs
  - Multicast delegate stubs
  - Reverse P/Invoke stubs (callbacks)
  - Unboxing stubs
  - Instantiating stubs
  - Async thunks (new in .NET 11)
- **Characteristics:**
  - Generated as IL and then JIT-compiled
  - Have associated MethodDesc objects
  - Full stack trace support
  - Can leverage JIT infrastructure

#### A.1.2 StubLinker (Assembly Code Generation) — Future Coverage
- **Location:** `src/coreclr/vm/stublink.cpp`, platform-specific implementations
- **Generated For:**
  - Prestub (method initialization)
  - Delegate invoke helpers
  - Shuffle thunks (tail call helpers)
  - Tailcall stubs (architecture-specific helpers)
  - COM callable wrappers
  - Various architecture-specific helper stubs
- **Characteristics:**
  - Direct assembly code emission
  - Fixed, predictable structure
  - No MethodDesc (except for InstantiatingStubs)
  - Platform-specific implementations (x86, x64, ARM, ARM64, RISC-V, LoongArch)

#### A.1.3 Virtual Stub Dispatch (VSD) — Future Coverage
- **Location:** `src/coreclr/vm/virtualcallstub.cpp`, `src/coreclr/vm/virtualcallstub.h`
- **Generated For:**
  - Interface dispatch calls
  - Virtual call optimization
- **Types:**
  - Lookup stubs (initial call site stub)
  - Dispatch stubs (monomorphic optimization)
  - Resolve stubs (polymorphic cache lookup)
- **Characteristics:**
  - Dynamically generated and cached
  - Performance-critical hot path
  - Extensive caching and backpatching

### A.2 Current StubManager Implementations

The codebase contains 11 StubManager types:

1. **ThePreStubManager** - Handles the prestub that initializes methods
2. **PrecodeStubManager** - Manages method precode stubs
3. **StubLinkStubManager** - Manages StubLinker-generated stubs
4. **JumpStubStubManager** - Handles jump stubs for JIT code
5. **RangeSectionStubManager** - Forwards to appropriate manager based on code section
6. **ILStubManager** - Manages DynamicMethodDesc-based ILStubs
7. **InteropDispatchStubManager** - Handles interop dispatch stubs
8. **TailCallStubManager** - Manages tail call helper stubs (x86 only)
9. **VirtualCallStubManager** - Handles virtual stub dispatch stubs
10. **VirtualCallStubManagerManager** - Coordinates multiple VSD managers
11. **CallCountingStubManager** - Manages tiered compilation call counting stubs

### A.3 Current Trace Mechanisms

StubManagers use several patterns for tracing:

1. **TRACE_MANAGED / TRACE_STUB** - Direct prediction of target address
2. **TRACE_FRAME_PUSH** - Pause at address, then query stack frame
3. **TRACE_MGR_PUSH** - Pause at address, then call TraceManager()
4. **TRACE_UNJITTED_METHOD** - Target is unjitted, return MethodDesc

Example from ILStubManager:
```cpp
BOOL ILStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination *trace)
{
    // ...
    trace->InitForManagerPush(stubStartAddress, this);
    return TRUE;
}
```

---

## Appendix B: Stub Type Summary

| Stub Type | Generation | Manager | Call Pattern | Feasibility |
|-----------|-----------|---------|--------------|-------------|
| PreStub | Assembly | ThePreStubManager | Call to init | Moderate |
| Precode | Assembly | PrecodeStubManager | Jump to target | High |
| Delegate Invoke | StubLinker | StubLinkStubManager | Call to target | High |
| Shuffle Thunk | StubLinker | StubLinkStubManager | Tail call | High |
| P/Invoke | IL-compiled | ILStubManager | calli | Moderate-High |
| COM Interop | IL-compiled | ILStubManager | calli | Moderate |
| Multicast Delegate | IL-compiled | ILStubManager | Multiple calls | Low |
| Unboxing Stub | IL-compiled | ILStubManager | Unbox + call | Moderate-High |
| Instantiating Stub | IL-compiled | ILStubManager | Call to target | Moderate-High |
| Async Thunk (.NET 11) | IL-compiled | ILStubManager | Call to target | Moderate-High |
| VSD Lookup | VSD | VirtualCallStubManager | Jump to resolver | Moderate |
| VSD Dispatch | VSD | VirtualCallStubManager | Conditional jump | Moderate |
| VSD Resolve | VSD | VirtualCallStubManager | Cache + jump | Moderate |
| Jump Stub | JIT | JumpStubStubManager | Direct jump | High |
| Tail Call Helper | Assembly | TailCallStubManager | Register indirect | Moderate |

## Appendix C: Example Debug Data Shape (IL-Compiled Stubs)

### Stub Annotation Record (Conceptual)
```cpp
// Conceptual data contract record (aligned with JIT debug info)
enum class StubAnnotationKind : uint8_t
{
  ControlTransfer, // Transfers control to target method
  StubRegionStart, // Beginning of stub-generated native region
  StubRegionEnd    // End of stub-generated native region
};

struct StubAnnotation
{
  uint32_t nativeOffset;   // Native offset inside the method body
    StubAnnotationKind kind;
};
```

### JIT Emission (Conceptual)
```cpp
// During JIT codegen for IL-compiled stubs
// Record native offsets for control transfers and stub regions.
void RecordStubAnnotations(const StubAnnotation* annotations, uint32_t count);
```

### ILStubManager Consumption (Conceptual)
```cpp
// Read JIT debug info + stub annotations and choose callout points for stepping.
BOOL ILStubManager::DoTraceStub(PCODE stubStartAddress, TraceDestination* trace)
{
  // Use inlining records from the JIT.
  // Use StubAnnotation records to locate control transfers and stub regions by native offset.
    // Configure stepping using existing managed-method logic.
    return TRUE;
}
```

---

## Appendix D: Maintenance Notes

1. **Validation:** Not in the initial scope; revisit testing once the design is better understood.
2. **Documentation:** This design document is sufficient for now; more documentation can be added later if needed.
3. **Fallback Strategy:** If debug data is missing, corrupted, or incorrect, it is acceptable for stepping to be inaccurate. A step-in may effectively behave like a step-over or step-out because the debugger adds extra breakpoints to stop execution at those points.

---

## Appendix E: References

### E.1 Existing Documentation
- [Virtual Stub Dispatch Design](../botr/virtual-stub-dispatch.md)
- [Book of the Runtime (BOTR)](../botr/README.md)
- Stub Manager comments in `src/coreclr/vm/stubmgr.h`
- StubLinker documentation in `src/coreclr/vm/stublink.h`

### E.2 Related Code Files
- **Stub Managers:** `src/coreclr/vm/stubmgr.cpp`, `src/coreclr/vm/stubmgr.h`
- **StubLinker:** `src/coreclr/vm/stublink.cpp`, `src/coreclr/vm/stublink.h`
- **IL-based stubs:** `src/coreclr/vm/ilstubresolver.cpp`, `src/coreclr/vm/ilstubcache.cpp`
- **VSD:** `src/coreclr/vm/virtualcallstub.cpp`, `src/coreclr/vm/virtualcallstub.h`
- **Debugger Controller:** `src/coreclr/debug/ee/controller.cpp`

### E.3 Platform-Specific Implementations
- **x86/x64:** `src/coreclr/vm/i386/stublinkerx86.cpp`, `src/coreclr/vm/amd64/`
- **ARM/ARM64:** `src/coreclr/vm/arm/`, `src/coreclr/vm/arm64/`
- **RISC-V:** `src/coreclr/vm/riscv64/`
- **LoongArch:** `src/coreclr/vm/loongarch64/`
