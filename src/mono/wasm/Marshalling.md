# Currrent state of marshalling

## Calling from JS to C
- exported functions (WASM)
  - is handle of C method, which could be called from JS with numeric parameters
- `ccall` (emscripten)
  - helper to make call to C method pointer
  - we use it for all calls from JS
  - converts JS strings to utf8 (char*)
  - copy byte array to WASM memory
  - converts return to boolean
  - allocates parameters on WASM stack!
  - we use it to implement `mono_wasm_invoke_method` and `mono_wasm_get_delegate_invoke`
- `cwrap` (emscripten)
  - create JS proxy to C method
  - are emscripten javascript proxies for calling existing C function
  - they use `ccall`
  - used in `cwraps.ts`

## Calling from JS to C#
- `mono_wasm_invoke_method` (JS runtime)
  - is JS proxy of C `mono_runtime_invoke`, via `cwrap` of `ccall`
  - which is way how to call C# from C
- `call_method` (JS runtime)
  - is way how to call C# method from JS
  - generates parameter converters and uses them
  - calls to C# via `mono_wasm_invoke_method`, via `ccall` to C `mono_runtime_invoke`
- `mono_bind_method` (JS runtime)
  - generates JS proxy for C# method
  - generates parameter converters
    - allocates parameters on WASM heap, C# or primitive types
    - uses type marshallers
  - generates code of the proxy
  - used also in `corebindings.ts` for C# 
- `mono_wasm_get_delegate_invoke` (JS runtime)
  - converts C# delegate to JS function
  - it `mono_method_get_call_signature` once
  - uses `call_method` via `ccall` to C `mono_runtime_invoke`
- `DotNet.invokeMethod` (Blazor)
  - `invokePossibleInstanceMethod` -> `invokeDotNetFromJS` - > `bindStaticMethod` -> (JS runtime) `mono_bind_method`
  - could be any C# signature, but not all types are easy to instantiate on JS side

## Calling from C to JS
- `extern` imported functions (WASM)
  - see `linked_functions` in dotnet.es6.lib.js

## Calling from C# to C
- `mono_add_internal_call` - ical for short
  - maps C# InternalCall to C method
  - `[MethodImpl(MethodImplOptions.InternalCall)]` attribute on C# method
  - there is no marshalling, `System.String` are exposed as `MonoString*`
  - `Mono.Profiler.Log.LogProfiler::`
  - `System.Threading.TimerQueue::` and `System.Threading.ThreadPool::`
  - `System.Diagnostics.StackFrame::`, `System.Diagnostics.StackTrace::`, `Mono.Runtime::mono_runtime_install_handlers`

## Calling from C# to JS
- `mono_add_internal_call` via `extern` to JS
  - `Interop/Runtime::*` -> see `corebindings.c`
    - `InvokeJSWithArgs` which is called from C# `JSObject`
  - `Interop/Runtime::InvokeJS` -> `mono_wasm_invoke_js`

  - `WebAssembly.JSInterop.InternalCalls::InvokeJS` (Blazor)
    - generics `TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, [AllowNull] T0 arg0, [AllowNull] T1 arg1, [AllowNull] T2 arg2)`
    - maps to `void* mono_wasm_invoke_js_blazor (MonoString **exceptionMessage, void *callInfo, void* arg0, void* arg1, void* arg2)`
    - -> `invokeJSFromDotNet`
    - `JSCallInfo` [see](https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebAssembly/JSInterop/src/JSCallInfo.cs)
      - or method name and JSon
      - or 3 arguments, they are not marshalled at all
    - `invokeJSFromDotNet` [see](https://github.com/dotnet/aspnetcore/blob/19252d64d9cce0d6a6a424853124ce3dff39675f/src/Components/Web.JS/src/Boot.WebAssembly.ts#L143)

## Marshalled objects types
- `js_to_mono_obj`
- `unbox_mono_obj`