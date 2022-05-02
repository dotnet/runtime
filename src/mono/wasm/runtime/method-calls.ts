// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root, mono_wasm_new_root_buffer, WasmRoot, WasmRootBuffer, mono_wasm_new_external_root } from "./roots";
import {
    JSHandle, MonoArray, MonoMethod, MonoObject,
    MonoObjectNull, MonoString, coerceNull as coerceNull,
    VoidPtrNull, MonoStringNull, MonoObjectRef,
    MonoStringRef
} from "./types";
import { BINDING, INTERNAL, Module, MONO, runtimeHelpers } from "./imports";
import { mono_array_root_to_js_array, unbox_mono_obj_root } from "./cs-to-js";
import { get_js_obj, mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";
// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore used by unsafe export
import { js_array_to_mono_array, js_to_mono_obj_root } from "./js-to-cs";
import {
    mono_bind_method,
    Converter, _compile_converter_for_marshal_string,
    _decide_if_result_is_marshaled, find_method,
    BoundMethodToken
} from "./method-binding";
import { conv_string, conv_string_root, js_string_to_mono_string, js_string_to_mono_string_root } from "./strings";
import cwraps from "./cwraps";
import { bindings_lazy_init } from "./startup";
import { _create_temp_frame, _release_temp_frame } from "./memory";
import { VoidPtr, Int32Ptr, EmscriptenModule } from "./types/emscripten";

function _verify_args_for_method_call(args_marshal: string/*ArgsMarshalString*/, args: any) {
    const has_args = args && (typeof args === "object") && args.length > 0;
    const has_args_marshal = typeof args_marshal === "string";

    if (has_args) {
        if (!has_args_marshal)
            throw new Error("No signature provided for method call.");
        else if (args.length > args_marshal.length)
            throw new Error("Too many parameter values. Expected at most " + args_marshal.length + " value(s) for signature " + args_marshal);
    }

    return has_args_marshal && has_args;
}

export function _get_buffer_for_method_call(converter: Converter, token: BoundMethodToken | null): VoidPtr | undefined {
    if (!converter)
        return VoidPtrNull;

    let result = VoidPtrNull;
    if (token !== null) {
        result = token.scratchBuffer || VoidPtrNull;
        token.scratchBuffer = VoidPtrNull;
    } else {
        result = converter.scratchBuffer || VoidPtrNull;
        converter.scratchBuffer = VoidPtrNull;
    }
    return result;
}

export function _get_args_root_buffer_for_method_call(converter: Converter, token: BoundMethodToken | null): WasmRootBuffer | undefined {
    if (!converter)
        return undefined;

    if (!converter.needs_root_buffer)
        return undefined;

    let result = null;
    if (token !== null) {
        result = token.scratchRootBuffer;
        token.scratchRootBuffer = null;
    } else {
        result = converter.scratchRootBuffer;
        converter.scratchRootBuffer = null;
    }

    if (result === null) {
        // TODO: Expand the converter's heap allocation and then use
        //  mono_wasm_new_root_buffer_from_pointer instead. Not that important
        //  at present because the scratch buffer will be reused unless we are
        //  recursing through a re-entrant call
        result = mono_wasm_new_root_buffer(converter.steps.length);
        // FIXME
        (<any>result).converter = converter;
    }

    return result;
}

function _release_args_root_buffer_from_method_call(
    converter?: Converter, token?: BoundMethodToken | null, argsRootBuffer?: WasmRootBuffer
) {
    if (!argsRootBuffer || !converter)
        return;

    // Store the arguments root buffer for re-use in later calls
    if (token && (token.scratchRootBuffer === null)) {
        argsRootBuffer.clear();
        token.scratchRootBuffer = argsRootBuffer;
    } else if (!converter.scratchRootBuffer) {
        argsRootBuffer.clear();
        converter.scratchRootBuffer = argsRootBuffer;
    } else {
        argsRootBuffer.release();
    }
}

function _release_buffer_from_method_call(
    converter: Converter | undefined, token?: BoundMethodToken | null, buffer?: VoidPtr
) {
    if (!converter || !buffer)
        return;

    if (token && !token.scratchBuffer)
        token.scratchBuffer = buffer;
    else if (!converter.scratchBuffer)
        converter.scratchBuffer = coerceNull(buffer);
    else if (buffer)
        Module._free(buffer);
}

function _convert_exception_for_method_call(result: WasmRoot<MonoString>, exception: WasmRoot<MonoObject>) {
    if (exception.value === MonoObjectNull)
        return null;

    const msg = conv_string_root(result);
    const err = new Error(msg!); //the convention is that invoke_method ToString () any outgoing exception
    // console.warn (`error ${msg} at location ${err.stack});
    return err;
}

/*
args_marshal is a string with one character per parameter that tells how to marshal it, here are the valid values:

i: int32
j: int32 - Enum with underlying type of int32
l: int64
k: int64 - Enum with underlying type of int64
f: float
d: double
s: string
S: interned string
o: js object will be converted to a C# object (this will box numbers/bool/promises)
m: raw mono object. Don't use it unless you know what you're doing

to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
*/
export function call_method_ref(method: MonoMethod, this_arg: WasmRoot<MonoObject> | MonoObjectRef | undefined, args_marshal: string/*ArgsMarshalString*/, args: ArrayLike<any>): any {
    // HACK: Sometimes callers pass null or undefined, coerce it to 0 since that's what wasm expects
    let this_arg_ref : MonoObjectRef | undefined = undefined;
    if (typeof (this_arg) === "number")
        this_arg_ref = this_arg;
    else if (typeof (this_arg) === "object")
        this_arg_ref = (<any>this_arg).address;
    else
        this_arg_ref = <any>coerceNull(this_arg);

    // Detect someone accidentally passing the wrong type of value to method
    if (typeof method !== "number")
        throw new Error(`method must be an address in the native heap, but was '${method}'`);
    if (!method)
        throw new Error("no method specified");
    if (typeof (this_arg_ref) !== "number")
        throw new Error(`this_arg must be a root instance, the address of a root, or undefined, but was ${this_arg}`);

    const needs_converter = _verify_args_for_method_call(args_marshal, args);

    let buffer = VoidPtrNull, converter = undefined, argsRootBuffer = undefined;
    let is_result_marshaled = true;

    // TODO: Only do this if the signature needs marshaling
    _create_temp_frame();

    // check if the method signature needs argument mashalling
    if (needs_converter) {
        converter = _compile_converter_for_marshal_string(args_marshal);

        is_result_marshaled = _decide_if_result_is_marshaled(converter, args.length);

        argsRootBuffer = _get_args_root_buffer_for_method_call(converter, null);

        const scratchBuffer = _get_buffer_for_method_call(converter, null);

        buffer = converter.compiled_variadic_function!(scratchBuffer, argsRootBuffer, method, args);
    }
    return _call_method_with_converted_args(method, <any>this_arg_ref, converter, null, buffer, is_result_marshaled, argsRootBuffer);
}


export function _handle_exception_for_call(
    converter: Converter | undefined, token: BoundMethodToken | null,
    buffer: VoidPtr, resultRoot: WasmRoot<MonoString>,
    exceptionRoot: WasmRoot<MonoObject>, argsRootBuffer?: WasmRootBuffer
): void {
    const exc = _convert_exception_for_method_call(resultRoot, exceptionRoot);
    if (!exc)
        return;

    _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
    throw exc;
}

function _handle_exception_and_produce_result_for_call(
    converter: Converter | undefined, token: BoundMethodToken | null,
    buffer: VoidPtr, resultRoot: WasmRoot<MonoString>,
    exceptionRoot: WasmRoot<MonoObject>, argsRootBuffer: WasmRootBuffer | undefined,
    is_result_marshaled: boolean
): any {
    _handle_exception_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);

    let result: any;

    if (is_result_marshaled)
        result = unbox_mono_obj_root(resultRoot);
    else
        result = resultRoot.value;

    _teardown_after_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
    return result;
}

