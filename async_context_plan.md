# Plan: Fix Async Context Save/Restore Bug

## Problem Statement
Currently, in `async.cpp`, the `SaveAndRestoreContexts` phase inserts try-finally blocks around **individual async calls** to save and restore SynchronizationContext/ExecutionContext. This is incorrect. The try-finally should wrap the **entire method body**, similar to how synchronized methods (MethodImplOptions.Synchronized) are handled.

## Current Architecture (Buggy)

### SaveAsyncContexts Phase (Early - after import, before inlining)
Located in: `async.cpp::Compiler::SaveAsyncContexts()`

**Current behavior (WRONG):**
- For each async call that needs context save/restore:
  1. Creates temp locals for ExecutionContext and SynchronizationContext
  2. Inserts `CaptureContexts` call **before the async call**
  3. If the call is in a try region, inserts a try-finally **around just that call**
  4. Inserts `RestoreContexts` call **after the async call** (or in finally)

**Problems:**
- Creates multiple try-finally regions (one per async call)
- Only protects individual calls, not the entire method
- Does not match the intended semantics

### TransformAsync Phase (Late - before lowering)
Located in: `async.cpp::AsyncTransformation::Run()`

Transforms async calls into state machine with suspension/resumption points. This phase is correct and should remain largely unchanged.

---

## Proposed Solution

Move context save/restore to be handled in `fgAddInternal()` phase (where synchronized methods are handled), wrapping the entire method body with a single try-finally.

### High-Level Changes

1. **Remove per-call try-finally insertion** from `SaveAsyncContexts`
2. **Add method-level try-finally** in `fgAddInternal()` phase
3. **Adjust context capture/restore logic** to work at method boundaries

---

## Detailed Implementation Plan

### Phase 1: Add Method-Level Try-Finally in fgAddInternal

**File:** `src/coreclr/jit/flowgraph.cpp`

#### Add new function `Compiler::fgAddAsyncContextSaveRestore()`:

**Location:** After `fgAddSyncMethodEnterExit()` (around line ~1600)

**Similar to:** `fgAddSyncMethodEnterExit()` (lines 1411-1600)

**Implementation:**
```cpp
void Compiler::fgAddAsyncContextSaveRestore()
{
    assert(compIsAsync());
    assert(UsesFunclets());
    assert(!fgFuncletsCreated);
    assert(fgPredsComputed);

    // Create try-finally structure similar to synchronized methods:
    // 0. Create context locals (ExecutionContext, SynchronizationContext)
    // 1. Split first BB to create try region entry
    // 2. Create finally handler block
    // 3. Add EH table entry
    // 4. Insert CaptureContexts call at try entry
    // 5. Insert RestoreContexts call in finally handler
    // 6. Handle all return blocks (convert to leave)
}
```

**Detailed Steps:**

0. **Create Context Locals:**
   - Create `lvaAsyncExecutionContextVar` (TYP_REF) using `lvaGrabTemp(true DEBUGARG(...))`
   - Create `lvaAsyncSynchronizationContextVar` (TYP_REF) using `lvaGrabTemp(true DEBUGARG(...))`
   - **Important:** These will need special-casing for OSR/EnC, same as `lvaMonAcquired`
   - Need to update code that handles `lvaMonAcquired` to also handle these context locals:
     - Frame layout code (to include them in frame header for EnC)
     - OSR transition code (to preserve/restore values)
     - Look for references to `lvaMonAcquired` and add similar handling for context locals
   - Initialize to null if not OSR (similar to lines 1551-1567 for lvaMonAcquired)

1. **Create Try Region:**
   - Split `fgFirstBB` to create `tryBegBB` (similar to line 1434)
   - Mark `tryLastBB = fgLastBB` before adding finally

2. **Create Finally Handler:**
   - Create new `BBJ_EHFINALLYRET` block after `tryLastBB`
   - Set artificial ref count
   - Insert `RestoreContexts` call
   - Add `GT_RETFILT` node to end finally

