// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export type { Int32Ptr, TypedArray } from "../types/emscripten";
export {
    GCHandleNull, JSHandleNull, MonoObjectNull, MonoObjectRefNull, VoidPtrNull
} from "../types/internal";
export type {
    BoundMarshalerToCs, BoundMarshalerToJs, GCHandle, JSFnHandle, JSFunctionSignature, JSHandle,
    JSMarshalerArgument, JSMarshalerArguments, JSMarshalerType, MarshalerToCs, MarshalerToJs, MonoMethod,
    MonoObject, MonoObjectRef, MonoString, MonoStringRef, WasmRoot
} from "../types/internal";

export { isThenable, wrap_as_cancelable_promise } from "../core/cancelable-promise";
export { assembly_load } from "../core/class-loader";
export { default as cwraps } from "../core/cwraps";
export {
    _lookup_js_owned_object, assert_not_disposed, cs_owned_js_handle_symbol, js_owned_gc_handle_symbol,
    mono_wasm_get_js_handle, mono_wasm_get_jsobj_from_js_handle, setup_managed_proxy, teardown_managed_proxy
} from "../core/gc-handles";
export { INTERNAL, Module, createPromiseController, loaderHelpers, mono_assert, runtimeHelpers } from "../core/globals";
export { mono_log_debug, mono_log_warn, mono_wasm_symbolicate_string } from "../core/logging";
export {
    _zero_region, getF32, getF64, getI16, getI32, getI64Big, getU16, getU32, getU8, localHeapViewF64, localHeapViewI32, localHeapViewU8, receiveWorkerHeapViews, setF32, setF64, setI16, setI32, setI32_unchecked, setI64Big, setU16, setU32, setU8
} from "../core/memory";
export { MeasuredBlock, endMeasure, startMeasure } from "../core/profiler";
export { mono_wasm_new_external_root, mono_wasm_new_root } from "../core/roots";
export { monoStringToString, stringToMonoStringRoot } from "../core/strings";
export { assert_synchronization_context } from "../pthreads/shared";
export { addUnsettledPromise, settleUnsettledPromise } from "../pthreads/shared/eventloop";
export { assert_bindings, wrap_error_root, wrap_no_error_root } from "./invoke-js";
export { bind_arg_marshal_to_cs, get_marshaler_to_cs_by_type, jsinteropDoc, marshal_exception_to_cs } from "./marshal-to-cs";
export { bind_arg_marshal_to_js, get_marshaler_to_js_by_type, marshal_exception_to_js } from "./marshal-to-js";


