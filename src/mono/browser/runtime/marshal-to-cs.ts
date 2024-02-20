// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";
import WasmEnableJsInteropByValue from "consts:wasmEnableJsInteropByValue";

import { isThenable } from "./cancelable-promise";
import cwraps from "./cwraps";
import { alloc_gcv_handle, assert_not_disposed, cs_owned_js_handle_symbol, js_owned_gc_handle_symbol, mono_wasm_get_js_handle, setup_managed_proxy, teardown_managed_proxy } from "./gc-handles";
import { Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import {
    ManagedError,
    set_gc_handle, set_js_handle, set_arg_type, set_arg_i32, set_arg_f64, set_arg_i52, set_arg_f32, set_arg_i16, set_arg_u8, set_arg_b8, set_arg_date,
    set_arg_length, get_arg, get_signature_arg1_type, get_signature_arg2_type, js_to_cs_marshalers,
    get_signature_res_type, bound_js_function_symbol, set_arg_u16, array_element_size,
    get_string_root, Span, ArraySegment, MemoryViewType, get_signature_arg3_type, set_arg_i64_big, set_arg_intptr,
    set_arg_element_type, ManagedObject, JavaScriptMarshalerArgSize, proxy_debug_symbol, get_arg_gc_handle, get_arg_type
} from "./marshal";
import { get_marshaler_to_js_by_type } from "./marshal-to-js";
import { _zero_region, forceThreadMemoryViewRefresh, localHeapViewF64, localHeapViewI32, localHeapViewU8 } from "./memory";
import { stringToMonoStringRoot, stringToUTF16 } from "./strings";
import { JSMarshalerArgument, JSMarshalerArguments, JSMarshalerType, MarshalerToCs, MarshalerToJs, BoundMarshalerToCs, MarshalerType } from "./types/internal";
import { TypedArray } from "./types/emscripten";
import { addUnsettledPromise, settleUnsettledPromise } from "./pthreads";
import { mono_log_debug } from "./logging";
import { complete_task } from "./managed-exports";
import { gc_locked } from "./gc-lock";

export const jsinteropDoc = "For more information see https://aka.ms/dotnet-wasm-jsinterop";

export function initialize_marshalers_to_cs(): void {
    if (js_to_cs_marshalers.size == 0) {
        js_to_cs_marshalers.set(MarshalerType.Array, marshal_array_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Span, _marshal_span_to_cs);
        js_to_cs_marshalers.set(MarshalerType.ArraySegment, _marshal_array_segment_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Boolean, marshal_bool_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Byte, _marshal_byte_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Char, _marshal_char_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int16, _marshal_int16_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int32, _marshal_int32_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int52, _marshal_int52_to_cs);
        js_to_cs_marshalers.set(MarshalerType.BigInt64, _marshal_bigint64_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Double, _marshal_double_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Single, _marshal_float_to_cs);
        js_to_cs_marshalers.set(MarshalerType.IntPtr, marshal_intptr_to_cs);
        js_to_cs_marshalers.set(MarshalerType.DateTime, _marshal_date_time_to_cs);
        js_to_cs_marshalers.set(MarshalerType.DateTimeOffset, _marshal_date_time_offset_to_cs);
        js_to_cs_marshalers.set(MarshalerType.String, marshal_string_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Exception, marshal_exception_to_cs);
        js_to_cs_marshalers.set(MarshalerType.JSException, marshal_exception_to_cs);
        js_to_cs_marshalers.set(MarshalerType.JSObject, marshal_js_object_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Object, _marshal_cs_object_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Task, _marshal_task_to_cs);
        js_to_cs_marshalers.set(MarshalerType.TaskResolved, _marshal_task_to_cs);
        js_to_cs_marshalers.set(MarshalerType.TaskRejected, _marshal_task_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Action, _marshal_function_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Function, _marshal_function_to_cs);
        js_to_cs_marshalers.set(MarshalerType.None, _marshal_null_to_cs);// also void
        js_to_cs_marshalers.set(MarshalerType.Discard, _marshal_null_to_cs);// also void
        js_to_cs_marshalers.set(MarshalerType.Void, _marshal_null_to_cs);// also void
        js_to_cs_marshalers.set(MarshalerType.DiscardNoWait, _marshal_null_to_cs);// also void
    }
}

export function bind_arg_marshal_to_cs(sig: JSMarshalerType, marshaler_type: MarshalerType, index: number): BoundMarshalerToCs | undefined {
    if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void || marshaler_type === MarshalerType.Discard || marshaler_type === MarshalerType.DiscardNoWait) {
        return undefined;
    }
    let res_marshaler: MarshalerToCs | undefined = undefined;
    let arg1_marshaler: MarshalerToJs | undefined = undefined;
    let arg2_marshaler: MarshalerToJs | undefined = undefined;
    let arg3_marshaler: MarshalerToJs | undefined = undefined;

    arg1_marshaler = get_marshaler_to_js_by_type(get_signature_arg1_type(sig));
    arg2_marshaler = get_marshaler_to_js_by_type(get_signature_arg2_type(sig));
    arg3_marshaler = get_marshaler_to_js_by_type(get_signature_arg3_type(sig));
    const marshaler_type_res = get_signature_res_type(sig);
    res_marshaler = get_marshaler_to_cs_by_type(marshaler_type_res);
    if (marshaler_type === MarshalerType.Nullable) {
        // nullable has nested type information, it's stored in res slot of the signature. The marshaler is the same as for non-nullable primitive type.
        marshaler_type = marshaler_type_res;
    }
    const converter = get_marshaler_to_cs_by_type(marshaler_type)!;
    const element_type = get_signature_arg1_type(sig);

    const arg_offset = index * JavaScriptMarshalerArgSize;
    return (args: JSMarshalerArguments, value: any) => {
        converter(<any>args + arg_offset, value, element_type, res_marshaler, arg1_marshaler, arg2_marshaler, arg3_marshaler);
    };
}

