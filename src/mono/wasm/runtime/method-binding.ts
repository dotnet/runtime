// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WasmRoot, WasmRootBuffer, mono_wasm_new_root } from "./roots";
import { MonoClass, MonoMethod, MonoObject, coerceNull, VoidPtrNull, MonoType, MarshalType } from "./types";
import { BINDING, Module, runtimeHelpers } from "./imports";
import { js_to_mono_enum, _js_to_mono_obj, _js_to_mono_uri } from "./js-to-cs";
import { js_string_to_mono_string, js_string_to_mono_string_interned } from "./strings";
import { _unbox_mono_obj_root_with_known_nonprimitive_type } from "./cs-to-js";
import {
    _create_temp_frame,
    getI32, getU32, getF32, getF64,
    setI32, setU32, setF32, setF64, setI64,
} from "./memory";
import {
    _get_args_root_buffer_for_method_call, _get_buffer_for_method_call,
    _handle_exception_for_call, _teardown_after_call
} from "./method-calls";
import cwraps, { wrap_c_function } from "./cwraps";
import { VoidPtr } from "./types/emscripten";

const primitiveConverters = new Map<string, Converter>();
const _signature_converters = new Map<string, Converter>();


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
    return cwraps.mono_wasm_assembly_find_method(klass, name, n);
}

export function get_method(method_name: string): MonoMethod {
    const res = find_method(runtimeHelpers.wasm_runtime_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_namespace + "." + runtimeHelpers.runtime_classname + ":" + method_name;
    return res;
}

export function bind_runtime_method(method_name: string, signature: string): Function {
    const method = get_method(method_name);
    return mono_bind_method(method, null, signature, "BINDINGS_" + method_name);
}


// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function _create_named_function(name: string, argumentNames: string[], body: string, closure: any): Function {
    let result = null;
    let closureArgumentList: any[] | null = null;
    let closureArgumentNames = null;

    if (closure) {
        closureArgumentNames = Object.keys(closure);
        closureArgumentList = new Array(closureArgumentNames.length);
        for (let i = 0, l = closureArgumentNames.length; i < l; i++)
            closureArgumentList[i] = closure[closureArgumentNames[i]];
    }

    const constructor = _create_rebindable_named_function(name, argumentNames, body, closureArgumentNames);
    // eslint-disable-next-line prefer-spread
    result = constructor.apply(null, closureArgumentList);

    return result;
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

    let rawFunctionText = "function " + escapedFunctionIdentifier + "(" +
        argumentNames.join(", ") +
        ") {\r\n" +
        body +
        "\r\n};\r\n";

    const lineBreakRE = /\r(\n?)/g;

    rawFunctionText =
        uriPrefix + strictPrefix +
        rawFunctionText.replace(lineBreakRE, "\r\n    ") +
        `    return ${escapedFunctionIdentifier};\r\n`;

    let result = null, keys = null;

    if (closureArgNames) {
        keys = closureArgNames.concat([rawFunctionText]);
    } else {
        keys = [rawFunctionText];
    }

    result = Function.apply(Function, keys);
    return result;
}

export function _create_primitive_converters(): void {
    const result = primitiveConverters;
    result.set("m", { steps: [{}], size: 0 });
    result.set("s", { steps: [{ convert: js_string_to_mono_string.bind(BINDING) }], size: 0, needs_root: true });
    result.set("S", { steps: [{ convert: js_string_to_mono_string_interned.bind(BINDING) }], size: 0, needs_root: true });
    // note we also bind first argument to false for both _js_to_mono_obj and _js_to_mono_uri, 
    // because we will root the reference, so we don't need in-flight reference
    // also as those are callback arguments and we don't have platform code which would release the in-flight reference on C# end
    result.set("o", { steps: [{ convert: _js_to_mono_obj.bind(BINDING, false) }], size: 0, needs_root: true });
    result.set("u", { steps: [{ convert: _js_to_mono_uri.bind(BINDING, false) }], size: 0, needs_root: true });

    // result.set ('k', { steps: [{ convert: js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
    result.set("j", { steps: [{ convert: js_to_mono_enum.bind(BINDING), indirect: "i32" }], size: 8 });

    result.set("i", { steps: [{ indirect: "i32" }], size: 8 });
    result.set("l", { steps: [{ indirect: "i64" }], size: 8 });
    result.set("f", { steps: [{ indirect: "float" }], size: 8 });
    result.set("d", { steps: [{ indirect: "double" }], size: 8 });
}

