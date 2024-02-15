// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";
import WasmEnableJsInteropByValue from "consts:wasmEnableJsInteropByValue";

import cwraps from "./cwraps";
import { _lookup_js_owned_object, mono_wasm_get_js_handle, mono_wasm_get_jsobj_from_js_handle, mono_wasm_release_cs_owned_object, register_with_jsv_handle, setup_managed_proxy, teardown_managed_proxy } from "./gc-handles";
import { Module, loaderHelpers, mono_assert } from "./globals";
import {
    ManagedObject, ManagedError,
    get_arg_gc_handle, get_arg_js_handle, get_arg_type, get_arg_i32, get_arg_f64, get_arg_i52, get_arg_i16, get_arg_u8, get_arg_f32,
    get_arg_b8, get_arg_date, get_arg_length, get_arg, set_arg_type,
    get_signature_arg2_type, get_signature_arg1_type, cs_to_js_marshalers,
    get_signature_res_type, get_arg_u16, array_element_size, get_string_root,
    ArraySegment, Span, MemoryViewType, get_signature_arg3_type, get_arg_i64_big, get_arg_intptr, get_arg_element_type, JavaScriptMarshalerArgSize, proxy_debug_symbol, set_js_handle
} from "./marshal";
import { monoStringToString, utf16ToString } from "./strings";
import { GCHandleNull, JSMarshalerArgument, JSMarshalerArguments, JSMarshalerType, MarshalerToCs, MarshalerToJs, BoundMarshalerToJs, MarshalerType, JSHandle } from "./types/internal";
import { TypedArray } from "./types/emscripten";
import { get_marshaler_to_cs_by_type, jsinteropDoc, marshal_exception_to_cs } from "./marshal-to-cs";
import { localHeapViewF64, localHeapViewI32, localHeapViewU8 } from "./memory";
import { call_delegate } from "./managed-exports";
import { gc_locked } from "./gc-lock";

export function initialize_marshalers_to_js(): void {
    if (cs_to_js_marshalers.size == 0) {
        cs_to_js_marshalers.set(MarshalerType.Array, _marshal_array_to_js);
        cs_to_js_marshalers.set(MarshalerType.Span, _marshal_span_to_js);
        cs_to_js_marshalers.set(MarshalerType.ArraySegment, _marshal_array_segment_to_js);
        cs_to_js_marshalers.set(MarshalerType.Boolean, _marshal_bool_to_js);
        cs_to_js_marshalers.set(MarshalerType.Byte, _marshal_byte_to_js);
        cs_to_js_marshalers.set(MarshalerType.Char, _marshal_char_to_js);
        cs_to_js_marshalers.set(MarshalerType.Int16, _marshal_int16_to_js);
        cs_to_js_marshalers.set(MarshalerType.Int32, marshal_int32_to_js);
        cs_to_js_marshalers.set(MarshalerType.Int52, _marshal_int52_to_js);
        cs_to_js_marshalers.set(MarshalerType.BigInt64, _marshal_bigint64_to_js);
        cs_to_js_marshalers.set(MarshalerType.Single, _marshal_float_to_js);
        cs_to_js_marshalers.set(MarshalerType.IntPtr, _marshal_intptr_to_js);
        cs_to_js_marshalers.set(MarshalerType.Double, _marshal_double_to_js);
        cs_to_js_marshalers.set(MarshalerType.String, marshal_string_to_js);
        cs_to_js_marshalers.set(MarshalerType.Exception, marshal_exception_to_js);
        cs_to_js_marshalers.set(MarshalerType.JSException, marshal_exception_to_js);
        cs_to_js_marshalers.set(MarshalerType.JSObject, _marshal_js_object_to_js);
        cs_to_js_marshalers.set(MarshalerType.Object, _marshal_cs_object_to_js);
        cs_to_js_marshalers.set(MarshalerType.DateTime, _marshal_datetime_to_js);
        cs_to_js_marshalers.set(MarshalerType.DateTimeOffset, _marshal_datetime_to_js);
        cs_to_js_marshalers.set(MarshalerType.Task, marshal_task_to_js);
        cs_to_js_marshalers.set(MarshalerType.TaskRejected, marshal_task_to_js);
        cs_to_js_marshalers.set(MarshalerType.TaskResolved, marshal_task_to_js);
        cs_to_js_marshalers.set(MarshalerType.TaskPreCreated, begin_marshal_task_to_js);
        cs_to_js_marshalers.set(MarshalerType.Action, _marshal_delegate_to_js);
        cs_to_js_marshalers.set(MarshalerType.Function, _marshal_delegate_to_js);
        cs_to_js_marshalers.set(MarshalerType.None, _marshal_null_to_js);
        cs_to_js_marshalers.set(MarshalerType.Void, _marshal_null_to_js);
        cs_to_js_marshalers.set(MarshalerType.Discard, _marshal_null_to_js);
    }
}