export function get_marshaler_to_cs_by_type(marshaler_type: MarshalerType): MarshalerToCs | undefined {
    if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
        return undefined;
    }
    const converter = js_to_cs_marshalers.get(marshaler_type);
    mono_assert(converter && typeof converter === "function", () => `ERR30: Unknown converter for type ${marshaler_type}`);
    return converter;
}

export function marshal_bool_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Boolean);
        set_arg_b8(arg, value);
    }
}

function _marshal_byte_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Byte);
        set_arg_u8(arg, value);
    }
}

function _marshal_char_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Char);
        set_arg_u16(arg, value);
    }
}

function _marshal_int16_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Int16);
        set_arg_i16(arg, value);
    }
}

function _marshal_int32_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Int32);
        set_arg_i32(arg, value);
    }
}

function _marshal_int52_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Int52);
        set_arg_i52(arg, value);
    }
}

function _marshal_bigint64_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.BigInt64);
        set_arg_i64_big(arg, value);
    }
}

function _marshal_double_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Double);
        set_arg_f64(arg, value);
    }
}

function _marshal_float_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.Single);
        set_arg_f32(arg, value);
    }
}

export function marshal_intptr_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.IntPtr);
        set_arg_intptr(arg, value);
    }
}

function _marshal_date_time_to_cs(arg: JSMarshalerArgument, value: Date): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        mono_check(value instanceof Date, "Value is not a Date");
        set_arg_type(arg, MarshalerType.DateTime);
        set_arg_date(arg, value);
    }
}

function _marshal_date_time_offset_to_cs(arg: JSMarshalerArgument, value: Date): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        mono_check(value instanceof Date, "Value is not a Date");
        set_arg_type(arg, MarshalerType.DateTimeOffset);
        set_arg_date(arg, value);
    }
}

export function marshal_string_to_cs(arg: JSMarshalerArgument, value: string) {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.String);
        mono_check(typeof value === "string", "Value is not a String");
        _marshal_string_to_cs_impl(arg, value);
    }
}

