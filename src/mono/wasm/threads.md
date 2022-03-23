# Threaded runtime #

## Building ##

Build with `/p:WasmEnableThreads=true`

**TODO**: Have two options - one for limited threading support for the runtime internals, and another to fully enable threading for user apps.


## Preprocessor defines ##

In `src/mono/mono`, `DISABLE_THREADS` is _not_ defined if threading is enabled.

If `src/mono/mono`, `__EMSCRIPTEN_THREADS__` _is_ defined if threading is enabled.

**TODO**: Add a define if threading is fully enabled for user apps.

## Browser thread, main thread ##

When the app starts, emscripten can optionally run `main` on a new worker instead of on the browser thread.

Mono does _not_ use this at this time.

## Running work on other threads ##

Emscripten provides an API to queue up callbacks to run on the main thread, or on a particular
worker thread.  See
[`emscripten/threading.h`](https://github.com/emscripten-core/emscripten/blob/main/system/include/emscripten/threading.h).

Mono exposes these functions as `mono_threads_wasm_async_run_in_main_thread`, etc in
`mono/utils/mono-threads-wasm.h`.

## Background tasks ##

The runtime has a number of tasks that are scheduled with `mono_threads_schedule_background_job`
(pumping the threadpool task queue, running GC finalizers, etc).

The background tasks will run on the main thread.  Calling `mono_threads_schedule_background_job` on
a worker thread will use `async_run_in_main_thread` to queue up work for the main thread.