export function bind_arg_marshal_to_js(sig: JSMarshalerType, marshaler_type: MarshalerType, index: number): BoundMarshalerToJs | undefined {
    if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
        return undefined;
    }

    let res_marshaler: MarshalerToJs | undefined = undefined;
    let arg1_marshaler: MarshalerToCs | undefined = undefined;
    let arg2_marshaler: MarshalerToCs | undefined = undefined;
    let arg3_marshaler: MarshalerToCs | undefined = undefined;

    arg1_marshaler = get_marshaler_to_cs_by_type(get_signature_arg1_type(sig));
    arg2_marshaler = get_marshaler_to_cs_by_type(get_signature_arg2_type(sig));
    arg3_marshaler = get_marshaler_to_cs_by_type(get_signature_arg3_type(sig));
    const marshaler_type_res = get_signature_res_type(sig);
    res_marshaler = get_marshaler_to_js_by_type(marshaler_type_res);
    if (marshaler_type === MarshalerType.Nullable) {
        // nullable has nested type information, it's stored in res slot of the signature. The marshaler is the same as for non-nullable primitive type.
        marshaler_type = marshaler_type_res;
    }
    const converter = get_marshaler_to_js_by_type(marshaler_type)!;
    const element_type = get_signature_arg1_type(sig);

    const arg_offset = index * JavaScriptMarshalerArgSize;
    return (args: JSMarshalerArguments) => {
        return converter(<any>args + arg_offset, element_type, res_marshaler, arg1_marshaler, arg2_marshaler, arg3_marshaler);
    };
}

export function get_marshaler_to_js_by_type(marshaler_type: MarshalerType): MarshalerToJs | undefined {
    if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
        return undefined;
    }
    const converter = cs_to_js_marshalers.get(marshaler_type);
    mono_assert(converter && typeof converter === "function", () => `ERR41: Unknown converter for type ${marshaler_type}. ${jsinteropDoc}`);
    return converter;
}

function _marshal_bool_to_js(arg: JSMarshalerArgument): boolean | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_b8(arg);
}

function _marshal_byte_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_u8(arg);
}

function _marshal_char_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_u16(arg);
}

function _marshal_int16_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_i16(arg);
}

export function marshal_int32_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_i32(arg);
}

function _marshal_int52_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_i52(arg);
}

function _marshal_bigint64_to_js(arg: JSMarshalerArgument): bigint | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_i64_big(arg);
}

function _marshal_float_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_f32(arg);
}

function _marshal_double_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_f64(arg);
}

function _marshal_intptr_to_js(arg: JSMarshalerArgument): number | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    return get_arg_intptr(arg);
}

function _marshal_null_to_js(): null {
    return null;
}

function _marshal_datetime_to_js(arg: JSMarshalerArgument): Date | null {
    const type = get_arg_type(arg);
    if (type === MarshalerType.None) {
        return null;
    }
    return get_arg_date(arg);
}

// NOTE: at the moment, this can't dispatch async calls (with Task/Promise return type). Therefore we don't have to worry about pre-created Task.
function _marshal_delegate_to_js(arg: JSMarshalerArgument, _?: MarshalerType, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs): Function | null {
    const type = get_arg_type(arg);
    if (type === MarshalerType.None) {
        return null;
    }

    const gc_handle = get_arg_gc_handle(arg);
    let result = _lookup_js_owned_object(gc_handle);
    if (result === null || result === undefined) {
        // this will create new Function for the C# delegate
        result = (arg1_js: any, arg2_js: any, arg3_js: any): any => {
            mono_assert(!WasmEnableThreads || !result.isDisposed, "Delegate is disposed and should not be invoked anymore.");
            // arg numbers are shifted by one, the real first is a gc handle of the callback
            return call_delegate(gc_handle, arg1_js, arg2_js, arg3_js, res_converter, arg1_converter, arg2_converter, arg3_converter);
        };
        result.dispose = () => {
            if (!result.isDisposed) {
                result.isDisposed = true;
                teardown_managed_proxy(result, gc_handle);
            }
        };
        result.isDisposed = false;
        if (BuildConfiguration === "Debug") {
            (result as any)[proxy_debug_symbol] = `C# Delegate with GCHandle ${gc_handle}`;
        }
        setup_managed_proxy(result, gc_handle);
    }

    return result;
}