export function _teardown_after_call(
    converter: Converter | undefined, token: BoundMethodToken | null,
    buffer: VoidPtr, resultRoot: WasmRoot<any>,
    exceptionRoot: WasmRoot<any>, argsRootBuffer?: WasmRootBuffer
): void {
    _release_temp_frame();
    _release_args_root_buffer_from_method_call(converter, token, argsRootBuffer);
    _release_buffer_from_method_call(converter, token, buffer);

    if (resultRoot) {
        resultRoot.clear();
        if ((token !== null) && (token.scratchResultRoot === null))
            token.scratchResultRoot = resultRoot;
        else
            resultRoot.release();
    }
    if (exceptionRoot) {
        exceptionRoot.clear();
        if ((token !== null) && (token.scratchExceptionRoot === null))
            token.scratchExceptionRoot = exceptionRoot;
        else
            exceptionRoot.release();
    }
}

function _call_method_with_converted_args(
    method: MonoMethod, this_arg_ref: MonoObjectRef, converter: Converter | undefined,
    token: BoundMethodToken | null, buffer: VoidPtr,
    is_result_marshaled: boolean, argsRootBuffer?: WasmRootBuffer
): any {
    const resultRoot = mono_wasm_new_root<MonoString>(), exceptionRoot = mono_wasm_new_root<MonoObject>();
    cwraps.mono_wasm_invoke_method_ref(method, this_arg_ref, buffer, exceptionRoot.address, resultRoot.address);
    return _handle_exception_and_produce_result_for_call(converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled);
}

