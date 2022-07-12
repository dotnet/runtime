// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { MonoObject, MonoString } from "./export-types";
import { EXPORTS, Module, runtimeHelpers } from "./imports";
import { generate_arg_marshal_to_cs } from "./marshal-to-cs";
import { marshal_exception_to_js, generate_arg_marshal_to_js } from "./marshal-to-js";
import {
    JSMarshalerArguments, JavaScriptMarshalerArgSize, JSFunctionSignature,
    JSMarshalerTypeSize, JSMarshalerSignatureHeaderSize,
    get_arg, get_sig, set_arg_type,
    get_signature_argument_count, is_args_exception, bound_cs_function_symbol, get_signature_version, MarshalerType,
} from "./marshal";
import { parseFQN, wrap_error_root } from "./method-calls";
import { mono_wasm_new_external_root, mono_wasm_new_root } from "./roots";
import { conv_string, conv_string_root } from "./strings";
import { mono_assert, MonoObjectRef, MonoStringRef } from "./types";
import { Int32Ptr } from "./types/emscripten";
import cwraps, { wrap_c_function } from "./cwraps";
import { find_method } from "./method-binding";
import { assembly_load } from "./class-loader";

const exportedMethods = new Map<string, Function>();

export function mono_wasm_bind_cs_function(fully_qualified_name: MonoStringRef, signature_hash: number, signature: JSFunctionSignature, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const fqn_root = mono_wasm_new_external_root<MonoString>(fully_qualified_name), resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    const anyModule = Module as any;
    try {
        const version = get_signature_version(signature);
        mono_assert(version === 1, () => `Signature version ${version} mismatch.`);

        const args_count = get_signature_argument_count(signature);
        const js_fqn = conv_string_root(fqn_root)!;
        mono_assert(js_fqn, "fully_qualified_name must be string");

        if (runtimeHelpers.config.diagnostic_tracing) {
            console.trace(`MONO_WASM: Binding [JSExport] ${js_fqn}`);
        }

        const { assembly, namespace, classname, methodname } = parseFQN(js_fqn);

        const asm = assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);

        const klass = cwraps.mono_wasm_assembly_find_class(asm, namespace, classname);
        if (!klass)
            throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);

        const wrapper_name = `__Wrapper_${methodname}_${signature_hash}`;
        const method = find_method(klass, wrapper_name, -1);
        if (!method)
            throw new Error(`Could not find method: ${wrapper_name} in ${klass} [${assembly}]`);

        const closure: any = {
            method, get_arg, signature,
            stackSave: anyModule.stackSave, stackAlloc: anyModule.stackAlloc, stackRestore: anyModule.stackRestore,
            conv_string,
            mono_wasm_new_root, init_void, init_result, /*init_argument,*/ marshal_exception_to_js, is_args_exception,
            mono_wasm_invoke_method_bound: wrap_c_function("mono_wasm_invoke_method_bound"),
        };
        const bound_js_function_name = "_bound_cs_" + `${namespace}_${classname}_${methodname}`.replace(/\./g, "_").replace(/\//g, "_");
        let body = `//# sourceURL=https://mono-wasm.invalid/${bound_js_function_name} \n`;
        let bodyToCs = "";
        let converter_names = "";

        for (let index = 0; index < args_count; index++) {
            const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
            const sig_offset = (index + 2) * JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize;
            const sig = get_sig(signature, index + 2);
            const { converters, call_body } = generate_arg_marshal_to_cs(sig, index + 2, arg_offset, sig_offset, `arguments[${index}]`, closure);
            converter_names += converters;
            bodyToCs += call_body;
        }
        const { converters: res_converters, call_body: res_call_body, marshaler_type: res_marshaler_type } = generate_arg_marshal_to_js(get_sig(signature, 1), 1, JavaScriptMarshalerArgSize, JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize, "js_result", closure);
        converter_names += res_converters;

        body += `const { method, get_arg, signature, stackSave, stackAlloc, stackRestore, mono_wasm_new_root, conv_string, init_void, init_result, init_argument, marshal_exception_to_js, is_args_exception, mono_wasm_invoke_method_bound ${converter_names} } = closure;\n`;
        // TODO named arguments instead of arguments keyword
        body += `return function ${bound_js_function_name} () {\n`;
        if (res_marshaler_type === MarshalerType.String) {
            body += "let root = null;\n";
        }
        body += "const sp = stackSave();\n";
        body += "try {\n";
        body += `  const args = stackAlloc(${(args_count + 2) * JavaScriptMarshalerArgSize});\n`;
        if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
            if (res_marshaler_type === MarshalerType.String) {
                body += "  root = mono_wasm_new_root(0);\n";
                body += "  init_result(args);\n";
            }
            else {
                body += "  init_result(args);\n";
            }
        } else {
            body += "  init_void(args);\n";
        }

        body += bodyToCs;

        body += "  const fail = mono_wasm_invoke_method_bound(method, args);\n";
        body += "  if (fail) throw new Error(\"ERR22: Unexpected error: \" + conv_string(fail));\n";
        body += "  if (is_args_exception(args)) throw marshal_exception_to_js(get_arg(args, 0));\n";
        if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
            body += res_call_body;
        }

        if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
            body += "return js_result;\n";
        }

        body += "} finally {\n";
        body += "  stackRestore(sp);\n";
        if (res_marshaler_type === MarshalerType.String) {
            body += "  if(root) root.release()\n";
        }
        body += "}}";
        const factory = new Function("closure", body);
        const bound_fn = factory(closure);
        bound_fn[bound_cs_function_symbol] = true;

        exportedMethods.set(js_fqn, bound_fn);
        _walk_exports_to_set_function(assembly, namespace, classname, methodname, signature_hash, bound_fn);
    }
    catch (ex: any) {
        Module.printErr(ex.toString());
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        fqn_root.release();
    }
}

