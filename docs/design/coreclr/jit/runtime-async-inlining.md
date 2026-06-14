# General runtime async inlining

This document outlines some challenges and thoughts behind general inlining of runtime async calls.

## Background

Runtime async comes with several user-visible behaviors that must be preserved when inlining.
These can be categorized in behaviors attached to the call site and behaviors attached to the callee.

### Await/call site behaviors

Custom awaiters and synchronous Task/ValueTask-returning functions can be inlined today, so below we consider only runtime async backed implementations.

- Every suspending await saves/restores the `ExecutionContext`.
Concretely this is what saves and restores the values of `AsyncLocal` if suspension/resumption happens around an await.
The behavior of this cannot be configured per-await, but it can be configured globally via `ExecutionContext.SuppressFlow()`.
For our purposes we can consider this to always require saving/restoration.
- For suspending `Task` and `ValueTask` awaits the resumption behavior is configurable:
  + By default, the current `SynchronizationContext` or `TaskScheduler` is captured and the continuation is posted to it.
  + If `ConfigureAwait(false)` is applied the continuation is run on the thread pool.

  The "location to continue", either a `SynchronizationContext`, `TaskScheduler` or the thread pool, is collectively called the _continuation context_ in this document.

For awaits that finish synchronously (i.e. do not suspend) no additional work happens at the call site.

To accomplish the above the JIT works together with async infrastructure:
- Suspension generates code to obtain and save the `ExecutionContext` into the `Continuation` object.
On resumption, async infrastructure restores the `ExecutionContext` before calling back into JIT'ed code.
- Suspension generates code to capture continuation context information into the `Continuation` object.
On resumption, async infrastructure ensures that the continuation runs in the captured continuation context.

The above are accomplished with several helpers returned via the `getAsyncInfo` JIT-EE helper:
- `AsyncHelpers.CaptureExecutionContext`
- `AsyncHelpers.CaptureContinuationContext`
- `AsyncHelpers.FinishSuspensionNoContinuationContext` (optimized case)
- `AsyncHelpers.FinishSuspensionWithContinuationContext` (optimized case)

### Function/callee behaviors
Every runtime async function body is wrapped with save and restore of the async contexts on the current Thread object.
This restore happens only when the function finishes synchronously.
Concretely a runtime async function ends up looking like:
```csharp
ExecutionContext execContext;
SynchronizationContext syncContext;
AsyncHelpers.CaptureContexts(out execContext, out syncContext);
try
{
  // actual user code
}
finally
{
  AsyncHelpers.RestoreContexts(resumed, execContext, syncContext);
}
```

Note the `resumed` argument, used to check if the runtime async function finished synchronously or not.
If the runtime async function finished after resuming then contexts are not restored.

Additionally, every suspension _also_ restores the contexts.
This is done by inserting a similar call
```csharp
AsyncHelpers.RestoreContextsOnSuspension(resumed, execContext, syncContext)
```
in suspensions.
This is folded into the `AsyncHelpers.FinishSuspensionNoContinuationContext` and `AsyncHelpers.FinishSuspensionWithContinuationContext` helpers.
The JIT uses those helpers whenever possible.

## Inlining runtime async calls
When inlining a runtime async call we remove both a call site and the callee.
This removes the user-visible behaviors discussed above.
To inline correctly we need to ensure the JIT preserves these behaviors.

### Suspending inside an inlinee

Consider a scenario like:

```csharp
async Task A()
{
  ...
  await B();
  ...
}

async Task B()
{
  ...
  await C();
  ...
}

async Task C()
{
  ...
  await Task.Yield();
  ...
}
```
Now assume that we have available bools `resumed_A`, `resumed_B`, `resumed_C` that represent whether `A`, `B` and `C` are currently running after being resumed (true), or whether they are running because they were started and have never suspended before (false).
Note that `resumed_C` and `resumed_B` can switch between true/false multiple times during the execution if `B` and `C` are in loops, while `resumed_A` will switch to `true` and then stay `true`.

Assume we inlined `B()` and `C()` into `A`, and that we are logically suspending on `Task.Yield()` in `C`.
What does this suspension capture? That depends on `resumed_B` and `resumed_C`.
- Let's first consider `!resumed_C`.
This means that we just started running `C`, and on suspension we would restore `ExecutionContext` and `SynchronizationContext` from the beginning of `C` into the `Thread` object.
If we hadn't inlined then `B`'s frame would be physically present on the stack due to `!resumed_C`, so after returning it would create and link its own continuation.
This continuation would capture the `ExecutionContext` restored by `C` and also capture continuation context information based on the `SynchronizationContext` restored by `C`.
- If `resumed_C` then `B`'s frame would not be physically present on the stack.
Rather, the existing `B` continuation would be linked to the newly created `C` continuation by async infrastructure.
It would maintain its values from the time we suspended in `C` with `!resumed_C`.

