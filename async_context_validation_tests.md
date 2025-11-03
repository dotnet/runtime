# Async Context Save/Restore - Validation Tests

This document describes validation tests for the async context save/restore bug fix.
These tests should be run after implementing the changes described in async_context_plan.md.

## Test Overview

The tests verify that:
1. Single try-finally wraps entire async method body
2. CaptureContexts called once at method entry
3. RestoreContexts called once in finally handler with suspended check (continuation != null)
4. No per-call try-finally blocks generated
5. No per-call suspended indicator locals generated
6. EH table structure matches synchronized methods pattern
7. Context save/restore semantics are correct
8. OSR and EnC work correctly with context locals

## Phase 1: Basic Validation Tests

### Test 1.1: Simple Async Method
**File:** `SimpleAsyncMethod.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> SimpleAsync()
    {
        await Task.Delay(100);
        return 42;
    }
}
```

**Expected JIT Dump:**
- Single try-finally EH region wrapping entire method
- CaptureContexts call at beginning of try block
- RestoreContexts call in finally handler with `(continuation != null)` check
- No per-call suspended indicator locals
- No multiple try-finally regions

**Validation Commands:**
```bash
# Set DOTNET_JitDump=SimpleAsync
# Set DOTNET_JitDisasm=SimpleAsync
# Run and examine output for EH table structure
```

---

### Test 1.2: Multiple Awaits in Sequence
**File:** `MultipleAwaitsSequence.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> MultipleAwaitsAsync()
    {
        await Task.Delay(100);
        int result = await Task.FromResult(42);
        await Task.Delay(200);
        return result + 10;
    }
}
```

**Expected JIT Dump:**
- Still only ONE try-finally region
- Single CaptureContexts at method entry
- Single RestoreContexts in finally
- Three suspension points, but no per-call context handling

---

### Test 1.3: Async Method with Early Return
**File:** `AsyncEarlyReturn.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> EarlyReturnAsync(bool condition)
    {
        if (condition)
        {
            return 0; // Early return before any await
        }
        
        await Task.Delay(100);
        return 42;
    }
}
```

**Expected JIT Dump:**
- Try-finally still wraps entire method body
- Early return converted to leave instruction (jumps to finally)
- Finally executes on both normal and early return paths

---

## Phase 2: Functional Testing

### Test 2.1: Awaits Inside Try-Catch
**File:** `AwaitInTryCatch.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> AwaitInTryCatchAsync()
    {
        try
        {
            await Task.Delay(100);
            throw new InvalidOperationException("Test exception");
        }
        catch (InvalidOperationException)
        {
            await Task.Delay(50);
            return 10;
        }
        catch
        {
            return -1;
        }
    }
}
```

**Expected JIT Dump:**
- Outer try-finally for context save/restore
- Inner try-catch-catch for user code
- EH table shows correct nesting (outer finally enclosing inner try-catch)
- Both suspension points (in try and in catch) within outer try region

**Runtime Validation:**
- Contexts correctly restored on exception path
- Exception properly propagated after context restore

---

### Test 2.2: Nested Try Blocks
**File:** `NestedTryBlocks.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> NestedTryAsync()
    {
        try
        {
            try
            {
                await Task.Delay(100);
                return 42;
            }
            finally
            {
                Console.WriteLine("Inner finally");
            }
        }
        finally
        {
            Console.WriteLine("Outer finally");
        }
    }
}
```

**Expected JIT Dump:**
- Three try-finally regions total:
  1. Outermost: context save/restore (inserted by JIT)
  2. Middle: user's outer finally
  3. Inner: user's inner finally
- Correct EH table nesting
- All user returns converted to leaves

---

### Test 2.3: Async with Generic Context
**File:** `AsyncGenericContext.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test<T> where T : class
{
    private T value;
    
    public async Task<T> GetValueAsync()
    {
        await Task.Delay(100);
        return value;
    }
}
```

