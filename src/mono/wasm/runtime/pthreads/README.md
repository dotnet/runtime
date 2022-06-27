# Mono PThreads interface

## Summary

This is an internal API for the Mono runtime that provides some JS-side conveniences for working with pthreads.

WebWorkers have a hierarchical relationship where the browser thread can talk to workers, but the workers cannot talk to each other.
On the other hand, pthreads have a peer relationship: any two threads can talk to each other.

In order to bridge the two designs, we have provide a mechanism for the threads to communicate with each other.

## Main thread API

In the main thread, `pthreads/browser` provides a `getThread` function that returns a `{ pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort }` object that can be used to communicate with the worker thread.

## Worker thread API

In the worker threads, `pthread/worker` provides `addThreadCreatedCallback ((pthread_ptr, main_port: MessagePort) => {...} )` that can be used from `startup.ts` (see `mono_wasm_pthread_worker_init`) to add a callback that will be called whenever a new pthread is created - it is passed the thread id, and a channel to the main thread.

## Implementation

   This is meant to provide a dedicated communication channel between a pthread and the main thread.
   The Emscripten threading APIs don't provide a way to send  [Transferable objects](https://developer.mozilla.org/en-US/docs/Glossary/Transferable_objects)
     from one pthread to another.  It is also not great for sending around JS objects in general.

   Instead, we hook a single custom message that gets called when a messsage is received from the pthread when it's created.

   This is how we set it up:

   1. We replace emscripten's `PThread.loadWasmModuleToWorker` and `PThread.theadInit`(`threadInitTLS` in later emscripten versions) method with our own that calls `afterLoadWasmModuleToWorker`.
   2. When Emscripten creates a worker that will run pthreads, we install an additional message handlers for `loadWasmModuleToWorker` and `threadInit`.
   3. Something in the native code calls `pthread_create`
   4. A pthread is created and emscripten sends a command to a worker to create a pthread.
   5. The worker runs the `threadInit` callback and the runtime calls `mono_wasm_on_pthread_created` in the new thread running on the new worker
   6. the worker wakes posts a "channel_created" message to the main thread on the worker message event handler
   7. our custom message handler runs on the main thread and receives the MessagePort
   8. now the main thread and the worker have a dedicated communication channel
   9. Optionally, the runtime can do something with the `mono_wasm_on_pthread_attached` callback which runs when a pthread first attaches to the Mono runtime. (for example if it was created by other native code from a nuget library)

  This could get better if the following things changed in Emscripten:

   1. If we could have a way to avoid collisions with Emscripten's own message handlers entirely, we wouldn't need a MessageChannel at all, we could just piggyback on the normal worker communication.