function _create_converter_for_marshal_string(args_marshal: string/*ArgsMarshalString*/): Converter {
    const steps = [];
    let size = 0;
    let is_result_definitely_unmarshaled = false,
        is_result_possibly_unmarshaled = false,
        result_unmarshaled_if_argc = -1,
        needs_root_buffer = false;

    for (let i = 0; i < args_marshal.length; ++i) {
        const key = args_marshal[i];

        if (i === args_marshal.length - 1) {
            if (key === "!") {
                is_result_definitely_unmarshaled = true;
                continue;
            } else if (key === "m") {
                is_result_possibly_unmarshaled = true;
                result_unmarshaled_if_argc = args_marshal.length - 1;
            }
        } else if (key === "!")
            throw new Error("! must be at the end of the signature");

        const conv = primitiveConverters.get(key);
        if (!conv)
            throw new Error("Unknown parameter type " + key);

        const localStep = Object.create(conv.steps[0]);
        localStep.size = conv.size;
        if (conv.needs_root)
            needs_root_buffer = true;
        localStep.needs_root = conv.needs_root;
        localStep.key = key;
        steps.push(localStep);
        size += conv.size;
    }

    return {
        steps, size, args_marshal,
        is_result_definitely_unmarshaled,
        is_result_possibly_unmarshaled,
        result_unmarshaled_if_argc,
        needs_root_buffer
    };
}

function _get_converter_for_marshal_string(args_marshal: string/*ArgsMarshalString*/): Converter {
    let converter = _signature_converters.get(args_marshal);
    if (!converter) {
        converter = _create_converter_for_marshal_string(args_marshal);
        _signature_converters.set(args_marshal, converter);
    }

    return converter;
}

