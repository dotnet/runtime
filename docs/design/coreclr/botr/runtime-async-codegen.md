# Responsibilities of a code generator for implementing the Runtime Async feature

This document describes the behaviors that a code generator must conform to to correctly make the runtime async feature work correctly.

This document is NOT intended to describe the runtime-async feature. That is better described in the runtime-async specification. See (https://github.com/dotnet/runtime/blob/main/docs/design/specs/runtime-async.md).



The general responsibilities of the runtime-async code generator

1. Wrap the body of Task and ValueTask returning functions in try/finally blocks which set/reset the `ExecutionContext` and `SynchronizationContext`.

2. Allow the async thunk logic to work.

3. Generate Async Debug info (Not yet described in this document)



# Identifying calls to Runtime-Async methods that can be handled by runtime-async

When compiling a call to a method that might be called in the optimized fashion, recognize the following sequence.

```
call[virt] <Method>
[ OPTIONAL ]
{
[ OPTIONAL - Used for ValueTask based ConfigureAwait ]
{
stloc X;
ldloca X
}
ldc.i4.0 / ldc.i4.1
call[virt] <ConfigureAwait> (The virt instruction is used for ConfigureAwait on a Task based runtime async function) NI_System_Threading_Tasks_Task_ConfigureAwait
}
call       <Await> One of the functions which matches NI_System_Runtime_CompilerServices_AsyncHelpers_Await
```

A search for this sequence is done if Method is known to be async.

The dispatch to async functions saves the `ExecutionContext` on suspension to be restored before resumption by `DispatchContinuations`.

If PREFIX_TASK_AWAIT_CONTINUE_ON_CAPTURED_CONTEXT, then continuation mode shall be ContinuationContextHandling::ContinueOnCapturedContext otherwise ContinuationContextHandling::ContinueOnThreadPool.



# Non-optimized pattern

It is also legal for code to have a simple direct usage of `AsyncHelpers.Await`, `AsyncHelpers.AwaitAwaiter`, `AsyncHelpers.UnsafeAwaitAwaiter` or `AsyncHelpers.TransparentAwait`. To support this these functions are marked as async even though they do not return a Task/ValueTask.

Like other async calls the dispatch to these functions will save and restore the execution context on suspension/resumption.

The dispatch to these functions will set continuation mode to ContinuationContextHandling::None

# Calli of an async function

The dispatch to these functions will save and restore the execution context only on async dispatch.

# The System.Runtime.CompilerServices.AsyncHelpers::AsyncSuspend intrinsic

When encountered, triggers the function to suspend immediately, and return the passed in Continuation.

# Accessing known continuation fields

The continuation flags encode how to access several well-known fields if they are present in the continuation.
- The execution context
- The continuation context
- The exception object
- The return value

Each field has a pair of (first bit, number of bits) used to indicate where its details are encoded in the flags.
When the field is present the index is non-zero and the offset is computed as (DataStart + (index - 1) * PointerSize).
If a field is not present the index is zero.

# Saving and restoring of contexts

Capture the execution context before the suspension. The context should be stored into the Continuation and its offset encoded via the scheme above. The context may be captured by calling `AsyncHelpers.CaptureExecutionContext`.

# ABI for async function handling

There is an additional argument which is the Continuation. When calling a function normally, this is always set to 0. When resuming, this is set to the Continuation object. There is also an extra return argument. It is either 0 or a Continuation. If it is a continuation, then the calling function needs to suspend (if it is an async function), or generate a Task/ValueTask (if it is a async function wrapper).

## Suspension path

This is what is used in calls to async functions made from async functions.

```
bool didSuspend = false; // Needed for the context restore

(result, continuation) = call func(NULL /\* Continuation argument \*/, args)
if (continuation != NULL)
{
    // Allocate new continuation
    // Capture Locals
    // Copy resumption details into continuation (Do things like call AsyncHelpers.CaptureContinuationContext or AsyncHelpers.CaptureExecutionContext as needed)
    // Chain to continuation returned from called function
    // IF in a function which saves the exec and sync contexts, and we haven't yet suspended, restore the old values.
    // return.

    // Resumption point

    // Copy values out of continuation (including captured sync context and execution context locals)
    // If the continuation may have an exception, check to see if its there, and if it is, throw it. Do this if the flags indicate an exception object is present.
    // If the continuation has a return value, copy it out of the continuation. Do this if the flags indicate a result is present.
}
```

## Thunks path

This is what is used in non-async functions when calling an async function. Generally used in the AsyncResumptionStub and in the Task returning thunk.
```
(result, continuation) = call func(NULL /\* Continuation argument \*/, args)
place result onto IL evaluation stack
Place continuation into a local for access using the AsyncHelpers.AsyncCallContinuation() helper function.
```

Implement an intrinsic for AsyncHelpers.AsyncCallContinuation() which will load the most recent value stored into the continuation local.

# Behavior of ContinuationContextHandling

This only applies to calls which where ContinuationContextHandling is not ContinuationContextHandling::None.

If set to ContinuationContextHandling::ContinueOnCapturedContext

- The Continuation shall have an allocated data member for the captured context, and its offset is encoded in the flags based on the scheme above.

- The Continuation will store the captured synchronization context. This can be done by calling `AsyncHelpers.CaptureContinuationContext(ref newContinuation.ContinuationContext, ref newContinuation.Flags)` while filling in the `Continuation`.

If set to ContinuationContextHandling::ContinueOnThreadPool
- The Continuation shall have the CORINFO_CONTINUATION_CONTINUE_ON_THREAD_POOL flag set

# Exception handling behavior

If an async function is called within a try block (In the jit hasTryIndex return true), allocate space for an exception in the continuation and encode its offset in the flags based on the scheme above.

# Locals handling

ByRef locals must not be captured. In fact, we should NULL out any locals which are ByRefs or ByRef-like. Currently we do not do this on synchronous execution, but logically possibly we should.

# Saving and restoring the synchronization and execution contexts

The code generator must save/restore the sync and execution contexts around the body of all Task/ValueTask methods when directly called with a null continuation context. The EE communicates when this is necessary with the `CORINFO_ASYNC_SAVE_CONTEXTS` flag returned through `getMethodInfo`.
