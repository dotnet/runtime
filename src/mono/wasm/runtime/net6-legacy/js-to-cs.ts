// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { isThenable } from "../cancelable-promise";
import { legacy_c_functions as cwraps } from "../cwraps";
import { js_owned_gc_handle_symbol, assert_not_disposed, cs_owned_js_handle_symbol, mono_wasm_get_js_handle, setup_managed_proxy, mono_wasm_release_cs_owned_object, teardown_managed_proxy, mono_wasm_get_jsobj_from_js_handle } from "../gc-handles";
import { Module } from "../globals";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { setI32_unchecked, setU32_unchecked, setF64, setB32 } from "../memory";
import { mono_wasm_new_root, mono_wasm_release_roots, mono_wasm_new_external_root } from "../roots";
import { js_string_to_mono_string_root, js_string_to_mono_string_interned_root } from "../strings";
import { MonoObject, is_nullish, MonoClass, MonoArray, MonoMethod, MonoObjectNull, JSHandle, MonoObjectRef, JSHandleNull, JSHandleDisposed, WasmRoot } from "../types";
import { TypedArray, Int32Ptr } from "../types/emscripten";
import { has_backing_array_buffer } from "./buffers";
import { legacyManagedExports } from "./corebindings";
import { get_js_owned_object_by_gc_handle_ref } from "./cs-to-js";
import { legacyHelpers, wasm_type_symbol } from "./globals";

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _js_to_mono_uri_root(should_add_in_flight: boolean, js_obj: any, result: WasmRoot<MonoObject>): void {
    switch (true) {
        case js_obj === null:
        case typeof js_obj === "undefined":
            result.clear();
            return;
        case typeof js_obj === "symbol":
        case typeof js_obj === "string":
            legacyManagedExports._create_uri_ref(js_obj, result.address);
            return;
        default:
            _extract_mono_obj_root(should_add_in_flight, js_obj, result);
            return;
    }
}

