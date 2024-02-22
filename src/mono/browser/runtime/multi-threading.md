# Multi-threading with JavaScript interop

## Meaningful configurations are:

 * Single-threaded mode as you know it since .Net 6
    - default, safe, tested, supported
    - from .Net 8 single-threaded build could be easily started also as a web worker
       - but you need your own messaging between UT and the dotnet running in the worker
       - demo https://github.com/ilonatommy/reactWithDotnetOnWebWorker
 * `MainThreadingMode.DeputyThread` + `JSThreadBlockingMode.NoBlockingWait` + `JSThreadInteropMode.SimpleSynchronousJSInterop`
    + **default threading**, safe, tested, supported
    + blocking `.Wait` is allowed on thread pool and new threads
    - blocking `.Wait` throws `PlatformNotSupportedException` on `JSWebWorker` and main thread
    - DOM events like `onClick` need to be asynchronous, if the handler needs use synchronous `[JSImport]`
    - synchronous calls to `[JSImport]`/`[JSExport]` can't synchronously call back

 * `MainThreadingMode.DeputyThread` + `JSThreadBlockingMode.AllowBlockingWait` + `JSThreadInteropMode.SimpleSynchronousJSInterop`
    + pragmatic for legacy codebase, which contains blocking code and can't be fully executed on thread pool or new threads
    - **could cause deadlocks !!!**
        - Use your own judgment before you opt in.
    - blocking `.Wait` is allowed on all threads!
    - blocking `.Wait` on pending JS `Task`/`Promise` (like HTTP/WS requests) could cause deadlocks!
        - reason is that blocked thread can't process the browser event loop
        - so it can't resolve the promises
        - even when it's longer `Promise`/`Task` chain
        - on other platforms, I/O typically doesn't have thread affinity. Browser is more prone to deadlock.
    - DOM events like `onClick` need to be asynchronous, if the handler needs use synchronous `[JSImport]`
    - synchronous calls to `[JSImport]`/`[JSExport]` can't synchronously call back

## Unsupported combinations are:
 * `MainThreadingMode.DeputyThread` + `JSThreadBlockingMode.NoBlockingWait` + `JSThreadInteropMode.NoSyncJSInterop`
    + very safe
    - HTTP/WS requests are not possible because it currently uses synchronous JS interop
    - Blazor doesn't work because it currently uses synchronous JS interop
 * `MainThreadingMode.UIThread`
    - not recommended, not tested, not supported!
    - can deadlock on creating new threads
    - can deadlock on blocking `.Wait` for a pending JS `Promise`/`Task`, including HTTP/WS requests
    - `.Wait` is spin-waiting in the UI thread - it blocks debugger, network, UI rendering, ...
    + JS interop to UI is faster, synchronous and re-entrant

### There could be more modes:
 - allow re-entrant synchronous JS interop on `JSWebWorker`.
    - This is possible because managed code is running on same thread as JS.
    - But it's nuanced to debug it, when things go wrong.
    - The model breaks when `JSObject`s with affinity to multiple `JSWebWorker`s is used in chain of synchronous calls dispatched to multiple threads.
 - allow re-entrant synchronous JS interop also on deputy thread.
    - This is not possible for deputy, because it would deadlock on call back to different thread.
    - The thread receiving the callback is still blocked waiting for the first synchronous call to finish.
