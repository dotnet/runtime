// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { isThenable } from "./cancelable-promise";
import wrapped_cs_functions from "./corebindings";
import cwraps from "./cwraps";
import { assert_not_disposed, cs_owned_js_handle_symbol, js_owned_gc_handle_symbol, mono_wasm_get_js_handle, setup_managed_proxy, teardown_managed_proxy } from "./gc-handles";
import { Module, runtimeHelpers } from "./imports";
import {
    JSMarshalerArgument, ManagedError,
    set_gc_handle, set_js_handle, set_arg_type, set_arg_i32, set_arg_f64, set_arg_i52, set_arg_f32, set_arg_i16, set_arg_u8, set_arg_b8, set_arg_date,
    set_arg_length, get_arg, is_args_exception, JavaScriptMarshalerArgSize, get_signature_type, get_signature_arg1_type, get_signature_arg2_type, cs_to_js_marshalers, js_to_cs_marshalers,
    MarshalerToCs, MarshalerToJs, get_signature_res_type, JSMarshalerArguments, bound_js_function_symbol, set_arg_u16, JSMarshalerType, array_element_size, get_string_root, Span, ArraySegment, MemoryViewType, get_signature_arg3_type, MarshalerType, set_arg_i64_big, set_arg_intptr, IDisposable, set_arg_element_type, ManagedObject
} from "./marshal";
import { marshal_exception_to_js } from "./marshal-to-js";
import { _zero_region } from "./memory";
import { conv_string, js_string_to_mono_string_root } from "./strings";
import { mono_assert, GCHandle, GCHandleNull } from "./types";
import { TypedArray } from "./types/emscripten";