The take away is that the suspension for the inlined `Task.Yield()` needs to roughly accomplish the following, in addition to storing its normal state:
```csharp
continuation = AllocOrReuseContinuation();
CaptureContinuationContext(continuation, ref continuation.ContinuationContext, ref continuation.Flags); // In the general case, but not for Task.Yield()
CaptureExecutionContext(continuation, ref continuation.ExecutionContext);
if (!resumed_C)
{
  RestoreContextsOnSuspension(false, execContext_C, syncContext_C);
  // Logical return to B and linking in B's continuation
  CaptureContinuationContext(continuation, ref continuation.ContinuationContextForB, ref continuation.FlagsForB)
  CaptureExecutionContext(continuation, ref continuation.ExecutionContextForB);
  if (!resumed_B)
  {
    RestoreContextsOnSuspension(false, execContext_B, syncContext_B);
    // Logical return to A and linking in A's continuation
    CaptureContinuationContext(continuation, ref continuation.ContinuationContextForA, ref continuation.FlagsForA)
    CaptureExecutionContext(continuation, ref continuation.ExecutionContextForA);

    RestoreContextsOnSuspension(resumed_A, execContext_A, syncContext_A);
  }
  else
  {
    // keep continuation.ContinuationContextForA, FlagsForA, ExecutionContextForA
  }
}
else
{
  // keep continuation.ContinuationContextForB, FlagsForB, ExecutionContextForB
}
```
Note also that we have the implications `resumed_C` implies `resumed_B` implies `resumed_A` simplifying the `else` cases here.
The else cases also rely on `continuation.ContinuationContextForB/A` staying assigned in the continuations, which is the case only because the JIT allocates a single continuation and reuses it throughout the execution of `A`.

This may look like a large amount of code for every suspension inside `C`, but sharing of tails is possible since all the inlined suspensions end with the same code that does not change per suspension site.

### Resuming inside an inlinee

