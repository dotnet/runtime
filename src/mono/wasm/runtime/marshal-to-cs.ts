import { cs_owned_js_handle_symbol, js_owned_gc_handle_symbol, mono_wasm_get_js_handle, mono_wasm_release_cs_owned_object } from "./gc-handles";
import { INTERNAL } from "./imports";
import {
    JavaScriptMarshalerArg, ManagedError,
    set_gc_handle, set_js_handle, set_root_ref, set_arg_type, set_arg_i32, set_arg_f64, set_arg_i64, set_arg_f32, set_arg_i16, set_arg_u8, set_arg_b8, set_arg_date
} from "./marshal";
import { knownTypes } from "./startup";
import { js_string_to_mono_string } from "./strings";
import { MonoType, MonoTypeNull, assert } from "./types";

export const js_to_cs_marshalers = new Map<MonoType, MarshalerToCs>();

export type MarshalerToCs = (arg: JavaScriptMarshalerArg, value: any) => void;

function _marshal_bool_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.bool);
    set_arg_b8(arg, value);
}

function _marshal_byte_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.byte);
    set_arg_u8(arg, value);
}

function _marshal_int16_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.int16);
    set_arg_i16(arg, value);
}

function _marshal_int32_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.int32);
    set_arg_i32(arg, value);
}

function _marshal_int64_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.int64);
    set_arg_i64(arg, value);
}

function _marshal_double_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.double);
    set_arg_f64(arg, value);
}

function _marshal_float_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.float);
    set_arg_f32(arg, value);
}

function _marshal_intptr_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    set_arg_type(arg, knownTypes.intptr);
    set_arg_i32(arg, value);
}

function _marshal_date_time_to_cs(arg: JavaScriptMarshalerArg, value: Date): void {
    set_arg_type(arg, knownTypes.date_time);
    set_arg_date(arg, value);
}

function _marshal_date_time_offset_to_cs(arg: JavaScriptMarshalerArg, value: Date): void {
    set_arg_type(arg, knownTypes.date_time_offset);
    set_arg_date(arg, value);
}

function _marshal_string_to_cs(arg: JavaScriptMarshalerArg, value: string) {
    if (!value) {
        set_arg_type(arg, MonoTypeNull);
    }
    else {
        const pStr = js_string_to_mono_string(value)!;
        set_root_ref(arg, pStr);
        set_arg_type(arg, knownTypes.string);
    }
}

function _marshal_task_to_cs(arg: JavaScriptMarshalerArg, value: Promise<any>) {
    if (!value) {
        set_arg_type(arg, MonoTypeNull);
        return;
    }
    /*const gc_handle = <GCHandle>((<any>value)[js_owned_gc_handle_symbol]);
    if (gc_handle) {
        // this is Task round trip
        set_arg_type(arg, knownTypes.task);
        set_gc_handle(arg, gc_handle);
        return;
    }*/
    const js_handle = mono_wasm_get_js_handle(value)!;
    set_js_handle(arg, js_handle);
    set_arg_type(arg, knownTypes.ijs_object);
    value.then(data => {
        INTERNAL.mono_wasm_resolve_tcs(js_handle, data);
        mono_wasm_release_cs_owned_object(js_handle);
    }).catch(reason => {
        INTERNAL.mono_wasm_reject_tcs(js_handle, reason);
        mono_wasm_release_cs_owned_object(js_handle);
    });
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function marshal_exception_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    if (!value) {
        set_arg_type(arg, MonoTypeNull);
    }
    else if (value instanceof ManagedError) {
        set_arg_type(arg, knownTypes.exception);
        // this is managed exception round-trip
        const gc_handle = (<any>value)[js_owned_gc_handle_symbol];
        set_gc_handle(arg, gc_handle);
    }
    else {
        set_arg_type(arg, knownTypes.jsexception);
        const message = value.toString();
        const pMessage = js_string_to_mono_string(message);
        set_root_ref(arg, pMessage);

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

function _marshal_js_object_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    if (value === undefined || value === null) {
        set_arg_type(arg, MonoTypeNull);
    }
    else {
        // if value was ManagedObject, it would be double proxied, but the C# signature requires that
        assert(!value[js_owned_gc_handle_symbol], "JSObject proxy of ManagedObject proxy is not supported");

        const js_handle = mono_wasm_get_js_handle(value)!;
        set_js_handle(arg, js_handle);
    }
}

function _marshal_cs_object_to_cs(arg: JavaScriptMarshalerArg, value: any): void {
    if (value === undefined || value === null) {
        set_arg_type(arg, MonoTypeNull);
    }
    else {
        const gc_handle = value[js_owned_gc_handle_symbol];
        if (!gc_handle) {
            const js_type = typeof (value);
            if (js_type === "string") {
                set_arg_type(arg, knownTypes.string);
                const pStr = js_string_to_mono_string(value);
                set_root_ref(arg, pStr);
            }
            else if (js_type === "number") {
                set_arg_type(arg, knownTypes.double);
                set_arg_f64(arg, value);
            }
            else if (js_type === "boolean") {
                set_arg_type(arg, knownTypes.bool);
                set_arg_b8(arg, value);
            }
            else if (value instanceof Date) {
                set_arg_type(arg, knownTypes.date_time);
                set_arg_date(arg, value);
            }
            else {
                assert(js_type == "object", () => `JSObject proxy is not supported for ${js_type} ${value}`);
                const js_handle = mono_wasm_get_js_handle(value);
                set_arg_type(arg, knownTypes.ijs_object);
                set_js_handle(arg, js_handle);
            }
        }
        else {
            set_arg_type(arg, knownTypes.cs_object);
            set_gc_handle(arg, gc_handle);
        }
    }
}

export function initialize_marshalers_to_cs(): void {
    if (js_to_cs_marshalers.size == 0) {
        //console.log(JSON.stringify(knownTypes, null, 2));
        js_to_cs_marshalers.set(knownTypes.bool, _marshal_bool_to_cs);
        js_to_cs_marshalers.set(knownTypes.byte, _marshal_byte_to_cs);
        js_to_cs_marshalers.set(knownTypes.int16, _marshal_int16_to_cs);
        js_to_cs_marshalers.set(knownTypes.int32, _marshal_int32_to_cs);
        js_to_cs_marshalers.set(knownTypes.int64, _marshal_int64_to_cs);
        js_to_cs_marshalers.set(knownTypes.double, _marshal_double_to_cs);
        js_to_cs_marshalers.set(knownTypes.float, _marshal_float_to_cs);
        js_to_cs_marshalers.set(knownTypes.intptr, _marshal_intptr_to_cs);
        js_to_cs_marshalers.set(knownTypes.date_time, _marshal_date_time_to_cs);
        js_to_cs_marshalers.set(knownTypes.date_time_offset, _marshal_date_time_offset_to_cs);
        js_to_cs_marshalers.set(knownTypes.string, _marshal_string_to_cs);
        js_to_cs_marshalers.set(knownTypes.exception, marshal_exception_to_cs);
        js_to_cs_marshalers.set(knownTypes.ijs_object, _marshal_js_object_to_cs);
        js_to_cs_marshalers.set(knownTypes.cs_object, _marshal_cs_object_to_cs);
        js_to_cs_marshalers.set(knownTypes.task, _marshal_task_to_cs);
    }
}
