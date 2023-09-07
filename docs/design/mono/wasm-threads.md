# Multi-threading on browser

## Goals
 - CPU intensive workloads on dotnet thread pool
 - enable blocking .Wait APIs from C# user code
     - Current public API throws PNSE for it
     - This is core part on MT value proposition.
     - If people want to use existing MT code-bases, most of the time, the code is full of locks. People want to use existing code as is.
 - allow HTTP and WS C# APIs to be used from any thread
     - Underlying JS object have thread affinity
 - don't change/break single threaded build. †
 - don't try to block on UI thread.

<sub><sup>† Note: all the text below discusses MT build only, unless explicit about ST build.</sup></sub>

## Design proposal TL;DR - Alternative 10
20. MAUI/BlazorWebView ... is the same thing
21. execute whole WASM runtime on a worker. Blocking is fine there.
22. The UI thread will only have `blazor.server.js` with small modifications.
23. This needs new marketing name! -> `WASM server` ???

# Detailed design

## Blazor - what changes vs MAUI server
- it still needs to compile for WASM target
- it will have threadpool and blocking C# `.Wait` and `lock()`
- it would not have Socket or DB connection

## Blazor - what changes vs "Blazor WASM"
- "onClick" would be asynchronous, same way as in Blazor server
- Blazor's [`IJSInProcessRuntime.Invoke`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsinprocessruntime.invoke) would not be available
- Blazor's [`IJSUnmarshalledRuntime`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsunmarshalledruntime) would not be available
- no JavaScript APIs on `globalThis.Blazor` or `globalThis.MONO`
- no runtime JS interop
- no runtime on UI thread
- Blazor united: progressive switch from remote server to WASM server, after WASM binaries arrived

## Blazor startup
- we will need to start emscripten on a worker and `postMessage` there all the configuration.
    - this is possibly the biggest effort
- probably no `Blazor.start`, `start.withResourceLoader` with returning `Response` object
- we may keep `dotnet.js` loader to run in the UI
    - TODO: prototype

### Blazor `renderBatch`
- streaming bytes - as Blazor server does
    - we use [RenderBatchWriter](https://github.com/dotnet/aspnetcore/blob/045afcd68e6cab65502fa307e306d967a4d28df6/src/Components/Shared/src/RenderBatchWriter.cs) in the WASM
    - we use `blazor.server.js` to render it
    - preferred if fast enough
    - could be ok to render batch by another thread while UI is rendering
- plan b) use existing `SharedArrayBuffer`
    - WASM would:
        - stop the GC
        - send the "read-memory-now"
        - await for "done" message
        - enable GC
    - UI could duplicate `conv_string` and `unbox_mono_obj` dark magic and use it

## Blazor JS interop
- will work as usual on "Blazor server"

## runtime JS Interop
- would be available only to workers, but it would not have access to UI thread and DOM
- could call sync and async calls both directions, promises and callbacks

## HTTP & WS
- would be running on a worker, will be accessible from all C# threads
    - we will overcome thread affinity internally

## C# Thread
- could block on synchronization primitives
- without JS interop. calling JSImport will PNSE.

## C# Threadpool Thread
- could block on synchronization primitives
- without JS interop. calling JSImport will PNSE.

## Blocking problem
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
     - and so, you need to have some way how to talk to UI

This is the main reason why we can't run MT dotnet also on UI thread.

## Alternatives
- "deputy thread" proposal https://github.com/dotnet/runtime/pull/91696

## Debugging
- VS debugger would work as usual
- Chrome dev tools would only see the events coming from `postMessage`
- Chrome dev tools debugging C# could be bit different, it possibly works already. The C# code would be in different node of the "source" tree view

## Non-blazor
- does Uno have similar "render from distance" architecture ?

## Open questions
- when MT emscripten starts on a WebWorker, does it know that it doesn't have to spin-block there ?

# Further improvements

## JSWebWorker with JS interop
- is C# thread created and disposed by new API for it
- could block on synchronization primitives
- there is JSSynchronizationContext installed on it
    - so that user code could dispatch back to it, in case that it needs to call JSObject proxy (with thread affinity)

## Promise, Task, Task<T>
- passing Promise should work everywhere.
- when marshaled to JS they bind to specific `Promise` and have affinity
- the `Task.SetResult` need to be marshaled on thread of the Promise.

## JSObject proxy
- has thread affinity, marked by private ThreadId.
    - in deputy worker, it will be always UI thread Id
    - the JSHandle always belongs to UI thread
- `Dispose` need to be called on the right thread.
    - how to do that during GC/finalizer ?
    - should we run finalizer per worker ?
- is it ok for `SynchronizationContext` to be public API
    - because it could contain UI thread SynchronizationContext, which user code should not be dispatched on.

## should we hide `SynchronizationContext` inside of the interop generated code.
- needs to be also inside generated nested marshalers
- is solution for deputy's SynchronizationContext same as for JSWebWorker's SynchronizationContext, from the code-gen perspective ?
- how could "HTTP from any C# thread" redirect this to the thread of fetch JS object affinity ?
- should generated code or the SynchronizationContext detect it from passed arguments ?
- TODO: figure out backward compatibility of already generated code. Must work on single threaded
- why not make user responsible for doing it, instead of changing generator ?
    - I implemented MT version of HTTP and WS by calling `SynchronizationContext.Send` and it's less than perfect. It's difficult to do it right: Error handling, asynchrony.

## SynchronizationContext
- we will need public C# API for it, `JSHost.xxxSynchronizationContext`
- maybe `JSHost.Post(direction, lambda)` without exposing the `SynchronizationContext` would be better.
    - we could avoid it by generating late bound ICall. Very ugly.
- on a JSWebWorker
    - to dispatch any calls of JSObject proxy members
    - to dispatch `Dispose()` of JSObject proxy
    - to dispatch `TaskCompletionSource.SetResult` etc

### dispatch alternatives
- we could use emscripten's `emscripten_dispatch_to_thread_async` or JS `postMessage`
- the details on how to interleave that with calls to `ToManaged` and `ToJS` for each argument may be tricky.

Related Net8 tracking https://github.com/dotnet/runtime/issues/85592

# TODO
- [ ] experiment with `test-main.js` to do the same
- [ ] experiment with Blazor to try easy version without JS extensibility concerns