export class TaskHolder {
    constructor(public promise: Promise<any>, public resolve_or_reject: (type: MarshalerType, js_handle: JSHandle, argInner: JSMarshalerArgument) => void) {
    }
}

export function marshal_task_to_js(arg: JSMarshalerArgument, _?: MarshalerType, res_converter?: MarshalerToJs): Promise<any> | null {
    const type = get_arg_type(arg);
    // this path is used only when Task is passed as argument to JSImport and virtual JSHandle would be used
    mono_assert(type != MarshalerType.TaskPreCreated, "Unexpected Task type: TaskPreCreated");

    // if there is synchronous result, return it
    const promise = try_marshal_sync_task_to_js(arg, type, res_converter);
    if (promise !== false) {
        return promise;
    }

    const jsv_handle = get_arg_js_handle(arg);
    const holder = create_task_holder(res_converter);
    register_with_jsv_handle(holder, jsv_handle);
    if (BuildConfiguration === "Debug") {
        (holder as any)[proxy_debug_symbol] = `TaskHolder with JSVHandle ${jsv_handle}`;
    }

    return holder.promise;
}

export function begin_marshal_task_to_js(arg: JSMarshalerArgument, _?: MarshalerType, res_converter?: MarshalerToJs): Promise<any> | null {
    // this path is used when Task is returned from JSExport/call_entry_point
    const holder = create_task_holder(res_converter);
    const js_handle = mono_wasm_get_js_handle(holder);
    if (BuildConfiguration === "Debug") {
        (holder as any)[proxy_debug_symbol] = `TaskHolder with JSHandle ${js_handle}`;
    }
    set_js_handle(arg, js_handle);
    set_arg_type(arg, MarshalerType.TaskPreCreated);
    return holder.promise;
}

export function end_marshal_task_to_js(args: JSMarshalerArguments, res_converter: MarshalerToJs | undefined, eagerPromise: Promise<any> | null) {
    // this path is used when Task is returned from JSExport/call_entry_point
    const res = get_arg(args, 1);
    const type = get_arg_type(res);

    // if there is no synchronous result, return eagerPromise we created earlier
    if (type === MarshalerType.TaskPreCreated) {
        return eagerPromise;
    }

    // otherwise drop the eagerPromise's handle
    const js_handle = mono_wasm_get_js_handle(eagerPromise);
    mono_wasm_release_cs_owned_object(js_handle);

    // get the synchronous result
    const promise = try_marshal_sync_task_to_js(res, type, res_converter);

    // make sure we got the result
    mono_assert(promise !== false, () => `Expected synchronous result, got: ${type}`);

    return promise;
}

function try_marshal_sync_task_to_js(arg: JSMarshalerArgument, type: MarshalerType, res_converter?: MarshalerToJs): Promise<any> | null | false {
    if (type === MarshalerType.None) {
        return null;
    }
    if (type === MarshalerType.TaskRejected) {
        return Promise.reject(marshal_exception_to_js(arg));
    }
    if (type === MarshalerType.TaskResolved) {
        const element_type = get_arg_element_type(arg);
        if (element_type === MarshalerType.Void) {
            return Promise.resolve();
        }
        // this will change the type to the actual type of the result
        set_arg_type(arg, element_type);
        if (!res_converter) {
            // when we arrived here from _marshal_cs_object_to_js
            res_converter = cs_to_js_marshalers.get(element_type);
        }
        mono_assert(res_converter, () => `Unknown sub_converter for type ${MarshalerType[element_type]}. ${jsinteropDoc}`);

        const val = res_converter(arg);
        return Promise.resolve(val);
    }
    return false;
}

function create_task_holder(res_converter?: MarshalerToJs) {
    const { promise, promise_control } = loaderHelpers.createPromiseController<any>();
    const holder = new TaskHolder(promise, (type, js_handle, argInner) => {
        if (type === MarshalerType.TaskRejected) {
            const reason = marshal_exception_to_js(argInner);
            promise_control.reject(reason);
        } else if (type === MarshalerType.TaskResolved) {
            const type = get_arg_type(argInner);
            if (type === MarshalerType.Void) {
                promise_control.resolve(undefined);
            } else {
                if (!res_converter) {
                    // when we arrived here from _marshal_cs_object_to_js
                    res_converter = cs_to_js_marshalers.get(type);
                }
                mono_assert(res_converter, () => `Unknown sub_converter for type ${MarshalerType[type]}. ${jsinteropDoc}`);

                const js_value = res_converter!(argInner);
                promise_control.resolve(js_value);
            }
        }
        else {
            mono_assert(false, () => `Unexpected type ${MarshalerType[type]}`);
        }
        mono_wasm_release_cs_owned_object(js_handle);
    });
    return holder;
}

