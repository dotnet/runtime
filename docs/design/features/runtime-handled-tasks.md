# Runtime Handled Tasks Experiment

The .NET Runtime has historically supported a programming model of developing async programs through use of the System.Threading.Tasks library provided by the runtime as well as the Roslyn async method support, which interacts with that tasks library to make it simple to write the callback based state machines that the runtime can use. Over the years several attempts have been made to optimize the performance and other characteristics of this scheme, but fundamentally those arrangements have relied on either tweaks to the core library's code base or adjustments to the IL code generated. This experiment seeks to identify what improvements to the async model can be achieved by moving the state machine computation and handling to being handled internally in the runtime.

## Experimental Goals

1. Find out if runtime generated async state machines are substantially better in performance than the existing async state machine model
2. Find out if we can implement a variation on async which has some subtle semantic behavior changes, but remains compatible with existing code which is written in the original style

## Experimental Approach

1. Develop new semantics which allow the runtime to implement state machines as part of the runtime instead of relying on the existing state machine generation.
2. Develop 1 or more implementations of the runtime which implement said semantics
3. Develop a version of the Roslyn compiler which is capable of building code using the new semantics
4. Develop a set of microbenchmarks for measuring the different performance characteristics of the new model
5. Develop a set of application scenario benchmarks

Throughput this document, any references to the term async2 refer exclusively to the new model.

## New Semantics

This section is a work in progress, but describes the current expected semantic changes.

### Specification of a method as following async2 semantics

To specify that a method follows async2 rules, a custom modifier to either `System.Threading.Tasks.Task`, ``System.Threading.Tasks.Task`1``, `System.Threading.Tasks.ValueTask`, or ``System.Threading.Tasks.ValueTask`1`` shall be placed as the last custom modifier before the return type in the signature of a method.

### Flowing `SyncronizationContext` and `ExecutionContext` and other behavior such as AsyncLocal
Unlike traditional C# compiler generated async, async2 methods shall not save/restore `SyncronizationContext` and `ExecutionContext` at function boundaries. Instead, changes to them can be observed by their callers (as long as the caller is not a Task to async2 thunk).

#### Integration with `SyncronizationContext` and resumption

A new attribute `System.Runtime.CompilerServices.ConfigureAwaitAttribute` will be defined. It can be applied at the assembly/module/type/method level. It shall be defined as 
```
namespace System.Runtime.CompilerServices
{
    class ConfigureAwaitAttribute : Attribute
    {
        public ConfigureAwaitAttribute(bool continueOnCapturedContext) { ContinueOnCapturedContext = continueOnCapturedContext; }
        public bool ContinueOnCapturedContext { get; }
    }
}
```

The behavior of this attribute shall be to apply configure await semantics for resuming suspended frames of execution. Notably, though, the semantics of exactly which `SynchronizationContext` to use is subtly different from that of traditional async. In traditional async since each async method saves/restores the `SynchronizationContext` an async method which suspended at an ConfigureAwait(true) await point awaiting async method `A` will always continue on the Synchronization context that existed before the call to async method `A`, where in the async2 model the context used will be that which is current just before calling into method `A`, but in the async2 model, the SynchronizationContext will be the context that was current just before the return statement of method `A`. In practice, we expect this to be a distinction without a difference, as all known intentional uses of `SyncrhonizationContext` are implemented via a pattern where the context is set in a function, and restored via a try/finally to its original state before returning from the function.

#### Implications of flowing `ExecutionContext` and async locals

`ExecutionContext` is used to represent the current values of the `AsyncLocal<T>`. This feature is colloquially known as async locals. The semantics of these locals is surprising to developers today, in that while any function can modify the current state of an aync local, if an async function returns, any mutations to the async local are lost. In contrast, this proposal changes to the model such that when an async2 function returns, the async local state is **not** reverted to its previous state.

### Integration with the existing api surface of Task and ValueTask based apis

Any MethodDef that is an async2 based method can also be called as a `Task`/`ValueTask` returning method. Likewise any MethodDef which returns `Task` or `ValueTask` can be called via an async2 entrypoint. In addition, MethodImpl records and virtual method override rules shall be adjusted so that if an interface or base type defines a virtual method using a signature of `Task`/`ValueTask` then it can be overriden via a method that is an async2 api. The same is true vice versa. For overrides implemented via `MethodImpl` the MethodImpl must describe the Body and Decl using signatures which are either both async2 or `Task`/`ValueTask` returning, but the MethodImpl will also apply to the other async variant. These overrides are permitted to be interleaved, so for example

