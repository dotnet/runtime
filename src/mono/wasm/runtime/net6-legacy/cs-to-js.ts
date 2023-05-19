// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Int32Ptr, VoidPtr } from "../types/emscripten";
import { MarshalType, MonoType, MarshalError, MonoTypeNull, MonoArray, MonoArrayNull, MonoObject, MonoObjectNull, GCHandle, MonoStringRef, MonoObjectRef, MonoString, JSHandleDisposed, is_nullish, WasmRoot } from "../types/internal";
import { _are_promises_supported } from "../cancelable-promise";
import { legacy_c_functions as cwraps } from "../cwraps";
import { mono_wasm_get_jsobj_from_js_handle, _lookup_js_owned_object, setup_managed_proxy, mono_wasm_get_js_handle, teardown_managed_proxy, assert_not_disposed } from "../gc-handles";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { ManagedObject } from "../marshal";
import { getU32, getI32, getF32, getF64, setI32_unchecked } from "../memory";
import { mono_wasm_new_root, mono_wasm_new_external_root } from "../roots";
import { conv_string_root } from "../strings";
import { legacyManagedExports } from "./corebindings";
import { legacyHelpers } from "./globals";
import { js_to_mono_obj_root } from "./js-to-cs";
import { mono_bind_method, mono_method_get_call_signature_ref } from "./method-binding";
import { createPromiseController } from "../globals";

const delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");

// this is only used from Blazor
export function unbox_mono_obj(mono_obj: MonoObject): any {
    if (mono_obj === MonoObjectNull)
        return undefined;

    const root = mono_wasm_new_root(mono_obj);
    try {
        return unbox_mono_obj_root(root);
    } finally {
        root.release();
    }
}

function _unbox_cs_owned_root_as_js_object(root: WasmRoot<any>) {
    // we don't need in-flight reference as we already have it rooted here
    const js_handle = legacyManagedExports._get_cs_owned_object_js_handle_ref(root.address, 0);
    const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
    return js_obj;
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root: WasmRoot<any>, type: MarshalType, typePtr: MonoType, unbox_buffer: VoidPtr): any {
    //See MARSHAL_TYPE_ defines in driver.c
    switch (type) {
        case MarshalType.NULL:
            return null;
        case MarshalType.INT64:
        case MarshalType.UINT64:
            // TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
            throw new Error("int64 not available");
        case MarshalType.STRING:
        case MarshalType.STRING_INTERNED:
            return conv_string_root(root);
        case MarshalType.VT:
            throw new Error("no idea on how to unbox value types");
        case MarshalType.DELEGATE:
            return _wrap_delegate_root_as_function(root);
        case MarshalType.TASK:
            return _unbox_task_root_as_promise(root);
        case MarshalType.OBJECT:
            return _unbox_ref_type_root_as_js_object(root);
        case MarshalType.ARRAY_BYTE:
        case MarshalType.ARRAY_UBYTE:
        case MarshalType.ARRAY_UBYTE_C:
        case MarshalType.ARRAY_SHORT:
        case MarshalType.ARRAY_USHORT:
        case MarshalType.ARRAY_INT:
        case MarshalType.ARRAY_UINT:
        case MarshalType.ARRAY_FLOAT:
        case MarshalType.ARRAY_DOUBLE:
            throw new Error("Marshaling of primitive arrays are not supported.");
        case <MarshalType>20: // clr .NET DateTime
            return new Date(legacyManagedExports._get_date_value_ref(root.address));
        case <MarshalType>21: // clr .NET DateTimeOffset
            return legacyManagedExports._object_to_string_ref(root.address);
        case MarshalType.URI:
            return legacyManagedExports._object_to_string_ref(root.address);
        case MarshalType.SAFEHANDLE:
            return _unbox_cs_owned_root_as_js_object(root);
        case MarshalType.VOID:
            return undefined;
        default:
            throw new Error(`no idea on how to unbox object of MarshalType ${type} at offset ${root.value} (root address is ${root.address})`);
    }
}