export function _compile_converter_for_marshal_string(args_marshal: string/*ArgsMarshalString*/): Converter {
    const converter = _get_converter_for_marshal_string(args_marshal);
    if (typeof (converter.args_marshal) !== "string")
        throw new Error("Corrupt converter for '" + args_marshal + "'");

    if (converter.compiled_function && converter.compiled_variadic_function)
        return converter;

    const converterName = args_marshal.replace("!", "_result_unmarshaled");
    converter.name = converterName;

    let body = [];
    let argumentNames = ["buffer", "rootBuffer", "method"];

    // worst-case allocation size instead of allocating dynamically, plus padding
    const bufferSizeBytes = converter.size + (args_marshal.length * 4) + 16;

    // ensure the indirect values are 8-byte aligned so that aligned loads and stores will work
    const indirectBaseOffset = ((((args_marshal.length * 4) + 7) / 8) | 0) * 8;

    const closure: any = {
        Module,
        _malloc: Module._malloc,
        mono_wasm_unbox_rooted: wrap_c_function("mono_wasm_unbox_rooted"),
        setI32,
        setU32,
        setF32,
        setF64,
        setI64
    };
    let indirectLocalOffset = 0;

    body.push(
        "if (!method) throw new Error('no method provided');",
        `if (!buffer) buffer = _malloc (${bufferSizeBytes});`,
        `let indirectStart = buffer + ${indirectBaseOffset};`,
        ""
    );

    for (let i = 0; i < converter.steps.length; i++) {
        const step = converter.steps[i];
        const closureKey = "step" + i;
        const valueKey = "value" + i;

        const argKey = "arg" + i;
        argumentNames.push(argKey);

        if (step.convert) {
            closure[closureKey] = step.convert;
            body.push(`let ${valueKey} = ${closureKey}(${argKey}, method, ${i});`);
        } else {
            body.push(`let ${valueKey} = ${argKey};`);
        }

        if (step.needs_root) {
            body.push("if (!rootBuffer) throw new Error('no root buffer provided');");
            body.push(`rootBuffer.set (${i}, ${valueKey});`);
        }

        // HACK: needs_unbox indicates that we were passed a pointer to a managed object, and either
        //  it was already rooted by our caller or (needs_root = true) by us. Now we can unbox it and
        //  pass the raw address of its boxed value into the callee.
        // FIXME: I don't think this is GC safe
        if (step.needs_unbox)
            body.push(`${valueKey} = mono_wasm_unbox_rooted (${valueKey});`);

        if (step.indirect) {
            const offsetText = `(indirectStart + ${indirectLocalOffset})`;

            switch (step.indirect) {
                case "u32":
                    body.push(`setU32(${offsetText}, ${valueKey});`);
                    break;
                case "i32":
                    body.push(`setI32(${offsetText}, ${valueKey});`);
                    break;
                case "float":
                    body.push(`setF32(${offsetText}, ${valueKey});`);
                    break;
                case "double":
                    body.push(`setF64(${offsetText}, ${valueKey});`);
                    break;
                case "i64":
                    body.push(`setI64(${offsetText}, ${valueKey});`);
                    break;
                default:
                    throw new Error("Unimplemented indirect type: " + step.indirect);
            }

            body.push(`setU32(buffer + (${i} * 4), ${offsetText});`);
            indirectLocalOffset += step.size!;
        } else {
            body.push(`setI32(buffer + (${i} * 4), ${valueKey});`);
            indirectLocalOffset += 4;
        }
        body.push("");
    }

    body.push("return buffer;");

    let bodyJs = body.join("\r\n"), compiledFunction = null, compiledVariadicFunction = null;
    try {
        compiledFunction = _create_named_function("converter_" + converterName, argumentNames, bodyJs, closure);
        converter.compiled_function = compiledFunction;
    } catch (exc) {
        converter.compiled_function = null;
        console.warn("compiling converter failed for", bodyJs, "with error", exc);
        throw exc;
    }


    argumentNames = ["existingBuffer", "rootBuffer", "method", "args"];
    const variadicClosure = {
        converter: compiledFunction
    };
    body = [
        "return converter(",
        "  existingBuffer, rootBuffer, method,"
    ];

    for (let i = 0; i < converter.steps.length; i++) {
        body.push(
            "  args[" + i +
            (
                (i == converter.steps.length - 1)
                    ? "]"
                    : "], "
            )
        );
    }

    body.push(");");

    bodyJs = body.join("\r\n");
    try {
        compiledVariadicFunction = _create_named_function("variadic_converter_" + converterName, argumentNames, bodyJs, variadicClosure);
        converter.compiled_variadic_function = compiledVariadicFunction;
    } catch (exc) {
        converter.compiled_variadic_function = null;
        console.warn("compiling converter failed for", bodyJs, "with error", exc);
        throw exc;
    }

    converter.scratchRootBuffer = null;
    converter.scratchBuffer = VoidPtrNull;

    return converter;
}

function _maybe_produce_signature_warning(converter: Converter) {
    if (converter.has_warned_about_signature)
        return;

    console.warn("MONO_WASM: Deprecated raw return value signature: '" + converter.args_marshal + "'. End the signature with '!' instead of 'm'.");
    converter.has_warned_about_signature = true;
}

export function _decide_if_result_is_marshaled(converter: Converter, argc: number): boolean {
    if (!converter)
        return true;

    if (
        converter.is_result_possibly_unmarshaled &&
        (argc === converter.result_unmarshaled_if_argc)
    ) {
        if (argc < converter.result_unmarshaled_if_argc)
            throw new Error(`Expected >= ${converter.result_unmarshaled_if_argc} argument(s) but got ${argc} for signature '${converter.args_marshal}'`);

        _maybe_produce_signature_warning(converter);
        return false;
    } else {
        if (argc < converter.steps.length)
            throw new Error(`Expected ${converter.steps.length} argument(s) but got ${argc} for signature '${converter.args_marshal}'`);

        return !converter.is_result_definitely_unmarshaled;
    }
}