export function call_static_method(fqn: string, args: any[], signature: string/*ArgsMarshalString*/): any {
    bindings_lazy_init();// TODO remove this once Blazor does better startup

    const method = mono_method_resolve(fqn);

    if (typeof signature === "undefined")
        signature = mono_method_get_call_signature_ref(method, undefined);

    return call_method_ref(method, undefined, signature, args);
}

export function mono_bind_static_method(fqn: string, signature?: string/*ArgsMarshalString*/): Function {
    bindings_lazy_init();// TODO remove this once Blazor does better startup

    const method = mono_method_resolve(fqn);

    if (typeof signature === "undefined")
        signature = mono_method_get_call_signature_ref(method, undefined);

    return mono_bind_method(method, null, signature!, fqn);
}

export function mono_bind_assembly_entry_point(assembly: string, signature?: string/*ArgsMarshalString*/): Function {
    bindings_lazy_init();// TODO remove this once Blazor does better startup

    const asm = cwraps.mono_wasm_assembly_load(assembly);
    if (!asm)
        throw new Error("Could not find assembly: " + assembly);

    const method = cwraps.mono_wasm_assembly_get_entry_point(asm);
    if (!method)
        throw new Error("Could not find entry point for assembly: " + assembly);

    if (!signature)
        signature = mono_method_get_call_signature_ref(method, undefined);

    return async function (...args: any[]) {
        if (args.length > 0 && Array.isArray(args[0]))
            args[0] = js_array_to_mono_array(args[0], true, false);
        return call_method_ref(method, undefined, signature!, args);
    };
}

export function mono_call_assembly_entry_point(assembly: string, args?: any[], signature?: string/*ArgsMarshalString*/): number {
    if (!args) {
        args = [[]];
    }
    return mono_bind_assembly_entry_point(assembly, signature)(...args);
}