export function _unbox_mono_obj_root_with_known_nonprimitive_type(root: WasmRoot<any>, type: MarshalType, unbox_buffer: VoidPtr): any {
    if (type >= MarshalError.FIRST)
        throw new Error(`Got marshaling error ${type} when attempting to unbox object at address ${root.value} (root located at ${root.address})`);

    let typePtr = MonoTypeNull;
    if ((type === MarshalType.VT) || (type == MarshalType.OBJECT)) {
        typePtr = <MonoType><any>getU32(unbox_buffer);
        if (<number><any>typePtr < 1024)
            throw new Error(`Got invalid MonoType ${typePtr} for object at address ${root.value} (root located at ${root.address})`);
    }

    return _unbox_mono_obj_root_with_known_nonprimitive_type_impl(root, type, typePtr, unbox_buffer);
}

export function unbox_mono_obj_root(root: WasmRoot<any>): any {
    if (root.value === 0)
        return undefined;

    const unbox_buffer = legacyHelpers._unbox_buffer;
    const type = cwraps.mono_wasm_try_unbox_primitive_and_get_type_ref(root.address, unbox_buffer, legacyHelpers._unbox_buffer_size);
    switch (type) {
        case MarshalType.INT:
            return getI32(unbox_buffer);
        case MarshalType.UINT32:
            return getU32(unbox_buffer);
        case MarshalType.POINTER:
            // FIXME: Is this right?
            return getU32(unbox_buffer);
        case MarshalType.FP32:
            return getF32(unbox_buffer);
        case MarshalType.FP64:
            return getF64(unbox_buffer);
        case MarshalType.BOOL:
            return (getI32(unbox_buffer)) !== 0;
        case MarshalType.CHAR:
            return String.fromCharCode(getI32(unbox_buffer));
        case MarshalType.NULL:
            return null;
        default:
            return _unbox_mono_obj_root_with_known_nonprimitive_type(root, type, unbox_buffer);
    }
}

export function mono_array_to_js_array(mono_array: MonoArray): any[] | null {
    if (mono_array === MonoArrayNull)
        return null;

    const arrayRoot = mono_wasm_new_root(mono_array);
    try {
        return mono_array_root_to_js_array(arrayRoot);
    } finally {
        arrayRoot.release();
    }
}

function is_nested_array_ref(ele: WasmRoot<MonoObject>) {
    return legacyManagedExports._is_simple_array_ref(ele.address);
}

export function mono_array_root_to_js_array(arrayRoot: WasmRoot<MonoArray>): any[] | null {
    if (arrayRoot.value === MonoArrayNull)
        return null;

    const arrayAddress = arrayRoot.address;
    const elemRoot = mono_wasm_new_root<MonoObject>();
    const elemAddress = elemRoot.address;

    try {
        const len = cwraps.mono_wasm_array_length_ref(arrayAddress);
        const res = new Array(len);
        for (let i = 0; i < len; ++i) {
            // TODO: pass arrayRoot.address and elemRoot.address into new API that copies
            cwraps.mono_wasm_array_get_ref(arrayAddress, i, elemAddress);

            if (is_nested_array_ref(elemRoot))
                res[i] = mono_array_root_to_js_array(<any>elemRoot);
            else
                res[i] = unbox_mono_obj_root(elemRoot);
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
    const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);
    return _wrap_delegate_gc_handle_as_function(gc_handle);
}

export function _wrap_delegate_gc_handle_as_function(gc_handle: GCHandle): Function {
    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);


    // If the function for this gc_handle was already collected (or was never created)
    if (!result) {

        // note that we do not implement function/delegate roundtrip
        result = function (...args: any[]) {
            assert_not_disposed(result);
            const boundMethod = result[delegate_invoke_symbol];
            return boundMethod(...args);
        };

        // bind the method
        const delegateRoot = mono_wasm_new_root<MonoObject>();
        get_js_owned_object_by_gc_handle_ref(gc_handle, delegateRoot.address);
        try {
            if (typeof result[delegate_invoke_symbol] === "undefined") {
                const method = cwraps.mono_wasm_get_delegate_invoke_ref(delegateRoot.address);
                const signature = mono_method_get_call_signature_ref(method, delegateRoot);
                const js_method = mono_bind_method(method, signature, true);
                result[delegate_invoke_symbol] = js_method.bind({ this_arg_gc_handle: gc_handle });
                if (!result[delegate_invoke_symbol]) {
                    throw new Error("System.Delegate Invoke method can not be resolved.");
                }
            }
        } finally {
            delegateRoot.release();
        }

        setup_managed_proxy(result, gc_handle);
    } else {
        assert_not_disposed(result);
    }

    return result;
}