```
class Base
{
    public virtual async Task<int> Method() { ... }
}
class Derived : Base
{
    public override async2 int Method() { ... }
}

class MoreDerived : Derived
{
    public override async Task<int> Method() { ... }
}
```

Any attempt to call an entrypoint will resolve to either an implementation which matches the signature, or in case of an async variant mismatch, will resolve to a runtime generated thunk which will call into the developer provided implementation.

Methods that return Task via generics do not follow this rule. For instance
```
class MyGeneric<T>
{
    T DoSomething() { ... }
}
```

`MyGeneric<Task>.DoSomething()` is not callable via an async2 entrypoint.

#### Thunk from an async2 api surface to Task based implementation

A thunk from an async2 api surface to a Task based implementation will have the following psuedocode, if a legal thunk can be made.

```
async2 ReturnType ThunkAsync(ParameterType param1, ParameterType2 param2, ...)
{
    return RuntimeHelpers.UnsafeAwaitAwaiterFromRuntimeAsync<TaskAwaiter<ReturnType>, ReturnType>(TargetMethod(param1, param2, ...));
}
```

If any of the parameter types to the target method are ref parameters, or ref structures, then the Thunk generated will be 
```
async2 ReturnType ThunkAsync(ParameterType param1, ParameterType2 param2, ...)
{
    throw new InvalidProgramException();
}
```

#### Thunk from the Task api surface to async2 based implementation

If any of the parameter types to the target method are ref parameters, or ref structures, then any thunk invoked will throw `InvalidProgramException`. Otherwise it will perform whatever runtime actions are necessary to transition to the async2 implementation, and return the appropriate Task object type on return.

Thunks are required to save and restore the `ExecutionContext` and `SynchronizationContext` such that callers of thunks cannot observe changes to them.

### Specifying custom scheduling of async2
Async2 shall integrate with `TaskScheduler.Current`.

### Semantics of async2 based IL code

#### IL Semantics

Async2 IL is broadly similar to the existing IL semantics with the following restrictions.

1. Usage of the `localloc` instruction is forbidden
3. The `ldloca` and `ldarga` instructions are redefined to return managed pointers instead of pointers.
4. A pinning local cannot be validly used with a local variable.
5. As an initial restriction that is not fundamental, the `tail.` prefix is not permitted

Notably, the design permits byrefs and ref structs to be used within async2 methods.

#### EH Semantics
No call to a method with an async2 modreq will be permitted in a finally, fault, filter, or catch clause. If such a thing exists, the program is invalid.

The StackTrace of a exception thrown within an async2 method shall include any async2 frames which are logically on the call stack up until the thunk which transfers execution to the root async2 method.

### Api surface for interacting with non Task/ValueTask apis which async from within async2 code

The runtime shall provide the following apis

```
        public static async2 void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
        public static async2 void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
```

Any IL in an async2 method which needs to await will follow the existing C# model of calling `IsCompleted` and `GetResult()` and in the middle instead of using the existing patterns for suspension shall call one of the above helper methods.

### Utilizing async2 based code from non-async2 based code

Non-async2 code is never permitted to directly invoke async2 code. Instead any and all calls into async2 functions shall be done by means of one of the thunks.

### Interaction with Reflection

Async2 methods will not be visible in reflection via the `Type.GetMethod`, `TypeInfo.DeclaredMethods`, `TypeInfo.DeclaredMembers` , or `Type.GetInterfaceMap()` apis. 

`Type.GetMembers` and `Type.GetMethods` will be able to find the async2 methods if and only if the `BindingFlags.Async2Visible` flag is set.

`Type.GetMemberWithSameMetadataDefinitionAs` will find the matching async variant method.


## Implementation of Unwinder based async2 prototype

The general design is to leverage the notion that a normal code generated function at a function call point is effectively a state machine. The index for resumption is the IP that returning to the function will set, and the stackframe + saved registers are the current state of the state machine. I believe we can achieve performance comparable to synchronous code with this scheme for non-suspended scenarios, and we may achieve acceptable overall performance as an async mechanism if suspension is relatively rare.

