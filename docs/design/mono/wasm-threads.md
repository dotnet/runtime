# Multi-threading on browser

## Goals
 - JS interop
     - async calls and callbacks
     - sync calls from C# to JS
     - sync calls from JS to C#
        - this proposal rejects this goal in order to solve other goals. See below.
     - from UI thread to "some C# main thread" and back
     - from dedicated worker to it's JavaScript state and back
 - CPU intensive workloads on dotnet thread pool
 - enable blocking .Wait APIs from C# user code
     - Current public API throws PNSE for it
 - allow HTTP and WS C# APIs to be used from any thread
     - Underlying JS object have thread affinity
 - don't change/break single threaded build. †
 - don't try to block on UI thread.

<sub><sup>† Note: all the text below discusses MT build only, unless explicit about ST build.</sup></sub>

## Problem
1. If you have multithreading, any thread might need to block while waiting for any other to release a lock.
     - locks are in the user code, in nuget packages, in Mono VM itself
     - there are managed and un-managed locks
     - in single-threaded build of the runtime, all of this is NOOP. That's why it works on UI thread.
2. UI thread in the browser can't synchronously block
     - you can spin-lock but it's bad idea.
         - Deadlock: when you spin-block, the JS timer loop and any messages are not pumping. But code in other threads may be waiting for some such event to resolve.
         - It eats your battery
         - Browser will kill your tab at random point (Aw, snap).
         - It's not deterministic and you can't really test your app to prove it harmless.
     - all the other threads/workers could synchronously block
3. JavaScript engine APIs and objects have thread affinity. The DOM and few other browser APIs are only available on the main UI "thread"
     - and so, you need to have C# interop with UI, but you can't block there.

## Design proposal TL;DR
4. execute C# code on "deputy worker", executing all user C# on behalf of the UI JavaScript
5. throw PNSE when UI JavaScript would call in any synchronous JSExport or callback to C#. ††

<sub><sup>†† This will prevent user C# code from trying to randomly synchronously block the caller on UI thread.</sup></sub>

## Alternatives
10. create emscripten engine on worker
- this is similar to 4. but not feasible
- it would break lot of existing JavaScript APIs
- it would make startup callbacks on wrong thread (blazor JS integration)
- we would have to re-write Blazor's `renderBatch` to bytes streaming.
11. throw PNSE any time C# code needs to block
- throwing PNSE (on lock attempt or spin-block) is easy, but it doesn't solve the "test my app and prove it valid" problem.
12. throw PNSE any time C# code or VM code needs needs to block
- Mono VM needs to hold lock while allocating memory, even on the UI thread.
13. modify `ConfigureAwait()`, work queue etc, to never dispatch to another thread.
- this would probably break user code expectations about dynamic behavior of tasks and how they run in parallel with each other.

# Design proposal details

## UI thread
- this is the main browser "thread", the one with DOM on it
- will start emscripten as usual
    - this includes C# which runs during mono startup
- will create Deputy worker as C# thread
- dispatch execution of C# `Task Main()` to the deputy worker.
- dispatch all async JSImport calls to the deputy worker.
- dispatch all async callbacks to the deputy worker.
- throw PNSE on any JSImport call from JS
- it will be valid C# thread, but not used directly by user code.
    - we will try to prevent user code from running on it and from needing to do so
- we will spin lock only for Mono VM code
    - we assume that Mono VM will block only shortly for operations like:
        - alloc/free memory
        - transform IL -> IR and update Mono VM shared state
    - we will spin lock before Blazor `renderBatch`
        - to wait for "pause GC"
    - we will spin lock during GC, if we hit barrier
        - TODO: is that short enough ?
    - we should never block for file operations or for network operations
        - TODO: how to prove it ?
        - Could we unregister the thread from Mono VM ? No, because we need the C# to dispatch calls in both directions in
- it will actually execute small chunks of C# code
    - the pieces which are in the generated code of the JSExport
    - containing dispatch to deputy worker's synchronization context
- could sync void C# methods be dispatched as fire and forget ?
    - no, because that would break the contract that they are blocking until finished.
    - also the errors would not propagate
- TODO: is there anything special about error propagation over the interop/thread boundary ?

## Deputy worker
- this is new concept introduced here. I needed some name for it 🤷‍♂️
- executing all **user C# code** on behalf of the UI JavaScript "thread"
    - that is also C# entry point `Task Main()` or `void Main()`
    - `void Main()` would return promise that never resolves to UI JavaScript