export function initialize_marshalers_to_cs(): void {
    if (js_to_cs_marshalers.size == 0) {
        js_to_cs_marshalers.set(MarshalerType.Array, _marshal_array_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Span, _marshal_span_to_cs);
        js_to_cs_marshalers.set(MarshalerType.ArraySegment, _marshal_array_segment_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Boolean, _marshal_bool_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Byte, _marshal_byte_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Char, _marshal_char_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int16, _marshal_int16_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int32, _marshal_int32_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Int52, _marshal_int52_to_cs);
        js_to_cs_marshalers.set(MarshalerType.BigInt64, _marshal_bigint64_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Double, _marshal_double_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Single, _marshal_float_to_cs);
        js_to_cs_marshalers.set(MarshalerType.IntPtr, _marshal_intptr_to_cs);
        js_to_cs_marshalers.set(MarshalerType.DateTime, _marshal_date_time_to_cs);
        js_to_cs_marshalers.set(MarshalerType.DateTimeOffset, _marshal_date_time_offset_to_cs);
        js_to_cs_marshalers.set(MarshalerType.String, _marshal_string_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Exception, marshal_exception_to_cs);
        js_to_cs_marshalers.set(MarshalerType.JSException, marshal_exception_to_cs);
        js_to_cs_marshalers.set(MarshalerType.JSObject, _marshal_js_object_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Object, _marshal_cs_object_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Task, _marshal_task_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Action, _marshal_function_to_cs);
        js_to_cs_marshalers.set(MarshalerType.Function, _marshal_function_to_cs);
        js_to_cs_marshalers.set(MarshalerType.None, _marshal_null_to_cs);// also void
        js_to_cs_marshalers.set(MarshalerType.Discard, _marshal_null_to_cs);// also void
        js_to_cs_marshalers.set(MarshalerType.Void, _marshal_null_to_cs);// also void
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function generate_arg_marshal_to_cs(sig: JSMarshalerType, index: number, arg_offset: number, sig_offset: number, jsname: string, closure: any): {
    converters: string,
    call_body: string,
    marshaler_type: MarshalerType
} {
    let converters = "";
    let converter_types = "";
    let call_body = "";
    const converter_name = "converter" + index;
    let converter_name_arg1 = "null";
    let converter_name_arg2 = "null";
    let converter_name_arg3 = "null";
    let converter_name_res = "null";

    let marshaler_type = get_signature_type(sig);
    if (marshaler_type === MarshalerType.None || marshaler_type === MarshalerType.Void) {
        return {
            converters,
            call_body,
            marshaler_type
        };
    }

    const marshaler_type_res = get_signature_res_type(sig);
    if (marshaler_type_res !== MarshalerType.None) {
        const converter = js_to_cs_marshalers.get(marshaler_type_res);
        mono_assert(converter && typeof converter === "function", () => `Unknow converter for type ${marshaler_type_res} at ${index}`);


        if (marshaler_type != MarshalerType.Nullable) {
            converter_name_res = "converter" + index + "_res";
            converters += ", " + converter_name_res;
            converter_types += " " + MarshalerType[marshaler_type_res];
            closure[converter_name_res] = converter;
        }
        else {
            marshaler_type = marshaler_type_res;
        }
    }

    const marshaler_type_arg1 = get_signature_arg1_type(sig);
    if (marshaler_type_arg1 !== MarshalerType.None) {
        const converter = cs_to_js_marshalers.get(marshaler_type_arg1);
        mono_assert(converter && typeof converter === "function", () => `Unknow converter for type ${marshaler_type_arg1} at ${index}`);

        converter_name_arg1 = "converter" + index + "_arg1";
        converters += ", " + converter_name_arg1;
        converter_types += " " + MarshalerType[marshaler_type_arg1];
        closure[converter_name_arg1] = converter;
    }

    const marshaler_type_arg2 = get_signature_arg2_type(sig);
    if (marshaler_type_arg2 !== MarshalerType.None) {
        const converter = cs_to_js_marshalers.get(marshaler_type_arg2);
        mono_assert(converter && typeof converter === "function", () => `Unknow converter for type ${marshaler_type_arg2} at ${index}`);

        converter_name_arg2 = "converter" + index + "_arg2";
        converters += ", " + converter_name_arg2;
        converter_types += " " + MarshalerType[marshaler_type_arg2];
        closure[converter_name_arg2] = converter;
    }

    const marshaler_type_arg3 = get_signature_arg3_type(sig);
    if (marshaler_type_arg3 !== MarshalerType.None) {
        const converter = cs_to_js_marshalers.get(marshaler_type_arg3);
        mono_assert(converter && typeof converter === "function", () => `Unknow converter for type ${marshaler_type_arg3} at ${index}`);

        converter_name_arg3 = "converter" + index + "_arg3";
        converters += ", " + converter_name_arg3;
        converter_types += " " + MarshalerType[marshaler_type_arg3];
        closure[converter_name_arg3] = converter;
    }

    const converter = js_to_cs_marshalers.get(marshaler_type);

    const arg_type_name = MarshalerType[marshaler_type];
    mono_assert(converter && typeof converter === "function", () => `Unknow converter for type ${arg_type_name} (${marshaler_type}) at ${index} `);

    converters += ", " + converter_name;
    converter_types += " " + arg_type_name;
    closure[converter_name] = converter;


    if (marshaler_type == MarshalerType.Task) {
        call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}, ${converter_name_res}); // ${converter_types} \n`;
    } else if (marshaler_type == MarshalerType.Action || marshaler_type == MarshalerType.Function) {
        call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}, ${converter_name_res}, ${converter_name_arg1}, ${converter_name_arg2}, ${converter_name_arg2}); // ${converter_types} \n`;
    } else {
        call_body = `  ${converter_name}(args + ${arg_offset}, ${jsname}, signature + ${sig_offset}); // ${converter_types} \n`;
    }

    return {
        converters,
        call_body,
        marshaler_type
    };
}

