# Design Document: Stub Target Debug Data

**Status:** Draft  
**Author:** GitHub Copilot  
**Date:** 2026-02-05

## 1. Executive Summary

This document proposes a design for adding debug data to runtime stubs that records the address of the instruction which branches or calls to the target method. This debug data will enable a simplified, general-purpose stub stepping mechanism in the debugger, reducing complexity and maintenance burden of the current bespoke StubManager prediction logic.

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

## 3. Survey of Existing Stubs

### 3.1 Stub Generation Mechanisms

Based on analysis of the codebase, stubs are generated through three primary mechanisms:

#### 3.1.1 ILStubs (IL-Based Generation)
- **Location:** `src/coreclr/vm/ilstubcache.cpp`, `src/coreclr/vm/ilstubresolver.cpp`
- **Generated For:**
  - P/Invoke stubs
  - COM interop stubs
  - Delegate invoke stubs
  - Multicast delegate stubs
  - Reverse P/Invoke stubs (callbacks)
- **Characteristics:** 
  - Generated as IL and then JIT-compiled
  - Have associated MethodDesc objects
  - Full stack trace support
  - Can leverage JIT infrastructure

#### 3.1.2 StubLinker (Assembly Code Generation)
- **Location:** `src/coreclr/vm/stublink.cpp`, platform-specific implementations
- **Generated For:**
  - Prestub (method initialization)
  - Delegate invoke helpers
  - Shuffle thunks (tail call helpers)
  - COM callable wrappers
  - Various architecture-specific helper stubs
- **Characteristics:**
  - Direct assembly code emission
  - Fixed, predictable structure
  - No MethodDesc (except for InstantiatingStubs)
  - Platform-specific implementations (x86, x64, ARM, ARM64, RISC-V, LoongArch)

#### 3.1.3 Virtual Stub Dispatch (VSD)
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

### 3.2 Current StubManager Implementations

The codebase contains 11 StubManager types:

1. **ThePreStubManager** - Handles the prestub that initializes methods
2. **PrecodeStubManager** - Manages method precode stubs
3. **StubLinkStubManager** - Manages StubLinker-generated stubs
4. **JumpStubStubManager** - Handles jump stubs for JIT code
5. **RangeSectionStubManager** - Forwards to appropriate manager based on code section
6. **ILStubManager** - Manages IL-generated stubs
7. **InteropDispatchStubManager** - Handles interop dispatch stubs
8. **TailCallStubManager** - Manages tail call helper stubs (x86 only)
9. **VirtualCallStubManager** - Handles virtual stub dispatch stubs
10. **VirtualCallStubManagerManager** - Coordinates multiple VSD managers
11. **CallCountingStubManager** - Manages tiered compilation call counting stubs

### 3.3 Current Trace Mechanisms

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

## 4. Proposed Solution

### 4.1 Core Concept

Add a small data structure for each stub that records:
- The address of the instruction that branches/calls to the target method
- Optionally, the offset of that instruction within the stub

A new general-purpose StubManager can then:
1. Read this debug data
2. Set a breakpoint at the recorded address
3. Single-step from that address to reach the target (managed code or another stub)

### 4.2 Stub Categories

Based on the survey, stubs fall into distinct categories regarding their ability to support this pattern:

#### 4.2.1 Good Candidates (High Feasibility)

**StubLinker-Generated Stubs:**
- Fixed, predictable structure
- Known branch/call instructions
- Debug data can be added during stub generation
- Examples: Delegate invoke, shuffle thunks, COM wrappers

**Simple ILStubs:**
- P/Invoke stubs with straightforward IL
- Delegate invoke stubs
- Known calli/call instructions in IL

#### 4.2.2 Moderate Candidates (Medium Feasibility)

**Virtual Stub Dispatch:**
- Lookup stubs: jump to resolver
- Dispatch stubs: conditional jump to target
- Resolve stubs: cache lookup then jump
- Challenges: Performance-critical, polymorphic behavior, extensive caching

**Prestub:**
- Calls into initialization logic
- Target not known until after initialization
- May still benefit from declaring the call-out point

#### 4.2.3 Challenging Candidates (Lower Feasibility)

**Complex ILStubs:**
- Multiple call sites to different targets
- Conditional logic determining target
- May require multiple debug data entries per stub

**Multicast Delegates:**
- Iterate through invocation list
- Multiple targets in sequence
- Current TRACE_MULTICAST_DELEGATE_HELPER already handles this specially

### 4.3 Debug Data Structure Options