function _marshal_string_to_cs_impl(arg: JSMarshalerArgument, value: string) {
    if (WasmEnableJsInteropByValue) {
        const bufferLen = value.length * 2;
        const buffer = Module._malloc(bufferLen);// together with Marshal.FreeHGlobal
        stringToUTF16(buffer as any, buffer as any + bufferLen, value);
        set_arg_intptr(arg, buffer);
        set_arg_length(arg, value.length);
    } else {
        const root = get_string_root(arg);
        try {
            stringToMonoStringRoot(value, root);
        }
        finally {
            root.release();
        }
    }
}

function _marshal_null_to_cs(arg: JSMarshalerArgument) {
    set_arg_type(arg, MarshalerType.None);
}

function _marshal_function_to_cs(arg: JSMarshalerArgument, value: Function, _?: MarshalerType, res_converter?: MarshalerToCs, arg1_converter?: MarshalerToJs, arg2_converter?: MarshalerToJs, arg3_converter?: MarshalerToJs): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
        return;
    }
    mono_check(value && value instanceof Function, "Value is not a Function");

    // TODO: we could try to cache value -> existing JSHandle
    const wrapper: any = function delegate_wrapper(args: JSMarshalerArguments) {
        const exc = get_arg(args, 0);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        const arg3 = get_arg(args, 4);

        const previousPendingSynchronousCall = runtimeHelpers.isPendingSynchronousCall;
        try {
            mono_assert(!WasmEnableThreads || !wrapper.isDisposed, "Function is disposed and should not be invoked anymore.");

            let arg1_js: any = undefined;
            let arg2_js: any = undefined;
            let arg3_js: any = undefined;
            if (arg1_converter) {
                arg1_js = arg1_converter(arg1);
            }
            if (arg2_converter) {
                arg2_js = arg2_converter(arg2);
            }
            if (arg3_converter) {
                arg3_js = arg3_converter(arg3);
            }
            runtimeHelpers.isPendingSynchronousCall = true; // this is alway synchronous call for now
            const res_js = value(arg1_js, arg2_js, arg3_js);
            if (res_converter) {
                res_converter(res, res_js);
            }

        } catch (ex) {
            marshal_exception_to_cs(exc, ex);
        } finally {
            runtimeHelpers.isPendingSynchronousCall = previousPendingSynchronousCall;
        }
    };

    wrapper[bound_js_function_symbol] = true;
    wrapper.isDisposed = false;
    wrapper.dispose = () => { wrapper.isDisposed = true; };
    const bound_function_handle = mono_wasm_get_js_handle(wrapper)!;
    if (BuildConfiguration === "Debug") {
        wrapper[proxy_debug_symbol] = `Proxy of JS Function with JSHandle ${bound_function_handle}: ${value.toString()}`;
    }
    set_js_handle(arg, bound_function_handle);
    set_arg_type(arg, MarshalerType.Function);//TODO or action ?
}

export class PromiseHolder extends ManagedObject {
    public isResolved = false;
    public isCanceled = false;
    public constructor(public promise: Promise<any>) {
        super();
    }
}