### JIT Code changes
1. Do not allow InlinedCallFrames to be linked to the frame chain across a suspend (Or disable p/invoke inlining in these methods)
2. Report frame pointers as ByRef
3. Report all pointers to locals as ByRef
4. Force the AwaitAwaiterFromRuntimeAsync to be treated as an async2 method even though it isn't marked as such (due to compiler limitations)
5. Do not allow the JIT to hold onto a thread static base pointer across await points.

### Thunk from the Task api surface to async2 based implementation

A thunk from the Task api surface to an async2 based implementation will have the following psuedocode if it is not required to throw `InvalidProgramException`

```
Task<ReturnType> ThunkAsync(ParameterType param1, ParameterType2 param2, ...)
{
    // A variant on this helper type will be defined for each type of `Task`/`ValueTask`/`Task<TResult>`/`ValueTask<TResult>`
    System.Runtime.CompilerServices.RuntimeTaskState<ReturnType> runtimeTaskState = new;

    // If the Task is parameterized there shall be a return type to work with.
    ReturnType result;

    runtimeTaskState.Push();
    try
    {
        try
        {
            // NOTE there is a special case for instance methods on valuetypes. For that case,
            // the thunk will construct a boxed instance of the this value, and then invoke the method
            // on that.
            result = TargetMethod(param1, param2, ...);
        }
        catch (Exception ex)
        {
            return runtimeTaskState.FromException(ex);
        }
        return runtimeTaskState.FromResult(result);
    }
    finally
    {
        runtimeTaskState.Pop();
    }
}
```

### Implementation of `AwaitAwaiterFromRuntimeAsync`
The implementation of this method captures every stack frame from itself down to the `ThunkAsync` function or `ResumptionFunc`. Each one of these stack frames is packaged up into a structure known as a `Tasklet`. These `Tasklet` objects are registered with the GC so that they will be properly reported, but that reporting is not based on normal GC reporting, but is instead a special codebase. Primarily this need to treat them as special is done so that ByRef pointers can be handled correctly. Once all of the stack frames are captured, then the set of unwound `Tasklet` and the awaiter object are placed in a thread local variable for use by the `ThunkAsync` or `ResumptionFunc` functions. Finally, the actual stack is unwound to the `ThunkAsync`/`ResumptionFunc` frame, which detects that the operation was suspended and does the appropriate thing. In the ThunkAsync implementation, this is done by the runtimeTaskState.FromResult method, which determines if it should ignore the result it acquired from the `TargetMethod` and instead just create a task to return. Finally, a delegate to `ResumptionFunc` is passed to the awaiter.

`Tasklet` structures are designed to hold the data of a frame, the instruction pointer where execution should resume, the current state of any callee preserved registers that are used by the function, and layout/unwind information for finding GC data/and preserved register locations within the frame. Of particular interest is the handling for preserved registers. In the normal case of execution of JIT generated code, when a function needs to use a callee preserved register, it will generate code to store the current value of the register during the prolog of the method, and restore it during the epilog of the method. This unwinder based approach reverses that approach so that the `Tasklet` copy of the stack frame stores the "current" value of the register, and it is reported to the GC in that location when the `Tasklet` goes through GC reporting. The other detail of particular interest is that the data necessary to describe the locations of these preserved registers turns out to be exactly the data necessary to unwind a stack frame.

The implementation of unwinding used to return back to the `ThunkAsync` method is currently written in a highly optimized assembly path which walks the list of preserved register locations, and resets them.

Current implementation of this unwinder scheme use the standard CoreCLR unwinder and GC information reporter to build data structures that describe the unwind/GC data at a particular instruction pointer address, but in theory this should be amenable to a high performance cache.

### Implementation of `ResumptionFunc`
The dispatcher takes a collection of Tasklets, and resumes the one at the top of the stack, and if it returns, will pop it off the stack and execute the next one. It shall maintain structures such that the EH stack walker can find the list of stack frames that are still held in `Tasklet`s. In addition it shall be responsible for ensuring that the correct `ExecutionContext`/`SynchronizationContext` is maintained, and any `Tasklet` is resumed using the appropriate scheduler provided to the runtime. This implementation today is partial, but the basic structure is present to do so.

#### Resumption of a Tasklet