function init_void(args: JSMarshalerArguments) {
    mono_assert(args && (<any>args) % 8 == 0, "Arg alignment");
    const exc = get_arg(args, 0);
    set_arg_type(exc, MarshalerType.None);

    const res = get_arg(args, 1);
    set_arg_type(res, MarshalerType.None);
}

function init_result(args: JSMarshalerArguments) {
    mono_assert(args && (<any>args) % 8 == 0, "Arg alignment");
    const exc = get_arg(args, 0);
    set_arg_type(exc, MarshalerType.None);

    const res = get_arg(args, 1);
    set_arg_type(res, MarshalerType.None);
}

export const exportsByAssembly: Map<string, any> = new Map();

function _walk_exports_to_set_function(assembly: string, namespace: string, classname: string, methodname: string, signature_hash: number, fn: Function): void {
    let scope: any = EXPORTS;
    const parts = `${namespace}.${classname}`.replace(/\//g, ".").split(".");

    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        let newscope = scope[part];
        if (!newscope) {
            newscope = {};
            scope[part] = newscope;
        }
        mono_assert(newscope, () => `${part} not found while looking up ${classname}`);
        scope = newscope;
    }

    if (!scope[methodname]) {
        scope[methodname] = fn;
    }
    scope[`${methodname}.${signature_hash}`] = fn;

    // do it again for per assemly scope
    let assemblyScope = exportsByAssembly.get(assembly);
    if (!assemblyScope) {
        assemblyScope = {};
        exportsByAssembly.set(assembly, assemblyScope);
        exportsByAssembly.set(assembly + ".dll", assemblyScope);
    }
    scope = assemblyScope;
    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        let newscope = scope[part];
        if (!newscope) {
            newscope = {};
            scope[part] = newscope;
        }
        mono_assert(newscope, () => `${part} not found while looking up ${classname}`);
        scope = newscope;
    }

    if (!scope[methodname]) {
        scope[methodname] = fn;
    }
    scope[`${methodname}.${signature_hash}`] = fn;
}

export async function mono_wasm_get_assembly_exports(assembly: string): Promise<any> {
    const asm = assembly_load(assembly);
    if (!asm)
        throw new Error("Could not find assembly: " + assembly);
    cwraps.mono_wasm_runtime_run_module_cctor(asm);

    return exportsByAssembly.get(assembly) || {};
}