3. **Add EH Table Entry:**
   - Call `fgTryAddEHTableEntries()` (similar to line 1456)
   - Set `ebdHandlerType = EH_HANDLER_FINALLY` (NOT fault)
   - Set try/handler begin/end blocks
   - Update enclosing try indices for existing EH regions

4. **Insert CaptureContexts Call:**
   - At beginning of `tryBegBB`
   - Pass addresses of context locals
   - Similar to current logic in SaveAsyncContexts (lines ~138-148)

5. **Insert RestoreContexts Call:**
   - In finally handler block
   - Pass context local values (ExecutionContext, SynchronizationContext)
   - Pass "suspended" argument: `lvaAsyncContinuationArg != null`
     - This indicates whether we actually suspended (vs. completed synchronously)
     - If null, we never suspended so contexts don't need restoration
     - If non-null, we suspended and contexts must be restored
   - Similar to current logic in SaveAsyncContexts (lines ~177-180)

6. **Convert Return Blocks:**
   - Similar to `fgConvertSyncReturnToLeave()` (lines 1691-1700)
   - Convert each `BBJ_RETURN` to `BBJ_CALLFINALLY/BBJ_CALLFINALLYRET` sequence
   - Ensure single merged return block exists

#### Modify `Compiler::fgAddInternal()`:

**Location:** Around lines 2403-2406

**Add call to new function:**
```cpp
// After synchronized method handling:
if (UsesFunclets() && (info.compFlags & CORINFO_FLG_SYNCH) != 0)
{
    fgAddSyncMethodEnterExit();
}

// NEW: Add async context save/restore try-finally
if (UsesFunclets() && compIsAsync())
{
    fgAddAsyncContextSaveRestore();
}
```

**Ordering consideration:**
- Must be called BEFORE `genReturnBB` is created/merged
- Must be after `fgAddSyncMethodEnterExit()` (if both are needed)
- Similar timing to synchronized methods

---

### Phase 2: Update Compiler State

**File:** `src/coreclr/jit/compiler.h`

#### Add new compiler fields:

```cpp
// In Compiler class:
unsigned lvaAsyncExecutionContextVar;       // temp for ExecutionContext
unsigned lvaAsyncSynchronizationContextVar; // temp for SynchronizationContext
```

#### Add new compiler method declaration:

```cpp
void fgAddAsyncContextSaveRestore();
```

---

### Phase 3: Remove SaveAsyncContexts Phase

**File:** `src/coreclr/jit/async.cpp` and `src/coreclr/jit/compiler.cpp`

#### Delete or stub out `Compiler::SaveAsyncContexts()`:

The entire phase is no longer needed since:
- Context locals are created in `fgAddAsyncContextSaveRestore()`
- Try-finally is added in `fgAddInternal()`
- No per-call logic is required
- No per-call suspended indicator locals are needed (use continuation parameter instead)

**Options:**
1. Delete the function entirely and remove from phase list in `compiler.cpp`
2. Make it a no-op that returns `PhaseStatus::MODIFIED_NOTHING`

#### Delete `Compiler::InsertTryFinallyForContextRestore()`:
- This function (lines ~279-369) is no longer needed
- Can be deleted entirely

#### Remove validation helpers if they exist:
- `Compiler::ValidateNoAsyncSavesNecessary()`
- `Compiler::ValidateNoAsyncSavesNecessaryInStatement()`

#### Remove suspended indicator code from TransformAsync:
- `AsyncTransformation::ClearSuspendedIndicator()` (lines ~1248-1277) - DELETE
- `AsyncTransformation::SetSuspendedIndicator()` (lines ~1281-1320) - DELETE
- Calls to these functions (lines 812, 1754)
- Per-call suspended indicator local creation (lines 109-110)
- AsyncSuspendedIndicator argument handling (lines 130-131, 180, 1257, 1292)

---

### Phase 4: Update TransformAsync Phase

**File:** `src/coreclr/jit/async.cpp` and `src/coreclr/jit/gentree.h`

#### Modify `AsyncTransformation` class:

The async transformation phase expects context locals to exist for certain calls. Update to use the method-level locals:

**In `AsyncTransformation::CreateLiveSetForSuspension()` (around lines 859-866):**