Once the `ResumptionFunc` has setup the global state to have the correct bits in it, it will call into a carefully crafted assembly stub (called `ResumeTaskletIntegerRegisterReturn` or `ResumeTaskletReferenceReturn`) which takes a `Tasklet*` as input as well as a pointer to a structure which holds the current return value that should be restored to the return value registers. This assembly stub shall copy the `Tasklet`'s stack frame onto the stack, swap any values in the preserved register locations with the current value of the preserved registers, and then tail jump to the instruction pointer held in the `Tasklet`. This resumption process was chosen to ensure that:
1. The return address on the shadow CET stack does not need any updates upon `Tasklet` resumption.
2. The runtime only resumes a single method at a time, which improves efficiency in the somewhat common case where there is a deep stack which has a loop in it.
3. Once resumed, the stack appears as a normal stack to all stackwalking operations.

### Expected characteristics of this approach
1. The performance of code which does not suspend is effectively the same as normal synchronous code. (confirmed)
2. The performance of code which suspends a lot is highly impacted by the cost of performing unwind, and somewhat by resumption cost. (under investigation)
3. The cost of GC while `Tasklet`s exist is dependent on the number of live `Tasklet` objects. This is almost certainly a fairly high cost. (under investigation)
4. This approach allows development of async code which uses ref parameters/locals and ByRefLike structures. (confirmed)

## Implementation of prototype based on JIT generated state machines

In the second prototype we leave it up to the JIT to generate the state machines for async2 functions.
When awaiting an async2 function from within an async2 function the JIT generates suspension code to capture the live state in a continuation.
In addition, the JIT generates code for each of these suspension points to be able to resume from the continuation that was created.

### Suspension

Communicating suspension is done by returning a non-zero continuation, using a special calling convention.
The continuation is a normal GC ref so special care must be taken within the JIT and within the VM to handle proper GC reporting of this return value.
On x64, the continuation is always returned in `rcx`.

When a callee suspends by returning a non-zero continuation the caller itself suspends by creating its own continuation and returning it.
Additionally, the generated suspension code links the continuations into a linked list.

`Continuation` is defined as a simple class in the BCL:
```csharp
internal sealed unsafe class Continuation
{
    public Continuation? Next;
    public delegate*<Continuation, Continuation?> Resume;
    public uint State;
    public CorInfoContinuationFlags Flags;
    public byte[]? Data;
    public object[]? GCData;
}
```

This incurs extra cost on every call to an async2 method in the form of a test for whether the returned continuation is non-zero.

### Resumption

In addition to returning a continuation async2 methods also accept a continuation as an additional parameter.
The parameter follows the normal calling convention; it is added in the parameter list after the generic context, return buffer or `this` parameter (whichever comes last).

Resuming an async2 method is done by passing a non-zero continuation.
The continuation must have been created by the same exact native code version that is being resumed.

Passing a zero continuation is the same as starting the state machine.
The JIT will automatically generate code to pass a zero continuation when calling an async2 method.

When the JIT generates code for an async2 function it requests the runtime to create a resumption stub.
The suspension code stores the pointer to the resumption stub in the `Continuation.Resume` field.

The resumption stub implements the following:
1. It calls into the original function passing the non-zero continuation
2. It passes default values for all arguments, to make sure that the callee has the expected stack frame set up
3. If the function returns a value, it ensures that the return value is propagated into the next continuation.
Continuations expect that the next continuation has allocated space for the return value in its `GCData[0]` field.
Currently return values are always boxed when propagating from one continuation to the next continuation.
4. If the function returned a new continuation (i.e. it suspended again), the resumption stub returns it back to the caller.

In pseudo-C#:
```csharp
static async2 int Foo(int a, int b)
{
    ...
}

static Continuation? IL_STUB_AsyncResume_Foo(Continuation continuation)
{
    delegate*<Continuation, int, int, int> foo = &Foo;
    int result = foo(continuation, 0, 0);
    Continuation? newContinuation = StubHelpers.Async2CallContinuation();

    if (newContinuation == null)
    {
        continuation.Next.GCData[0] = (object)result;
    }

    return newContinuation;
}
```

### Intrinsics used for suspension/resumption

To interact with the async2 calling convention the JIT/VM provides the following intrinsics:
```csharp
// Retrieve the continuation returned by an async2 function
[Intrinsic]
internal static Continuation? Async2CallContinuation() => null;

// Suspend the current function by immediately returning with a specific non-zero continuation.
[Intrinsic]
private static void SuspendAsync2(Continuation continuation) => throw new UnreachableException();
```