function _marshal_task_to_cs(arg: JSMarshalerArgument, value: Promise<any>, _?: MarshalerType, res_converter?: MarshalerToCs) {
    const handleIsPreallocated = get_arg_type(arg) == MarshalerType.TaskPreCreated;
    if (value === null || value === undefined) {
        if (WasmEnableThreads && handleIsPreallocated) {
            // This is multi-threading return from JSImport with Task result and we can't return synchronously,
            // because C# caller could be on different thread and sent us an async message.
            // It already returned pending Task to it's own caller.
            const err = new Error("InvalidOperationException: Task return with null value is not supported in multi-threading scenario.");
            // Alternatively we can return promise and resolve it with null/default value.
            value = Promise.reject(err);
        } else {
            set_arg_type(arg, MarshalerType.None);
            return;
        }
    }
    mono_check(isThenable(value), "Value is not a Promise");

    const gc_handle = handleIsPreallocated ? get_arg_gc_handle(arg) : alloc_gcv_handle();
    if (!handleIsPreallocated) {
        set_gc_handle(arg, gc_handle);
        set_arg_type(arg, MarshalerType.Task);
    }
    const holder = new PromiseHolder(value);
    setup_managed_proxy(holder, gc_handle);
    if (BuildConfiguration === "Debug") {
        (holder as any)[proxy_debug_symbol] = `PromiseHolder with GCHandle ${gc_handle}`;
    }

    if (WasmEnableThreads)
        addUnsettledPromise();

    function resolve(data: any) {
        if (!loaderHelpers.is_runtime_running()) {
            mono_log_debug("This promise can't be propagated to managed code, mono runtime already exited.");
            return;
        }
        try {
            mono_assert(!holder.isDisposed, "This promise can't be propagated to managed code, because the Task was already freed.");
            mono_assert(!holder.isResolved, "This promise already resolved.");
            mono_assert(!holder.isCanceled, "This promise already canceled.");
            holder.isResolved = true;
            if (WasmEnableThreads) {
                forceThreadMemoryViewRefresh();
                settleUnsettledPromise();
            }
            // we can unregister the GC handle just on JS side
            teardown_managed_proxy(holder, gc_handle, /*skipManaged: */ true);
            // order of operations with teardown_managed_proxy matters
            // so that managed user code running in the continuation could allocate the same GCHandle number and the local registry would be already ok with that
            complete_task(gc_handle, false, null, data, res_converter || _marshal_cs_object_to_cs);
        }
        catch (ex) {
            try {
                loaderHelpers.mono_exit(1, ex);
            }
            catch (ex2) {
                // there is no point to propagate the exception into the unhandled promise rejection
            }
        }
    }

    function reject(reason: any) {
        if (!loaderHelpers.is_runtime_running()) {
            mono_log_debug("This promise can't be propagated to managed code, mono runtime already exited.", reason);
            return;
        }
        try {
            mono_assert(!holder.isDisposed, "This promise can't be propagated to managed code, because the Task was already freed.");
            mono_assert(!holder.isResolved, "This promise already resolved.");
            holder.isResolved = true;
            if (WasmEnableThreads) {
                forceThreadMemoryViewRefresh();
                settleUnsettledPromise();
            }
            // we can unregister the GC handle just on JS side
            teardown_managed_proxy(holder, gc_handle, /*skipManaged: */ true);
            // order of operations with teardown_managed_proxy matters
            complete_task(gc_handle, holder.isCanceled, reason, null, undefined);
        }
        catch (ex) {
            try {
                loaderHelpers.mono_exit(1, ex);
            }
            catch (ex2) {
                // there is no point to propagate the exception into the unhandled promise rejection
            }
        }
    }

    value.then(resolve).catch(reject);
}

export function marshal_exception_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else if (value instanceof ManagedError) {
        set_arg_type(arg, MarshalerType.Exception);
        // this is managed exception round-trip
        const gc_handle = assert_not_disposed(value);
        set_gc_handle(arg, gc_handle);
    }
    else {
        mono_check(typeof value === "object" || typeof value === "string", () => `Value is not an Error ${typeof value}`);
        set_arg_type(arg, MarshalerType.JSException);
        const message = value.toString();
        _marshal_string_to_cs_impl(arg, message);
        const known_js_handle = value[cs_owned_js_handle_symbol];
        if (known_js_handle) {
            set_js_handle(arg, known_js_handle);
        }
        else {
            const js_handle = mono_wasm_get_js_handle(value)!;
            if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
                value[proxy_debug_symbol] = `JS Error with JSHandle ${js_handle}`;
            }
            set_js_handle(arg, js_handle);
        }
    }
}