export function mono_wasm_resolve_or_reject_promise(args: JSMarshalerArguments): void {
    const exc = get_arg(args, 0);
    try {
        loaderHelpers.assert_runtime_running();

        const res = get_arg(args, 1);
        const arg_handle = get_arg(args, 2);
        const arg_value = get_arg(args, 3);

        const type = get_arg_type(arg_handle);
        const js_handle = get_arg_js_handle(arg_handle);

        const holder = mono_wasm_get_jsobj_from_js_handle(js_handle) as TaskHolder;
        mono_assert(holder, () => `Cannot find Promise for JSHandle ${js_handle}`);

        holder.resolve_or_reject(type, js_handle, arg_value);
        if (WasmEnableThreads && get_arg_b8(res)) {
            // this works together with AllocHGlobal in JSFunctionBinding.ResolveOrRejectPromise
            Module._free(args as any);
        }
        else {
            set_arg_type(res, MarshalerType.Void);
            set_arg_type(exc, MarshalerType.None);
        }

    } catch (ex: any) {
        if (WasmEnableThreads) {
            mono_assert(false, () => `Failed to resolve or reject promise ${ex}`);
        }
        marshal_exception_to_cs(exc, ex);
    }
}

export function marshal_string_to_js(arg: JSMarshalerArgument): string | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    if (WasmEnableJsInteropByValue) {
        const buffer = get_arg_intptr(arg);
        const len = get_arg_length(arg) * 2;
        const value = utf16ToString(<any>buffer, <any>buffer + len);
        Module._free(buffer as any);
        return value;
    }
    else {
        const root = get_string_root(arg);
        try {
            const value = monoStringToString(root);
            return value;
        } finally {
            root.release();
        }
    }
}

export function marshal_exception_to_js(arg: JSMarshalerArgument): Error | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    if (type == MarshalerType.JSException) {
        // this is JSException roundtrip
        const js_handle = get_arg_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }

    const gc_handle = get_arg_gc_handle(arg);
    let result = _lookup_js_owned_object(gc_handle);
    if (result === null || result === undefined) {
        // this will create new ManagedError
        const message = marshal_string_to_js(arg);
        result = new ManagedError(message!);

        if (BuildConfiguration === "Debug") {
            (result as any)[proxy_debug_symbol] = `C# Exception with GCHandle ${gc_handle}`;
        }
        setup_managed_proxy(result, gc_handle);
    }

    return result;
}

function _marshal_js_object_to_js(arg: JSMarshalerArgument): any {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    const js_handle = get_arg_js_handle(arg);
    const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
    mono_assert(js_obj !== undefined, () => `JS object JSHandle ${js_handle} was not found`);
    return js_obj;
}

function _marshal_cs_object_to_js(arg: JSMarshalerArgument): any {
    const marshaler_type = get_arg_type(arg);
    if (marshaler_type == MarshalerType.None) {
        return null;
    }
    if (marshaler_type == MarshalerType.JSObject) {
        const js_handle = get_arg_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }

    if (marshaler_type == MarshalerType.Array) {
        const element_type = get_arg_element_type(arg);
        return _marshal_array_to_js_impl(arg, element_type);
    }

    if (marshaler_type == MarshalerType.Object) {
        const gc_handle = get_arg_gc_handle(arg);
        if (gc_handle === GCHandleNull) {
            return null;
        }

        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);

        // If the JS object for this gc_handle was already collected (or was never created)
        if (!result) {
            result = new ManagedObject();
            if (BuildConfiguration === "Debug") {
                (result as any)[proxy_debug_symbol] = `C# Object with GCHandle ${gc_handle}`;
            }
            setup_managed_proxy(result, gc_handle);
        }

        return result;
    }

    // other types
    const converter = cs_to_js_marshalers.get(marshaler_type);
    mono_assert(converter, () => `Unknown converter for type ${MarshalerType[marshaler_type]}. ${jsinteropDoc}`);
    return converter(arg);
}

function _marshal_array_to_js(arg: JSMarshalerArgument, element_type?: MarshalerType): Array<any> | TypedArray | null {
    mono_assert(!!element_type, "Expected valid element_type parameter");
    return _marshal_array_to_js_impl(arg, element_type);
}