**Expected JIT Dump:**
- Context locals marked for OSR/EnC preservation
- Generic context (type parameter) kept alive if needed
- KeepAlive field in continuation if using method descriptor generics context

---

## Phase 3: Edge Cases

### Test 3.1: Async with No Awaits
**File:** `AsyncNoAwait.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> NoAwaitAsync()
    {
        // Completes synchronously
        return 42;
    }
}
```

**Expected JIT Dump:**
- Try-finally still inserted (overhead minimal)
- CaptureContexts still called
- RestoreContexts checks continuation == null, skips restore
- No suspension points generated

---

### Test 3.2: Async with Only Synchronous Code Paths
**File:** `AsyncSyncPaths.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> SyncPathAsync(bool useAsync)
    {
        if (!useAsync)
        {
            return 42; // Synchronous completion
        }
        
        await Task.Delay(100);
        return 10;
    }
}
```

**Expected JIT Dump:**
- Single try-finally wraps entire method
- Synchronous path: continuation is null, RestoreContexts does nothing
- Asynchronous path: continuation is non-null, RestoreContexts executes

---

### Test 3.3: Async with ConfigureAwait(false)
**File:** `AsyncConfigureAwaitFalse.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> ConfigureAwaitFalseAsync()
    {
        await Task.Delay(100).ConfigureAwait(false);
        return 42;
    }
}
```

**Expected JIT Dump:**
- Continuation context handling respects ConfigureAwait setting
- ContinuationContextHandling flag set appropriately
- RestoreContexts still checks suspended flag

---

## Phase 4: OSR and EnC Testing

### Test 4.1: Async with OSR Transition
**File:** `AsyncOSR.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> LoopWithAwaitAsync()
    {
        int sum = 0;
        for (int i = 0; i < 10000; i++)
        {
            await Task.Yield();
            sum += i;
        }
        return sum;
    }
}
```

**Expected Behavior:**
- Context locals preserved across OSR transition
- Tier0 and OSR methods agree on context local locations
- OSR IL offset stored in continuation (for tier0 -> OSR transitions)

**Test Steps:**
1. Run with tiered compilation enabled
2. Force OSR transition (hot loop)
3. Verify contexts maintained across transition
4. Check continuation has OSR IL offset field

---

### Test 4.2: Async with EnC Edit
**File:** `AsyncEnC.cs`
```csharp
using System;
using System.Threading.Tasks;

public class Test
{
    public static async Task<int> EnCTestAsync()
    {
        await Task.Delay(100);
        int x = 42; // <-- Set breakpoint here for EnC edit
        await Task.Delay(100);
        return x;
    }
}
```

**Test Steps:**
1. Start debugging
2. Hit breakpoint
3. Make EnC edit (change return value)
4. Continue execution
5. Verify context locals preserved in frame header
6. Verify edit applied correctly

**Expected:**
- Context locals in frame header (EnC requirement)
- Edit applies successfully
- Contexts maintained across edit

---

## Phase 5: Context Semantics Validation

### Test 5.1: ExecutionContext Flows Correctly
**File:** `ExecutionContextFlow.cs`
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class Test
{
    public static async Task ExecutionContextTestAsync()
    {
        var asyncLocal = new AsyncLocal<int>();
        asyncLocal.Value = 42;
        
        await Task.Delay(100);
        
        // Should still be 42 after resumption
        Console.WriteLine($"AsyncLocal value: {asyncLocal.Value}");
    }
}
```

**Runtime Validation:**
- AsyncLocal value preserved across await
- ExecutionContext captured at method entry
- ExecutionContext restored in finally (if suspended)

---

### Test 5.2: SynchronizationContext Restored Correctly
**File:** `SynchronizationContextRestore.cs`
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

public class CustomSyncContext : SynchronizationContext { }

public class Test
{
    public static async Task SyncContextTestAsync()
    {
        var customContext = new CustomSyncContext();
        SynchronizationContext.SetSynchronizationContext(customContext);
        
        var before = SynchronizationContext.Current;
        
        await Task.Delay(100);
        
        var after = SynchronizationContext.Current;
        
        Console.WriteLine($"Same context: {ReferenceEquals(before, after)}");
    }
}
```

