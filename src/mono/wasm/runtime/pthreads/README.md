# Mono PThreads interface

## Summary

This is an internal API for the Mono runtime that provides some JS-side conveniences for working with pthreads.

WebWorkers have a hierarchical relationship where the browser thread can talk to workers, but the workers cannot talk to each other.
On the other hand, pthreads have a peer relationship: any two threads can talk to each other.

In order to bridge the two designs, we have provide a mechanism for the threads to communicate with each other.

## Main thread API

In the main thread, `pthreads/browser` provides a `getThread` function that returns a `{ pthread_ptr: pthread_ptr, worker: Worker, port: MessagePort }` object that can be used to communicate with the worker thread.

## Worker thread API

In the worker threads, `pthread/worker` provides `addThreadCreatedCallback ((pthread_ptr, main_port: MessagePort) => {...} )` that can be used from `startup.ts` to add a callback that will be called whenever a new pthread is created - it is passed the thread id, and a channel to the main thread.

## Implementation

   This is meant to provide a dedicated communication channel between a pthread and the main thread.
   The Emscripten threading APIs don't provide a way to send  [Transferable objects](https://developer.mozilla.org/en-US/docs/Glossary/Transferable_objects)
     from one pthread to another.  It is also not great for sending around JS objects in general.

   Instead, we hook a single custom message that gets called when a messsage is received from the pthread when it's created.

   This is how we set it up:

   1. Something in the runtime calls mono_thread_create_internal()
   2. the runtime creates a 32-bit integer location on the new thread's stack (worker_notify_ptr) that we can use to synchronize
   3. the runtime thread start routine calls mono_wasm_on_pthread_created in the new thread
      - that function immediately blocks on worker_notify_ptr using Atomics.wait
   4. the runtime posts an async function to the main thread to run mono_wasm_pthread_on_pthread_created_main_thread
     - that function adds does worker.addEventHandler("message", monoMessageFromWorkerHandlerForMainThread)
     - that handler's only job is to receive a "channel_created" message from the worker.
     - the main thread wakes the worker by doing Atomics.store and Atomics.notify
   5. the worker wakes up and posts a "channel_created" message to the main thread
   6. our custom message handler runs on the main thread and receives the MessagePort
   7. now the main thread and the worker have a dedicated communication channel

  This could get better if the following things changed in Emscripten:

  1. There was some hook on Module.Pthread.loadWasmModuleToWorker so that we could add our own message handler at the same time that Emscripten adds its own.
     That would let us eliminate queuing async work on the main thread and block the worker until it's done.
     This is because we can't easily add a handler to _every_ new worker as it is added to the pthread worker pool.

  2. If we could have a way to avoid collisions with Emscripten's own message handlers entirely, we wouldn't need a MessageChannel at all, we could just piggyback on the normal worker communication.

**FIXME**: we really need to hook deeper - if a third party library creates a thread we need to attach it when it attaches to the runtime.
