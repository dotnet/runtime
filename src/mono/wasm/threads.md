# Threaded runtime #

## Building the runtime ##

Build the runtime with `/p:MonoWasmBuildVariant=multithread` to enable support for multi-threading.

Build with `/p:MonoWasmBuildVariant=perftrace` to enable support for EventPipe diagnostics - this enabled threading, but only for "internal" utility threads.  User code is not allowed to start threads.

Do not combine these options, just turn on one or the other.

## Building sample apps ##

Sample apps use the "public" properties `WasmEnableThreads` and `WasmEnablePerfTracing` to enable
the relevant functionality.  This also works with released versions of .NET 7 or later and the
`wasmbrowser` template.

## Libraries feature defines ##

We use the `FeatureWasmThreads` property in the libraries projects to conditionally define
`FEATURE_WASM_THREADS` which is used to affect how the libraries are built for the multi-threaded
runtime.

We use the `FeatureWasmPerfTracing` property in the libraries projects to
conditionally define `FEATURE_WASM_PERFTRACING` which is used to affect how the
libraries are built for a runtime that is single-threaded for users, but
internally can use multithreading for EventPipe diagnostics.

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

**TODO** For `FeatureWasmPerfTracing`, the implementation should check *some
runtime constant* and throw PNSE if diagnostics are not enabled.

## Native runtime preprocessor defines ##

In `src/mono/mono` and `src/mono/wasm` `DISABLE_THREADS` is defined for single-threaded builds (same
as mono's existing `-DENABLE_MINIMAL=threads` option).  In multi-threaded builds, `DISABLE_THREADS`
is _not_ defined.

For `WasmEnablePerfTracing`, `DISABLE_THREADS` is undefined (ie threading is enabled), but starting
user threads is not supported and `DISABLE_WASM_USER_THREADS` is defined (ie there is a
`-DENABLE_MINIMAL=wasm-user-threads` option)

Additionally, `__EMSCRIPTEN_THREADS__` is defined by emscripten if threading is enabled.

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

## Debugger tests ##

To run the debugger tests in the runtime [built with enabled support for multi-threading](#building-the-runtime) we use:
```
dotnet test src/mono/wasm/debugger/DebuggerTestSuite -e RuntimeConfiguration=Debug -e Configuration=Debug -e DebuggerHost=chrome -e WasmEnableThreads=true -e WASM_TESTS_USING_VARIANT=multithreaded
```