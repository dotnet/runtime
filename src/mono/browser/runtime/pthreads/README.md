# Mono PThreads interface

## Summary

This is an internal API for the Mono runtime that provides some JS-side conveniences for working with pthreads.

Currently this provides an API to register lifecycle event handlers that will run inside workers when Emscripten runs pthreads on the worker.

Additionally, the API sets up a dedicated dotnet-specific channel for sending messages between the browser thread and individual pthreads.  Unlike the WebWorker `worker.postMessage` `DedicatedWorkerGlobalScope.postMessage` these ports:

1. Are not used by Emscripten or any other JS library.  They are specific to dotnet.
2. Are tied to the lifetime of the *pthread* not of the worker.  If Emscripten reuses a worker to start a new pthread, the runtime will get a new message channel between that new pthread and the browser thread.

In the future, this may also provide APIs to establish a dedicated channel between any two arbitrary pthreads.
WebWorkers have a hierarchical relationship where the browser thread can talk to workers, but the workers cannot talk to each other.
On the other hand, pthreads in native code have a peer relationship: any two threads can talk to each other.  The new API will help bridge the gap and provide a mechanism for the threads to communicate with each other in JS.

## Main thread API

In the main thread, `pthreads/ui-thread` provides a `getThread` function that returns a `{ pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort }` object that can be used to communicate with the worker thread.

## Worker thread API

In the worker threads, `pthread/worker-*` provides `currentWorkerThreadEvents` which is an [`EventTarget`](https://developer.mozilla.org/en-US/docs/Web/API/EventTarget) that fires `'dotnet:pthread:created'` and `'dotnet:pthread:attached'` events when a pthread is started on the worker, and when that pthread attaches to the Mono runtime. A good place to add event listeners is in `mono_wasm_pthread_worker_init` in `startup.ts`.
The events have a `portToMain` property which is a dotnet-specific `MessagePort` for posting messages to the main thread and for listening for messages from the main thread.

## Implementation

   This is meant to provide a dedicated communication channel between a pthread and the main thread.
   The Emscripten threading APIs don't provide a way to send  [Transferable objects](https://developer.mozilla.org/en-US/docs/Glossary/Transferable_objects)
     from one pthread to another.  It is also not great for sending around JS objects in general.

   Instead, we hook a single custom message that gets called when a messsage is received from the pthread when it's created.

   This is how we set it up:

   1. We replace emscripten's `PThread.loadWasmModuleToWorker` and `PThread.theadInit`(`threadInitTLS` in later emscripten versions) method with our own that call `afterLoadWasmModuleToWorker` and `afterThreadInit`, respectively.
   2. On the main browser thread `PThread.loadWasmModuleToWorker` is called to create workers for Emscripten's worker pool.  It calls our `afterLoadWasmModuleToWorker`.
   3. `afterLoadWasmModuleToWorker` installs a `worker.AddEventListener("message", handler)` handler that watches for a custom mono "channel_created" message which receives a pthread id and a MessagePort whenever a thread is created on that worker.
   4. Something in the native code calls `pthread_create`
   5. A pthread is created and emscripten sends a command to a worker to create a pthread.
   6. The worker runs Emscripten's `PThread.threadInit` JS function which calls our `afterThreadInit` in the new thread running on the new worker
   7. the worker wakes posts the "channel_created" message to the main thread on the worker message event handler
   8. our custom message handler runs on the main thread and receives the MessagePort
   9. now the main thread and the worker have a dedicated communication channel

Additionally, inside the worker we fire `'dotnet:pthread:created'` and `dotnet:pthread:attached'` events
when the worker begins running a new pthread, and when that pthread attaches to the Mono runtime, respectively.

This could get better if the following things changed in Emscripten:

   1. If we could have a way to avoid collisions with Emscripten's own message handlers entirely, we wouldn't need a MessageChannel at all, we could just piggyback on the normal worker communication.