- this thread **could block** on synchronization primitives just fine!
- doesn't expose JavaScript state to user code.
    - because JSHandle has thread affinity and it's unique per JS thread.
    - as optimization we could consider running HTTP and WS client here, instead of UI thread. But JSHandle problem.
- has SynchronizationContext installed on it
    - So that C# calls could be dispatched to it by runtime
- throw PNSE on attempt to marshal sync C# delegate to UI JavaScript
    - or throw later only when you try to call the function from JS side.
- can run C# finalizers
- will run GC
- this cross-threading dispatch will have performance impact for the JS interop.
    - TODO: measure how much
    - this should not impact Blazor `renderBatch` perf.
- VS debugger would connect to mono as usual. But chrome dev tools experience may be different, because it's will be async with the C# part.

## JSWebWorker with JS interop
- is C# thread created and disposed by new API for it
- could block on synchronization primitives
- there is JSSynchronizationContext installed on it
    - so that user code could dispatch back to it, in case that it needs to call JSObject proxy (with thread affinity)

## C# Thread
- could block on synchronization primitives
- without JS interop. calling JSImport will PNSE.

## C# Threadpool Thread
- could block on synchronization primitives
- without JS interop. calling JSImport will PNSE.

## JSImport and marshaled JS functions
- both sync and async could be called on all threads
- sync: when called from C# it will use `SynchronizationContext.Send` and block caller.
- async: when called from C# it will use `SynchronizationContext.Post` and marshal promise immediately.
- when this is worker -> worker, `SynchronizationContext` should invoke it inline

## JSExport & C# delegates
- sync: will throw PNSE if called from UI JavaScript
- sync: will just work when called from JSWebWorker JavaScript
- async JSExport: will work on all threads. Will marshal promise and return immediately.
- async Delegate: are there any async callback possible yet ? The code gen doesn't support it yet in Net8.
- `getAssemblyExports` need to bind JS on UI thread, but register on deputy thread
- hide `SynchronizationContext.Send` and `SynchronizationContext.Post` inside of the generated code.
    - fast on worker -> worker

## Promise
- passing Promise should work everywhere.
- from UI javaScript it would be passed as Task to deputy worker
- open question: passing JS promise to deputy should be fine. But does the `resolve()` need to block UI thread ?

## Task, Task<T>
- passing Task should work everywhere.
- when marshaled to JS they bind to specific Promise and have affinity
    - the `SetResult` need to be marshaled on thread of the Promise.
    - The proxy of the Promise knows which `SynchronizationContext` to dispatch to.
    - on UI thread it's the UI thread's SynchronizationContext, not deputy's
    - TODO: could same task be marshaled to multiple JS workers ?

## JSObject proxy
- has thread affinity, marked by private ThreadId.
    - in deputy worker, it will be always UI thread Id
    - the JSHandle always belongs to UI thread
- `Dispose` need to be called on the right thread.
    - how to do that during GC/finalizer ?
    - should we run finalizer per worker ?
- is it ok for `SynchronizationContext` to be public API
    - because it could contain UI thread SynchronizationContext, which user code should not be dispatched on.

## JSHost.GlobalThis, JSHost.DotnetInstance, JSHost.ImportAsync
- calls will be dispatched from deputy thread to UI JavaScript
- on JSWebWorker call will stay on the same thread.

## SynchronizationContext
- we will need public C# API for it, `JSHost.xxxSynchronizationContext`
- maybe `JSHost.Post(direction, lambda)` without exposing the `SynchronizationContext` would be better.
    - we could avoid it by generating late bound ICall. Very ugly.
- hide `SynchronizationContext.Send` and `SynchronizationContext.Post` inside of the generated code.
    - needs to be also inside generated nested marshalers
    - is solution for deputy's SynchronizationContext same as for JSWebWorker's SynchronizationContext, from the code-gen perspective ?
    - how could "HTTP from any C# thread" redirect this to the thread of fetch JS object affinity ?
    - should generated code or the SynchronizationContext detect it from passed arguments ?
    - TODO: figure out backward compatibility of already generated code. Must work on single threaded
- why not make user responsible for doing it, instead of changing generator ?
    - I implemented MT version of HTTP and WS by calling `SynchronizationContext.Send` and it's less than perfect. It's difficult to do it right: Error handling, asynchrony.
