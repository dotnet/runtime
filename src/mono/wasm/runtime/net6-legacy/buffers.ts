// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { mono_wasm_new_external_root } from "../roots";
import { MonoArray, MonoObjectRef, MonoObject } from "../types/internal";
import { Int32Ptr, TypedArray } from "../types/emscripten";
import { js_to_mono_obj_root } from "./js-to-cs";
import { localHeapViewU8 } from "../memory";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function mono_wasm_typed_array_from_ref(pinned_array: MonoArray, begin: number, end: number, bytes_per_element: number, type: number, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const res = typed_array_from(pinned_array, begin, end, bytes_per_element, type);
        // returns JS typed array like Int8Array, to be wraped with JSObject proxy
        js_to_mono_obj_root(res, resultRoot, true);
        wrap_no_error_root(is_exception);
    } catch (exc) {
        wrap_error_root(is_exception, String(exc), resultRoot);
    } finally {
        resultRoot.release();
    }
}

// Creates a new typed array from pinned array address from pinned_array allocated on the heap to the typed array.
// 	 address of managed pinned array -> copy from heap -> typed array memory
function typed_array_from(pinned_array: MonoArray, begin: number, end: number, bytes_per_element: number, type: number) {

    // typed array
    let newTypedArray: TypedArray | null = null;

    switch (type) {
        case 5:
            newTypedArray = new Int8Array(end - begin);
            break;
        case 6:
            newTypedArray = new Uint8Array(end - begin);
            break;
        case 7:
            newTypedArray = new Int16Array(end - begin);
            break;
        case 8:
            newTypedArray = new Uint16Array(end - begin);
            break;
        case 9:
            newTypedArray = new Int32Array(end - begin);
            break;
        case 10:
            newTypedArray = new Uint32Array(end - begin);
            break;
        case 13:
            newTypedArray = new Float32Array(end - begin);
            break;
        case 14:
            newTypedArray = new Float64Array(end - begin);
            break;
        case 15: // This is a special case because the typed array is also byte[]
            newTypedArray = new Uint8ClampedArray(end - begin);
            break;
        default:
            throw new Error("Unknown array type " + type);
    }

    typedarray_copy_from(newTypedArray, pinned_array, begin, end, bytes_per_element);
    return newTypedArray;
}

// Copy the pinned array address from pinned_array allocated on the heap to the typed array.
// 	 address of managed pinned array -> copy from heap -> typed array memory
function typedarray_copy_from(typed_array: TypedArray, pinned_array: MonoArray, begin: number, end: number, bytes_per_element: number) {

    // JavaScript typed arrays are array-like objects and provide a mechanism for accessing
    // raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
    // split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
    //  is an object representing a chunk of data; it has no format to speak of, and offers no
    // mechanism for accessing its contents. In order to access the memory contained in a buffer,
    // you need to use a view. A view provides a context - that is, a data type, starting offset,
    // and number of elements - that turns the data into an actual typed array.
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
    if (has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT) {
        // Some sanity checks of what is being asked of us
        // lets play it safe and throw an error here instead of assuming to much.
        // Better safe than sorry later
        if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT)
            throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");

        // how much space we have to work with
        let num_of_bytes = (end - begin) * bytes_per_element;
        // how much typed buffer space are we talking about
        const view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
        // only use what is needed.
        if (num_of_bytes > view_bytes)
            num_of_bytes = view_bytes;

        // Create a new view for mapping
        const typedarrayBytes = new Uint8Array(typed_array.buffer, 0, num_of_bytes);
        // offset index into the view
        const offset = begin * bytes_per_element;
        // Set view bytes to value from HEAPU8
        typedarrayBytes.set(localHeapViewU8().subarray(<any>pinned_array + offset, <any>pinned_array + offset + num_of_bytes));
        return num_of_bytes;
    }
    else {
        throw new Error("Object '" + typed_array + "' is not a typed array");
    }
}


export function has_backing_array_buffer(js_obj: TypedArray): boolean {
    return typeof SharedArrayBuffer !== "undefined"
        ? js_obj.buffer instanceof ArrayBuffer || js_obj.buffer instanceof SharedArrayBuffer
        : js_obj.buffer instanceof ArrayBuffer;
}