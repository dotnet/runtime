# Threaded runtime #

## Building ##

Build with `/p:WasmEnableThreads=true`

**TODO**: Have two options - one for limited threading support for the runtime internals, and another to fully enable threading for user apps.

## Libraries feature defines ##

We use the `FeatureWasmThreads` property in the libraries projects to conditionallyie define
`FEATURE_WASM_THREADS` which is used to affect how the libraries are built for the multi-threaded
runtime.

**TODO** add a separate feature property for WebAssembly EventPipe diagnostics.

### Ref asssemblies ###

For ref assemblies that have APIs that are related to threading, we use
`[UnsupportedOSPlatform("browser")]` under a `FEATURE_WASM_THREADS` define to mark APIs that are not
supported with the single-threaded runtime.  Each such ref assembly (for example
`System.Threading.Thread`) is defined in two places: `src/libraries/System.Threading.Thread/ref` for
the single-threaded ref assemblies, and
`src/libraries/System.Threading.Thread.WebAssembly.Threading/ref/` for the multi-threaded ref
assemblies.  By default users compile against the single-threaded ref assemblies, but by adding a
`PackageReference` to `Microsoft.NET.WebAssembly.Threading`, they get the multi-threaded ref
assemblies.

### Implementation assemblies ###

The implementation (in `System.Private.CoreLib`) we check
`System.Threading.Thread.IsThreadStartSupported` or call
`System.Threading.Thread.ThrowIfNoThreadStart()` to guard code paths that depends on
multi-threading.  The property is a boolean constant that will allow the IL trimmer or the
JIT/interpreter/AOT to drop the multi-threaded implementation in the single-threaded CoreLib.

The implementation should not use `[UnsupportedOSPlatform("browser")]`

## Native runtime preprocessor defines ##

In `src/mono/mono`, `DISABLE_THREADS` is _not_ defined if threading is enabled.

If `src/mono/wasm`, `__EMSCRIPTEN_THREADS__` _is_ defined if threading is enabled.

**TODO**: Add a define if threading is only enabled internally, not for user apps.

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