- Currently excludes `asyncInfo.SynchronizationContextLclNum` from liveness
- **Change:** Use method-level `comp->lvaAsyncSynchronizationContextVar` instead
- Note: No need to handle per-call "suspended indicator" locals anymore

**In `AsyncTransformation::FillInDataOnSuspension()` (around lines 1593-1654):**

- Currently looks for `callInfo.SynchronizationContextLclNum` (lines 1598, 1624)
- This is set per-call in SaveAsyncContexts
- **Change:** Use method-level `comp->lvaAsyncSynchronizationContextVar` instead
- Remove assert at line 1598
- Update line 1624 to use method-level local
- Note: Per-call "suspended indicator" handling is removed; continuation parameter is the indicator

**In `AsyncTransformation::RestoreFromDataOnResumption()` (around lines 1786-1811):**

- Similar changes for ExecutionContext restore (if needed)
- Use method-level context locals

#### Remove field from AsyncCallInfo struct:

**File:** `src/coreclr/jit/gentree.h` (around line 4404)

Delete the field:
```cpp
unsigned SynchronizationContextLclNum = BAD_VAR_NUM;  // DELETE THIS LINE
```

This is trivial cleanup - just delete one line from the struct definition.

---

### Phase 7: Testing Considerations

#### Test Scenarios:

1. **Simple async method** with single await
2. **Multiple awaits** in sequence
3. **Awaits inside try-catch** blocks
4. **Awaits inside nested try blocks**
5. **Async method that is also generic**
6. **Async method with early returns**
7. **Combination with synchronized methods** (edge case)

#### Validation:

- Verify single try-finally wraps entire method
- Verify CaptureContexts at method entry
- Verify RestoreContexts in finally handler
- Verify all return blocks converted to leave
- Check EH table structure is correct
- Run existing async tests

---

## Implementation Order

1. **Step 1:** Add compiler fields (Phase 2)
2. **Step 2:** Implement fgAddAsyncContextSaveRestore with local creation (Phase 1)
3. **Step 3:** Call new function from fgAddInternal (Phase 1)
4. **Step 4:** Update TransformAsync to use method-level locals AND remove AsyncCallInfo field (Phase 4)
5. **Step 5:** Delete SaveAsyncContexts phase and related code (Phase 3)
6. **Step 6:** Add OSR/EnC special-casing for context locals (Phase 7)
7. **Step 7:** Test and validate (Phase 8)

---

## Key Design Decisions

### 1. Finally vs Fault
**Choice:** Use `EH_HANDLER_FINALLY` (not fault)
**Reason:** Context restore should run on both normal and exceptional exit

### 2. Single vs Multiple Try-Finally
**Choice:** Single try-finally wrapping entire method
**Reason:** Matches synchronized methods pattern, cleaner EH structure

### 3. Unconditional vs Conditional Restore
**Choice:** Unconditional restore in finally
**Reason:** Simpler, always safe, minor perf impact

### 4. When to Insert Try-Finally
**Choice:** In fgAddInternal phase
**Reason:** Same as synchronized methods, before funclets created

### 5. Context Local Lifetime
**Choice:** Method-level locals (not per-call)
**Reason:** Single try-finally needs single set of locals

---

## Potential Issues and Mitigations

### Issue 1: OSR and EnC Support
- Context locals must be preserved across OSR transitions (like lvaMonAcquired for synchronized methods)
- Context locals must be preserved for EnC edits
- **Implementation Required:**
  - Find all code that special-cases `lvaMonAcquired` for OSR/EnC
  - Add similar special-casing for `lvaAsyncExecutionContextVar` and `lvaAsyncSynchronizationContextVar`
  - Likely locations: frame layout, OSR state building/restoration, EnC frame header handling
  - Search for `lvaMonAcquired` in codebase to find all places that need updates

### Issue 2: All Async Methods Need Context Save/Restore
- All runtime async functions require context save/restore semantics
- No need to check per-call or set flags
- **Decision:** Always add try-finally for async methods (compIsAsync() is sufficient check)

