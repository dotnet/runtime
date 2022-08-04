// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_get_jsobj_from_js_handle, mono_wasm_get_js_handle } from "./gc-handles";
import { marshal_exception_to_cs, generate_arg_marshal_to_cs } from "./marshal-to-cs";
import { get_signature_argument_count, JavaScriptMarshalerArgSize, bound_js_function_symbol, JSMarshalerTypeSize, get_sig, JSMarshalerSignatureHeaderSize, get_signature_version, MarshalerType, get_signature_type } from "./marshal";
import { setI32 } from "./memory";
import { conv_string_root, js_string_to_mono_string_root } from "./strings";
import { mono_assert, JSHandle, MonoObject, MonoObjectRef, MonoString, MonoStringRef, JSFunctionSignature, JSMarshalerArguments, WasmRoot } from "./types";
import { Int32Ptr } from "./types/emscripten";
import { IMPORTS, INTERNAL, Module, runtimeHelpers } from "./imports";
import { generate_arg_marshal_to_js } from "./marshal-to-js";
import { mono_wasm_new_external_root } from "./roots";
import { mono_wasm_symbolicate_string } from "./debug";

export function mono_wasm_bind_js_function(function_name: MonoStringRef, module_name: MonoStringRef, signature: JSFunctionSignature, function_js_handle: Int32Ptr, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    const function_name_root = mono_wasm_new_external_root<MonoString>(function_name),
        module_name_root = mono_wasm_new_external_root<MonoString>(module_name),
        resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const version = get_signature_version(signature);
        mono_assert(version === 1, () => `Signature version ${version} mismatch.`);

        const js_function_name = conv_string_root(function_name_root)!;
        const js_module_name = conv_string_root(module_name_root)!;
        if (runtimeHelpers.diagnosticTracing) {
            console.debug(`MONO_WASM: Binding [JSImport] ${js_function_name} from ${js_module_name}`);
        }
        const fn = mono_wasm_lookup_function(js_function_name, js_module_name);
        const args_count = get_signature_argument_count(signature);

        const closure: any = { fn, marshal_exception_to_cs, signature };
        const bound_js_function_name = "_bound_js_" + js_function_name.replace(/\./g, "_");
        let body = `//# sourceURL=https://mono-wasm.invalid/${bound_js_function_name} \n`;
        let converter_names = "";


        let bodyToJs = "";
        let pass_args = "";
        for (let index = 0; index < args_count; index++) {
            const arg_offset = (index + 2) * JavaScriptMarshalerArgSize;
            const sig_offset = (index + 2) * JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize;
            const arg_name = `arg${index}`;
            const sig = get_sig(signature, index + 2);
            const { converters, call_body } = generate_arg_marshal_to_js(sig, index + 2, arg_offset, sig_offset, arg_name, closure);
            converter_names += converters;
            bodyToJs += call_body;
            if (pass_args === "") {
                pass_args += arg_name;
            } else {
                pass_args += `, ${arg_name}`;
            }
        }
        const { converters: res_converters, call_body: res_call_body, marshaler_type: res_marshaler_type } = generate_arg_marshal_to_cs(get_sig(signature, 1), 1, JavaScriptMarshalerArgSize, JSMarshalerTypeSize + JSMarshalerSignatureHeaderSize, "js_result", closure);
        converter_names += res_converters;

        body += `const { signature, fn, marshal_exception_to_cs ${converter_names} } = closure;\n`;
        body += `return function ${bound_js_function_name} (args) { try {\n`;
        // body += `console.log("${bound_js_function_name}")\n`;
        body += bodyToJs;


        if (res_marshaler_type === MarshalerType.Void) {
            body += `  const js_result = fn(${pass_args});\n`;
            body += `  if (js_result !== undefined) throw new Error('Function ${js_function_name} returned unexpected value, C# signature is void');\n`;
        }
        else if (res_marshaler_type === MarshalerType.Discard) {
            body += `  fn(${pass_args});\n`;
        }
        else {
            body += `  const js_result = fn(${pass_args});\n`;
            body += res_call_body;
        }

        for (let index = 0; index < args_count; index++) {
            const sig = get_sig(signature, index + 2);
            const marshaler_type = get_signature_type(sig);
            if (marshaler_type == MarshalerType.Span) {
                const arg_name = `arg${index}`;
                body += `  ${arg_name}.dispose();\n`;
            }
        }

        body += "} catch (ex) {\n";
        body += "  marshal_exception_to_cs(args, ex);\n";
        body += "}}";
        const factory = new Function("closure", body);
        const bound_fn = factory(closure);
        bound_fn[bound_js_function_symbol] = true;
        const bound_function_handle = mono_wasm_get_js_handle(bound_fn)!;
        setI32(function_js_handle, <any>bound_function_handle);
    } catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        function_name_root.release();
    }
}

