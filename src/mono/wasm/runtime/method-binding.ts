// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WasmRoot, WasmRootBuffer, mono_wasm_new_root } from "./roots";
import { Module, runtimeHelpers } from "./imports";
import { js_to_mono_enum, _js_to_mono_obj, _js_to_mono_uri } from "./js-to-cs";
import { _unbox_mono_obj_root_with_known_nonprimitive_type } from "./cs-to-js";
import {
    MonoClass, MonoMethod, MonoObject, coerceNull, MonoString, MonoObjectNull,
    VoidPtrNull, MonoType, MarshalSignatureInfo, MonoTypeNull
} from "./types";
import { js_string_to_mono_string, js_string_to_mono_string_interned, conv_string } from "./strings";
import {
    _create_temp_frame,
    getI32, getU32, getF32, getF64,
    setI32, setU32, setF32, setF64, setI64,
} from "./memory";
import { _pick_automatic_converter_for_type } from "./custom-marshaler";
import {
    _get_args_root_buffer_for_method_call, _get_buffer_for_method_call,
    _handle_exception_for_call, _teardown_after_call,
    _convert_exception_for_method_call,
} from "./method-calls";
import cwraps from "./cwraps";
import { VoidPtr } from "./types/emscripten";
import cswraps from "./corebindings";

const _signature_converters = new Map<string, SignatureConverter | Map<MonoMethod, SignatureConverter>>();
const _method_descriptions = new Map<MonoMethod, string>();
const _method_signature_info_table = new Map<MonoMethod, MarshalSignatureInfo>();
const _bound_method_cache = new Map<string, Function>();

export const bindings_named_closures = new Map<string, any>();
let bindings_named_closures_initialized = false;

export function _get_type_name(typePtr: MonoType): string {
    if (!typePtr)
        return "<null>";
    return cwraps.mono_wasm_get_type_name(typePtr);
}

export function _get_type_aqn(typePtr: MonoType): string {
    if (!typePtr)
        return "<null>";
    return cwraps.mono_wasm_get_type_aqn(typePtr);
}

export function _get_class_name(classPtr: MonoClass): string {
    if (!classPtr)
        return "<null>";
    return cwraps.mono_wasm_get_type_name(cwraps.mono_wasm_class_get_type(classPtr));
}

export function find_method(klass: MonoClass, name: string, n: number): MonoMethod {
    const result = cwraps.mono_wasm_assembly_find_method(klass, name, n);
    if (result) {
        _method_descriptions.set(result, name);
    }
    return result;
}

export function get_method(method_name: string): MonoMethod {
    const res = find_method(runtimeHelpers.wasm_runtime_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_namespace + "." + runtimeHelpers.runtime_classname + ":" + method_name;
    return res;
}

export function bind_runtime_method(method_name: string, signature: ArgsMarshalString): Function {
    const method = get_method(method_name);
    return mono_bind_method(method, null, signature, "BINDINGS_" + method_name);
}


// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _create_named_function(name: string, argumentNames: string[], body: string, closure: any): Function {
    let closureArgumentNames = null;

    if (closure)
        closureArgumentNames = Object.keys(closure);

    const constructor = _create_rebindable_named_function(name, argumentNames, body, closureArgumentNames);
    return constructor(closure);
}

export function _create_rebindable_named_function(name: string, argumentNames: string[], body: string, closureArgNames: string[] | null): Function {
    const strictPrefix = "\"use strict\";\r\n";
    let uriPrefix = "", escapedFunctionIdentifier = "";

    if (name) {
        uriPrefix = "//# sourceURL=https://mono-wasm.invalid/" + name + "\r\n";
        escapedFunctionIdentifier = name;
    } else {
        escapedFunctionIdentifier = "unnamed";
    }

    let closurePrefix = "";
    if (closureArgNames) {
        for (let i = 0; i < closureArgNames.length; i++) {
            const argName = closureArgNames[i];
            closurePrefix += `const ${argName} = __closure__.${argName};\r\n`;
        }
        closurePrefix += "\r\n";
    }


    let rawFunctionText = "function " + escapedFunctionIdentifier + "(" +
        argumentNames.join(", ") +
        ") {\r\n";

    rawFunctionText += body + "\r\n";

    const lineBreakRE = /\r(\n?)/g;

    rawFunctionText =
        uriPrefix + strictPrefix + closurePrefix +
        rawFunctionText.replace(lineBreakRE, "\r\n    ") +
        `};\r\nreturn ${escapedFunctionIdentifier};\r\n`;

    /*
    console.log(rawFunctionText);
    console.log("");
    */

    return new Function("__closure__", rawFunctionText);
}