#### Option A: In-Stub Data Structure
```cpp
struct StubTargetDebugData
{
    PCODE stubAddress;           // Base address of stub
    uint32_t targetCallOffset;   // Offset to branch/call instruction
    uint32_t flags;              // Type flags, metadata
};
```

**Pros:**
- Simple, direct association
- No additional lookups needed
- Can be embedded at end of stub code

**Cons:**
- Increases stub size
- Cache locality impact
- Different sizes for different stub types

#### Option B: Side Table with Hash Lookup
```cpp
class StubTargetDebugDataTable
{
    HashMap<PCODE, StubTargetDebugData> m_table;
    CrstStatic m_lock;
    
public:
    bool TryGetDebugData(PCODE stubAddr, StubTargetDebugData* pData);
    void RecordDebugData(PCODE stubAddr, uint32_t offset, uint32_t flags);
};
```

**Pros:**
- No stub size increase
- Flexible schema evolution
- Can be omitted entirely in Release builds

**Cons:**
- Hash lookup overhead
- Requires synchronization
- Additional memory overhead for table

#### Option C: Stub Class Extension
```cpp
class Stub
{
    // Existing fields...
    uint32_t m_numCodeBytesAndFlags;
    
    // New flag bit: HAS_DEBUG_DATA
    bool HasDebugData() { return (m_numCodeBytesAndFlags & DEBUG_DATA_BIT) != 0; }
    
    // Debug data stored immediately after stub code
    uint32_t* GetDebugDataPtr();
};
```

**Pros:**
- Minimal overhead when present
- Optional (controlled by flag bit)
- Natural association with Stub object

**Cons:**
- Only works for StubLinker stubs (not ILStubs)
- Variable stub structure complexity
- Requires flag bit (limited availability)

### 4.4 Recommended Hybrid Approach

**For StubLinker Stubs (Option C):**
- Extend Stub class with optional debug data
- Store offset to target instruction after code bytes
- Enable only in Debug/Checked builds initially

**For ILStubs (Option B with special handling):**
- Use JIT debug information infrastructure
- Record call/calli instruction offsets during IL generation
- Leverage existing ILStubResolver metadata

**For VSD Stubs:**
- Initially keep existing TraceManager approach
- Consider adding debug data as optimization later
- Performance impact must be carefully evaluated

### 4.5 Implementation Phases

#### Phase 1: Infrastructure (Weeks 1-2)
- Define StubTargetDebugData structure
- Extend Stub class with HasDebugData flag
- Create helper methods for reading/writing debug data
- Add debug data table infrastructure

#### Phase 2: StubLinker Integration (Weeks 3-4)
- Modify StubLinker to track branch/call instructions
- Emit debug data during stub finalization
- Add tests for various stub types

#### Phase 3: StubManager Updates (Weeks 5-6)
- Create UniversalStubManager (or extend existing manager)
- Implement debug-data-based DoTraceStub
- Fallback to existing StubManagers for unsupported stubs

#### Phase 4: ILStub Integration (Weeks 7-8)
- Extend ILStubLinker to record call instruction offsets
- Store in ILStubResolver metadata
- Update ILStubManager to use debug data

#### Phase 5: Testing & Validation (Weeks 9-10)
- Debugger stepping tests
- Performance benchmarks
- Stress testing
- Documentation updates

## 5. Open Questions

### 5.1 Technical Questions

1. **VSD Performance Impact:** What is the acceptable overhead for adding debug data to VSD stubs? These are extremely performance-sensitive.

2. **Multiple Call Sites:** How should we handle stubs with multiple call/branch instructions? Options:
   - Record all of them (array of offsets)
   - Record only the "primary" target
   - Use heuristics to determine most relevant

3. **Dynamic Stub Updates:** VSD stubs are patched/updated dynamically. How do we maintain debug data consistency?
   - Option A: Update debug data on each patch
   - Option B: Debug data points to stable location
   - Option C: Invalidate debug data on patch

4. **Cross-Platform Consistency:** Different platforms have different stub implementations. Should debug data format be:
   - Platform-agnostic (same structure everywhere)
   - Platform-specific (optimized for each architecture)
   - Hybrid (common core with platform extensions)

5. **Debugging Build Impact:** Should debug data be:
   - Debug builds only
   - Checked builds only
   - All builds (with minimal overhead)

### 5.2 Design Questions

1. **Scope of First Implementation:** Should we target all stub types or start with a subset?
   - Recommendation: Start with StubLinker stubs (highest benefit-to-effort ratio)