// this is only used from Blazor
/**
 * @deprecated Not GC or thread safe. For blazor use only
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function js_to_mono_obj(js_obj: any): MonoObject {
    const temp = mono_wasm_new_root<MonoObject>();
    try {
        js_to_mono_obj_root(js_obj, temp, false);
        return temp.value;
    } finally {
        temp.release();
    }
}

/**
 * @deprecated Not GC or thread safe
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _js_to_mono_obj_unsafe(should_add_in_flight: boolean, js_obj: any): MonoObject {
    const temp = mono_wasm_new_root<MonoObject>();
    try {
        js_to_mono_obj_root(js_obj, temp, should_add_in_flight);
        return temp.value;
    } finally {
        temp.release();
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function js_to_mono_obj_root(js_obj: any, result: WasmRoot<MonoObject>, should_add_in_flight: boolean): void {
    if (is_nullish(result))
        throw new Error("Expected (value, WasmRoot, boolean)");

    switch (true) {
        case js_obj === null:
        case typeof js_obj === "undefined":
            result.clear();
            return;
        case typeof js_obj === "number": {
            let box_class: MonoClass;
            if ((js_obj | 0) === js_obj) {
                setI32_unchecked(legacyHelpers._box_buffer, js_obj);
                box_class = legacyHelpers._class_int32;
            } else if ((js_obj >>> 0) === js_obj) {
                setU32_unchecked(legacyHelpers._box_buffer, js_obj);
                box_class = legacyHelpers._class_uint32;
            } else {
                setF64(legacyHelpers._box_buffer, js_obj);
                box_class = legacyHelpers._class_double;
            }

            cwraps.mono_wasm_box_primitive_ref(box_class, legacyHelpers._box_buffer, 8, result.address);
            return;
        }
        case typeof js_obj === "string":
            js_string_to_mono_string_root(js_obj, <any>result);
            return;
        case typeof js_obj === "symbol":
            js_string_to_mono_string_interned_root(js_obj, <any>result);
            return;
        case typeof js_obj === "boolean":
            setB32(legacyHelpers._box_buffer, js_obj);
            cwraps.mono_wasm_box_primitive_ref(legacyHelpers._class_boolean, legacyHelpers._box_buffer, 4, result.address);
            return;
        case isThenable(js_obj) === true: {
            _wrap_js_thenable_as_task_root(js_obj, result);
            return;
        }
        case js_obj.constructor.name === "Date":
            // getTime() is always UTC
            legacyManagedExports._create_date_time_ref(js_obj.getTime(), result.address);
            return;
        default:
            _extract_mono_obj_root(should_add_in_flight, js_obj, result);
            return;
    }
}

function _extract_mono_obj_root(should_add_in_flight: boolean, js_obj: any, result: WasmRoot<MonoObject>): void {
    result.clear();

    if (js_obj === null || typeof js_obj === "undefined")
        return;

    if (js_obj[js_owned_gc_handle_symbol] !== undefined) {
        // for js_owned_gc_handle we don't want to create new proxy
        // since this is strong gc_handle we don't need to in-flight reference
        const gc_handle = assert_not_disposed(js_obj);
        get_js_owned_object_by_gc_handle_ref(gc_handle, result.address);
        return;
    }
    if (js_obj[cs_owned_js_handle_symbol]) {
        get_cs_owned_object_by_js_handle_ref(js_obj[cs_owned_js_handle_symbol], should_add_in_flight, result.address);

        // It's possible the managed object corresponding to this JS object was collected,
        //  in which case we need to make a new one.
        // FIXME: This check is not thread safe
        if (!result.value) {
            delete js_obj[cs_owned_js_handle_symbol];
        }
    }

    // FIXME: This check is not thread safe
    if (!result.value) {
        // Obtain the JS -> C# type mapping.
        const wasm_type = js_obj[wasm_type_symbol];
        const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;

        const js_handle = mono_wasm_get_js_handle(js_obj);

        legacyManagedExports._create_cs_owned_proxy_ref(js_handle, wasm_type_id, should_add_in_flight ? 1 : 0, result.address);
    }
}

// https://github.com/Planeshifter/emscripten-examples/blob/master/01_PassingArrays/sum_post.js
function js_typedarray_to_heap(typedArray: TypedArray) {
    const numBytes = typedArray.length * typedArray.BYTES_PER_ELEMENT;
    const ptr = Module._malloc(numBytes);
    const heapBytes = new Uint8Array(Module.HEAPU8.buffer, <any>ptr, numBytes);
    heapBytes.set(new Uint8Array(typedArray.buffer, typedArray.byteOffset, numBytes));
    return heapBytes;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function js_typed_array_to_array_root(js_obj: any, result: WasmRoot<MonoArray>): void {
    // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
    // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
    // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
    //  is an object representing a chunk of data; it has no format to speak of, and offers no
    // mechanism for accessing its contents. In order to access the memory contained in a buffer,
    // you need to use a view. A view provides a context - that is, a data type, starting offset,
    // and number of elements - that turns the data into an actual typed array.
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
    if (has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT) {
        const arrayType = js_obj[wasm_type_symbol];
        const heapBytes = js_typedarray_to_heap(js_obj);
        cwraps.mono_wasm_typed_array_new_ref(<any>heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType, result.address);
        Module._free(<any>heapBytes.byteOffset);
    }
    else {
        throw new Error("Object '" + js_obj + "' is not a typed array");
    }
}

/**
 * @deprecated Not GC or thread safe
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function js_typed_array_to_array(js_obj: any): MonoArray {
    const temp = mono_wasm_new_root<MonoArray>();
    try {
        js_typed_array_to_array_root(js_obj, temp);
        return temp.value;
    } finally {
        temp.release();
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/explicit-module-boundary-types
export function js_to_mono_enum(js_obj: any, method: MonoMethod, parmIdx: number): number {
    if (typeof (js_obj) !== "number")
        throw new Error(`Expected numeric value for enum argument, got '${js_obj}'`);

    return js_obj | 0;
}

export function js_array_to_mono_array(js_array: any[], asString: boolean, should_add_in_flight: boolean): MonoArray {
    const arrayRoot = mono_wasm_new_root<MonoArray>();
    if (asString)
        cwraps.mono_wasm_string_array_new_ref(js_array.length, arrayRoot.address);
    else
        cwraps.mono_wasm_obj_array_new_ref(js_array.length, arrayRoot.address);
    const elemRoot = mono_wasm_new_root(MonoObjectNull);
    const arrayAddress = arrayRoot.address;
    const elemAddress = elemRoot.address;

    try {
        for (let i = 0; i < js_array.length; ++i) {
            let obj = js_array[i];
            if (asString)
                obj = obj.toString();

            js_to_mono_obj_root(obj, elemRoot, should_add_in_flight);
            cwraps.mono_wasm_obj_array_set_ref(arrayAddress, i, elemAddress);
        }

        return arrayRoot.value;
    } finally {
        mono_wasm_release_roots(arrayRoot, elemRoot);
    }
}

export function _wrap_js_thenable_as_task_root(thenable: Promise<any>, resultRoot: WasmRoot<MonoObject>): {
    then_js_handle: JSHandle,
} {
    if (!thenable) {
        resultRoot.clear();
        return <any>null;
    }

    // hold strong JS reference to thenable while in flight
    // ideally, this should be hold alive by lifespan of the resulting C# Task, but this is good cheap aproximation
    const thenable_js_handle = mono_wasm_get_js_handle(thenable);

    // Note that we do not implement promise/task roundtrip.
    // With more complexity we could recover original instance when this Task is marshaled back to JS.
    // TODO optimization: return the tcs.Task on this same call instead of _get_tcs_task
    const tcs_gc_handle = legacyManagedExports._create_tcs();
    const holder: any = { tcs_gc_handle };
    setup_managed_proxy(holder, tcs_gc_handle);
    thenable.then((result) => {
        legacyManagedExports._set_tcs_result_ref(tcs_gc_handle, result);
    }, (reason) => {
        legacyManagedExports._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
    }).finally(() => {
        // let go of the thenable reference
        mono_wasm_release_cs_owned_object(thenable_js_handle);
        teardown_managed_proxy(holder, tcs_gc_handle); // this holds holder alive for finalizer, until the promise is freed
    });


    legacyManagedExports._get_tcs_task_ref(tcs_gc_handle, resultRoot.address);

    // returns raw pointer to tcs.Task
    return {
        then_js_handle: thenable_js_handle,
    };
}

export function mono_wasm_typed_array_to_array_ref(js_handle: JSHandle, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const resultRoot = mono_wasm_new_external_root<MonoArray>(result_address);
    try {
        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (is_nullish(js_obj)) {
            wrap_error_root(is_exception, "ERR06: Invalid JS object handle '" + js_handle + "'", resultRoot);
            return;
        }

        // returns pointer to C# array
        js_typed_array_to_array_root(js_obj, resultRoot);
        wrap_no_error_root(is_exception);
    } catch (exc) {
        wrap_error_root(is_exception, String(exc), resultRoot);
    } finally {
        resultRoot.release();
    }
}

// when should_add_in_flight === true, the JSObject would be temporarily hold by Normal gc_handle, so that it would not get collected during transition to the managed stack.
// its InFlight gc_handle would be freed when the instance arrives to managed side via Interop.Runtime.ReleaseInFlight
export function get_cs_owned_object_by_js_handle_ref(js_handle: JSHandle, should_add_in_flight: boolean, result: MonoObjectRef): void {
    if (js_handle === JSHandleNull || js_handle === JSHandleDisposed) {
        setI32_unchecked(result, 0);
        return;
    }
    legacyManagedExports._get_cs_owned_object_by_js_handle_ref(js_handle, should_add_in_flight ? 1 : 0, result);
}