export function mono_bind_method(method: MonoMethod, this_arg: MonoObject | null, args_marshal: string/*ArgsMarshalString*/, friendly_name: string): Function {
    if (typeof (args_marshal) !== "string")
        throw new Error("args_marshal argument invalid, expected string");
    this_arg = coerceNull(this_arg);

    let converter: Converter | null = null;
    if (typeof (args_marshal) === "string") {
        converter = _compile_converter_for_marshal_string(args_marshal);
    }

    // FIXME
    const unbox_buffer_size = 8192;
    const unbox_buffer = Module._malloc(unbox_buffer_size);

    const token: BoundMethodToken = {
        friendlyName: friendly_name,
        method,
        converter,
        scratchRootBuffer: null,
        scratchBuffer: VoidPtrNull,
        scratchResultRoot: mono_wasm_new_root(),
        scratchExceptionRoot: mono_wasm_new_root()
    };
    const closure: any = {
        Module,
        mono_wasm_new_root,
        _create_temp_frame,
        _get_args_root_buffer_for_method_call,
        _get_buffer_for_method_call,
        _handle_exception_for_call,
        _teardown_after_call,
        mono_wasm_try_unbox_primitive_and_get_type: wrap_c_function("mono_wasm_try_unbox_primitive_and_get_type"),
        _unbox_mono_obj_root_with_known_nonprimitive_type,
        invoke_method: wrap_c_function("mono_wasm_invoke_method"),
        method,
        this_arg,
        token,
        unbox_buffer,
        unbox_buffer_size,
        getI32,
        getU32,
        getF32,
        getF64
    };

    const converterKey = converter ? "converter_" + converter.name : "";
    if (converter)
        closure[converterKey] = converter;

    const argumentNames = [];
    const body = [
        "_create_temp_frame();",
        "let resultRoot = token.scratchResultRoot, exceptionRoot = token.scratchExceptionRoot;",
        "token.scratchResultRoot = null;",
        "token.scratchExceptionRoot = null;",
        "if (resultRoot === null)",
        "	resultRoot = mono_wasm_new_root ();",
        "if (exceptionRoot === null)",
        "	exceptionRoot = mono_wasm_new_root ();",
        ""
    ];

    if (converter) {
        body.push(
            `let argsRootBuffer = _get_args_root_buffer_for_method_call(${converterKey}, token);`,
            `let scratchBuffer = _get_buffer_for_method_call(${converterKey}, token);`,
            `let buffer = ${converterKey}.compiled_function(`,
            "    scratchBuffer, argsRootBuffer, method,"
        );

        for (let i = 0; i < converter.steps.length; i++) {
            const argName = "arg" + i;
            argumentNames.push(argName);
            body.push(
                "    " + argName +
                (
                    (i == converter.steps.length - 1)
                        ? ""
                        : ", "
                )
            );
        }

        body.push(");");

    } else {
        body.push("let argsRootBuffer = null, buffer = 0;");
    }

    if (converter && converter.is_result_definitely_unmarshaled) {
        body.push("let is_result_marshaled = false;");
    } else if (converter && converter.is_result_possibly_unmarshaled) {
        body.push(`let is_result_marshaled = arguments.length !== ${converter.result_unmarshaled_if_argc};`);
    } else {
        body.push("let is_result_marshaled = true;");
    }

    // We inline a bunch of the invoke and marshaling logic here in order to eliminate the GC pressure normally
    //  created by the unboxing part of the call process. Because unbox_mono_obj(_root) can return non-numeric
    //  types, v8 and spidermonkey allocate and store its result on the heap (in the nursery, to be fair).
    // For a bound method however, we know the result will always be the same type because C# methods have known
    //  return types. Inlining the invoke and marshaling logic means that even though the bound method has logic
    //  for handling various types, only one path through the method (for its appropriate return type) will ever
    //  be taken, and the JIT will see that the 'result' local and thus the return value of this function are
    //  always of the exact same type. All of the branches related to this end up being predicted and low-cost.
    // The end result is that bound method invocations don't always allocate, so no more nursery GCs. Yay! -kg
    body.push(
        "",
        "resultRoot.value = invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());",
        `_handle_exception_for_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
        "",
        "let resultPtr = resultRoot.value, result = undefined;"
    );

    if (converter) {
        if (converter.is_result_possibly_unmarshaled)
            body.push("if (!is_result_marshaled) ");

        if (converter.is_result_definitely_unmarshaled || converter.is_result_possibly_unmarshaled)
            body.push("    result = resultPtr;");

        if (!converter.is_result_definitely_unmarshaled)
            body.push(
                "if (is_result_marshaled && (resultPtr !== 0)) {",
                // For the common scenario where the return type is a primitive, we want to try and unbox it directly
                //  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
                //  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
                //  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
                "    let resultType = mono_wasm_try_unbox_primitive_and_get_type (resultPtr, unbox_buffer, unbox_buffer_size);",
                "    switch (resultType) {",
                `    case ${MarshalType.INT}:`,
                "        result = getI32(unbox_buffer); break;",
                `    case ${MarshalType.POINTER}:`, // FIXME: Is this right?
                `    case ${MarshalType.UINT32}:`,
                "        result = getU32(unbox_buffer); break;",
                `    case ${MarshalType.FP32}:`,
                "        result = getF32(unbox_buffer); break;",
                `    case ${MarshalType.FP64}:`,
                "        result = getF64(unbox_buffer); break;",
                `    case ${MarshalType.BOOL}:`,
                "        result = getI32(unbox_buffer) !== 0; break;",
                `    case ${MarshalType.CHAR}:`,
                "        result = String.fromCharCode(getI32(unbox_buffer)); break;",
                "    default:",
                "        result = _unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType, unbox_buffer); break;",
                "    }",
                "}"
            );
    } else {
        throw new Error("No converter");
    }

    if (friendly_name) {
        const escapeRE = /[^A-Za-z0-9_$]/g;
        friendly_name = friendly_name.replace(escapeRE, "_");
    }

    let displayName = friendly_name || ("clr_" + method);

    if (this_arg)
        displayName += "_this" + this_arg;

    body.push(
        `_teardown_after_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
        "return result;"
    );

    const bodyJs = body.join("\r\n");

    const result = _create_named_function(displayName, argumentNames, bodyJs, closure);

    return result;
}