export function mono_wasm_invoke_bound_function(bound_function_js_handle: JSHandle, args: JSMarshalerArguments): void {
    const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
    mono_assert(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_js_function_symbol], () => `Bound function handle expected ${bound_function_js_handle}`);
    bound_fn(args);
}

export function mono_wasm_set_module_imports(module_name: string, moduleImports: any) {
    importedModules.set(module_name, moduleImports);
    if (runtimeHelpers.diagnosticTracing)
        console.debug(`MONO_WASM: added module imports '${module_name}'`);
}

function mono_wasm_lookup_function(function_name: string, js_module_name: string): Function {
    mono_assert(function_name && typeof function_name === "string", "function_name must be string");

    let scope: any = IMPORTS;
    const parts = function_name.split(".");
    if (js_module_name) {
        scope = importedModules.get(js_module_name);
        mono_assert(scope, () => `ES6 module ${js_module_name} was not imported yet, please call JSHost.Import() first.`);
    }
    else if (parts[0] === "INTERNAL") {
        scope = INTERNAL;
        parts.shift();
    }
    else if (parts[0] === "globalThis") {
        scope = globalThis;
        parts.shift();
    }

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        const newscope = scope[part];
        mono_assert(newscope, () => `${part} not found while looking up ${function_name}`);
        scope = newscope;
    }

    const fname = parts[parts.length - 1];
    const fn = scope[fname];

    mono_assert(typeof (fn) === "function", () => `${function_name} must be a Function but was ${typeof fn}`);

    // if the function was already bound to some object it would stay bound to original object. That's good.
    return fn.bind(scope);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function set_property(self: any, name: string, value: any): void {
    mono_assert(self, "Null reference");
    self[name] = value;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function get_property(self: any, name: string): any {
    mono_assert(self, "Null reference");
    return self[name];
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function has_property(self: any, name: string): boolean {
    mono_assert(self, "Null reference");
    return name in self;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function get_typeof_property(self: any, name: string): string {
    mono_assert(self, "Null reference");
    return typeof self[name];
}

export function get_global_this(): any {
    return globalThis;
}

export const importedModulesPromises: Map<string, Promise<any>> = new Map();
export const importedModules: Map<string, Promise<any>> = new Map();

export async function dynamic_import(module_name: string, module_url: string): Promise<any> {
    mono_assert(module_name, "Invalid module_name");
    mono_assert(module_url, "Invalid module_name");
    let promise = importedModulesPromises.get(module_name);
    const newPromise = !promise;
    if (newPromise) {
        if (runtimeHelpers.diagnosticTracing)
            console.debug(`MONO_WASM: importing ES6 module '${module_name}' from '${module_url}'`);
        promise = import(/* webpackIgnore: true */module_url);
        importedModulesPromises.set(module_name, promise);
    }
    const module = await promise;
    if (newPromise) {
        importedModules.set(module_name, module);
        if (runtimeHelpers.diagnosticTracing)
            console.debug(`MONO_WASM: imported ES6 module '${module_name}' from '${module_url}'`);
    }
    return module;
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

        res = mono_wasm_symbolicate_string(res);
    }
    if (is_exception) {
        Module.setValue(is_exception, 1, "i32");
    }
    return res;
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function wrap_error_root(is_exception: Int32Ptr | null, ex: any, result: WasmRoot<MonoObject>): void {
    const res = _wrap_error_flag(is_exception, ex);
    js_string_to_mono_string_root(res, <any>result);
}