function _marshal_bool_to_cs(arg: JSMarshalerArgument, value: any): void {
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

function _marshal_intptr_to_cs(arg: JSMarshalerArgument, value: any): void {
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
        mono_assert(value instanceof Date, "Value is not a Date");
        set_arg_type(arg, MarshalerType.DateTime);
        set_arg_date(arg, value);
    }
}

function _marshal_date_time_offset_to_cs(arg: JSMarshalerArgument, value: Date): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        mono_assert(value instanceof Date, "Value is not a Date");
        set_arg_type(arg, MarshalerType.DateTimeOffset);
        set_arg_date(arg, value);
    }
}

function _marshal_string_to_cs(arg: JSMarshalerArgument, value: string) {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        set_arg_type(arg, MarshalerType.String);
        mono_assert(typeof value === "string", "Value is not a String");
        _marshal_string_to_cs_impl(arg, value);
    }
}

function _marshal_string_to_cs_impl(arg: JSMarshalerArgument, value: string) {
    const root = get_string_root(arg);
    try {
        js_string_to_mono_string_root(value, root);
    }
    finally {
        root.release();
    }
}

function _marshal_null_to_cs(arg: JSMarshalerArgument) {
    set_arg_type(arg, MarshalerType.None);
}

function _marshal_function_to_cs(arg: JSMarshalerArgument, value: Function, _?: JSMarshalerType, res_converter?: MarshalerToCs, arg1_converter?: MarshalerToJs, arg2_converter?: MarshalerToJs, arg3_converter?: MarshalerToJs): void {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
        return;
    }
    mono_assert(value && value instanceof Function, "Value is not a Function");

    // TODO: we could try to cache value -> exising JSHandle
    const marshal_function_to_cs_wrapper: any = (args: JSMarshalerArguments) => {
        const exc = get_arg(args, 0);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        const arg3 = get_arg(args, 4);

        try {
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
            const res_js = value(arg1_js, arg2_js, arg3_js);
            if (res_converter) {
                res_converter(res, res_js);
            }

        } catch (ex) {
            marshal_exception_to_cs(exc, ex);
        }
    };

    marshal_function_to_cs_wrapper[bound_js_function_symbol] = true;
    const bound_function_handle = mono_wasm_get_js_handle(marshal_function_to_cs_wrapper)!;
    set_js_handle(arg, bound_function_handle);
    set_arg_type(arg, MarshalerType.Function);//TODO or action ?
}

export class TaskCallbackHolder implements IDisposable {
    public promise: Promise<any>

    public constructor(promise: Promise<any>) {
        this.promise = promise;
    }

    dispose(): void {
        teardown_managed_proxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (<any>this)[js_owned_gc_handle_symbol] === GCHandleNull;
    }
}