export function mono_wasm_create_cs_owned_object_ref(core_name: MonoStringRef, args: MonoObjectRef, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const argsRoot = mono_wasm_new_external_root<MonoArray>(args),
        nameRoot = mono_wasm_new_external_root<MonoString>(core_name),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const js_name = conv_string_root(nameRoot);
        if (!js_name) {
            wrap_error_root(is_exception, "Invalid name @" + nameRoot.value, resultRoot);
            return;
        }

        const coreObj = (<any>globalThis)[js_name];
        if (coreObj === null || typeof coreObj === "undefined") {
            wrap_error_root(is_exception, "JavaScript host object '" + js_name + "' not found.", resultRoot);
            return;
        }

        try {
            const js_args = mono_array_root_to_js_array(argsRoot);

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
            js_to_mono_obj_root(js_handle, resultRoot, false);
            wrap_no_error_root(is_exception);
        } catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
            return;
        }
    } finally {
        resultRoot.release();
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
    const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);

    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);

    // If the promise for this gc_handle was already collected (or was never created)
    if (!result) {
        const explicitFinalization = () => teardown_managed_proxy(result, gc_handle);

        const { promise, promise_control } = createPromiseController(explicitFinalization, explicitFinalization);

        // note that we do not implement promise/task roundtrip
        // With more complexity we could recover original instance when this promise is marshaled back to C#.
        result = promise;

        // register C# side of the continuation
        legacyManagedExports._setup_js_cont_ref(root.address, promise_control);

        setup_managed_proxy(result, gc_handle);
    }

    return result;
}

export function _unbox_ref_type_root_as_js_object(root: WasmRoot<MonoObject>): any {

    if (root.value === MonoObjectNull)
        return null;

    // this could be JSObject proxy of a js native object
    // we don't need in-flight reference as we already have it rooted here
    const js_handle = legacyManagedExports._try_get_cs_owned_object_js_handle_ref(root.address, 0);
    if (js_handle) {
        if (js_handle === JSHandleDisposed) {
            throw new Error("Cannot access a disposed JSObject at " + root.value);
        }
        return mono_wasm_get_jsobj_from_js_handle(js_handle);
    }
    // otherwise this is C# only object

    // get strong reference to Object
    const gc_handle = legacyManagedExports._get_js_owned_object_gc_handle_ref(root.address);

    // see if we have js owned instance for this gc_handle already
    let result = _lookup_js_owned_object(gc_handle);

    // If the JS object for this gc_handle was already collected (or was never created)
    if (is_nullish(result)) {
        result = new ManagedObject();

        setup_managed_proxy(result, gc_handle);
    }

    return result;
}

export function get_js_owned_object_by_gc_handle_ref(gc_handle: GCHandle, result: MonoObjectRef): void {
    if (!gc_handle) {
        setI32_unchecked(result, 0);
        return;
    }
    // this is always strong gc_handle
    legacyManagedExports._get_js_owned_object_by_gc_handle_ref(gc_handle, result);
}
