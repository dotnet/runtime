// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root, WasmRoot } from "./roots";
import {
    GCHandle, JSHandleDisposed, MonoArray,
    MonoArrayNull, MonoObject, MonoObjectNull, MonoString
} from "./types";
import { Module, runtimeHelpers } from "./modules";
import { conv_string } from "./strings";
import corebindings from "./corebindings";
import cwraps from "./cwraps";
import { get_js_owned_object_by_gc_handle, js_owned_gc_handle_symbol, mono_wasm_get_jsobj_from_js_handle, mono_wasm_get_js_handle, _js_owned_object_finalized, _js_owned_object_registry, _lookup_js_owned_object, _register_js_owned_object, _use_finalization_registry } from "./gc-handles";
import { mono_method_get_call_signature, call_method, wrap_error } from "./method-calls";
import { _js_to_mono_obj } from "./js-to-cs";
import { _are_promises_supported, _create_cancelable_promise } from "./cancelable-promise";

const delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");
const delegate_invoke_signature_symbol = Symbol.for("wasm delegate_invoke_signature");

// this is only used from Blazor
export function unbox_mono_obj(mono_obj: MonoObject): any {
    if (mono_obj === MonoObjectNull)
        return undefined;

    const root = mono_wasm_new_root(mono_obj);
    try {
        return _unbox_mono_obj_root(root);
    } finally {
        root.release();
    }
}

function _unbox_cs_owned_root_as_js_object(root: WasmRoot<any>) {
    // we don't need in-flight reference as we already have it rooted here
    const js_handle = corebindings._get_cs_owned_object_js_handle(root.value, 0);
    const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
    return js_obj;
}

export function _unbox_mono_obj_root_with_known_nonprimitive_type(root: WasmRoot<any>, type: number): any {
    if (root.value === undefined)
        throw new Error(`Expected a root but got ${root}`);

    //See MARSHAL_TYPE_ defines in driver.c
    switch (type) {
        case 26: // int64
        case 27: // uint64
            // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
            throw new Error("int64 not available");
        case 3: // string
        case 29: // interned string
            return conv_string(root.value);
        case 4: //vts
            throw new Error("no idea on how to unbox value types");
        case 5: // delegate
            return _wrap_delegate_root_as_function(root);
        case 6: // Task
            return _unbox_task_root_as_promise(root);
        case 7: // ref type
            return _unbox_ref_type_root_as_js_object(root);
        case 10: // arrays
        case 11:
        case 12:
        case 13:
        case 14:
        case 15:
        case 16:
        case 17:
        case 18:
            throw new Error("Marshalling of primitive arrays are not supported.  Use the corresponding TypedArray instead.");
        case 20: // clr .NET DateTime
            return new Date(corebindings._get_date_value(root.value));
        case 21: // clr .NET DateTimeOffset
            return corebindings._object_to_string(root.value);
        case 22: // clr .NET Uri
            return corebindings._object_to_string(root.value);
        case 23: // clr .NET SafeHandle/JSObject
            return _unbox_cs_owned_root_as_js_object(root);
        case 30:
            return undefined;
        default:
            throw new Error(`no idea on how to unbox object kind ${type} at offset ${root.value} (root address is ${root.get_address()})`);
    }
}

export function _unbox_mono_obj_root(root: WasmRoot<any>): any {
    if (root.value === 0)
        return undefined;

    const type = cwraps.mono_wasm_try_unbox_primitive_and_get_type(root.value, runtimeHelpers._unbox_buffer);
    switch (type) {
        case 1: // int
            return Module.HEAP32[<any>runtimeHelpers._unbox_buffer / 4];
        case 25: // uint32
            return Module.HEAPU32[<any>runtimeHelpers._unbox_buffer / 4];
        case 24: // float32
            return Module.HEAPF32[<any>runtimeHelpers._unbox_buffer / 4];
        case 2: // float64
            return Module.HEAPF64[<any>runtimeHelpers._unbox_buffer / 8];
        case 8: // boolean
            return (Module.HEAP32[<any>runtimeHelpers._unbox_buffer / 4]) !== 0;
        case 28: // char
            return String.fromCharCode(Module.HEAP32[<any>runtimeHelpers._unbox_buffer / 4]);
        default:
            return _unbox_mono_obj_root_with_known_nonprimitive_type(root, type);
    }
}

export function mono_array_to_js_array(mono_array: MonoArray): any[] | null {
    if (mono_array === MonoArrayNull)
        return null;

    const arrayRoot = mono_wasm_new_root(mono_array);
    try {
        return _mono_array_root_to_js_array(arrayRoot);
    } finally {
        arrayRoot.release();
    }
}

function is_nested_array(ele: MonoObject) {
    return corebindings._is_simple_array(ele);
}

export function _mono_array_root_to_js_array(arrayRoot: WasmRoot<MonoArray>): any[] | null {
    if (arrayRoot.value === MonoArrayNull)
        return null;

    const elemRoot = mono_wasm_new_root<MonoObject>();

    try {
        const len = cwraps.mono_wasm_array_length(arrayRoot.value);
        const res = new Array(len);
        for (let i = 0; i < len; ++i) {
            elemRoot.value = cwraps.mono_wasm_array_get(arrayRoot.value, i);

            if (is_nested_array(elemRoot.value))
                res[i] = _mono_array_root_to_js_array(<any>elemRoot);
            else
                res[i] = _unbox_mono_obj_root(elemRoot);
        }
        return res;
    } finally {
        elemRoot.release();
    }
}