export function get_method_signature_info (typePtr : MonoType, methodPtr : MonoMethod) : MarshalSignatureInfo {
    if (!methodPtr)
        throw new Error("Method ptr not provided");

    let result = _method_signature_info_table.get(methodPtr);
    const classMismatch = !!result && (result.typePtr !== typePtr);
    if (!result) {
        const typeName = _get_type_name(typePtr);
        const json = cswraps.make_marshal_signature_info(typePtr, methodPtr);
        if (!json)
            throw new Error(`MakeMarshalSignatureInfo failed for type ${typeName}`);

        result = <MarshalSignatureInfo>JSON.parse(json);
        result.typePtr = typePtr;

        if (classMismatch)
            console.log("WARNING: Class ptr mismatch for signature info, so caching is disabled");
        else
            _method_signature_info_table.set(methodPtr, result);
    }
    return result;
}

function _get_converter_for_marshal_string(typePtr: MonoType, method: MonoMethod, args_marshal: ArgsMarshalString): SignatureConverter | undefined {
    let converter = _signature_converters.get(args_marshal);
    let map : Map<MonoMethod, SignatureConverter> | null = null;
    if (converter instanceof Map) {
        map = converter;
        converter = map.get(method);
    }
    return converter;
}

function _setSpan (offset : VoidPtr, span : Array<number>) : void {
    if (!Array.isArray(span) || (span.length !== 2))
        throw new Error(`Span must be an array of shape [offset, length_in_elements] but was ${span}`);
    setU32(offset, span[0]);
    setU32(<any>offset + 4, span[1]);
}

function _bindingsError (message : string) {
    throw new Error(message);
}

function _generate_args_marshaler (typePtr: MonoType, method: MonoMethod, args_marshal: ArgsMarshalString): string {
    const argsRoot = mono_wasm_new_root<MonoString>(),
        resultRoot = mono_wasm_new_root<MonoObject>(),
        exceptionRoot = mono_wasm_new_root<MonoObject>();
    const generatorMethod = get_method("GenerateArgsMarshaler");
    const buffer = <number><any>Module._malloc(64);

    try {
        argsRoot.value = js_string_to_mono_string(args_marshal);

        // Manually assemble an arguments buffer
        // (RuntimeTypeHandle, RuntimeMethodHandle, string)
        setU32(buffer + 16, typePtr);
        setU32(buffer + 32, method);
        setU32(buffer + 0, buffer + 16);
        setU32(buffer + 4, buffer + 32);
        setU32(buffer + 8, argsRoot.value);

        // Invoke the managed method
        resultRoot.value = cwraps.mono_wasm_invoke_method(generatorMethod, MonoObjectNull, <VoidPtr><any>buffer, <VoidPtr><any>exceptionRoot.get_address());
        // If it threw an exception, this will yield us a JS Error instance to throw
        const exc = _convert_exception_for_method_call(<MonoString>resultRoot.value, exceptionRoot.value);
        if (exc)
            throw exc;
        // Otherwise it returned a managed String containing the JS for our new function
        return <string>conv_string(<MonoString>resultRoot.value);
    } finally {
        resultRoot.release();
        exceptionRoot.release();
        argsRoot.release();
        Module._free(<VoidPtr><any>buffer);
    }
}