- on a JSWebWorker
    - to dispatch any calls of JSObject proxy members
    - to dispatch `Dispose()` of JSObject proxy
    - to dispatch `TaskCompletionSource.SetResult` etc
- on the UI thread
    - same as above
    - as alternative we could only have there emscripten C dispatcher
        - it will need some public API any way, to be called from generated code.
- on the deputy thread
    - to dispatch async calls from UI thread to it

### dispatch alternatives
- we could use emscripten's `emscripten_dispatch_to_thread_async` or JS `postMessage`
- the details on how to interleave that with calls to `ToManaged` and `ToJS` for each argument may be tricky.

## Blazor - what breaks when MT build
- as compared to single threaded runtime, the major difference would be no synchronous callbacks.
    - for example from DOM `onClick`. This is one of the reasons people prefer ST WASM over Blazor Server.
    - but there is really [no way around it](#problem), because you can't have both MT and sync calls from UI.
- implement Blazor's `WebAssemblyDispatcher` to dispatch [`Component.InvokeAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase.invokeasync) to deputy thread.
    - process feedback from https://github.com/dotnet/aspnetcore/pull/48991 and make more async
- Blazor renderBatch will continue working even with legacy interop in place.
    - Because it only reads memory and it doesn't call back to Mono VM.
- Blazor's [`IJSInProcessRuntime.Invoke`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsinprocessruntime.invoke) should still work, because it's C#->JS direction
- Blazor's [`IJSUnmarshalledRuntime`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsunmarshalledruntime) should still work, because it's C#->JS direction
- TODO: Review Blazor's JavaScript APIs!

# Current state 2023 Sep
 - we already ship MT version of the runtime in the wasm-tools workload.
 - It's enabled by `<WasmEnableThreads>true</WasmEnableThreads>` and it requires COOP HTTP headers.
 - It will serve extra file `dotnet.native.worker.js`.
 - This will also start in Blazor project, but UI rendering would not work.
 - we have pre-allocated pool of browser Web Workers which are mapped to pthread dynamically.
 - we can configure pthread to keep running after synchronous thread_main finished. That's necessary to run any async tasks involving JavaScript interop.
 - GC is running on UI thread/worker.
 - legacy interop has problems with GC boundaries.
 - JSImport & JSExport work
 - There is private JSSynchronizationContext implementation which is too synchronous
 - There is draft of public C# API for creating JSWebWorker with JS interop. It must be dedicated un-managed resource, because we could not cleanup JS state created by user code.
 - There is MT version of HTTP & WS clients, which could be called from any thread but it's also too synchronous implementation.
 - Many unit tests fail on MT https://github.com/dotnet/runtime/pull/91536
 - there are MT C# ref assemblies, which don't throw PNSE for MT build of the runtime for blocking APIs.

## Task breakdown
- [ ] rename `WebWorker` API to `JSWebWorker` ?
- [ ] design details of JSImport binding, allocation, asynchrony
- [ ] design details of JSExport binding, allocation, asynchrony
- [ ] `ToManaged(out Task)` to be called before the actual JS method
- [ ] public API for `JSHost.<Target>SynchronizationContext` which could be used by code generator.
- [ ] change the code gen for JSImport
- [ ] change the code gen for JSExport
- [ ] reimplement `JSSynchronizationContext` to be more async
- [ ] implement Blazor's `WebAssemblyDispatcher`
- [ ] reimplement HTTP and WS with the new code gen without direct SynchronizationContext use
    - [ ] there is synchronous callback from JS event to C# in HTTP code.
- [ ] make C# finalizers work
- [ ] throw PNSE - fail fast, so that users discover limits in the dev loop
    - [ ] on any MT use of `mono_bind_static_method` from legacy interop.
        - Because it's synchronous. It throws on JSWebWorker already.
    - [ ] on UI synchronous JSImport
    - [ ] on UI synchronous C# delegate callback
    - [ ] throw fatal if somehow C# code was blocking on UI thread.
- [ ] optinal: make underlying emscripten WebWorker pool allocation dynamic, or provide C# API for that.
- [ ] optinal: implement async function/delegate marshaling in JSImport/JSExport parameters.
- [ ] optinal: enable blocking HTTP/WS APIs
- [ ] optinal: enable lazy DLL download by blocking the caller
- [ ] measure perf impact

Related Net8 tracking https://github.com/dotnet/runtime/issues/85592