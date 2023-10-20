# Async with JIT generated state machines

## Introduction

This document describes a potential way to move the async transformation from Roslyn to the JIT.
The approach described here has the JIT transform async methods into a state machine in a way that such async methods can both suspend and be resumed.
As a goal, the design strives to make modifications such that the impact on the JIT's codegen of the synchronous path is minimized.

To achieve that, we design around a more or less standard call/return stack pattern in the synchronous case, and then build on top of it the necessary infrastructure to handle resumption.

## Dispatching continuations

The main complication happens when continuations are to be dispatched.
When that occurs the stack logically inverts compared to the normal synchronous call: the deepest frame's continuation has to be called first, and once it returns, it has to ensure that it calls the next continuation.
There are multiple issues that the machinery today has to handle around this:
1. It has to move continuations to other stack frames to ensure the stack is not exhausted
2. It has to propagate exceptions thrown inside later frames to the next continuations; it cannot rely on normal EH to propagate exceptions, since the logical callers are no longer on the stack.

I expect that 2. has some impact on the quality of the JIT generated code since the JIT can be more conservative inside EH constructs; however, the impact may not be that high given that much of the state is not expected to be live in the handler.

To solve both issues we propose using an an async dispatcher.
The idea is to move the responsibility of invoking follow-up continuations from the async method to the async dispatcher.
This is very similar to the current tailcall via helpers approach.

The async dispatcher will look something like (details omitted, full version of this is given below):

```csharp
enum Suspended
{
    Yes,
    No,
}

struct Continuation
{
    public object Next;
    public delegate*<object, Exception, Suspended> Resume;
    public int State;
}

public static void DispatchContinuations(object boxedContinuation, Exception exToPropagate)
{
    do
    {
        ref Continuation continuation = ref Unsafe.Unbox<Continuation>(boxedContinuation);
        try
        {
            if (continuation.Resume(boxedContinuation, exToPropagate) == Suspended.Yes)
            {
                break;
            }
        }
        catch (Exception ex)
        {
            exToPropagate = ex;
        }

        boxedContinuation = continuation.Next;
    } while (boxedContinuation != null);
}
```

The first time an asynchronous continuation is resumed it is expected that such a dispatcher is set up on the stack frame by the code doing the actual asynchronous resumption (for example, a socket receive callback).

The JIT must set up all the stuff to make the above dispatching work.
1. It has to generate code to create a continuation when suspension happens.
   Logically, the continuation captures the live state of the function that is suspending, including some way to figure out where in the function suspension happened.
   Furthermore, the continuation must somehow be registered with the callee to know what to call when it is resuming.
   
2. It has to generate code to allow resuming from a continuation.
   This involves:
   1. Determining where in the function control needs to be transferred back to.
      It may not always be possible to directly transfer control back, in particular when the function has EH, in which case we can employ a "switchboard" approach at the beginning of each `try` (OSR already must do this).
   2. Based on the above, determining what we expect to have been saved in the continuation and restoring registers/stack slots from it.
   3. If any resumption exception is present, invoking necessary EH constructs based on it. 
      The simple way to do this is just to rethrow the exception after transferring control back to the suspension point.
      Optimizations here may be possible, for example by having the dispatcher skip continuations without EH.

The continuation also saves space for the return value of the callee.

## Resumption stubs
We expect to split the second step above into two components.
1. A part that is handled by the JIT as part of generating the async method and that becomes part of that function's codegen.
   We'd like to ensure this part has minimal negative impact on the CQ of the async method to not harm the common synchronous path.
   This code will involve reloading registers and stack locations, and transferring control back to the right place in the function.
   It will mean the async methods have a special calling convention to identify whether they are being resumed or whether they are being invoked normally.
2. The second part is an IL stub created by the runtime on request of the JIT.
   The continuations created by the JIT will point at such a resumption stub.
   The resumption stubs are necessary because of the following:
   1. When the function is resumed, something must allocate stack space for the arguments and set registers containing arguments to sensible values, for stack scanning/GC purposes
   2. If the resumed function returns normally, then something must propagate the return value back to the next continuation, if required
   3. Since the async method has a special calling convention for resumption, something needs to know how to call it specially and where to pass the continuation.

## Continuation layout
The `Continuation` struct shown above will be a prefix of the continuation struct created by the JIT.
For example, for the following case:

```csharp
static async2 void Foo(int a, int b)
{
    BarReturn ret = await Bar();
    b += ret.val + a;
    int ret2 = await Baz();
    Console.WriteLine(b);
}

// uninlined
static async2 BarReturn Bar()
{
    return new BarReturn {val = 0};
}

// uninlined
static async2 int Baz()
{
    await Yield();
    return 0;
}
```

