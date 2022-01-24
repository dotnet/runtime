# Current state of method binding and parameter marshalling
(as of 2022-01-17)

This is not 100% exhaustive. This is not intended as guide to exernal usage, rather as inventory before cleanup.

## Calling from JS to C
- imported functions (WASM)
  - is handle of C method, which could be called from JS with numeric parameters
  - see `EMSCRIPTEN_KEEPALIVE` macro in C code
- `ccall` (emscripten)
  - helper to make call to C method pointer (export)
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
    - allocates parameters on WASM heap, re-uses buffer when possible
    - uses built in type marshallers, according to `args_marshal` signature.
    - boxes primitive types, because `mono_runtime_invoke` accepts array of parameters
  - generates code of the proxy, calling `ccall` to C `mono_runtime_invoke`
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
  - `Interop/Runtime::InvokeJSWithArgs` which is called from C# `JSObject` -> `mono_wasm_invoke_js_with_args`
  - `Interop/Runtime::InvokeJS` -> `mono_wasm_invoke_js`
  - `WebAssembly.JSInterop.InternalCalls::InvokeJS` (Blazor)
    - generics `TRes InvokeJS<T0, T1, T2, TRes>(out string exception, ref JSCallInfo callInfo, T0 arg0, T1 arg1, T2 arg2)`
    - maps to `void* mono_wasm_invoke_js_blazor (MonoString **exceptionMessage, void *callInfo, void* arg0, void* arg1, void* arg2)`
    - -> `invokeJSFromDotNet`
    - `JSCallInfo` [see](https://github.com/dotnet/aspnetcore/blob/main/src/Components/WebAssembly/JSInterop/src/JSCallInfo.cs)
      - or method name and JSon
      - or 3 arguments, they are not marshalled at all
    - `invokeJSFromDotNet` [see](https://github.com/dotnet/aspnetcore/blob/19252d64d9cce0d6a6a424853124ce3dff39675f/src/Components/Web.JS/src/Boot.WebAssembly.ts#L143)

## Pass JS object to C#
- as parameter on `mono_bind_method` or converted delegate call
- as parameters on `Promise.resolve` `_set_tcs_result` via `mono_bind_method`
- `js_to_mono_obj`
  - `number` -> boxed C# int, uint, double
  - `boolean` -> boxed C# bool
  - `string` -> `MonoString*`, possibly interned, via copy by character and  `mono_wasm_string_from_utf16`
  - `Promise` -> `Task<object>`, gc_handle finalization
  - `Date` -> `DateTime`
  - any proxied C# object -> C# object unwrapped
  - JS `object` -> C# `JSObject`
  - JS `Function`, -> C# `JSObject` derived type `Function`
  - JS `Array`, `ArrayBuffer`, `DataView`, `Map`, `SharedArrayBuffer` -> C# `JSObject` derived types
  - JS `Uint8Array` .. `Float64Array` -> C# `TypedArray` derived types
  - each of above does multiple `ccall`s or even calls to C#. Allocation, buffer copy, handle lookup, boxing.
- return value from `mono_add_internal_call`, via manual conversion to `MonoObject*` or `MonoString*`
- return value from `mono_wasm_get_by_index` or `mono_wasm_get_object_property` or `mono_wasm_get_global_object`
- return value from `mono_wasm_invoke_js_with_args` or `mono_wasm_compile_function`
- `mono_wasm_invoke_js` and `mono_wasm_invoke_js_blazor` return exceptions or primitive types
- `_js_to_mono_uri` - create C# `Uri`. Only when `mono_bind_method` is called with `u` in `args_marshal`

## Pass C# object to JS
- as parameter on `mono_wasm_set_object_property` or `mono_wasm_set_by_index`
- return value on methods wrapped with `mono_bind_method`
- `unbox_mono_obj`
  - `MonoString` -> JS `string`, interning
  - `Delegate` -> JS `Function`, gc_handle finalization
  - `Task` -> `Promise`, gc_handle finalization
  - `DateTime` -> JS `Date`, no timezone
  - `DateTimeOffset` -> JS `string`
  - `Uri` -> JS `string`
  - `SafeHandle` which is meant to be `JSObject` proxy -> unwrapped original JS object, array, function
  - each of above does multiple `ccall`s. Buffer copy, handle lookup, unboxing, C# `ToString()`.


# Possible improvements 

## calling from C# to JS
- A) add `InvokeJSFunctionByName` public API with generic parameters
  - minimal draft https://github.com/dotnet/runtime/pull/64062
  - handle return value
    - should return value live of the same buffer ?
  - Introduce `IJSObjectReference` for private `JSObject` ?
  - replace Blazor's use-case with it
  - cover with tests
- B) optimize `InvokeJSFunctionByName`
  - eleminate need for C code
  - split parameter coversion to C# and JS sides. So that we don't cross the C#/JS boundary for each marshalled paramater.
  - dynamic size of the buffer, dynamic length of parameters
  - multiple end to end thunks by number of the parameters
- C) add `[GeneratedJsImportAttribute("globalThis.console.log")]`
  - see https://github.com/dotnet/runtime/blob/main/docs/design/features/source-generator-pinvokes.md
  - generate C# code with specific convertors only
  - generate JS with C# code
    - generate on runtime from pre-generated signature data ? Because unrolled JS code would be too large.
    - for code paths in runtime, generate on dev machine and store in git. Include in rollup+emcc pipeline.
- D) custom coverters for `[GeneratedJsImportAttribute]`
  - register custom coverters
  - custom coverter with receive pointer to the buffer
  - custom coverter could set size of buffer space?
- E) bind methods on JSObject via JS method handle, rather than C# name string 
- F) handle ES6 imports like Blazor
  - `module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./scripts/components/TimeDisplay.js");`

## Possible improvements calling from JS to C#
- G) add `[GeneratedJsExportAttribute("EXPORTS.myMethod")]`
  - it's mirror of `GeneratedJsImportAttribute`
    - it would wrap the C# method with callable thunk
    - it would generate the JS code and expose it as member `EXPORTS` next to `BINDING` object for example.
  - use `stackalloc` in JS to allocate 
  - call JS side of the marshallers
  - call the C# thunk via `mono_runtime_invoke()`
  - call the C# side of marshallers
- H) restrict `BINDING.mono_bind_method` to only bind to methods annotated with `GeneratedJsExportAttribute`
- I) replace `mono_runtime_invoke()` with `mono_marshal_get_thunk_invoke_wrapper()` ?
  - why everything needs to be boxed ?

## Possible improvements for marshallers
- J) drop DateTime marshaller, see what happens
- K) drop Uri and only include it if the generated signature contains it
- L) drop support for Delegate/method
  - so that all methods have known signatures at compile time
  - see that we don't use it internally in runtime
- M) so that we could drop reflection from runtime
  - only if we could drop or restrict `BINDING.mono_bind_method` API

## Does the performance matter ?
- N) measure
  - add perf counters for allocation
  - add perf counters for `ccall`
  - add perf for `JSObject.call` etc
- what is good real life benchmark application