**Runtime Validation:**
- SynchronizationContext captured at method entry
- Context restored after synchronous completion
- Context preserved/restored correctly on suspension path

---

## Phase 6: Performance Validation

### Test 6.1: Overhead Measurement
**File:** `OverheadTest.cs`
```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;

public class Test
{
    public static async Task<long> MeasureOverheadAsync(int iterations)
    {
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            await Task.CompletedTask;
        }
        
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }
}
```

**Metrics to Compare:**
- Before fix: Per-call try-finally overhead
- After fix: Single method-level try-finally overhead
- Expected: Similar or slightly better performance (fewer EH regions)

---

## Phase 7: Regression Testing

### Test 7.1: Run Existing Async Tests
**Location:** `src/tests/baseservices/async/`

**Command:**
```bash
build.cmd -subset tests.buildonly -c Release
build.cmd -subset tests.run -c Release -test baseservices/async
```

**Expected:**
- All existing async tests pass
- No regressions in async functionality

---

### Test 7.2: Libraries Async Tests
**Location:** `src/libraries/System.Threading.Tasks/tests/`

**Command:**
```bash
cd src/libraries/System.Threading.Tasks
dotnet build /t:test
```

**Expected:**
- All Task/async tests pass
- Context flow tests pass
- Cancellation tests pass

---

## Validation Checklist

After running all tests, verify:

- [ ] All Phase 1 tests show correct EH structure (single try-finally)
- [ ] All Phase 2 tests pass with correct nested EH handling
- [ ] All Phase 3 edge cases handled correctly
- [ ] Phase 4 OSR/EnC tests verify context local preservation
- [ ] Phase 5 context semantics tests pass at runtime
- [ ] Phase 6 performance is same or better than before
- [ ] Phase 7 all existing tests pass (no regressions)

---

## JIT Dump Analysis Commands

To analyze JIT output for any test:

```bash
# Windows
set DOTNET_JitDump=<MethodName>
set DOTNET_JitDisasm=<MethodName>
set DOTNET_JitDumpEHTable=1

# Linux/Mac
export DOTNET_JitDump=<MethodName>
export DOTNET_JitDisasm=<MethodName>
export DOTNET_JitDumpEHTable=1

# Run test
dotnet run
```

Look for in output:
1. **EH Table:**
   - Single outer try-finally for context save/restore
   - Correct nesting of user EH regions within it

2. **Try Begin Block:**
   - CaptureContexts call at beginning
   - Takes addresses of lvaAsyncExecutionContextVar and lvaAsyncSynchronizationContextVar

3. **Finally Handler:**
   - RestoreContexts call
   - Check: `lvaAsyncContinuationArg != null` (suspended indicator)
   - GT_RETFILT at end

4. **Return Blocks:**
   - All converted to BBJ_ALWAYS (leave instructions)
   - Jump to shared return block outside try region

---

## Success Criteria Summary

✅ **Code Changes:**
- fgAddAsyncContextSaveRestore implemented
- Context locals created in fgAddInternal
- SaveAsyncContexts phase stubbed out
- Suspended indicator code removed
- AsyncCallInfo.SynchronizationContextLclNum removed

✅ **Validation:**
- Simple async methods compile correctly
- Complex EH nesting works
- Edge cases handled
- OSR/EnC preserves context locals
- Runtime semantics correct
- No performance regression
- All existing tests pass

---

## Notes for Test Execution

1. **Don't Build:** As requested, do not attempt to build or run these tests
2. **Manual Review:** These tests are for manual execution after code changes
3. **JIT Dumps:** Most validation is done by examining JIT dumps, not runtime behavior
4. **EH Table:** Pay special attention to EH table structure in dumps
5. **Locals:** Verify lvaAsyncExecutionContextVar and lvaAsyncSynchronizationContextVar exist and are used correctly