/*
We currently don't use these types because it makes typeScript compiler very slow.

declare const enum ArgsMarshal {
    Int32 = "i", // int32
    Int32Enum = "j", // int32 - Enum with underlying type of int32
    Int64 = "l", // int64
    Int64Enum = "k", // int64 - Enum with underlying type of int64
    Float32 = "f", // float
    Float64 = "d", // double
    String = "s", // string
    Char = "s", // interned string
    JSObj = "o", // js object will be converted to a C# object (this will box numbers/bool/promises)
    MONOObj = "m", // raw mono object. Don't use it unless you know what you're doing
}

// to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
type _ExtraArgsMarshalOperators = "!" | "";

export type ArgsMarshalString = ""
    | `${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`
    | `${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${ArgsMarshal}${_ExtraArgsMarshalOperators}`;
*/

type ConverterStepIndirects = "u32" | "i32" | "float" | "double" | "i64"

export type Converter = {
    steps: {
        convert?: boolean | Function;
        needs_root?: boolean;
        needs_unbox?: boolean;
        indirect?: ConverterStepIndirects;
        size?: number;
    }[];
    size: number;
    args_marshal?: string/*ArgsMarshalString*/;
    is_result_definitely_unmarshaled?: boolean;
    is_result_possibly_unmarshaled?: boolean;
    result_unmarshaled_if_argc?: number;
    needs_root_buffer?: boolean;
    key?: string;
    name?: string;
    needs_root?: boolean;
    needs_unbox?: boolean;
    compiled_variadic_function?: Function | null;
    compiled_function?: Function | null;
    scratchRootBuffer?: WasmRootBuffer | null;
    scratchBuffer?: VoidPtr;
    has_warned_about_signature?: boolean;
    convert?: Function | null;
    method?: MonoMethod | null;
}

export type BoundMethodToken = {
    friendlyName: string;
    method: MonoMethod;
    converter: Converter | null;
    scratchRootBuffer: WasmRootBuffer | null;
    scratchBuffer: VoidPtr;
    scratchResultRoot: WasmRoot<MonoObject>;
    scratchExceptionRoot: WasmRoot<MonoObject>;
}