### Issue 3: Interaction with Existing EH
- Method may already have try-catch blocks
- New try-finally must be outermost
- **Mitigation:** Follow synchronized method pattern (updates enclosing indices)

### Issue 4: OSR and Patchpoints
- Async methods support OSR (On-Stack Replacement)
- Need to ensure context save/restore works across OSR transitions
- **Mitigation:** Test thoroughly, may need special handling

### Issue 5: Return Block Conversion
- All return blocks must be converted to leave (jump to finally)
- Must ensure single merged return block exists
- **Mitigation:** Follow synchronized method pattern, ensure genReturnBB is created

---

## Code Locations Reference

### Files to Modify:
1. `src/coreclr/jit/flowgraph.cpp` - fgAddAsyncContextSaveRestore implementation (create locals + try-finally)
2. `src/coreclr/jit/compiler.h` - Add fields and method declaration
3. `src/coreclr/jit/async.cpp` - TransformAsync updates and delete SaveAsyncContexts phase
4. `src/coreclr/jit/compiler.cpp` - Remove SaveAsyncContexts from phase list
5. `src/coreclr/jit/gentree.h` - Remove field from AsyncCallInfo (one line deletion)

### Key Functions:
- `Compiler::SaveAsyncContexts()` - DELETE entirely (or make no-op)
- `Compiler::InsertTryFinallyForContextRestore()` - DELETE entirely
- `AsyncTransformation::ClearSuspendedIndicator()` - DELETE entirely
- `AsyncTransformation::SetSuspendedIndicator()` - DELETE entirely
- `Compiler::fgAddInternal()` - Add call to new function
- `Compiler::fgAddSyncMethodEnterExit()` - Reference implementation
- `Compiler::fgConvertSyncReturnToLeave()` - Return conversion pattern
- `AsyncTransformation::FillInDataOnSuspension()` - Update context handling
- `AsyncTransformation::RestoreFromDataOnResumption()` - Update context handling

### Helper Functions:
- `fgTryAddEHTableEntries()` - Add EH table entry
- `fgSplitBlockAtBeginning()` - Create try entry
- `fgNewBBafter()` - Create finally handler
- `fgNewStmtAtBeg()` - Insert capture call
- `gtNewCallNode()` - Create helper calls

---

## Validation Plan

### Phase 1: Basic Validation
- Compile simple async method
- Verify EH table has single try-finally
- Verify CaptureContexts at method entry
- Verify RestoreContexts in finally

### Phase 2: Functional Testing
- Run existing runtime async tests
- Test with various async patterns
- Test with nested EH
- Test with multiple awaits

### Phase 3: Performance Validation
- Measure overhead of method-level vs per-call approach
- Should be minimal difference (one capture/restore either way)

### Phase 4: Edge Cases
- Async + synchronized (if allowed)
- Async with no awaits
- Async with only synchronous code paths
- Async with early returns
- Async with OSR transitions
- Async with EnC edits

---

## Timeline Estimate

- **Phase 1 (fgAddInternal changes + local creation):** 2-3 days
- **Phase 2 (Compiler state):** 0.5 days
- **Phase 3 (Delete SaveAsyncContexts):** 0.5 days
- **Phase 4 (TransformAsync updates + AsyncCallInfo field removal):** 1 day
- **Phase 7 (OSR/EnC special-casing):** 1-2 days
- **Phase 8 (Testing):** 2-3 days
- **Total:** ~7.5-10 days

Note: SaveAsyncContexts phase is completely removed. Context locals are created directly in fgAddAsyncContextSaveRestore() in the fgAddInternal phase, similar to how synchronized methods create lvaMonAcquired.

---

## Success Criteria

1. ✅ Single try-finally wraps entire method body
2. ✅ CaptureContexts called once at method entry (in try)
3. ✅ RestoreContexts called once in finally handler with suspended check (continuation != null)
4. ✅ All existing async tests pass
5. ✅ No per-call try-finally blocks generated
6. ✅ No per-call suspended indicator locals generated
7. ✅ EH table structure matches synchronized methods pattern
8. ✅ Context save/restore semantics are correct
9. ✅ OSR and EnC work correctly with context locals
