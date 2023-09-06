# Multi-threading on browser

## Goals
 - don't change/break single threaded build. †
 - JS interop
     - both sync and async calls and callbacks
        - sync calls from JS to C# are part of the problem, see below
     - from UI thread to "some C# main thread" and back
     - from dedicated worker to it's JavaScript state and back
 - CPU intensive workloads on dotnet thread pool
 - enable blocking .Wait APIs from C# user code
     - Current public API throws PNSE for it
 - allow HTTP and WS C# APIs to be used from any thread
     - Underlying JS object have thread affinity

<sub><sup>† Note: all the text below discusses MT build only, unless explicit about ST build.</sup></sub>

# Problem
1. If you have multithreading, any thread might need to block while waiting for any other to release a lock.
     - in user code, in nuget packages, in Mono VM itself, there are managed and un-managed locks
     - in single-threaded build of the runtime, all of this is NOOP. That's why it works.
2. UI thread in the browser can't synchronously block
     - you can spin-lock but it's bad idea. 
         - It eats your battery 
         - Browser will kill your tab at random point (Aw, snap).
         - It's not deterministic and you can't really test your app to prove it harmless.
     - all the other threads/workers could synchronously block
3. JavaScript engine APIs and objects have thread affinity. The DOM and few other browser APIs are only available on the main UI "thread"
     - and so, you need to have C# interop with UI, but you can't block there.

## Design proposal

### TL;DR
4. execute C# code on "deputy worker", executing all user C# on behalf of the UI JavaScript
5. throw PNSE when UI JavaScript would call in any synchronous JSExport or callback to C#. This will prevent Mono from trying to randomly block the call.

### Alternatives
10. create emscripten engine on worker
- this is similar to 4. but not feasible
- it would break lot of existing JavaScript APIs
- it would make startup callbacks on wrong thread (blazor JS integration)
- we would have to re-write Blazor renderBatch
11. throw PNSE any time C# code needs to block
- It's not deterministic and you can't really test your app to prove it harmless.
12. throw PNSE any time C# code or VM code needs needs to block
- Mono VM needs to hold lock while allocating memory, even on the UI thread.

## Design proposal details

### UI thread
- this is the main browser "thread", the one with DOM on it
- will start emscripten as usual
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

### Deputy worker
- executing all **user C# code** on behalf of the UI JavaScript "thread"
    - that is also C# entry point `Task Main()` or `void Main()`
    - `void Main()` would return promise that never resolves to UI JavaScript
- doesn't expose JavaScript state to user code.
    - as optimization we could consider running HTTP and WS client here, instead of UI thread.
- has SynchronizationContext installed on it
    - So that C# calls could be dispatched to it by runtime
- throw PNSE on attempt to marshal sync C# delegate to UI JavaScript
- can run C# finalizers
- will run GC

### WebWorker with JS interop
- is C# thread created and disposed by new API for it
- there is JSSynchronizationContext installed on it
    - so that user code could dispatch back to it, in case that it needs to call JSObject proxy (with thread affinity)

### C# Thread
- without JS interop. calling JSImport will PNSE.

### C# Threadpool Thread
- without JS interop. calling JSImport will PNSE.

### JSImport

### JSExport

### Promise

### Task, Task<T>

### Blazor
 - provide SynchronizationContext which would dispatch [`Component.InvokeAsync`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase.invokeasync) to 
 - Blazor renderBatch will continue working even with legacy interop in place. 
   - Because it only reads memory and it doesn't call back to Mono VM.
 - throw PNSE from Blazor's [`IJSInProcessRuntime.Invoke`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsinprocessruntime.invoke)
 - throw PNSE from Blazor's any call to [`IJSUnmarshalledRuntime `](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsunmarshalledruntime)

## Current state 2023 Sep
 - we have pre-allocated pool of webWorker which are mapped to pthread dynamically.
 - we can configure pthread to keep running after synchronous thread_main finished. That's necessary to run any async tasks involving JavaScript interop.
 - GC is running on UI thread/worker.
 - legacy interop has problems with GC boundaries.
 - JSImport & JSExport work
 - There is private JSSynchronizationContext implementation which is too synchronous
 - There is draft of public C# API for creating WebWorker with JS interop. It must be dedicated un-managed resource, because we could not cleanup JS state created by user code.
 - There is MT version of HTTP & WS clients, which could be called from any thread but it's also too synchronous implementation.
 - Many unit tests fail on MT https://github.com/dotnet/runtime/pull/91536

## Task breakdown

 - [ ] make C# finalizers work


Tracking https://github.com/dotnet/runtime/issues/85592