function _generate_bound_method (typePtr: MonoType, method: MonoMethod, args_marshal: ArgsMarshalString, friendly_name: string): string {
    const argsRoot = mono_wasm_new_root<MonoString>(),
        nameRoot = mono_wasm_new_root<MonoString>(),
        resultRoot = mono_wasm_new_root<MonoObject>(),
        exceptionRoot = mono_wasm_new_root<MonoObject>();
    const generatorMethod = get_method("GenerateBoundMethod");
    const buffer = <number><any>Module._malloc(64);

    try {
        argsRoot.value = js_string_to_mono_string(args_marshal);
        nameRoot.value = js_string_to_mono_string(friendly_name);

        // Manually assemble an arguments buffer
        // (RuntimeTypeHandle, RuntimeMethodHandle, string)
        setU32(buffer + 16, typePtr);
        setU32(buffer + 32, method);
        setU32(buffer + 0, buffer + 16);
        setU32(buffer + 4, buffer + 32);
        setU32(buffer + 8, argsRoot.value);
        setU32(buffer + 12, nameRoot.value);

        // Invoke the managed method
        resultRoot.value = cwraps.mono_wasm_invoke_method(generatorMethod, MonoObjectNull, <VoidPtr><any>buffer, <VoidPtr><any>exceptionRoot.get_address());
        // If it threw an exception, this will yield us a JS Error instance to throw
        const exc = _convert_exception_for_method_call(<MonoString>resultRoot.value, exceptionRoot.value);
        if (exc)
            throw exc;
        // Otherwise it returned a managed String containing the JS for our new function
        return <string>conv_string(<MonoString>resultRoot.value);
    } finally {
        resultRoot.release();
        exceptionRoot.release();
        nameRoot.release();
        argsRoot.release();
        Module._free(<VoidPtr><any>buffer);
    }
}

function _initialize_bindings_named_closures () {
    // HACK: Populate the lookup table used by compiled closures
    const closure: any = {
        _create_temp_frame,
        _error: _bindingsError,
        _get_args_root_buffer_for_method_call,
        _get_buffer_for_method_call,
        _handle_exception_for_call,
        _js_to_mono_obj,
        _js_to_mono_uri,
        _malloc: Module._malloc,
        _pick_automatic_converter_for_type,
        _setSpan,
        _teardown_after_call,
        _unbox_mono_obj_root_with_known_nonprimitive_type,
        invoke_method: cwraps.mono_wasm_invoke_method,
        js_string_to_mono_string_interned,
        js_string_to_mono_string,
        js_to_mono_enum,
        mono_wasm_new_root,
        mono_wasm_try_unbox_primitive_and_get_type: cwraps.mono_wasm_try_unbox_primitive_and_get_type,
        mono_wasm_unbox_rooted: cwraps.mono_wasm_unbox_rooted,
        getF32,
        getF64,
        getI32,
        getU32,
        setF32,
        setF64,
        setI32,
        setI64,
        setU32,
    };
    for (const k in closure)
        bindings_named_closures.set(k, closure[k]);
}

function _get_api (key: string): Function {
    if (!bindings_named_closures_initialized) {
        bindings_named_closures_initialized = true;
        _initialize_bindings_named_closures();
    }

    const result = bindings_named_closures.get(key);
    if (!result || typeof(result) !== "function")
        throw new Error(`Expected ${key} to be a function but was '${result}'`);
    return result;
}

export function _compile_converter_for_marshal_string(typePtr: MonoType, method: MonoMethod, args_marshal: ArgsMarshalString): SignatureConverter {
    const converter = _get_converter_for_marshal_string(typePtr, method, args_marshal);
    if (converter && converter.compiled_function && converter.compiled_variadic_function)
        return converter;

    let csFuncResult : any = null;
    // HACK: We invoke this method directly instead of using the cswraps. version, since that wrapper relies on this function
    const js = _generate_args_marshaler(typePtr, method, args_marshal);
    const csFunc = new Function("get_api", "get_type_converter", js);
    csFuncResult = csFunc(_get_api, _pick_automatic_converter_for_type);
    return csFuncResult;
}