The runtime/JIT would create these continuation structures:

```csharp
struct ContinuationFoo1
{
    public object Next;
    public delegate*<ref ContinuationFoo1, Exception, object> Resume;
    public int State;
    public BarReturn Returned;
    public int a;
    public int b;
}

struct ContinuationFoo2
{
    public object Next;
    public delegate*<ref ContinuationFoo2, Exception, object> Resume;
    public int State;
    public int Returned;
    // int a; not necessary since a is not live here
    public int b;
}

struct ContinuationBaz1
{
    public object Next;
    public delegate*<ref ContinuationBaz1, Exception, object> Resume;
    public int State;
}
```

and the resumption stubs may look like:
```csharp
object ResumeFoo1(object continuation, Exception exToPropagate)
{
    object newContinuation = null;
    // No return value to propagate back since 'Foo' returns void
    RuntimeHelpers.Async2Call(ref newContinuation);
    Foo();
    return newContinuation;
}

object ResumeBaz1(object continuation, Exception exToPropagate)
{
    object newContinuation = null;
    RuntimeHelpers.Async2Call(ref newContinuation);
    int bazResult = Baz();
    if (newContinuation == null)
    {
        // Points to ContinuationFoo2 always in the example above
        Unsafe.Unbox<ContinuationWithResult<int>>(continuation.Next).Result = bazResult;
    }

    return newContinuation;
}

struct ContinuationWithResult<T>
{
    public object Next;
    public delegate*<ref ContinuationWithResult<T>, Exception, object> Resume;
    public int State;
    public T Result;
}
```

The continuations contain GC pointers, so we will need some way to create these types dynamically, with their corresponding GC info.
They will be lifted to the heap by boxing.

## Custom calling convention

To call the new async methods, a custom calling convention is necessary for two reasons:
1. The async method needs the continuation in case it is being resumed, or null in case it isn't
2. The async method returns its boxed continuation to the caller in case it suspended.

For 1 we can model it after generic contexts by adding another argument, or we can use a special register (this might be problematic on arm32).
For 2 we expect to return the box in a separate register.

`RuntimeHelpers.CallAsync2` used above is a JIT intrinsic that allows IL code to interface with the new calling convention.
It has signature:
```csharp
[Intrinsic]
internal static T CallAsync2<T>(T callResult, ref Continuation continuation, out object newContinuation)
```

IL that calls this intrinsic is only considered valid if the first argument was pushed as a direct result of a `call`, `calli` or `callvirt` instruction.

To interface with the new calling convention on the return side we also provide `RuntimeHelpers.Async2Return`:
```csharp
[Intrinsic]
internal static T Async2Return<T>(T result, object continuation);

[Intrinsic]
internal static void Async2Return(object continuation);
```

`RuntimeHelpers.Async2Return` must appear right before a `ret` IL instruction.

Note that normal returns in an async2 method are equivalent to `return RuntimeHelpers.Async2Return(result, null)`.`

## Synchronous calls
For synchronous calls we pay the following costs:
* On entry to the function, we must check if there is a continuation, meaning that we are resuming.
  This cost can probably be avoided in common cases if we build some support for generating multiple entry points to a function in the JIT/VM.
* To call an async method we must pass a zero continuation.
* On return of an async method we must check whether it suspended or not.
* For functions with suspension points inside a a `try` block, we may pay additional cost at the beginning of every `try` and `finally` block.
  These costs may be avoidable with more JIT work.

## Interoperating with async2 calls
To integrate async2 with the existing ecosystem we need to be able to make calls between async2 and async1 methods.
Async1 in this case refers to anything the C# compiler would consider awaitable; however, here we reduce the scope and consider only `Task`, `Task<T>`, `ValueTask` and `ValueTask<T>`.

Calling async1 methods is done through the runtime functions described in [David's document](https://dev.azure.com/dnceng/internal/_git/dotnet-runtime?path=/docs/design/features/runtime-handled-tasks.md&version=GBdev/davidwr/async2-experiment&_a=preview):
```csharp
public interface ICriticalNotifyCompletion2 : ICriticalNotifyCompletion
{
    bool IsCompleted { get; }
    void GetResult();
}
public interface ICriticalNotifyCompletion2<TResult> : ICriticalNotifyCompletion
{
    bool IsCompleted { get; }
    TResult GetResult();
}
public interface INotifyCompletion2 : INotifyCompletion
{
    bool IsCompleted { get; }
    void GetResult();
}
public interface INotifyCompletion2<TResult> : INotifyCompletion
{
    bool IsCompleted { get; }
    TResult GetResult();
}