function _marshal_task_to_cs(arg: JSMarshalerArgument, value: Promise<any>, _?: JSMarshalerType, res_converter?: MarshalerToCs) {
    if (value === null || value === undefined) {
        set_arg_type(arg, MarshalerType.None);
        return;
    }
    mono_assert(isThenable(value), "Value is not a Promise");

    const anyModule = Module as any;
    const gc_handle: GCHandle = wrapped_cs_functions._create_task_callback();
    set_gc_handle(arg, gc_handle);
    set_arg_type(arg, MarshalerType.Task);
    const holder = new TaskCallbackHolder(value);
    setup_managed_proxy(holder, gc_handle);

    value.then(data => {
        const sp = anyModule.stackSave();
        try {
            const args = anyModule.stackAlloc(JavaScriptMarshalerArgSize * 3);
            const exc = get_arg(args, 0);
            set_arg_type(exc, MarshalerType.None);
            const res = get_arg(args, 1);
            set_arg_type(res, MarshalerType.None);
            set_gc_handle(res, <any>gc_handle);
            const arg1 = get_arg(args, 2);
            if (!res_converter) {
                _marshal_cs_object_to_cs(arg1, data);
            } else {
                res_converter(arg1, data);
            }
            const fail = cwraps.mono_wasm_invoke_method_bound(runtimeHelpers.complete_task_method, args);
            if (fail) throw new Error("ERR22: Unexpected error: " + conv_string(fail));
            if (is_args_exception(args)) throw marshal_exception_to_js(exc);
        } finally {
            anyModule.stackRestore(sp);
        }
        teardown_managed_proxy(holder, gc_handle); // this holds holder alive for finalizer, until the promise is freed, (holding promise instead would not work)
    }).catch(reason => {
        const sp = anyModule.stackSave();
        try {
            const args = anyModule.stackAlloc(JavaScriptMarshalerArgSize * 3);
            const res = get_arg(args, 1);
            set_arg_type(res, MarshalerType.None);
            set_gc_handle(res, gc_handle);
            const exc = get_arg(args, 0);
            if (typeof reason === "string" || reason === null || reason === undefined) {
                reason = new Error(reason || "");
            }
            marshal_exception_to_cs(exc, reason);
            const fail = cwraps.mono_wasm_invoke_method_bound(runtimeHelpers.complete_task_method, args);
            if (fail) throw new Error("ERR24: Unexpected error: " + conv_string(fail));
            if (is_args_exception(args)) throw marshal_exception_to_js(exc);
        } finally {
            anyModule.stackRestore(sp);
        }
        teardown_managed_proxy(holder, gc_handle); // this holds holder alive for finalizer, until the promise is freed
    });
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
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
        mono_assert(typeof value === "object" || typeof value === "string", () => `Value is not an Error ${typeof value}`);
        set_arg_type(arg, MarshalerType.JSException);
        const message = value.toString();
        _marshal_string_to_cs_impl(arg, message);

        const known_js_handle = value[cs_owned_js_handle_symbol];
        if (known_js_handle) {
            set_js_handle(arg, known_js_handle);
        }
        else {
            const js_handle = mono_wasm_get_js_handle(value)!;
            set_js_handle(arg, js_handle);
        }
    }
}

function _marshal_js_object_to_cs(arg: JSMarshalerArgument, value: any): void {
    if (value === undefined || value === null) {
        set_arg_type(arg, MarshalerType.None);
    }
    else {
        // if value was ManagedObject, it would be double proxied, but the C# signature requires that
        mono_assert(value[js_owned_gc_handle_symbol] === undefined, "JSObject proxy of ManagedObject proxy is not supported");
        mono_assert(typeof value === "function" || typeof value === "object", () => `JSObject proxy of ${typeof value} is not supported`);

        set_arg_type(arg, MarshalerType.JSObject);
        const js_handle = mono_wasm_get_js_handle(value)!;
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
                set_arg_type(arg, MarshalerType.JSException);
                const js_handle = mono_wasm_get_js_handle(value);
                set_js_handle(arg, js_handle);
            }
            else if (value instanceof Uint8Array) {
                _marshal_array_to_cs_impl(arg, value, MarshalerType.Byte);
            }
            else if (value instanceof Float64Array) {
                _marshal_array_to_cs_impl(arg, value, MarshalerType.Double);
            }
            else if (value instanceof Int32Array) {
                _marshal_array_to_cs_impl(arg, value, MarshalerType.Int32);
            }
            else if (Array.isArray(value)) {
                _marshal_array_to_cs_impl(arg, value, MarshalerType.Object);
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
                set_js_handle(arg, js_handle);
            }
            else {
                throw new Error(`JSObject proxy is not supported for ${js_type} ${value}`);
            }
        }
        else {
            assert_not_disposed(value);
            if (value instanceof ArraySegment) {
                throw new Error("NotImplementedException: ArraySegment");
            }
            else if (value instanceof ManagedError) {
                set_arg_type(arg, MarshalerType.Exception);
                set_gc_handle(arg, gc_handle);
            }
            else if (value instanceof ManagedObject) {
                set_arg_type(arg, MarshalerType.Object);
                set_gc_handle(arg, gc_handle);
            } else {
                throw new Error("NotImplementedException " + js_type);
            }
        }
    }
}