2. **Backward Compatibility:** How do we handle:
   - Debuggers that don't understand new debug data
   - Old StubManagers vs. new universal manager
   - Mixed scenarios during transition

3. **DAC Support:** Debug data must be accessible from debugger process:
   - How to marshal data structures
   - DACized pointer handling
   - Performance implications

4. **Out-of-Process Debugger API:** What API should expose this data?
   - ICorDebug extension
   - New IXCLRData interfaces
   - Diagnostic protocol extension

### 5.3 Maintenance Questions

1. **Validation:** How to ensure debug data stays in sync with stub code?
   - Automated tests comparing prediction with actual execution
   - Assertions during stub generation
   - Periodic verification passes

2. **Documentation:** What level of documentation is needed?
   - For stub authors
   - For debugger developers
   - For runtime maintainers

3. **Fallback Strategy:** What happens when debug data is:
   - Missing (stub generated without it)
   - Corrupted (memory corruption)
   - Incorrect (bug in generation)

## 6. Success Criteria

### 6.1 Functional Requirements

1. **Correctness:** Debugger successfully steps through stubs to target methods
2. **Coverage:** At least 80% of common stubs supported in Phase 1
3. **Reliability:** No regressions in existing debugger functionality

### 6.2 Performance Requirements

1. **Cold Startup:** No measurable regression in application startup time
2. **Stub Generation:** <5% overhead in stub creation time
3. **Memory Overhead:** <1KB per 100 stubs for debug data

### 6.3 Maintainability Requirements

1. **Reduced Complexity:** Eliminate at least 50% of StubManager-specific trace logic
2. **Test Coverage:** >90% coverage for debug data generation and consumption
3. **Documentation:** Complete developer guide for adding debug data to new stub types

## 7. Risks and Mitigations

### 7.1 Performance Risk
**Risk:** Adding debug data increases memory usage and affects performance-critical paths  
**Mitigation:**
- Start with Debug/Checked builds only
- Careful measurement at each phase
- Opt-in mechanism for Release builds
- Zero overhead when feature disabled

### 7.2 Complexity Risk
**Risk:** New infrastructure adds complexity during transition period  
**Mitigation:**
- Maintain existing StubManagers as fallback
- Gradual rollout, one stub type at a time
- Comprehensive testing at each step
- Clear rollback strategy

### 7.3 Compatibility Risk
**Risk:** Changes may break existing debuggers or tools  
**Mitigation:**
- Maintain backward compatibility
- Version debug data format
- Feature flag for new behavior
- Extensive compatibility testing

### 7.4 Platform Risk
**Risk:** Platform-specific implementations may diverge  
**Mitigation:**
- Define common abstractions
- Platform-specific tests
- Regular cross-platform validation
- Shared test infrastructure

## 8. Alternatives Considered

### 8.1 Enhanced Symbol Information
Instead of runtime debug data, enhance PDB symbols to describe stub behavior.

**Pros:** Reuses existing infrastructure  
**Cons:** Not available for dynamically-generated stubs; requires PDB present

### 8.2 IL Annotation
Extend IL metadata to include stub stepping hints.

**Pros:** Familiar to IL tools  
**Cons:** Only works for ILStubs; doesn't help StubLinker stubs

### 8.3 Debugger-Side Heuristics
Improve debugger's pattern recognition to step through stubs.

**Pros:** No runtime changes needed  
**Cons:** Fragile; platform-specific; high maintenance burden

## 9. Next Steps

1. **Gather Feedback** on this design document from:
   - Runtime team
   - Debugger team
   - JIT team (for ILStub integration)

2. **Create Prototype** of StubLinker integration:
   - Add debug data to simple delegate invoke stub
   - Implement UniversalStubManager
   - Demonstrate end-to-end stepping

3. **Performance Baseline:**
   - Measure current stub generation time
   - Measure current stepping performance
   - Establish metrics for comparison

4. **Refine Design** based on:
   - Prototype learnings
   - Performance data
   - Team feedback

## 10. References

### 10.1 Existing Documentation
- [Virtual Stub Dispatch Design](../botr/virtual-stub-dispatch.md)
- [Book of the Runtime (BOTR)](../botr/README.md)
- Stub Manager comments in `src/coreclr/vm/stubmgr.h`
- StubLinker documentation in `src/coreclr/vm/stublink.h`

