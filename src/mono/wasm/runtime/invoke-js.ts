// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { marshal_exception_to_cs, bind_arg_marshal_to_cs } from "./marshal-to-cs";
import { get_signature_argument_count, bound_js_function_symbol, get_sig, get_signature_version, MarshalerType, get_signature_type, imported_js_function_symbol } from "./marshal";
import { setI32 } from "./memory";
import { conv_string_root, js_string_to_mono_string_root } from "./strings";
import { mono_assert, MonoObject, MonoObjectRef, MonoString, MonoStringRef, JSFunctionSignature, JSMarshalerArguments, WasmRoot, BoundMarshalerToJs, JSFnHandle, BoundMarshalerToCs, JSHandle } from "./types";
import { Int32Ptr } from "./types/emscripten";
import { IMPORTS, INTERNAL, Module, runtimeHelpers } from "./imports";
import { bind_arg_marshal_to_js } from "./marshal-to-js";
import { mono_wasm_new_external_root } from "./roots";
import { mono_wasm_symbolicate_string } from "./logging";
import { mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";

const fn_wrapper_by_fn_handle: Function[] = <any>[null];// 0th slot is dummy, we never free bound functions

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

        const arg_marshalers: (BoundMarshalerToJs)[] = new Array(args_count);
        const arg_cleanup: (Function | undefined)[] = new Array(args_count);
        let has_cleanup = false;
        for (let index = 0; index < args_count; index++) {
            const sig = get_sig(signature, index + 2);
            const marshaler_type = get_signature_type(sig);
            const arg_marshaler = bind_arg_marshal_to_js(sig, marshaler_type, index + 2);
            mono_assert(arg_marshaler, "ERR42: argument marshaler must be resolved");
            arg_marshalers[index] = arg_marshaler;
            if (marshaler_type === MarshalerType.Span) {
                arg_cleanup[index] = (js_arg: any) => {
                    if (js_arg) {
                        js_arg.dispose();
                    }
                };
                has_cleanup = true;
            }
        }
        const res_sig = get_sig(signature, 1);
        const res_marshaler_type = get_signature_type(res_sig);
        const res_converter = bind_arg_marshal_to_cs(res_sig, res_marshaler_type, 1);

        const closure: BindingClosure = {
            fn,
            args_count,
            arg_marshalers,
            res_converter,
            has_cleanup,
            arg_cleanup
        };
        let bound_fn: Function;
        if (args_count == 0 && !res_converter) {
            bound_fn = bind_fn_0V(closure);
        }
        else if (args_count == 1 && !has_cleanup && !res_converter) {
            bound_fn = bind_fn_1V(closure);
        }
        else if (args_count == 1 && !has_cleanup && res_converter) {
            bound_fn = bind_fn_1R(closure);
        }
        else if (args_count == 2 && !has_cleanup && res_converter) {
            bound_fn = bind_fn_2R(closure);
        }
        else {
            bound_fn = bind_fn(closure);
        }

        (<any>bound_fn)[imported_js_function_symbol] = true;
        const fn_handle = fn_wrapper_by_fn_handle.length;
        fn_wrapper_by_fn_handle.push(bound_fn);
        setI32(function_js_handle, <any>fn_handle);
    } catch (ex: any) {
        Module.printErr(ex.toString());
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
        function_name_root.release();
    }
}

function bind_fn_0V(closure: BindingClosure) {
    const fn = closure.fn;
    (<any>closure) = null;
    return function bound_fn_0V(args: JSMarshalerArguments) {
        try {
            // call user function
            fn();
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
    };
}

function bind_fn_1V(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    (<any>closure) = null;
    return function bound_fn_1V(args: JSMarshalerArguments) {
        try {
            const arg1 = marshaler1(args);
            // call user function
            fn(arg1);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
    };
}

function bind_fn_1R(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const res_converter = closure.res_converter!;
    (<any>closure) = null;
    return function bound_fn_1R(args: JSMarshalerArguments) {
        try {
            const arg1 = marshaler1(args);
            // call user function
            const js_result = fn(arg1);
            res_converter(args, js_result);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
    };
}

function bind_fn_2R(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const marshaler2 = closure.arg_marshalers[1]!;
    const res_converter = closure.res_converter!;
    (<any>closure) = null;
    return function bound_fn_2R(args: JSMarshalerArguments) {
        try {
            const arg1 = marshaler1(args);
            const arg2 = marshaler2(args);
            // call user function
            const js_result = fn(arg1, arg2);
            res_converter(args, js_result);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
    };
}

function bind_fn(closure: BindingClosure) {
    const args_count = closure.args_count;
    const arg_marshalers = closure.arg_marshalers;
    const res_converter = closure.res_converter;
    const arg_cleanup = closure.arg_cleanup;
    const has_cleanup = closure.has_cleanup;
    const fn = closure.fn;
    (<any>closure) = null;
    return function bound_fn(args: JSMarshalerArguments) {
        try {
            const js_args = new Array(args_count);
            for (let index = 0; index < args_count; index++) {
                const marshaler = arg_marshalers[index]!;
                const js_arg = marshaler(args);
                js_args[index] = js_arg;
            }

            // call user function
            const js_result = fn(...js_args);

            if (res_converter) {
                res_converter(args, js_result);
            }

            if (has_cleanup) {
                for (let index = 0; index < args_count; index++) {
                    const cleanup = arg_cleanup[index];
                    if (cleanup) {
                        cleanup(js_args[index]);
                    }
                }
            }
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
    };
}

type BindingClosure = {
    fn: Function,
    args_count: number,
    arg_marshalers: (BoundMarshalerToJs)[],
    res_converter: BoundMarshalerToCs | undefined,
    has_cleanup: boolean,
    arg_cleanup: (Function | undefined)[]
}

export function mono_wasm_invoke_bound_function(bound_function_js_handle: JSHandle, args: JSMarshalerArguments): void {
    const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
    mono_assert(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_js_function_symbol], () => `Bound function handle expected ${bound_function_js_handle}`);
    bound_fn(args);
}

export function mono_wasm_invoke_import(fn_handle: JSFnHandle, args: JSMarshalerArguments): void {
    const bound_fn = fn_wrapper_by_fn_handle[<any>fn_handle];
    mono_assert(bound_fn, () => `Imported function handle expected ${fn_handle}`);
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
