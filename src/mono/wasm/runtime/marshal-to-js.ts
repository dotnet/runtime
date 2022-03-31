import { PromiseControl, promise_control_symbol, _create_cancelable_promise } from "./cancelable-promise";
import { MonoString } from "./export-types";
import { _lookup_js_owned_object, js_owned_gc_handle_symbol, _use_finalization_registry, _js_owned_object_registry, _register_js_owned_object, mono_wasm_get_jsobj_from_js_handle, _js_owned_object_table } from "./gc-handles";
import { ManagedObject, get_gc_handle, get_js_handle, get_root_ref, JavaScriptMarshalerArg, ManagedError, get_arg_type, get_arg_i32, get_arg_f64, get_arg_i64, get_arg_i16, get_arg_u8, get_arg_f32, get_arg_b8, get_arg_date } from "./marshal";
import { _get_type_name } from "./method-binding";
import { knownTypes } from "./startup";
import { conv_string } from "./strings";
import { assert, GCHandle, MonoType, MonoTypeNull } from "./types";

export const cs_to_js_marshalers = new Map<MonoType, MarshalerToJs>();
export type MarshalerToJs = (arg: JavaScriptMarshalerArg) => any;

function _marshal_bool_to_js(arg: JavaScriptMarshalerArg): boolean {
    return get_arg_b8(arg);
}

function _marshal_byte_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_u8(arg);
}

function _marshal_int16_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_i16(arg);
}

function _marshal_int32_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_i32(arg);
}

function _marshal_int64_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_i64(arg);
}

function _marshal_float_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_f32(arg);
}

function _marshal_double_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_f64(arg);
}

function _marshal_intptr_to_js(arg: JavaScriptMarshalerArg): number {
    return get_arg_i32(arg);
}

function _marshal_datetime_to_js(arg: JavaScriptMarshalerArg): Date {
    return get_arg_date(arg);
}

function _marshal_task_to_js(arg: JavaScriptMarshalerArg): Promise<any> | null {
    const type = get_arg_type(arg);
    if (type == MonoTypeNull) {
        return null;
    }

    /*
    if (type == knownTypes.ijs_object) {
        // this is Proxy roundtrip
        const js_handle = get_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }*/

    assert(type == knownTypes.task, "Expecting GCHandle of Task");

    const gc_handle = get_gc_handle(arg);

    const cleanup = () => _js_owned_object_table.delete(gc_handle);

    const { promise } = _create_cancelable_promise(cleanup, cleanup);

    (<any>promise)[js_owned_gc_handle_symbol] = gc_handle;

    _register_js_owned_object(gc_handle, promise);

    return promise;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_resolve_task(gc_handle: GCHandle, data: any): void {
    const promise = _lookup_js_owned_object(gc_handle);
    if (!promise) return;
    const promise_control = <PromiseControl>promise[promise_control_symbol];
    try {
        promise_control.resolve(data);
    } catch (ex) {
        promise_control.reject("" + ex);
    }
}

export function mono_wasm_reject_task(gc_handle: GCHandle, reason: Error): void {
    const promise = _lookup_js_owned_object(gc_handle);
    if (!promise) return;
    const promise_control = <PromiseControl>promise[promise_control_symbol];
    try {
        promise_control.reject(reason);
    } catch (ex) {
        promise_control.reject("" + ex);
    }
}


function _marshal_string_to_js(arg: JavaScriptMarshalerArg): string | null {
    const type = get_arg_type(arg);
    if (type == MonoTypeNull) {
        return null;
    }
    const ref = <MonoString>get_root_ref(arg);
    const value = conv_string(ref);
    return value;
}

export function marshal_exception_to_js(arg: JavaScriptMarshalerArg): Error | null {
    const type = get_arg_type(arg);
    if (type == MonoTypeNull) {
        return null;
    }
    if (type == knownTypes.jsexception) {
        // this is JSException roundtrip
        const js_handle = get_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }

    const gc_handle = get_gc_handle(arg);
    let result = _lookup_js_owned_object(gc_handle);
    if (!result) {
        // this will create new ManagedError
        const messagePtr = <MonoString>get_root_ref(arg);
        assert(messagePtr, "Null messagePtr");
        const message = conv_string(messagePtr)!;
        result = new ManagedError(message);
        setup_managed_proxy(result, gc_handle);
    }

    return result;
}

function _marshal_js_object_to_js(arg: JavaScriptMarshalerArg): any {
    const type = get_arg_type(arg);
    if (type == MonoTypeNull) {
        return null;
    }
    const js_handle = get_js_handle(arg);
    const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
    return js_obj;
}

function _marshal_cs_object_to_js(arg: JavaScriptMarshalerArg): any {
    const mono_type = get_arg_type(arg);
    if (mono_type == MonoTypeNull) {
        return null;
    }
    if (mono_type == knownTypes.ijs_object) {
        const js_handle = get_js_handle(arg);
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        return js_obj;
    }

    if (mono_type == knownTypes.cs_object) {
        const gc_handle = get_gc_handle(arg);
        if (!gc_handle) {
            return null;
        }

        // see if we have js owned instance for this gc_handle already
        let result = _lookup_js_owned_object(gc_handle);

        // If the JS object for this gc_handle was already collected (or was never created)
        if (!result) {
            result = new ManagedObject();
            setup_managed_proxy(result, gc_handle);
        }

        return result;
    }

    // other types
    const converter = cs_to_js_marshalers.get(mono_type);
    assert(converter, () => `Unknow converter for type ${_get_type_name(mono_type)}`);
    return converter(arg);
}

function setup_managed_proxy(result: any, gc_handle: GCHandle) {
    // keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
    result[js_owned_gc_handle_symbol] = gc_handle;

    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    if (_use_finalization_registry) {
        // register for GC of the C# object after the JS side is done with the object
        _js_owned_object_registry.register(result, gc_handle);
    }

    // register for instance reuse
    // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
    _register_js_owned_object(gc_handle, result);
}

export function initialize_marshalers_to_js(): void {
    if (cs_to_js_marshalers.size == 0) {
        cs_to_js_marshalers.set(knownTypes.bool, _marshal_bool_to_js);
        cs_to_js_marshalers.set(knownTypes.byte, _marshal_byte_to_js);
        cs_to_js_marshalers.set(knownTypes.int16, _marshal_int16_to_js);
        cs_to_js_marshalers.set(knownTypes.int32, _marshal_int32_to_js);
        cs_to_js_marshalers.set(knownTypes.int64, _marshal_int64_to_js);
        cs_to_js_marshalers.set(knownTypes.float, _marshal_float_to_js);
        cs_to_js_marshalers.set(knownTypes.intptr, _marshal_intptr_to_js);
        cs_to_js_marshalers.set(knownTypes.double, _marshal_double_to_js);
        cs_to_js_marshalers.set(knownTypes.string, _marshal_string_to_js);
        cs_to_js_marshalers.set(knownTypes.exception, marshal_exception_to_js);
        cs_to_js_marshalers.set(knownTypes.ijs_object, _marshal_js_object_to_js);
        cs_to_js_marshalers.set(knownTypes.cs_object, _marshal_cs_object_to_js);
        cs_to_js_marshalers.set(knownTypes.date_time, _marshal_datetime_to_js);
        cs_to_js_marshalers.set(knownTypes.date_time_offset, _marshal_datetime_to_js);
        cs_to_js_marshalers.set(knownTypes.task, _marshal_task_to_js);
    }
}