function _marshal_array_to_cs(arg: JSMarshalerArgument, value: Array<any> | TypedArray, sig?: JSMarshalerType): void {
    mono_assert(!!sig, "Expected valid sig paramater");
    const element_type = get_signature_arg1_type(sig);
    _marshal_array_to_cs_impl(arg, value, element_type);
}

function _marshal_array_to_cs_impl(arg: JSMarshalerArgument, value: Array<any> | TypedArray, element_type: MarshalerType): void {
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
            mono_assert(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            cwraps.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(<any>buffer_ptr, index);
                _marshal_string_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.Object) {
            mono_assert(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            cwraps.mono_wasm_register_root(buffer_ptr, buffer_length, "marshal_array_to_cs");
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(<any>buffer_ptr, index);
                _marshal_cs_object_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.JSObject) {
            mono_assert(Array.isArray(value), "Value is not an Array");
            _zero_region(buffer_ptr, buffer_length);
            for (let index = 0; index < length; index++) {
                const element_arg = get_arg(buffer_ptr, index);
                _marshal_js_object_to_cs(element_arg, value[index]);
            }
        }
        else if (element_type == MarshalerType.Byte) {
            mono_assert(Array.isArray(value) || value instanceof Uint8Array, "Value is not an Array or Uint8Array");
            const targetView = Module.HEAPU8.subarray(<any>buffer_ptr, buffer_ptr + length);
            targetView.set(value);
        }
        else if (element_type == MarshalerType.Int32) {
            mono_assert(Array.isArray(value) || value instanceof Int32Array, "Value is not an Array or Int32Array");
            const targetView = Module.HEAP32.subarray(<any>buffer_ptr >> 2, (buffer_ptr >> 2) + length);
            targetView.set(value);
        }
        else if (element_type == MarshalerType.Double) {
            mono_assert(Array.isArray(value) || value instanceof Float64Array, "Value is not an Array or Float64Array");
            const targetView = Module.HEAPF64.subarray(<any>buffer_ptr >> 3, (buffer_ptr >> 3) + length);
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

function _marshal_span_to_cs(arg: JSMarshalerArgument, value: Span, sig?: JSMarshalerType): void {
    mono_assert(!!sig, "Expected valid sig paramater");
    mono_assert(!value.isDisposed, "ObjectDisposedException");
    checkViewType(sig, value._viewType);

    set_arg_type(arg, MarshalerType.Span);
    set_arg_intptr(arg, value._pointer);
    set_arg_length(arg, value.length);
}

// this only supports round-trip
function _marshal_array_segment_to_cs(arg: JSMarshalerArgument, value: ArraySegment, sig?: JSMarshalerType): void {
    mono_assert(!!sig, "Expected valid sig paramater");
    const gc_handle = assert_not_disposed(value);
    mono_assert(gc_handle, "Only roundtrip of ArraySegment instance created by C#");
    checkViewType(sig, value._viewType);
    set_arg_type(arg, MarshalerType.ArraySegment);
    set_arg_intptr(arg, value._pointer);
    set_arg_length(arg, value.length);
    set_gc_handle(arg, gc_handle);
}

function checkViewType(sig: JSMarshalerType, viewType: MemoryViewType) {
    const element_type = get_signature_arg1_type(sig);
    if (element_type == MarshalerType.Byte) {
        mono_assert(MemoryViewType.Byte == viewType, "Expected MemoryViewType.Byte");
    }
    else if (element_type == MarshalerType.Int32) {
        mono_assert(MemoryViewType.Int32 == viewType, "Expected MemoryViewType.Int32");
    }
    else if (element_type == MarshalerType.Double) {
        mono_assert(MemoryViewType.Double == viewType, "Expected MemoryViewType.Double");
    }
    else {
        throw new Error(`NotImplementedException ${MarshalerType[element_type]} `);
    }
}

