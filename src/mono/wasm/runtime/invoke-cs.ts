// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, runtimeHelpers } from "./imports";
import { generate_arg_marshal_to_cs } from "./marshal-to-cs";
import { marshal_exception_to_js, generate_arg_marshal_to_js } from "./marshal-to-js";
import {
    JavaScriptMarshalerArgSize,
    JSMarshalerTypeSize, JSMarshalerSignatureHeaderSize,
    get_arg, get_sig,
    get_signature_argument_count, is_args_exception, bound_cs_function_symbol, get_signature_version, MarshalerType, alloc_stack_frame,
} from "./marshal";
import { mono_wasm_new_external_root } from "./roots";
import { conv_string, conv_string_root } from "./strings";
import { mono_assert, MonoObjectRef, MonoStringRef, MonoString, MonoObject, MonoMethod, JSMarshalerArguments, JSFunctionSignature } from "./types";
import { Int32Ptr } from "./types/emscripten";
import cwraps from "./cwraps";
import { assembly_load } from "./class-loader";
import { wrap_error_root } from "./invoke-js";

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

        if (runtimeHelpers.diagnosticTracing) {
            console.debug(`MONO_WASM: Binding [JSExport] ${js_fqn}`);
        }

        const { assembly, namespace, classname, methodname } = parseFQN(js_fqn);

        const asm = assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);

        const klass = cwraps.mono_wasm_assembly_find_class(asm, namespace, classname);
        if (!klass)
            throw new Error("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);

        const wrapper_name = `__Wrapper_${methodname}_${signature_hash}`;
        const method = cwraps.mono_wasm_assembly_find_method(klass, wrapper_name, -1);
        if (!method)
            throw new Error(`Could not find method: ${wrapper_name} in ${klass} [${assembly}]`);

        const closure: any = {
            method, signature,
            stackSave: anyModule.stackSave, stackRestore: anyModule.stackRestore,
            alloc_stack_frame,
            invoke_method_and_handle_exception
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

        body += `const { method, signature, stackSave, stackRestore,  alloc_stack_frame, invoke_method_and_handle_exception ${converter_names} } = closure;\n`;
        // TODO named arguments instead of arguments keyword
        body += `return function ${bound_js_function_name} () {\n`;
        body += "const sp = stackSave();\n";
        body += "try {\n";
        body += `  const args = alloc_stack_frame(${(args_count + 2)});\n`;

        body += bodyToCs;

        body += "  invoke_method_and_handle_exception(method, args);\n";
        if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
            body += res_call_body;
        }

        if (res_marshaler_type !== MarshalerType.Void && res_marshaler_type !== MarshalerType.Discard) {
            body += "  return js_result;\n";
        }

        body += "} finally {\n";
        body += "  stackRestore(sp);\n";
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

export function invoke_method_and_handle_exception(method: MonoMethod, args: JSMarshalerArguments): void {
    const fail = cwraps.mono_wasm_invoke_method_bound(method, args);
    if (fail) throw new Error("ERR24: Unexpected error: " + conv_string(fail));
    if (is_args_exception(args)) {
        const exc = get_arg(args, 0);
        throw marshal_exception_to_js(exc);
    }
}

export const exportsByAssembly: Map<string, any> = new Map();
function _walk_exports_to_set_function(assembly: string, namespace: string, classname: string, methodname: string, signature_hash: number, fn: Function): void {
    const parts = `${namespace}.${classname}`.replace(/\//g, ".").split(".");
    let scope: any = undefined;
    let assemblyScope = exportsByAssembly.get(assembly);
    if (!assemblyScope) {
        assemblyScope = {};
        exportsByAssembly.set(assembly, assemblyScope);
        exportsByAssembly.set(assembly + ".dll", assemblyScope);
    }
    scope = assemblyScope;
    for (let i = 0; i < parts.length; i++) {
        const part = parts[i];
        if (part != "") {
            let newscope = scope[part];
            if (typeof newscope === "undefined") {
                newscope = {};
                scope[part] = newscope;
            }
            mono_assert(newscope, () => `${part} not found while looking up ${classname}`);
            scope = newscope;
        }
    }

    if (!scope[methodname]) {
        scope[methodname] = fn;
    }
    scope[`${methodname}.${signature_hash}`] = fn;
}

export async function mono_wasm_get_assembly_exports(assembly: string): Promise<any> {
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    const result = exportsByAssembly.get(assembly);
    if (!result) {
        const asm = assembly_load(assembly);
        if (!asm)
            throw new Error("Could not find assembly: " + assembly);
        cwraps.mono_wasm_runtime_run_module_cctor(asm);
    }

    return exportsByAssembly.get(assembly) || {};
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
        throw new Error("No assembly name specified " + fqn);
    if (!classname.trim())
        throw new Error("No class name specified " + fqn);
    if (!methodname.trim())
        throw new Error("No method name specified " + fqn);
    return { assembly, namespace, classname, methodname };
}