export function mono_wasm_invoke_js_with_args_ref(js_handle: JSHandle, method_name: MonoStringRef, args: MonoObjectRef, is_exception: Int32Ptr, result_address: MonoObjectRef): any {
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
        if (!obj) {
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
        if (!obj) {
            wrap_error_root(is_exception, "ERR01: Invalid JS object handle '" + js_handle + "' while geting '" + js_name + "'", resultRoot);
            return;
        }

        const m = obj[js_name];
        js_to_mono_obj_root(m, resultRoot, true);
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
        if (!js_obj) {
            wrap_error_root(is_exception, "ERR02: Invalid JS object handle '" + js_handle + "' while setting '" + property + "'", resultRoot);
            return;
        }

        let result = false;

        const js_value = unbox_mono_obj_root(valueRoot);

        if (createIfNotExist) {
            js_obj[property] = js_value;
            result = true;
        }
        else {
            result = false;
            if (!createIfNotExist) {
                if (!Object.prototype.hasOwnProperty.call(js_obj, property)) {
                    js_to_mono_obj_root(false, resultRoot, false);
                    return;
                }
            }
            if (hasOwnProperty === true) {
                if (Object.prototype.hasOwnProperty.call(js_obj, property)) {
                    js_obj[property] = js_value;
                    result = true;
                }
            }
            else {
                js_obj[property] = js_value;
                result = true;
            }
        }
        js_to_mono_obj_root(result, resultRoot, false);
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
        if (!obj) {
            wrap_error_root(is_exception, "ERR03: Invalid JS object handle '" + js_handle + "' while getting [" + property_index + "]", resultRoot);
            return;
        }

        const m = obj[property_index];
        js_to_mono_obj_root(m, resultRoot, true);
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
        if (!obj) {
            wrap_error_root(is_exception, "ERR04: Invalid JS object handle '" + js_handle + "' while setting [" + property_index + "]", resultRoot);
            return;
        }

        const js_value = unbox_mono_obj_root(valueRoot);
        obj[property_index] = js_value;
        resultRoot.clear();
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
        else {
            globalObj = (<any>globalThis)[js_name];
        }

        // TODO returning null may be useful when probing for browser features
        if (globalObj === null || typeof globalObj === undefined) {
            wrap_error_root(is_exception, "Global object '" + js_name + "' not found.", resultRoot);
            return;
        }

        js_to_mono_obj_root(globalObj, resultRoot, true);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        nameRoot.release();
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function _wrap_error_flag(is_exception: Int32Ptr | null, ex: any): string {
    let res = "unknown exception";
    if (ex) {
        res = ex.toString();
        const stack = ex.stack;
        if (stack) {
            // Some JS runtimes insert the error message at the top of the stack, some don't,
            //  so normalize it by using the stack as the result if it already contains the error
            if (stack.startsWith(res))
                res = stack;
            else
                res += "\n" + stack;
        }

        res = INTERNAL.mono_wasm_symbolicate_string(res);
    }
    if (is_exception) {
        Module.setValue(is_exception, 1, "i32");
    }
    return res;
}

/**
 * @deprecated Not GC or thread safe
 */
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function wrap_error(is_exception: Int32Ptr | null, ex: any): MonoString {
    const res = _wrap_error_flag(is_exception, ex);
    return js_string_to_mono_string(res)!;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function wrap_error_root(is_exception: Int32Ptr | null, ex: any, result: WasmRoot<MonoObject>): void {
    const res = _wrap_error_flag(is_exception, ex);
    js_string_to_mono_string_root(res, <any>result);
}

export function mono_method_get_call_signature_ref(method: MonoMethod, mono_obj?: WasmRoot<MonoObject>): string/*ArgsMarshalString*/ {
    return call_method_ref(
        runtimeHelpers.get_call_sig_ref, undefined, "im",
        [method, mono_obj ? mono_obj.address : runtimeHelpers._null_root.address]
    );
}

export function parseFQN(fqn: string)
    : { assembly: string, namespace: string, classname: string, methodname: string } {
    const assembly = fqn.substring(fqn.indexOf("[") + 1, fqn.indexOf("]")).trim();
    fqn = fqn.substring(fqn.indexOf("]") + 1).trim();

    const methodname = fqn.substring(fqn.indexOf(":") + 1);
    fqn = fqn.substring(0, fqn.indexOf(":")).trim();

    let namespace = "";
    let classname = fqn;
    if (fqn.indexOf(".") != -1) {
        const idx = fqn.lastIndexOf(".");
        namespace = fqn.substring(0, idx);
        classname = fqn.substring(idx + 1);
    }

    if (!assembly.trim())
        throw new Error("No assembly name specified");
    if (!classname.trim())
        throw new Error("No class name specified");
    if (!methodname.trim())
        throw new Error("No method name specified");
    return { assembly, namespace, classname, methodname };
}

export function mono_method_resolve(fqn: string): MonoMethod {
    const { assembly, namespace, classname, methodname } = parseFQN(fqn);

    const asm = cwraps.mono_wasm_assembly_load(assembly);
    if (!asm)
        throw new Error("Could not find assembly: " + assembly);

    const klass = cwraps.mono_wasm_assembly_find_class(asm, namespace, classname);
    if (!klass)
        throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);

    const method = find_method(klass, methodname, -1);
    if (!method)
        throw new Error("Could not find method: " + methodname);
    return method;
}

// Blazor specific custom routine
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_wasm_invoke_js_blazor(exceptionMessage: Int32Ptr, callInfo: any, arg0: any, arg1: any, arg2: any): void | number {
    try {
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

// code like `App.call_test_method();`
export function mono_wasm_invoke_js(code: MonoString, is_exception: Int32Ptr): MonoString | null {
    if (code === MonoStringNull)
        return MonoStringNull;

    const js_code = conv_string(code)!;

    try {
        const closedEval = function (Module: EmscriptenModule, MONO: any, BINDING: any, INTERNAL: any, code: string) {
            return eval(code);
        };
        const res = closedEval(Module, MONO, BINDING, INTERNAL, js_code);
        Module.setValue(is_exception, 0, "i32");
        if (typeof res === "undefined" || res === null)
            return MonoStringNull;

        return js_string_to_mono_string(res.toString());
    } catch (ex) {
        return wrap_error(is_exception, ex);
    }
}

// TODO is this unused code ?
// Compiles a JavaScript function from the function data passed.
// Note: code snippet is not a function definition. Instead it must create and return a function instance.
// code like `return function() { App.call_test_method(); };`
export function mono_wasm_compile_function_ref(code: MonoStringRef, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const codeRoot = mono_wasm_new_external_root<MonoString>(code),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);

    const js_code = conv_string_root(codeRoot);
    if (!js_code) {
        js_to_mono_obj_root(MonoStringNull, resultRoot, true);
        return;
    }

    try {
        const closure = {
            Module, MONO, BINDING, INTERNAL
        };
        const fn_body_template = `const {Module, MONO, BINDING, INTERNAL} = __closure; ${js_code} ;`;
        const fn_defn = new Function("__closure", fn_body_template);
        const res = fn_defn(closure);
        if (!res || typeof res !== "function") {
            wrap_error_root(is_exception, "Code must return an instance of a JavaScript function. Please use `return` statement to return a function.", resultRoot);
            return;
        }
        Module.setValue(is_exception, 0, "i32");

        js_to_mono_obj_root(res, resultRoot, true);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
    }
}