These intrinsics are NOT used by user code, but they are used internally by the async1<->async2 adapter later, which will be described later.

`Async2CallContinuation()` is also used by generated resumption stubs to capture the returned continuation.

Since the continuation is a normal parameter no intrinsic is required to allow the resumption stub to pass the continuation to the target.
Instead, the resumption stub calls the target by `calli` with a signature that includes the continuation, in the same way as IL instantiating stubs work.

### Handling OSR

There is additional complexity necessary to handle OSR correctly:
1. OSR expects that the frame was set up by a tier-0 method so resumption directly in an OSR method is not possible.
Instead, the JIT generates code in the tier0 version to check for an OSR continuation in which case the tier-0 method transitions immediately
to the OSR method on resumption.
The resumption stub created for an OSR method thus points to its corresponding tier-0 code version.
This is currently implemented by storing a link between OSR methods and corresponding tier-0 methods in `PatchpointInfo`.
2. When resuming in a tier-0 method, if we transition to the OSR method by normal means, the OSR method will see a non-zero continuation that belongs to the tier-0 method.
The OSR method must ignore the continuation in this case.

### Interop: the async1<->async2 adapter layer

Thunks of the same shapes are provided as for the unwinding prototype.

#### async2 -> async1

The only way to actually begin suspension of a chain of async2 methods happens when calling into an async1 method that suspends.
As described previously these boundaries are generally expected to be handled by Roslyn by IL codegen that involves a check for `IsCompleted` followed by a call into one of the two `AwaitAwaiterFromRuntimeAsync` variants in the asynchronous case, finally followed by a `GetResult` call.
The runtime will automatically provide such a stub if an async1 method is called with an async2 signature.

The implementation of `AwaitAwaiterFromRuntimeAsync` is provided by the BCL.
For the JIT state machines prototype it looks as follows:

```csharp
private struct RuntimeAsyncAwaitState
{
    public Continuation? SentinelContinuation;
    public INotifyCompletion? Notifier;
}

[ThreadStatic]
private static RuntimeAsyncAwaitState t_runtimeAsyncAwaitState;

public static async2 void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
{
    ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
    Continuation? sentinelContinuation = state.SentinelContinuation;
    if (sentinelContinuation == null)
        state.SentinelContinuation = sentinelContinuation = new Continuation();

    state.Notifier = awaiter;
    SuspendAsync2(sentinelContinuation);
}

```

The helper starts the suspension by allocating a continuation into TLS, and returning it back to the caller.
This is going to result in the JIT codegen unwinding through all calling async2 functions and linking all continuations together.
The end result will be a linked list of continuations where the head of the continuation is available through TLS.

The state stored in TLS is acted on by the async1 -> async2 adapter.

#### async1 -> async2
The async1 -> async2 adapter, which allows calling an async2 function as if it returns a `Task`, is implemented in the following pseudo-C#:

```csharp
static Task<int> FooAdapter(int a, int b)
{
    int result;
    Continuation? continuation = null;
    try
    {
        delegate*<Continuation, int, int, int> foo = &Foo;
        result = foo(null, a, b);
        continuation = StubHelpers.Async2CallContinuation();
    }
    catch (Exception ex)
    {
        return Task.FromException<int>(ex);
    }

    if (continuation == null)
        return Task.FromResult(result);

    return FinalizeTaskReturningThunk<int>(continuation);
}
```
(TODO: ExecutionContext, SynchronizationContext handling...)

The interesting bit is what happens in the asynchronous case, where we ended up in the async2 -> async1 adapter described above, which stored the head of the continuation into TLS.
This is handled inside `Task<T> FinalizeTaskReturningThunk<T>(Continuation continuation)`:

```csharp
private static Task<T> FinalizeTaskReturningThunk<T>(Continuation continuation)
{
    TaskCompletionSource<T> tcs = new();
    ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
    object[] gcData = new object[3];
    gcData[2] = tcs;
    continuation.Next = new Continuation
    {
        Resume = &ResumeTaskCompletionSource<T>,
        GCData = gcData,
        Flags = CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION | CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA,
    };

    Continuation headContinuation = state.SentinelContinuation!.Next!;
    state.Notifier!.OnCompleted(() => DispatchContinuations(headContinuation));
    return tcs.Task;
}

private static Continuation? ResumeTaskCompletionSource<T>(Continuation continuation, Exception? exception)
{
    var tcs = (TaskCompletionSource<T>)continuation.GCData![2];
    Exception ex = (Exception)continuation.GCData![1];
    if (ex == null)
    {
        tcs.SetResult((T)continuation.GCData![0]);
    }
    else
    {
        tcs.SetException(ex);
    }

    return null;
}
```
A synthetic continuation is linked to the end of the chain; when resumed, this continuation will trigger the returned `Task<T>` to be completed via the `TaskCompletionSource<T>`.
With the entire continuation set up properly the `OnCompleted` method of the awaiter stored in TLS can be invoked; it queues a callback to the `DispatchContinuations` function with the head continuation retrieved through TLS.

The final piece of the puzzle is what happens when the awaiter actually invokes the callback asynchronously.
It delegates to `DispatchContinuations`, another function part of the BCL.
This function repeatedly dispatches the next continuation by calling its resumption stub.
It also accounts for potential exceptions thrown, and propagates it into the next relevant continuation.
Finally, it handles the case where a resumed async2 function suspends again.

```csharp
private static unsafe void DispatchContinuations(Continuation? continuation)
{
    Debug.Assert(continuation != null);

    while (true)
    {
        Continuation? newContinuation = null;
        try
        {
            newContinuation = continuation.Resume(continuation);
        }
        catch (Exception ex)
        {
            continuation = UnwindToPossibleHandler(continuation);
            continuation.GCData![(continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_RESULT_IN_GCDATA) != 0 ? 1 : 0] = ex;
            continue;
        }

        if (newContinuation != null)
        {
            newContinuation.Next = continuation.Next;
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation headContinuation = state.SentinelContinuation!.Next!;
            state.Notifier!.OnCompleted(() => DispatchContinuations(headContinuation));
            return;
        }

        continuation = continuation.Next;
        if (continuation == null)
            break;
    }
}

private static Continuation UnwindToPossibleHandler(Continuation continuation)
{
    while (true)
    {
        Debug.Assert(continuation.Next != null);
        continuation = continuation.Next;
        if ((continuation.Flags & CorInfoContinuationFlags.CORINFO_CONTINUATION_NEEDS_EXCEPTION) != 0)
            return continuation;
    }
}
```

## Shared implementation between Unwinder and JIT focused implementation of runtime tasks

It turns out that due to the api design of how async2 code interacts with the existing await pattern in IL, the thunk from async2 to existing Tasks and ValueTasks is the same for all designs.

### Thunk from the async2 surface to Task api implementation

```
async2 ReturnType Thunk(ParameterType param1, ParameterType2 param2, ...)
{
    var awaiter = TargetMethod(param1, param2, ...).GetAwaiter();
    if (!awaiter.IsCompleted)
    {
        RuntimeHelpers.UnsafeAwaitAwaiterFromRuntimeAsync(awaiter);
    }
    return awaiter.GetResult();
}
```

## C# Language changes

TBD, and not part of this experiment. We have built something. It is strictly for demonstrating that code CAN be generated.

Major identified concerns are

1. What about code which today looks like `public async Task<Task<Task<int>>> Method()` What would the new syntax be?
2. It is desirable to be able to implement methods that return `ValueTask` as well as just `Task` with an async2 method. How do we distinguish?
3. There is an existing concept for `async void` methods. Does that model need to exist in the new system.

## Microbenchmarks

1. Async computation of fibonacci via recursive algorithm, with and without various amounts of yielding
2. Suspension with stacks of various depths
3. Suspension with stacks where the operation of the task operates in a sawtooth pattern. For instance, suspend first at depth 20, then reach depth 30, return to depth 10 and suspend, reach depth 30, return to depth 10 and suspend, etc. Measure the impact of suspending with greater and lesser depths.
4. Measure the performance of thunks to async2 functions which do effectively nothing.
5. Benchmark the effect of long lived suspended state vs short lived suspended state
6. Benchmark the performance of EH at various depths.

## Scenario benchmarks

1. Serialization/Deserialization of JSON data to an asynchronous stream
2. ASP.NET servicing of requests where must of the pipeline is converted to the async2 model