public static async2 void AwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion2
public static async2 TResult AwaitAwaiterFromRuntimeAsync<TResult, TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion2<TResult>
public static async2 void UnsafeAwaitAwaiterFromRuntimeAsync<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion2
public static async2 TResult UnsafeAwaitAwaiterFromRuntimeAsync<TResult, TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion2<TResult>
```

Calling an async2 method follows the same principles as in David's document, i.e. a thunk is created to wrap it in a Task<T>.

When a function suspends the JIT will link all continuations into a chain as the call stack unwinds.
To make this thread safe with `INotifyCompletion` we must delay calling `OnCompleted` until the continuations have all been linked.
Thus, the caller will be responsible for doing this; it is either done by the dispatcher, or done as part of the async1 -> async2 thunk.

The following shows an example of what the async2 -> async1 adapting layer may look like.
It conceptually just stores the awaiter and prepares for the caller to call `OnCompleted`.

```csharp
internal struct RuntimeAsyncAwaitState
{
    public Async2DispatcherBase Dispatcher;
    public object SentinelContinuation;
}

[ThreadStatic]
public static RuntimeAsyncAwaitState t_runtimeAsyncAwait;

public static async2 TResult UnsafeAwaitAwaiterFromRuntimeAsync<TResult, TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion2<TResult>
{
    if (awaiter.IsCompleted)
        return awaiter.GetResult();

    ref RuntimeAsyncAwaitState runtimeAsyncAwait = ref t_runtimeAsyncAwait;

    if (runtimeAsyncAwait.SentinelContinuation == null)
        runtimeAsyncAwait.SentinelContinuation = new Continuation();

    runtimeAsyncAwait.Dispatcher = new Async2Dispatcher<TResult, TAwaiter>(awaiter);
    return RuntimeHelpers.Async2Return(default, runtimeAsyncAwait.SentinelContinuation);
}

internal abstract class Async2DispatcherBase
{
    public abstract void Setup(object continuation);
}

internal sealed class Async2Dispatcher<TResult, TAwaiter> : Async2DispatcherBase where TAwaiter : ICriticalNotifyCompletion2<TResult>
{
    private readonly object _continuation;
    private readonly TAwaiter _awaiter;

    public Async2Dispatcher(TAwaiter awaiter)
    {
        _awaiter = awaiter;
    }

    public override void Setup(object continuation)
    {
        _continuation = continuation;
        _awaiter.UnsafeOnCompleted(Run);
    }

    private void Run()
    {
        // Omitted
    }
}
```

The function returns a sentinel continuation which the JIT will write the first continuation into such that the caller knows where the chain starts.

The remaining part is to describe what the async1 -> async2 adapter layer looks like.
It consists of the following:
* The thunk that calls an async2 method and produces a Task<T>
* The `Async2Dispatcher.Run` method that is the callback of the asynchronous operation
* The final implementation of the dispatcher

Let us start with the thunk:
```csharp
public static Task<T> ThunkAsync<T>(ParameterType1 param1, ParameterType2 param2, ...)
{
    object boxedContinuation;
    T result;
    try
    {
        result = RuntimeHelpers.CallAsync2(TargetMethod(param1, param2, ...), ref Unsafe.NullRef<Continuation>(), out boxedContinuation);
    }
    catch (Exception ex)
    {
        return Task.FromException(ex);
    }

    if (boxedContinuation == null)
        return Task.FromResult(result);

    // Link in a final continuation that fires the TCS.
    TaskCompletionSource<T> tcs = new();
    ref Continuation continuation = ref Unsafe.Unbox<Continuation>(boxedContinuation);
    continuation.Next = new TaskCompletionSourceContinuation<T>
    {
        Resume = &ResumeTaskCompletionSource<T>,
        TCS = tcs,
    };

    // Now call OnCompleted on the awaiter we hit in the leaf.
    ref RuntimeAsyncAwaitState runtimeAsyncAwait = ref t_runtimeAsyncAwait;
    object headContinuation = Unsafe.Unbox<Continuation>(runtimeAsyncAwait.SentinelContinuation).Next;
    runtimeAsyncAwait.Dispatcher.Setup(headContinuation);
    return tcs.Task;
}

private static object ResumeTaskCompletionSource<T>(ref TaskCompletionSourceContinuation<T> continuation, Exception ex)
{
    if (ex == null)
    {
        continuation.TCS.SetResult(continuation.Result);
    }
    else
    {
        continuation.TCS.SetException(ex);
    }

