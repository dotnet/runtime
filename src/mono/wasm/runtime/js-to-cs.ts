// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, runtimeHelpers } from "./imports";
import {
    cs_owned_js_handle_symbol, get_cs_owned_object_by_js_handle, get_js_owned_object_by_gc_handle, js_owned_gc_handle_symbol,
    mono_wasm_get_jsobj_from_js_handle, mono_wasm_get_js_handle,
    mono_wasm_release_cs_owned_object, _js_owned_object_registry, _use_finalization_registry
} from "./gc-handles";
import corebindings from "./corebindings";
import cwraps from "./cwraps";
import { mono_wasm_new_root, mono_wasm_release_roots } from "./roots";
import { wrap_error } from "./method-calls";
import { js_string_to_mono_string, js_string_to_mono_string_interned } from "./strings";
import { isThenable } from "./cancelable-promise";
import { has_backing_array_buffer } from "./buffers";
import { Int32Ptr, JSHandle, MonoArray, MonoMethod, MonoObject, MonoObjectNull, MonoString, wasm_type_symbol } from "./types";
import { setI32, setU32, setF64 } from "./memory";

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _js_to_mono_uri(should_add_in_flight: boolean, js_obj: any): MonoObject {
    switch (true) {
        case js_obj === null:
        case typeof js_obj === "undefined":
            return MonoObjectNull;
        case typeof js_obj === "symbol":
        case typeof js_obj === "string":
            return corebindings._create_uri(js_obj);
        default:
            return _extract_mono_obj(should_add_in_flight, js_obj);
    }
}