export function _wrap_delegate_root_as_function(root: WasmRoot<MonoObject>): Function | null {
    if (root.value === MonoObjectNull)
        return null;

    // get strong reference to the Delegate
    const gc_handle = corebindings._get_js_owned_object_gc_handle(root.value);
    return _wrap_delegate_gc_handle_as_function(gc_handle);
}

export function _wrap_delegate_gc_handle_as_function(gc_handle: GCHandle, after_listener_callback?: () => void): Function {
    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);

    // If the function for this gc_handle was already collected (or was never created)
    if (!result) {
        // note that we do not implement function/delegate roundtrip
        result = function (...args: any[]) {
            const delegateRoot = mono_wasm_new_root(get_js_owned_object_by_gc_handle(gc_handle));
            try {
                const res = call_method(result[delegate_invoke_symbol], delegateRoot.value, result[delegate_invoke_signature_symbol], args);
                if (after_listener_callback) {
                    after_listener_callback();
                }
                return res;
            } finally {
                delegateRoot.release();
            }
        };

        // bind the method
        const delegateRoot = mono_wasm_new_root(get_js_owned_object_by_gc_handle(gc_handle));
        try {
            if (typeof result[delegate_invoke_symbol] === "undefined") {
                result[delegate_invoke_symbol] = cwraps.mono_wasm_get_delegate_invoke(delegateRoot.value);
                if (!result[delegate_invoke_symbol]) {
                    throw new Error("System.Delegate Invoke method can not be resolved.");
                }
            }

            if (typeof result[delegate_invoke_signature_symbol] === "undefined") {
                result[delegate_invoke_signature_symbol] = mono_method_get_call_signature(result[delegate_invoke_symbol], delegateRoot.value);
            }
        } finally {
            delegateRoot.release();
        }

        // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry. Except in case of EventListener where we cleanup after unregistration.
        if (_use_finalization_registry) {
            // register for GC of the deleate after the JS side is done with the function
            _js_owned_object_registry.register(result, gc_handle);
        }

        // register for instance reuse
        // NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef. Except in case of EventListener where we cleanup after unregistration.
        _register_js_owned_object(gc_handle, result);
    }

    return result;
}

export function mono_wasm_create_cs_owned_object(core_name: MonoString, args: MonoArray, is_exception: Int32Ptr): MonoObject {
    const argsRoot = mono_wasm_new_root(args), nameRoot = mono_wasm_new_root(core_name);
    try {
        const js_name = conv_string(nameRoot.value);
        if (!js_name) {
            return wrap_error(is_exception, "Invalid name @" + nameRoot.value);
        }

        const coreObj = (<any>globalThis)[js_name];
        if (coreObj === null || typeof coreObj === "undefined") {
            return wrap_error(is_exception, "JavaScript host object '" + js_name + "' not found.");
        }

        try {
            const js_args = _mono_array_root_to_js_array(argsRoot);

            // This is all experimental !!!!!!
            const allocator = function (constructor: Function, js_args: any[] | null) {
                // Not sure if we should be checking for anything here
                let argsList = [];
                argsList[0] = constructor;
                if (js_args)
                    argsList = argsList.concat(js_args);
                // eslint-disable-next-line prefer-spread
                const tempCtor = constructor.bind.apply(constructor, <any>argsList);
                const js_obj = new tempCtor();
                return js_obj;
            };

            const js_obj = allocator(coreObj, js_args);
            const js_handle = mono_wasm_get_js_handle(js_obj);
            // returns boxed js_handle int, because on exception we need to return String on same method signature
            // here we don't have anything to in-flight reference, as the JSObject doesn't exist yet
            return _js_to_mono_obj(false, js_handle);
        } catch (ex) {
            return wrap_error(is_exception, ex);
        }
    } finally {
        argsRoot.release();
        nameRoot.release();
    }
}

function _unbox_task_root_as_promise(root: WasmRoot<MonoObject>) {
    if (root.value === MonoObjectNull)
        return null;

    if (!_are_promises_supported)
        throw new Error("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");

    // get strong reference to Task
    const gc_handle = corebindings._get_js_owned_object_gc_handle(root.value);

    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);

    // If the promise for this gc_handle was already collected (or was never created)
    if (!result) {
        const explicitFinalization = _use_finalization_registry
            ? undefined
            : () => _js_owned_object_finalized(gc_handle);

        const { promise, promise_control } = _create_cancelable_promise(explicitFinalization, explicitFinalization);

        // note that we do not implement promise/task roundtrip
        // With more complexity we could recover original instance when this promise is marshaled back to C#.
        result = promise;

        // register C# side of the continuation
        corebindings._setup_js_cont(root.value, promise_control);

        // register for GC of the Task after the JS side is done with the promise
        if (_use_finalization_registry) {
            _js_owned_object_registry.register(result, gc_handle);
        }

        // register for instance reuse
        _register_js_owned_object(gc_handle, result);
    }

    return result;
}

function _unbox_ref_type_root_as_js_object(root: WasmRoot<MonoObject>) {

    if (root.value === MonoObjectNull)
        return null;

    // this could be JSObject proxy of a js native object
    // we don't need in-flight reference as we already have it rooted here
    const js_handle = corebindings._try_get_cs_owned_object_js_handle(root.value, 0);
    if (js_handle) {
        if (js_handle === JSHandleDisposed) {
            throw new Error("Cannot access a disposed JSObject at " + root.value);
        }
        return mono_wasm_get_jsobj_from_js_handle(js_handle);
    }
    // otherwise this is C# only object

    // get strong reference to Object
    const gc_handle = corebindings._get_js_owned_object_gc_handle(root.value);

    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);

    // If the JS object for this gc_handle was already collected (or was never created)
    if (!result) {
        result = {};

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

    return result;
}