    return null;
}

public struct TaskCompletionSourceContinuation<T>
{
    public object Next;
    public delegate*<ref TaskCompletionSourceContinuation<T>, Exception, object> Resume;
    public int State;
    public T Result;
    public TaskCompletionSource<T> TCS;
}
```
The thunk has a fast path in case the async2 method finished synchronously.
If not then the function creates a TCS and links in a synthetic continuation that will invoke this TCS.
It retrieves the head continuation (which is the successor of the sentinel continuation that was saved in the leaf), and finally it calls `Async2DispatcherBase.Setup` which is responsible for actually calling `OnCompleted` on the saved awaiter.

The implementation of `Async2Dispatcher<TResult, TAwaiter>.Run` was omitted above.
This is the first function called by the asynchronous callback, and it needs to propagate the result into the continuation and start running the dispatcher.
```csharp
internal sealed class Async2Dispatcher<TResult, TAwaiter> : Async2DispatcherBase where TAwaiter : ICriticalNotifyCompletion2<TResult>
{
    private readonly object _continuation;
    private readonly TAwaiter _awaiter;

    private void Run()
    {
        Exception exToPropagate = null;
        try
        {
            TResult result = _awaiter.GetResult();
            Unsafe.Unbox<ContinuationWithResult<TResult>>(_continuation).Result = result;
        }
        catch (Exception ex)
        {
            exToPropagate = ex;
        }

        RuntimeHelpers.DispatchContinuations(_continuation, exToPropagate);
    }
}
```

Finally, the dispatcher must be expanded to also handle registering the continuation chain for the asynchronous callback, in case a continuation suspends one more time:
```csharp
public static void DispatchContinuations(object boxedContinuation, Exception exToPropagate)
{
    do
    {
        ref Continuation continuation = ref Unsafe.Unbox<Continuation>(boxedContinuation);
        try
        {
            object newContinuation = continuation.Resume(ref continuation, exToPropagate);

            if (newContinuation != null)
            {
                Unsafe.Unbox<Continuation>(newContinuation).Next = continuation.Next;

                ref RuntimeAsyncAwaitState runtimeAsyncAwait = ref t_runtimeAsyncAwait;
                object headContinuation = Unsafe.Unbox<Continuation>(runtimeAsyncAwait.SentinelContinuation).Next;
                runtimeAsyncAwait.Dispatcher.Setup(headContinuation);
                return;
            }
        }
        catch (Exception ex)
        {
            exToPropagate = ex;
        }

        boxedContinuation = continuation.Next;
    } while (boxedContinuation != null);
}
```
It also needs to transfer the next continuation stored in the previous (now invoked continuation) to the newly returned one.

## Thoughts/questions

1. Is it better to allocate a "mega"-continuation and reuse it? It will mean less type loader traffic, but more stuff kept on the heap overall.
It will also allow us to skip transferring the "Next" from the previous continuation when a resumed continuation suspends again
2. We know address-exposed locals become unexposed at suspension points.
How do we make use of this? Likely we need a custom liveness pass.

## Required JIT work

1. A pass that walks the IR and finds all awaited async calls.
   At these points we must identify live state (so run some kind of liveness) and generate code to create continuations in case the callee suspends.
   We must also create the IR that restores the live state from the continuation, and throw the exception.
3. Prolog work to handle "resume" vs "start".
4. Work to handle suspension and resumption within `try`

Some things to consider:
* Should the pass happen early or late? Early means less live state, but likely impact on CQ.
  In particular the resumption code is going to create a lot of synthetic control flow that I expect would have significant CQ impact.
  Doing it late will probably make it harder to introduce the boxing and generate the registration IR.
  Also, doing it later means more chance for optimizations to create illegal constructs (e.g. local addresses that span suspension points).
* Doing it late may allow us to simply jump directly into nested `try` blocks and exit `try` blocks without invoking finallies, though I am not sure about that.
* We need to be able to introduce control flow around awaited async calls.
  If done early we can use `gtSplitTree`.
  If done late we need to track LIR edges and introduce temps.
* How do we represent the new calls internally? They effectively define two things (return value and the "suspended").
  ~~We can probably cheat and consider these as always defining an additional pseudo-local.~~ No we cannot, because encoding that in LSRA and codegen is a nightmare when there is no tree to attach the RPs to.
* How about if the JIT creates local addresses that span across awaits? Do we need to represent the calls as opaque defs of all live state? That seems conservative and might not even fully avoid the problem.

## Optional JIT work

1. Support for generating multiple different entry points.
2. Deal with the fact that liveness of address exposed locals is going to be very conservative.