Let's imagine we resume at the inlined `Task.Yield()` in `C`.
Initially such a resumption progesses as normal: async infrastructure restores the `ExecutionContext` that was saved.
If this was a task await (which it isn't, but often will be), the async infrastructure ensures it is running in the right continuation context.
Then it resumes in the code.

When `C` logically returns to `B` we need to handle what the async infrastructure would have handled for us.
Call this the _post-inline IR_.
- We need to restore the `ExecutionContext` that we captured for the suspension at `C()`.
Logically, we need to do something like
```csharp
if (resumed_C)
  AsyncHelpers.RestoreExecutionContext(Thread.CurrentThreadAssumedInitialized, continuation.ExecutionContextForB);
```
- We need to ensure we're running on the proper continuation context.
The JIT cannot accomplish this on its own; there is no way to directly set up the environment without posting a callback.
Hence, in the case where we are not running in the right context we need help from async infrastructure to proceed with a suspension+switch+resumption:
```csharp
if (resumed_C && !AsyncHelpers.IsOnRightContext(continuation.ContinuationContextForB, continuation.FlagsForB))
  await AsyncHelpers.SwitchContext(continuation.ContinuationContextForB, continuation.FlagsForB);
```

We expect `IsOnRightContext` to be true almost always, since it is very rare that the current synchronization context is modified, especially during the execution of async methods.
If this was a common case then it is unlikely that inlining would ever be profitable.

### Handling synchronous saves and restores of contexts

Recall that every runtime async function's body was wrapped with a save/restore of the contexts.
Given `resumed_A`, `resumed_B` and `resumed_C` it is not hard to insert these for every inlinee.
For example, when inlining `C` we insert code similar to:

```csharp
ExecutionContext execContext;
SynchronizationContext syncContext;
AsyncHelpers.CaptureContexts(out execContext, out syncContext);
try
{
  // actual user code
}
finally
{
  AsyncHelpers.RestoreContexts(resumed_C, execContext, syncContext);
}
```

### Special cases

The only case supported today is when we know the inlinee never suspends, by virtue of having no awaits at all.
This simplifies the cases above since `resumed_B` and `resumed_C` are always false, and all suspension points generated belong to `A`.

### Computing `resumed_A`, `resumed_B`, `resumed_C` ...

Today we compute the `resumed` value only for the top-level async function, since it is computed based on the async continuation parameter.
With inlinees this strategy no longer works.
Instead insert IR to compute these values at several points:
1. When starting a function `F` (whether it be the top function or start of an inlined function), assign `resumed_F = false`
2. When resuming `F` at one of its resumption points assign `resumed_F = true`
3. When logically returning from `F` to its caller, inherit the resumption status: `resumed_caller |= resumed_F`.

Note that these are introduced at different points. (1) and (3) will likely be inserted during `PHASE_SAVE_ASYNC_CONTEXTS` while (2) will be inserted in `PHASE_ASYNC`.

(2) is a store to `resumed_F` inserted very late.
Until the async transformation we will model this as a `LCL_ADDR` well-known argument added to all async calls.

It is important that the JIT can reason about `resumed` booleans to be able to eliminate the cruft for inlinees that are proven to never suspend.
We would like constant propagation to realize `resumed_C` is always false if `C` has no suspension point, for example.

### Representation of post-inline IR

The post-inline IR we need to insert when `C` returns to `B` looks like:

```csharp
if (resumed_C)
{
  AsyncHelpers.RestoreExecutionContext(Thread.CurrentThreadAssumedInitialized, continuation.ExecutionContextForB);

  if (!AsyncHelpers.IsOnRightContext(continuation.ContinuationContextForB, continuation.FlagsForB))
  {
    await AsyncHelpers.SwitchContext(continuation.ContinuationContextForB, continuation.FlagsForB);
  }

  resumed_B = true;
}
```

It is not clear how to represent `continuation.ExecutionContextForB`, `continuation.ContinuationContextForB` and `continuation.FlagsForB`.
This IR is likely something to insert as part of inlining, yet at that point we have not laid out the continuation yet.
To solve this we will introduce a `GT_CONTINUATION_FIELD_OFFSET` node that represents the field offset and that is replaced by a constant as part of the async transformation.

### Exceptions

Inlined functions can finish by throwing exceptions too.
Under normal circumstances the async infrastructures catches the exception and looks for the next continuation that may handle the exception.
Once located, the same resumption mechanism applies: the `ExecutionContext` is restored and the proper continuation context is ensured.

Correct handling requires catching the possible exception and then rethrowing it in the post-inline IR.
The expansion for calls in try clauses would look roughly like:
```csharp
try
{
  // user code for C
}
catch (throwable ex) when (resumed_C)
{
  throwable_C = ex;
}

if (resumed_C)
{
  AsyncHelpers.RestoreExecutionContext(Thread.CurrentThreadAssumedInitialized, continuation.ExecutionContextForB);

  if (!AsyncHelpers.IsOnRightContext(continuation.ContinuationContextForB, continuation.FlagsForB))
  {
    await AsyncHelpers.SwitchContext(continuation.ContinuationContextForB, continuation.FlagsForB);
  }

  resumed_B = true;

  if (throwable_C != null)
    ThrowExact(throwable_C);
}
```

Initially we will skip inlining async calls that may suspend in try clauses.

## Some examples

In the following assume the presence of two synchronization contexts `_syncContext1` and `_syncContext2` that set up the global environment in some distinguishable way.

```csharp
public static async Task Foo()
{
    SynchronizationContext.SetSynchronizationContext(_syncContext1);
    await Bar();
}

private static async Task Bar()
{
    SynchronizationContext.SetSynchronizationContext(_syncContext2);
    await Baz();
}

private static async Task Baz()
{
    await new AlwaysThreadPoolAwaitable();
}

private struct AlwaysThreadPoolAwaitable : INotifyCompletion
{
    public bool IsCompleted => false;
    public void OnCompleted(Action continuation)
    {
        ThreadPool.QueueUserWorkItem(_ => { Thread.Sleep(100); continuation(); });
    }
    public void GetResult() { }

    public AlwaysThreadPoolAwaitable GetAwaiter() => this;
}
```

- `Foo()` runs in the ambient synchronization context but switches its own synchronization context before awaiting `Bar()`. Its continuation captures `_syncContext1` as the continuation context.
- `Bar()` also switches its own synchronization context before awaiting `Baz()`. Its continuation captures `_syncContext2` as the continuation context.
- `Baz()` always suspends, switching onto a thread pool thread. No continuation context is captured for custom awaitables.
- On resumption, async infrastructure runs `Baz` directly in the context of `AlwaysThreadPoolAwaitable`
- Next, async infrastructure switches to `_syncContext2` to run `Bar`'s continuation
- Next, async infrastructure switches to `_syncContext1` to run `Foo`'s continuation

Inlining in this case would need to suspend+switch+resume in both the `IsOnRightContext` checks.

```csharp
private static AsyncLocal<int> s_local = new();
public static async Task Foo()
{
    s_local.Value = 1;
    await Bar();
    Console.WriteLine(s_local.Value);
}

private static async Task Bar()
{
    s_local.Value = 2;
    await Baz();
    Console.WriteLine(s_local.Value);
}

private static async Task Baz()
{
    s_local.Value = 3;
    await new AlwaysThreadPoolAwaitable();
    Console.WriteLine(s_local.Value);
}

Similar to above, except this time we do not need a full bail out; rather, the post-inline handling will restore the `ExecutionContext` to make sure the value of `s_local` is properly restored.
Proper output is 3,2,1 and relies on `ExecutionContext`s having been restored by the post-inline handling.
```