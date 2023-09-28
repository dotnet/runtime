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
As well as define that `Task`/`ValueTask` and other awaiters provide awaiters provided implement the `INotifyCompletion2`/`ICriticalNotification2` interfaces. The AwaitAwaiterFromRuntimeAsync provides a means for code generators to await on anything that is awaitable today with a simple set of IL.

### Utilizing async2 based code from non-async2 based code

Non-async2 code is never permitted to directly invoke async2 code. Instead any and all calls into async2 functions shall be done by means of one of the thunks.

### Interaction with Reflection

Async2 methods will not be visible in reflection via the `Type.GetMethod`, `TypeInfo.DeclaredMethods`, `TypeInfo.DeclaredMembers` , or `Type.GetInterfaceMap()` apis. 

`Type.GetMembers` and `Type.GetMethods` will be able to find the async2 methods if and only if the `BindingFlags.Async2Visible` flag is set.

`Type.GetMemberWithSameMetadataDefinitionAs` will find the matching async variant method.

### Modification of existing async infrastructure

The only known modifications are to add the various new `INotifyCompletion2` and `INotifyCompletion3` interfaces to the awaitable types in the core libraries. 

## Implementation of async2 design 1

TBD, design in progress... there are some notes, but this is very incomplete at this moment.

The general design is to leverage the notion that a normal code generated function at a function call point is effectively a state machine. The index for resumption is the IP that returning to the function will set, and the stackframe + saved registers are the current state of the state machine. I believe we can achieve performance comparable to synchronous code with this scheme for non-suspended scenarios, and we may achieve acceptable overall performance as an async mechanism if suspension is relatively rare.

#### JIT Code changes
1. Do not allow InlinedCallFrames to be linked to the frame chain across a suspend (Or disable p/invoke inlining in these methods)
2. Report frame pointers as ByRef
3. Report all pointers to locals as ByRef
4. (If possible) tweak codegen for return processing to not use the address returned in RAX when doing byref returns
5. Force the AwaitAwaiterFromRuntimeAsync to be treated as an async2 method even though it isn't marked as such (due to compiler limitations)

#### Thunk from the Task api surface to async2 based implementation

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

## C# Language changes

TBD

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

