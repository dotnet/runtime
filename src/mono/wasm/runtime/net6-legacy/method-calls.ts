// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import MonoWasmThreads from "consts:monoWasmThreads";
import { get_js_obj, mono_wasm_get_jsobj_from_js_handle } from "../gc-handles";
import { Module, runtimeHelpers, INTERNAL, ENVIRONMENT_IS_PTHREAD } from "../globals";
import { wrap_error_root, wrap_no_error_root } from "../invoke-js";
import { _release_temp_frame } from "../memory";
import { mono_wasm_new_external_root, mono_wasm_new_root } from "../roots";
import { find_entry_point } from "../run";
import { conv_string_root, js_string_to_mono_string_root } from "../strings";
import { JSHandle, MonoStringRef, MonoObjectRef, MonoArray, MonoString, MonoObject, is_nullish, WasmRoot } from "../types/internal";
import { Int32Ptr, VoidPtr } from "../types/emscripten";
import { mono_array_root_to_js_array, unbox_mono_obj_root } from "./cs-to-js";
import { js_array_to_mono_array, js_to_mono_obj_root } from "./js-to-cs";
import { Converter, BoundMethodToken, mono_method_resolve, mono_method_get_call_signature_ref, mono_bind_method } from "./method-binding";

const boundMethodsByFqn: Map<string, Function> = new Map();

export function _teardown_after_call(
    converter: Converter | undefined, token: BoundMethodToken | null,
    buffer: VoidPtr,
    resultRoot: WasmRoot<any>,
    exceptionRoot: WasmRoot<any>,
    thisArgRoot: WasmRoot<MonoObject>,
    sp: VoidPtr
): void {
    _release_temp_frame();
    Module.stackRestore(sp);

    if (typeof (resultRoot) === "object") {
        resultRoot.clear();
        if ((token !== null) && (token.scratchResultRoot === null))
            token.scratchResultRoot = resultRoot;
        else
            resultRoot.release();
    }
    if (typeof (exceptionRoot) === "object") {
        exceptionRoot.clear();
        if ((token !== null) && (token.scratchExceptionRoot === null))
            token.scratchExceptionRoot = exceptionRoot;
        else
            exceptionRoot.release();
    }
    if (typeof (thisArgRoot) === "object") {
        thisArgRoot.clear();
        if ((token !== null) && (token.scratchThisArgRoot === null))
            token.scratchThisArgRoot = thisArgRoot;
        else
            thisArgRoot.release();
    }
}

export function mono_bind_static_method(fqn: string, signature?: string/*ArgsMarshalString*/): Function {
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");

    const key = `${fqn}-${signature}`;
    let js_method = boundMethodsByFqn.get(key);
    if (js_method === undefined) {
        const method = mono_method_resolve(fqn);

        if (typeof signature === "undefined")
            signature = mono_method_get_call_signature_ref(method, undefined);

        js_method = mono_bind_method(method, signature!, false, fqn);
        boundMethodsByFqn.set(key, js_method);
    }
    return js_method;
}

export function mono_bind_assembly_entry_point(assembly: string, signature?: string/*ArgsMarshalString*/): Function {
    const method = find_entry_point(assembly);
    if (typeof (signature) !== "string")
        signature = mono_method_get_call_signature_ref(method, undefined);

    const js_method = mono_bind_method(method, signature!, false, "_" + assembly + "__entrypoint");

    return async function (...args: any[]) {
        if (args.length > 0 && Array.isArray(args[0]))
            args[0] = js_array_to_mono_array(args[0], true, false);
        return js_method(...args);
    };
}

export function mono_call_assembly_entry_point(assembly: string, args?: any[], signature?: string/*ArgsMarshalString*/): number {
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    if (!args) {
        args = [[]];
    }
    return mono_bind_assembly_entry_point(assembly, signature)(...args);
}

export function mono_wasm_invoke_js_with_args_ref(js_handle: JSHandle, method_name: MonoStringRef, args: MonoObjectRef, is_exception: Int32Ptr, result_address: MonoObjectRef): any {
    if (MonoWasmThreads && ENVIRONMENT_IS_PTHREAD) {
        throw new Error("Legacy interop is not supported with WebAssembly threads.");
    }
    const argsRoot = mono_wasm_new_external_root<MonoArray>(args),
        nameRoot = mono_wasm_new_external_root<MonoString>(method_name),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const js_name = conv_string_root(nameRoot);
        if (!js_name || (typeof (js_name) !== "string")) {
            wrap_error_root(is_exception, "ERR12: Invalid method name object @" + nameRoot.value, resultRoot);
            return;
        }

        const obj = get_js_obj(js_handle);
        if (is_nullish(obj)) {
            wrap_error_root(is_exception, "ERR13: Invalid JS object handle '" + js_handle + "' while invoking '" + js_name + "'", resultRoot);
            return;
        }

        const js_args = mono_array_root_to_js_array(argsRoot);

        try {
            const m = obj[js_name];
            if (typeof m === "undefined")
                throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
            const res = m.apply(obj, js_args);

            js_to_mono_obj_root(res, resultRoot, true);
            wrap_no_error_root(is_exception);
        } catch (ex) {
            wrap_error_root(is_exception, ex, resultRoot);
        }
    } finally {
        argsRoot.release();
        nameRoot.release();
        resultRoot.release();
    }
}