export function mono_bind_method(method: MonoMethod, this_arg: MonoObject | null, args_marshal: ArgsMarshalString, friendly_name: string): Function {
    if (typeof (args_marshal) !== "string")
        throw new Error("args_marshal argument invalid, expected string");
    this_arg = coerceNull(this_arg);

    // We implement a simple lookup cache here to prevent repeated bind_method calls on the same target
    //  from exhausting the set of available scratch roots. This is mostly useful for automated tests,
    //  but it may also save some naive callers from rare runtime failures
    const cacheKey = `m${method}_a${args_marshal}`;
    if (!this_arg) {
        if (_bound_method_cache.has(cacheKey)) {
            const cacheHit = _bound_method_cache.get(cacheKey);
            return <Function>cacheHit;
        }
    }

    // FIXME
    const unboxBufferSize = 8192;

    const token: BoundMethodToken = {
        method,
        converter: null, // Initialized later
        unboxBuffer: Module._malloc(unboxBufferSize),
        unboxBufferSize,
        // We shove this_arg into a root ASAP since our invokes below could cause
        //  a GC to occur and move the object
        thisArgRoot: (<any>this_arg | 0) !== 0 ? mono_wasm_new_root(this_arg) : null,
        scratchRootBuffer: null,
        scratchBuffer: VoidPtrNull,
        scratchResultRoot: mono_wasm_new_root(),
        scratchExceptionRoot: mono_wasm_new_root()
    };

    let typePtr : MonoType = MonoTypeNull;

    let converter: SignatureConverter | null = null;
    if (typeof (args_marshal) === "string") {
        const classPtr = cwraps.mono_wasm_get_class_for_bind_or_invoke(this_arg, method);
        if (!classPtr)
            throw new Error(`Could not get class ptr for bind_method with this (${this_arg}) and method (${method})`);
        typePtr = cwraps.mono_wasm_class_get_type(classPtr);
        converter = _compile_converter_for_marshal_string(typePtr, method, args_marshal);
    }
    token.converter = converter;

    if (friendly_name) {
        const escapeRE = /[^A-Za-z0-9_$]/g;
        friendly_name = friendly_name.replace(escapeRE, "_");
    }

    const bodyJs = _generate_bound_method(typePtr, method, args_marshal, friendly_name);
    const ctor = new Function("get_api", "token", bodyJs);
    const result = ctor(_get_api, token);

    // HACK: If the bound method has a this-arg, we don't want to store it into the cache
    //  since this indicates that the caller may be binding lots of methods onto instances
    if (!this_arg)
        _bound_method_cache.set(cacheKey, result);

    return result;
}

declare const enum ArgsMarshal {
    Int32 = "i", // int32
    Int32Enum = "j", // int32 - Enum with underlying type of int32
    Int64 = "l", // int64
    Int64Enum = "k", // int64 - Enum with underlying type of int64
    Float32 = "f", // float
    Float64 = "d", // double
    String = "s", // string
    InternedString = "S", // interned string
    Uri = "u",
    JSObj = "o", // js object will be converted to a C# object (this will box numbers/bool/promises)
    MONOObj = "m", // raw mono object. Don't use it unless you know what you're doing
    Auto = "a", // the bindings layer will select an appropriate converter based on the C# method signature
    ByteSpan = "b", // Span<byte>
}

// to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
type _ExtraArgsMarshalOperators = "!" | "";

// TODO make this more efficient so we can add more parameters (currently it only checks up to 4). One option is to add a
// blank to the ArgsMarshal enum but that doesn't solve the TS limit of number of options in 1 type
// Take the marshaling enums and convert to all the valid strings for type checking.
export type ArgsMarshalString = ""
    | `${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`;

export type TypeConverter = {
    needs_unbox: boolean;
    needs_root: boolean;
    convert: Function;
}

export type SignatureConverter = {
    arg_count: number;
    size: number;
    args_marshal?: ArgsMarshalString;
    is_result_definitely_unmarshaled?: boolean;
    needs_root_buffer?: boolean;
    key?: string;
    name?: string;
    compiled_variadic_function?: Function | null;
    compiled_function?: Function | null;
    scratchRootBuffer?: WasmRootBuffer | null;
    scratchBuffer?: VoidPtr;
    has_warned_about_signature?: boolean;
    method?: MonoMethod | null;
    root_buffer_size?: number;
}

export type BoundMethodToken = {
    method: MonoMethod;
    converter: SignatureConverter | null;
    scratchRootBuffer: WasmRootBuffer | null;
    scratchBuffer: VoidPtr;
    unboxBuffer: VoidPtr;
    unboxBufferSize: number;
    thisArgRoot: WasmRoot<MonoObject> | null;
    scratchResultRoot: WasmRoot<MonoObject>;
    scratchExceptionRoot: WasmRoot<MonoObject>;
}