export function marshal_js_object_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === undefined || value === null) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        // if value was ManagedObject, it would be double proxied, but the C# signature requires that
        mono_check(value[js_owned_gc_handle_symbol] === undefined, () => `JSObject proxy of ManagedObject proxy is not supported. ${jsinteropDoc}`);
        mono_check(typeof value === "function" || typeof value === "object", () => `JSObject proxy of ${typeof value} is not supported`);

        set_arg_type(arg, MarshalerType.JSObject);
        const js_handle = mono_wasm_get_js_handle(value)!;
        if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
            value[proxy_debug_symbol] = `JS Object with JSHandle ${js_handle}`;
        }
        set_js_handle(arg, js_handle);
    }
}

function _marshal_cs_object_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === undefined || value === null) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        const gc_handle = value[js_owned_gc_handle_symbol];
        const js_type = typeof (value);
        if (gc_handle === undefined) {
            if (js_type === "string" || js_type === "symbol") {
                set_arg_type(arg, MarshalerType.String);
                _marshal_string_to_cs_impl(arg, value);
            }
            else if (js_type === "number") {
                set_arg_type(arg, MarshalerType.Double);
                set_arg_f64(arg, value);
            }
            else if (js_type === "bigint") {
                // we do it because not all bigint values could fit into Int64
                throw new Error("NotImplementedException: bigint");
            }
            else if (js_type === "boolean") {
                set_arg_type(arg, MarshalerType.Boolean);
                set_arg_b8(arg, value);
            }
            else if (value instanceof Date) {
                set_arg_type(arg, MarshalerType.DateTime);
                set_arg_date(arg, value);
            }
            else if (value instanceof Error) {
                marshal_exception_to_cs(arg, value);
            }
            else if (value instanceof Uint8Array) {
                marshal_array_to_cs_impl(arg, value, MarshalerType.Byte);
            }
            else if (value instanceof Float64Array) {
                marshal_array_to_cs_impl(arg, value, MarshalerType.Double);
            }
            else if (value instanceof Int32Array) {
                marshal_array_to_cs_impl(arg, value, MarshalerType.Int32);
            }
            else if (Array.isArray(value)) {
                marshal_array_to_cs_impl(arg, value, MarshalerType.Object);
            }
            else if (value instanceof Int16Array
                || value instanceof Int8Array
                || value instanceof Uint8ClampedArray
                || value instanceof Uint16Array
                || value instanceof Uint32Array
                || value instanceof Float32Array
            ) {
                throw new Error("NotImplementedException: TypedArray");
            }
            else if (isThenable(value)) {
                _marshal_task_to_cs(arg, value);
            }
            else if (value instanceof Span) {
                throw new Error("NotImplementedException: Span");
            }
            else if (js_type == "object") {
                const js_handle = mono_wasm_get_js_handle(value);
                set_arg_type(arg, MarshalerType.JSObject);
                if (BuildConfiguration === "Debug" && Object.isExtensible(value)) {
                    value[proxy_debug_symbol] = `JS Object with JSHandle ${js_handle}`;
                }
                set_js_handle(arg, js_handle);
            }
            else {
                throw new Error(`JSObject proxy is not supported for ${js_type} ${value}`);
            }
        }
        else {
            assert_not_disposed(value);
            if (value instanceof ArraySegment) {
                throw new Error("NotImplementedException: ArraySegment. " + jsinteropDoc);
            }
            else if (value instanceof ManagedError) {
                set_arg_type(arg, MarshalerType.Exception);
                set_gc_handle(arg, gc_handle);
            }
            else if (value instanceof ManagedObject) {
                set_arg_type(arg, MarshalerType.Object);
                set_gc_handle(arg, gc_handle);
            } else {
                throw new Error("NotImplementedException " + js_type + ". " + jsinteropDoc);
            }
        }
    }
}

export function marshal_array_to_cs(arg: JSMarshalerArgument, value: Array<any> | TypedArray | undefined | null, element_type?: MarshalerType): void {
    mono_assert(!!element_type, "Expected valid element_type parameter");
    marshal_array_to_cs_impl(arg, value, element_type);
}