export function mono_wasm_get_object_property_ref(js_handle: JSHandle, property_name: MonoStringRef, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const nameRoot = mono_wasm_new_external_root<MonoString>(property_name),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const js_name = conv_string_root(nameRoot);
        if (!js_name) {
            wrap_error_root(is_exception, "Invalid property name object '" + nameRoot.value + "'", resultRoot);
            return;
        }

        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (is_nullish(obj)) {
            wrap_error_root(is_exception, "ERR01: Invalid JS object handle '" + js_handle + "' while geting '" + js_name + "'", resultRoot);
            return;
        }

        const m = obj[js_name];
        js_to_mono_obj_root(m, resultRoot, true);
        wrap_no_error_root(is_exception);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        nameRoot.release();
    }
}

export function mono_wasm_set_object_property_ref(js_handle: JSHandle, property_name: MonoStringRef, value: MonoObjectRef, createIfNotExist: boolean, hasOwnProperty: boolean, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const valueRoot = mono_wasm_new_external_root<MonoObject>(value),
        nameRoot = mono_wasm_new_external_root<MonoString>(property_name),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {

        const property = conv_string_root(nameRoot);
        if (!property) {
            wrap_error_root(is_exception, "Invalid property name object '" + property_name + "'", resultRoot);
            return;
        }

        const js_obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (is_nullish(js_obj)) {
            wrap_error_root(is_exception, "ERR02: Invalid JS object handle '" + js_handle + "' while setting '" + property + "'", resultRoot);
            return;
        }

        const js_value = unbox_mono_obj_root(valueRoot);

        if (createIfNotExist) {
            js_obj[property] = js_value;
        }
        else {
            if (!createIfNotExist) {
                if (!Object.prototype.hasOwnProperty.call(js_obj, property)) {
                    return;
                }
            }
            if (hasOwnProperty === true) {
                if (Object.prototype.hasOwnProperty.call(js_obj, property)) {
                    js_obj[property] = js_value;
                }
            }
            else {
                js_obj[property] = js_value;
            }
        }
        wrap_no_error_root(is_exception, resultRoot);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        nameRoot.release();
        valueRoot.release();
    }
}

export function mono_wasm_get_by_index_ref(js_handle: JSHandle, property_index: number, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (is_nullish(obj)) {
            wrap_error_root(is_exception, "ERR03: Invalid JS object handle '" + js_handle + "' while getting [" + property_index + "]", resultRoot);
            return;
        }

        const m = obj[property_index];
        js_to_mono_obj_root(m, resultRoot, true);
        wrap_no_error_root(is_exception);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
    }
}

export function mono_wasm_set_by_index_ref(js_handle: JSHandle, property_index: number, value: MonoObjectRef, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const valueRoot = mono_wasm_new_external_root<MonoObject>(value),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const obj = mono_wasm_get_jsobj_from_js_handle(js_handle);
        if (is_nullish(obj)) {
            wrap_error_root(is_exception, "ERR04: Invalid JS object handle '" + js_handle + "' while setting [" + property_index + "]", resultRoot);
            return;
        }

        const js_value = unbox_mono_obj_root(valueRoot);
        obj[property_index] = js_value;
        wrap_no_error_root(is_exception, resultRoot);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        valueRoot.release();
    }
}

export function mono_wasm_get_global_object_ref(global_name: MonoStringRef, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const nameRoot = mono_wasm_new_external_root<MonoString>(global_name),
        resultRoot = mono_wasm_new_external_root(result_address);
    try {
        const js_name = conv_string_root(nameRoot);

        let globalObj;

        if (!js_name) {
            globalObj = globalThis;
        }
        else if (js_name == "Module") {
            globalObj = Module;
        }
        else if (js_name == "INTERNAL") {
            globalObj = INTERNAL;
        }
        else {
            globalObj = (<any>globalThis)[js_name];
        }

        // TODO returning null may be useful when probing for browser features
        if (globalObj === null || typeof globalObj === undefined) {
            wrap_error_root(is_exception, "Global object '" + js_name + "' not found.", resultRoot);
            return;
        }

        js_to_mono_obj_root(globalObj, resultRoot, true);
        wrap_no_error_root(is_exception);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        nameRoot.release();
    }
}

// Blazor specific custom routine
export function mono_wasm_invoke_js_blazor(exceptionMessage: Int32Ptr, callInfo: any, arg0: any, arg1: any, arg2: any): void | number {
    try {
        if (MonoWasmThreads) {
            throw new Error("Legacy interop is not supported with WebAssembly threads.");
        }
        const blazorExports = (<any>globalThis).Blazor;
        if (!blazorExports) {
            throw new Error("The blazor.webassembly.js library is not loaded.");
        }

        return blazorExports._internal.invokeJSFromDotNet(callInfo, arg0, arg1, arg2);
    } catch (ex: any) {
        const exceptionJsString = ex.message + "\n" + ex.stack;
        const exceptionRoot = mono_wasm_new_root<MonoString>();
        js_string_to_mono_string_root(exceptionJsString, exceptionRoot);
        exceptionRoot.copy_to_address(<any>exceptionMessage);
        exceptionRoot.release();
        return 0;
    }
}