function _marshal_array_to_js_impl(arg: JSMarshalerArgument, element_type: MarshalerType): Array<any> | TypedArray | null {
    const type = get_arg_type(arg);
    if (type == MarshalerType.None) {
        return null;
    }
    const elementSize = array_element_size(element_type);
    mono_assert(elementSize != -1, () => `Element type ${MarshalerType[element_type]} not supported`);
    const buffer_ptr = get_arg_intptr(arg);
    const length = get_arg_length(arg);
    let result: Array<any> | TypedArray | null = null;
    if (element_type == MarshalerType.String) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const element_arg = get_arg(<any>buffer_ptr, index);
            result[index] = marshal_string_to_js(element_arg);
        }
        if (!WasmEnableJsInteropByValue) {
            mono_assert(!WasmEnableThreads || !gc_locked, "GC must not be locked when disposing a GC root");
            cwraps.mono_wasm_deregister_root(<any>buffer_ptr);
        }
    }
    else if (element_type == MarshalerType.Object) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const element_arg = get_arg(<any>buffer_ptr, index);
            result[index] = _marshal_cs_object_to_js(element_arg);
        }
        if (!WasmEnableJsInteropByValue) {
            mono_assert(!WasmEnableThreads || !gc_locked, "GC must not be locked when disposing a GC root");
            cwraps.mono_wasm_deregister_root(<any>buffer_ptr);
        }
    }
    else if (element_type == MarshalerType.JSObject) {
        result = new Array(length);
        for (let index = 0; index < length; index++) {
            const element_arg = get_arg(<any>buffer_ptr, index);
            result[index] = _marshal_js_object_to_js(element_arg);
        }
    }
    else if (element_type == MarshalerType.Byte) {
        const sourceView = localHeapViewU8().subarray(<any>buffer_ptr, buffer_ptr + length);
        result = sourceView.slice();//copy
    }
    else if (element_type == MarshalerType.Int32) {
        const sourceView = localHeapViewI32().subarray(buffer_ptr >> 2, (buffer_ptr >> 2) + length);
        result = sourceView.slice();//copy
    }
    else if (element_type == MarshalerType.Double) {
        const sourceView = localHeapViewF64().subarray(buffer_ptr >> 3, (buffer_ptr >> 3) + length);
        result = sourceView.slice();//copy
    }
    else {
        throw new Error(`NotImplementedException ${MarshalerType[element_type]}. ${jsinteropDoc}`);
    }
    Module._free(<any>buffer_ptr);
    return result;
}

function _marshal_span_to_js(arg: JSMarshalerArgument, element_type?: MarshalerType): Span {
    mono_assert(!!element_type, "Expected valid element_type parameter");

    const buffer_ptr = get_arg_intptr(arg);
    const length = get_arg_length(arg);
    let result: Span | null = null;
    if (element_type == MarshalerType.Byte) {
        result = new Span(<any>buffer_ptr, length, MemoryViewType.Byte);
    }
    else if (element_type == MarshalerType.Int32) {
        result = new Span(<any>buffer_ptr, length, MemoryViewType.Int32);
    }
    else if (element_type == MarshalerType.Double) {
        result = new Span(<any>buffer_ptr, length, MemoryViewType.Double);
    }
    else {
        throw new Error(`NotImplementedException ${MarshalerType[element_type]}. ${jsinteropDoc}`);
    }
    return result;
}

function _marshal_array_segment_to_js(arg: JSMarshalerArgument, element_type?: MarshalerType): ArraySegment {
    mono_assert(!!element_type, "Expected valid element_type parameter");

    const buffer_ptr = get_arg_intptr(arg);
    const length = get_arg_length(arg);
    let result: ArraySegment | null = null;
    if (element_type == MarshalerType.Byte) {
        result = new ArraySegment(<any>buffer_ptr, length, MemoryViewType.Byte);
    }
    else if (element_type == MarshalerType.Int32) {
        result = new ArraySegment(<any>buffer_ptr, length, MemoryViewType.Int32);
    }
    else if (element_type == MarshalerType.Double) {
        result = new ArraySegment(<any>buffer_ptr, length, MemoryViewType.Double);
    }
    else {
        throw new Error(`NotImplementedException ${MarshalerType[element_type]}. ${jsinteropDoc}`);
    }
    const gc_handle = get_arg_gc_handle(arg);
    if (BuildConfiguration === "Debug") {
        (result as any)[proxy_debug_symbol] = `C# ArraySegment with GCHandle ${gc_handle}`;
    }
    setup_managed_proxy(result, gc_handle);

    return result;
}