### 10.2 Related Code Files
- **Stub Managers:** `src/coreclr/vm/stubmgr.cpp`, `src/coreclr/vm/stubmgr.h`
- **StubLinker:** `src/coreclr/vm/stublink.cpp`, `src/coreclr/vm/stublink.h`
- **ILStubs:** `src/coreclr/vm/ilstubresolver.cpp`, `src/coreclr/vm/ilstubcache.cpp`
- **VSD:** `src/coreclr/vm/virtualcallstub.cpp`, `src/coreclr/vm/virtualcallstub.h`
- **Debugger Controller:** `src/coreclr/debug/ee/controller.cpp`

### 10.3 Platform-Specific Implementations
- **x86/x64:** `src/coreclr/vm/i386/stublinkerx86.cpp`, `src/coreclr/vm/amd64/`
- **ARM/ARM64:** `src/coreclr/vm/arm/`, `src/coreclr/vm/arm64/`
- **RISC-V:** `src/coreclr/vm/riscv64/`
- **LoongArch:** `src/coreclr/vm/loongarch64/`

---

## Appendix A: Stub Type Summary

| Stub Type | Generation | Manager | Call Pattern | Feasibility |
|-----------|-----------|---------|--------------|-------------|
| PreStub | Assembly | ThePreStubManager | Call to init | Moderate |
| Precode | Assembly | PrecodeStubManager | Jump to target | High |
| Delegate Invoke | StubLinker | StubLinkStubManager | Call to target | High |
| Shuffle Thunk | StubLinker | StubLinkStubManager | Tail call | High |
| P/Invoke | ILStub | ILStubManager | calli | Moderate-High |
| COM Interop | ILStub | ILStubManager | calli | Moderate |
| Multicast Delegate | ILStub | ILStubManager | Multiple calls | Low |
| VSD Lookup | VSD | VirtualCallStubManager | Jump to resolver | Moderate |
| VSD Dispatch | VSD | VirtualCallStubManager | Conditional jump | Moderate |
| VSD Resolve | VSD | VirtualCallStubManager | Cache + jump | Moderate |
| Jump Stub | JIT | JumpStubStubManager | Direct jump | High |
| Tail Call Helper | Assembly | TailCallStubManager | Register indirect | Moderate |

## Appendix B: Example Debug Data Implementation

### Stub Class Extension
```cpp
// In stublink.h
class Stub
{
    // ... existing members ...
    
    enum
    {
        DEBUG_DATA_BIT = 0x08000000,  // New flag bit
        // ... existing flags ...
    };
    
public:
    bool HasTargetDebugData() const
    {
        return (m_numCodeBytesAndFlags & DEBUG_DATA_BIT) != 0;
    }
    
    uint32_t GetTargetCallOffset() const
    {
        _ASSERTE(HasTargetDebugData());
        // Debug data stored as uint32_t after code bytes
        uint32_t* pData = (uint32_t*)(GetBlob() + GetNumCodeBytes());
        return *pData;
    }
    
    void SetTargetDebugData(uint32_t offset)
    {
        m_numCodeBytesAndFlags |= DEBUG_DATA_BIT;
        uint32_t* pData = (uint32_t*)(GetBlob() + GetNumCodeBytes());
        *pData = offset;
    }
};
```

### StubLinker Integration
```cpp
// In stublink.cpp
class StubLinker
{
    CodeLabel* m_pTargetCallLabel;  // New member
    
public:
    // New method to mark target call instruction
    void MarkTargetCall()
    {
        m_pTargetCallLabel = EmitNewCodeLabel();
    }
    
    Stub* Link(LoaderHeap *heap, DWORD flags, const char *stubType)
    {
        // ... existing linking code ...
        
        // If we marked a target call, record its offset
        if (m_pTargetCallLabel != nullptr)
        {
            uint32_t offset = GetLabelOffset(m_pTargetCallLabel);
            pStub->SetTargetDebugData(offset);
        }
        
        return pStub;
    }
};
```

### Universal Stub Manager
```cpp
// New or in stubmgr.cpp
class UniversalStubManager : public StubManager
{
public:
    BOOL DoTraceStub(PCODE stubStartAddress, TraceDestination *trace) override
    {
        Stub* pStub = Stub::RecoverStub(stubStartAddress);
        
        if (pStub->HasTargetDebugData())
        {
            uint32_t offset = pStub->GetTargetCallOffset();
            PCODE targetCallAddr = stubStartAddress + offset;
            
            // Set breakpoint at the call instruction
            trace->InitForFramePush(targetCallAddr);
            return TRUE;
        }
        
        // Fallback to existing logic
        return FALSE;
    }
};
```