// this is only used from Blazor
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function js_to_mono_obj(js_obj: any): MonoObject {
    return _js_to_mono_obj(false, js_obj);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _js_to_mono_obj(should_add_in_flight: boolean, js_obj: any): MonoObject {
    switch (true) {
        case js_obj === null:
        case typeof js_obj === "undefined":
            return MonoObjectNull;
        case typeof js_obj === "number": {
            let result = null;
            if ((js_obj | 0) === js_obj)
                result = _box_js_int(js_obj);
            else if ((js_obj >>> 0) === js_obj)
                result = _box_js_uint(js_obj);
            else
                result = _box_js_double(js_obj);

            if (!result)
                throw new Error(`Boxing failed for ${js_obj}`);

            return result;
        } case typeof js_obj === "string":
            return <any>js_string_to_mono_string(js_obj);
        case typeof js_obj === "symbol":
            return <any>js_string_to_mono_string_interned(js_obj);
        case typeof js_obj === "boolean":
            return _box_js_bool(js_obj);
        case isThenable(js_obj) === true: {
            const { task_ptr } = _wrap_js_thenable_as_task(js_obj);
            // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
            return task_ptr;
        }
        case js_obj.constructor.name === "Date":
            // getTime() is always UTC
            return corebindings._create_date_time(js_obj.getTime());
        default:
            return _extract_mono_obj(should_add_in_flight, js_obj);
    }
}

function _extract_mono_obj(should_add_in_flight: boolean, js_obj: any): MonoObject {
    if (js_obj === null || typeof js_obj === "undefined")
        return MonoObjectNull;

    let result = null;
    if (js_obj[js_owned_gc_handle_symbol]) {
        // for js_owned_gc_handle we don't want to create new proxy
        // since this is strong gc_handle we don't need to in-flight reference
        result = get_js_owned_object_by_gc_handle(js_obj[js_owned_gc_handle_symbol]);
        return result;
    }
    if (js_obj[cs_owned_js_handle_symbol]) {
        result = get_cs_owned_object_by_js_handle(js_obj[cs_owned_js_handle_symbol], should_add_in_flight);

        // It's possible the managed object corresponding to this JS object was collected,
        //  in which case we need to make a new one.
        if (!result) {
            delete js_obj[cs_owned_js_handle_symbol];
        }
    }

    if (!result) {
        // Obtain the JS -> C# type mapping.
        const wasm_type = js_obj[wasm_type_symbol];
        const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;

        const js_handle = mono_wasm_get_js_handle(js_obj);

        result = corebindings._create_cs_owned_proxy(js_handle, wasm_type_id, should_add_in_flight ? 1 : 0);
    }

    return result;
}

function _box_js_int(js_obj: number) {
    setI32(runtimeHelpers._box_buffer, js_obj);
    return cwraps.mono_wasm_box_primitive(runtimeHelpers._class_int32, runtimeHelpers._box_buffer, 4);
}

function _box_js_uint(js_obj: number) {
    setU32(runtimeHelpers._box_buffer, js_obj);
    return cwraps.mono_wasm_box_primitive(runtimeHelpers._class_uint32, runtimeHelpers._box_buffer, 4);
}

function _box_js_double(js_obj: number) {
    setF64(runtimeHelpers._box_buffer, js_obj);
    return cwraps.mono_wasm_box_primitive(runtimeHelpers._class_double, runtimeHelpers._box_buffer, 8);
}

export function _box_js_bool(js_obj: boolean): MonoObject {
    setI32(runtimeHelpers._box_buffer, js_obj ? 1 : 0);
    return cwraps.mono_wasm_box_primitive(runtimeHelpers._class_boolean, runtimeHelpers._box_buffer, 4);
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
export function js_typed_array_to_array(js_obj: any): MonoArray {
    // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
    // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
    // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
    //  is an object representing a chunk of data; it has no format to speak of, and offers no
    // mechanism for accessing its contents. In order to access the memory contained in a buffer,
    // you need to use a view. A view provides a context — that is, a data type, starting offset,
    // and number of elements — that turns the data into an actual typed array.
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
    if (has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT) {
        const arrayType = js_obj[wasm_type_symbol];
        const heapBytes = js_typedarray_to_heap(js_obj);
        const bufferArray = cwraps.mono_wasm_typed_array_new(<any>heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType);
        Module._free(<any>heapBytes.byteOffset);
        return bufferArray;
    }
    else {
        throw new Error("Object '" + js_obj + "' is not a typed array");
    }
}

// eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/explicit-module-boundary-types
export function js_to_mono_enum(js_obj: any, method: MonoMethod, parmIdx: number): number {
    if (typeof (js_obj) !== "number")
        throw new Error(`Expected numeric value for enum argument, got '${js_obj}'`);

    return js_obj | 0;
}

export function js_array_to_mono_array(js_array: any[], asString: boolean, should_add_in_flight: boolean): MonoArray {
    const mono_array = asString ? cwraps.mono_wasm_string_array_new(js_array.length) : cwraps.mono_wasm_obj_array_new(js_array.length);
    const arrayRoot = mono_wasm_new_root(mono_array);
    const elemRoot = mono_wasm_new_root(MonoObjectNull);

    try {
        for (let i = 0; i < js_array.length; ++i) {
            let obj = js_array[i];
            if (asString)
                obj = obj.toString();

            elemRoot.value = _js_to_mono_obj(should_add_in_flight, obj);
            cwraps.mono_wasm_obj_array_set(arrayRoot.value, i, elemRoot.value);
        }

        return mono_array;
    } finally {
        mono_wasm_release_roots(arrayRoot, elemRoot);
    }
}

export function _wrap_js_thenable_as_task(thenable: Promise<any>): {
    task_ptr: MonoObject,
    then_js_handle: JSHandle,

} {

    if (!thenable)
        return <any>null;

    // hold strong JS reference to thenable while in flight
    // ideally, this should be hold alive by lifespan of the resulting C# Task, but this is good cheap aproximation
    const thenable_js_handle = mono_wasm_get_js_handle(thenable);

    // Note that we do not implement promise/task roundtrip. 
    // With more complexity we could recover original instance when this Task is marshaled back to JS.
    // TODO optimization: return the tcs.Task on this same call instead of _get_tcs_task
    const tcs_gc_handle = corebindings._create_tcs();
    thenable.then((result) => {
        corebindings._set_tcs_result(tcs_gc_handle, result);
        // let go of the thenable reference
        mono_wasm_release_cs_owned_object(thenable_js_handle);

        // when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
        if (!_use_finalization_registry) {
            corebindings._release_js_owned_object_by_gc_handle(tcs_gc_handle);
        }
    }, (reason) => {
        corebindings._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
        // let go of the thenable reference
        mono_wasm_release_cs_owned_object(thenable_js_handle);

        // when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
        if (!_use_finalization_registry) {
            corebindings._release_js_owned_object_by_gc_handle(tcs_gc_handle);
        }
    });

    // collect the TaskCompletionSource with its Task after js doesn't hold the thenable anymore
    if (_use_finalization_registry) {
        _js_owned_object_registry.register(thenable, tcs_gc_handle);
    }

    // returns raw pointer to tcs.Task
    return {
        task_ptr: corebindings._get_tcs_task(tcs_gc_handle),
        then_js_handle: thenable_js_handle,
    };
}

export function mono_wasm_typed_array_to_array(js_handle: JSHandle, is_exception: Int32Ptr): MonoArray | MonoString {
    const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
    if (!js_obj) {
        return wrap_error(is_exception, "ERR06: Invalid JS object handle '" + js_handle + "'");
    }

    // returns pointer to C# array
    return js_typed_array_to_array(js_obj);
}