export function marshal_array_to_cs_impl(arg: JSMarshalerArgument, value: Array<any> | TypedArray | undefined | null, element_type: MarshalerType): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        const element_size = array_element_size(element_type);
        mono_assert(element_size != -1, () => `Element type ${MarshalerType[element_type]} not supported`);
        const length = value.length;
        const buffer_length = element_size * length;
        const buffer_ptr = <any>Module._malloc(buffer_length);
        if (element_type == MarshalerType.String) {
            mono_check(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            if (!WasmEnableJsInteropByValue) {
                mono_assert(!WasmEnableThreads || !gc_locked, "GC must not be locked when creating a GC root");
                cwraps.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
            }
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(<any>buffer_ptr, index);
                marshal_string_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.Object) {
            mono_check(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            if (!WasmEnableJsInteropByValue) {
                mono_assert(!WasmEnableThreads || !gc_locked, "GC must not be locked when creating a GC root");
                cwraps.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
            }
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(<any>buffer_ptr, index);
                _marshal_cs_object_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.JSObject) {
            mono_check(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(buffer_ptr, index);
                marshal_js_object_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.Byte) {
            mono_check(Array.isArray(value) || value instanceof Uint8Array, "Value is not an Array or Uint8Array");
            const targetView = localHeapViewU8().subarray(<any>buffer_ptr, buffer_ptr + length);
            targetView.set(value);
        }
        else if (element_type == MarshalerType.Int32) {
            mono_check(Array.isArray(value) || value instanceof Int32Array, "Value is not an Array or Int32Array");
            const targetView = localHeapViewI32().subarray(<any>buffer_ptr >> 2, (buffer_ptr >> 2) + length);
            targetView.set(value);
        }
        else if (element_type == MarshalerType.Double) {
            mono_check(Array.isArray(value) || value instanceof Float64Array, "Value is not an Array or Float64Array");
            const targetView = localHeapViewF64().subarray(<any>buffer_ptr >> 3, (buffer_ptr >> 3) + length);
            targetView.set(value);
        }
        else {
            throw new Error("not implemented");
        }
        set_arg_intptr(arg, buffer_ptr);
        set_arg_type(arg, MarshalerType.Array);
        set_arg_element_type(arg, element_type);
        set_arg_length(arg, value.length);
    }
}

function _marshal_span_to_cs(arg: JSMarshalerArgument, value: Span, element_type?: MarshalerType): void {
    mono_assert(!!element_type, "Expected valid element_type parameter");
    mono_check(!value.isDisposed, "ObjectDisposedException");
    checkViewType(element_type, value._viewType);

    set_arg_type(arg, MarshalerType.Span);
    set_arg_intptr(arg, value._pointer);
    set_arg_length(arg, value.length);
}

// this only supports round-trip
function _marshal_array_segment_to_cs(arg: JSMarshalerArgument, value: ArraySegment, element_type?: MarshalerType): void {
    mono_assert(!!element_type, "Expected valid element_type parameter");
    const gc_handle = assert_not_disposed(value);
    mono_assert(gc_handle, "Only roundtrip of ArraySegment instance created by C#");
    checkViewType(element_type, value._viewType);
    set_arg_type(arg, MarshalerType.ArraySegment);
    set_arg_intptr(arg, value._pointer);
    set_arg_length(arg, value.length);
    set_gc_handle(arg, gc_handle);
}

function checkViewType(element_type: MarshalerType, viewType: MemoryViewType) {
    if (element_type == MarshalerType.Byte) {
        mono_check(MemoryViewType.Byte == viewType, "Expected MemoryViewType.Byte");
    }
    else if (element_type == MarshalerType.Int32) {
        mono_check(MemoryViewType.Int32 == viewType, "Expected MemoryViewType.Int32");
    }
    else if (element_type == MarshalerType.Double) {
        mono_check(MemoryViewType.Double == viewType, "Expected MemoryViewType.Double");
    }
    else {
        throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
    }
}

