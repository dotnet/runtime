// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { marshal_exception_to_cs, bind_arg_marshal_to_cs } from "./marshal-to-cs";
import { get_signature_argument_count, bound_js_function_symbol, get_sig, get_signature_version, get_signature_type, imported_js_function_symbol, get_signature_handle, get_signature_function_name, get_signature_module_name, is_receiver_should_free } from "./marshal";
import { setI32_unchecked, receiveWorkerHeapViews, forceThreadMemoryViewRefresh } from "./memory";
import { stringToMonoStringRoot } from "./strings";
import { MonoObject, MonoObjectRef, JSFunctionSignature, JSMarshalerArguments, WasmRoot, BoundMarshalerToJs, JSFnHandle, BoundMarshalerToCs, JSHandle, MarshalerType } from "./types/internal";
import { Int32Ptr } from "./types/emscripten";
import { INTERNAL, Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { bind_arg_marshal_to_js } from "./marshal-to-js";
import { mono_wasm_new_external_root } from "./roots";
import { mono_log_debug, mono_wasm_symbolicate_string } from "./logging";
import { mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { wrap_as_cancelable_promise } from "./cancelable-promise";

export const js_import_wrapper_by_fn_handle: Function[] = <any>[null];// 0th slot is dummy, main thread we free them on shutdown. On web worker thread we free them when worker is detached.

export function mono_wasm_bind_js_import(signature: JSFunctionSignature, is_exception: Int32Ptr, result_address: MonoObjectRef): void {
    if (WasmEnableThreads) return;
    assert_js_interop();
    const resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        bind_js_import(signature);
        wrap_no_error_root(is_exception, resultRoot);
    } catch (ex: any) {
        Module.err(ex.toString());
        wrap_error_root(is_exception, ex, resultRoot);
    } finally {
        resultRoot.release();
    }
}

export function mono_wasm_invoke_jsimport(signature: JSFunctionSignature, args: JSMarshalerArguments) {
    if (!WasmEnableThreads) return;
    assert_js_interop();

    const function_handle = get_signature_handle(signature);

    let bound_fn = js_import_wrapper_by_fn_handle[function_handle];
    if (bound_fn == undefined) {
        // it was not bound yet, let's do it now
        bound_fn = bind_js_import(signature);
    }
    mono_assert(bound_fn, () => `Imported function handle expected ${function_handle}`);

    bound_fn(args);
}

export function mono_wasm_invoke_jsimport_ST(function_handle: JSFnHandle, args: JSMarshalerArguments): void {
    if (WasmEnableThreads) return;
    const bound_fn = js_import_wrapper_by_fn_handle[<any>function_handle];
    mono_assert(bound_fn, () => `Imported function handle expected ${function_handle}`);
    bound_fn(args);
}

function bind_js_import(signature: JSFunctionSignature): Function {
    assert_js_interop();
    const mark = startMeasure();

    const version = get_signature_version(signature);
    mono_assert(version === 2, () => `Signature version ${version} mismatch.`);

    const js_function_name = get_signature_function_name(signature)!;
    const js_module_name = get_signature_module_name(signature)!;
    const function_handle = get_signature_handle(signature);

    mono_log_debug(`Binding [JSImport] ${js_function_name} from ${js_module_name} module`);

    const fn = mono_wasm_lookup_js_import(js_function_name, js_module_name);
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

    const is_discard_no_wait = res_marshaler_type == MarshalerType.DiscardNoWait;
    const is_async = res_marshaler_type == MarshalerType.Task || res_marshaler_type == MarshalerType.TaskPreCreated;

    const closure: BindingClosure = {
        fn,
        fqn: js_module_name + ":" + js_function_name,
        args_count,
        arg_marshalers,
        res_converter,
        has_cleanup,
        arg_cleanup,
        is_discard_no_wait,
        is_async,
        isDisposed: false,
    };
    let bound_fn: WrappedJSFunction;
    if (is_async || is_discard_no_wait || has_cleanup) {
        bound_fn = bind_fn(closure);
    }
    else {
        if (args_count == 0 && !res_converter) {
            bound_fn = bind_fn_0V(closure);
        }
        else if (args_count == 1 && !res_converter) {
            bound_fn = bind_fn_1V(closure);
        }
        else if (args_count == 1 && res_converter) {
            bound_fn = bind_fn_1R(closure);
        }
        else if (args_count == 2 && res_converter) {
            bound_fn = bind_fn_2R(closure);
        } else {
            bound_fn = bind_fn(closure);
        }
    }

    // this is just to make debugging easier by naming the function in the stack trace.
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        try {
            bound_fn = new Function("fn", "return (function JSImport_" + js_function_name.replaceAll(".", "_") + "(){ return fn.apply(this, arguments)});")(bound_fn);
        }
        catch (ex) {
            runtimeHelpers.cspPolicy = true;
        }
    }

    function async_bound_fn(args: JSMarshalerArguments): void {
        if (WasmEnableThreads) {
            forceThreadMemoryViewRefresh();
        }
        bound_fn(args);
    }
    function sync_bound_fn(args: JSMarshalerArguments): void {
        const previous = runtimeHelpers.isPendingSynchronousCall;
        try {
            runtimeHelpers.isPendingSynchronousCall = true;
            if (WasmEnableThreads) {
                forceThreadMemoryViewRefresh();
            }
            bound_fn(args);
        }
        finally {
            runtimeHelpers.isPendingSynchronousCall = previous;
        }
    }

    let wrapped_fn: WrappedJSFunction;
    if (is_async || is_discard_no_wait) {
        wrapped_fn = async_bound_fn;
    }
    else {
        wrapped_fn = sync_bound_fn;
    }

    (<any>wrapped_fn)[imported_js_function_symbol] = closure;

    js_import_wrapper_by_fn_handle[function_handle] = wrapped_fn;

    endMeasure(mark, MeasuredBlock.bindJsFunction, js_function_name);

    return wrapped_fn;
}

function bind_fn_0V(closure: BindingClosure) {
    const fn = closure.fn;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_0V(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            // call user function
            fn();
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
        finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1V(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1V(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            const arg1 = marshaler1(args);
            // call user function
            fn(arg1);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
        finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1R(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1R(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            const arg1 = marshaler1(args);
            // call user function
            const js_result = fn(arg1);
            res_converter(args, js_result);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
        finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_2R(closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const marshaler2 = closure.arg_marshalers[1]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_2R(args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            const arg1 = marshaler1(args);
            const arg2 = marshaler2(args);
            // call user function
            const js_result = fn(arg1, arg2);
            res_converter(args, js_result);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        }
        finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
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
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn(args: JSMarshalerArguments) {
        const receiver_should_free = WasmEnableThreads && is_receiver_should_free(args);
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
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
        finally {
            if (receiver_should_free) {
                Module._free(args as any);
            }
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

type WrappedJSFunction = (args: JSMarshalerArguments) => void;

type BindingClosure = {
    fn: Function,
    fqn: string,
    isDisposed: boolean,
    args_count: number,
    arg_marshalers: (BoundMarshalerToJs)[],
    res_converter: BoundMarshalerToCs | undefined,
    has_cleanup: boolean,
    is_discard_no_wait: boolean,
    is_async: boolean,
    arg_cleanup: (Function | undefined)[]
}

export function mono_wasm_invoke_js_function(bound_function_js_handle: JSHandle, args: JSMarshalerArguments): void {
    const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
    mono_assert(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_js_function_symbol], () => `Bound function handle expected ${bound_function_js_handle}`);
    bound_fn(args);
}

export function mono_wasm_set_module_imports(module_name: string, moduleImports: any) {
    importedModules.set(module_name, moduleImports);
    mono_log_debug(`added module imports '${module_name}'`);
}

function mono_wasm_lookup_js_import(function_name: string, js_module_name: string | null): Function {
    mono_assert(function_name && typeof function_name === "string", "function_name must be string");

    let scope: any = {};
    const parts = function_name.split(".");
    if (js_module_name) {
        scope = importedModules.get(js_module_name);
        if (WasmEnableThreads) {
            mono_assert(scope, () => `ES6 module ${js_module_name} was not imported yet, please call JSHost.ImportAsync() on the UI or JSWebWorker thread first.`);
        } else {
            mono_assert(scope, () => `ES6 module ${js_module_name} was not imported yet, please call JSHost.ImportAsync() first.`);
        }
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

export function set_property(self: any, name: string, value: any): void {
    mono_check(self, "Null reference");
    self[name] = value;
}

export function get_property(self: any, name: string): any {
    mono_check(self, "Null reference");
    return self[name];
}

export function has_property(self: any, name: string): boolean {
    mono_check(self, "Null reference");
    return name in self;
}

export function get_typeof_property(self: any, name: string): string {
    mono_check(self, "Null reference");
    return typeof self[name];
}

export function get_global_this(): any {
    return globalThis;
}

export const importedModulesPromises: Map<string, Promise<any>> = new Map();
export const importedModules: Map<string, Promise<any>> = new Map();

export function dynamic_import(module_name: string, module_url: string): Promise<any> {
    assert_js_interop();
    mono_assert(module_name && typeof module_name === "string", "module_name must be string");
    mono_assert(module_url && typeof module_url === "string", "module_url must be string");
    let promise = importedModulesPromises.get(module_name);
    const newPromise = !promise;
    if (newPromise) {
        mono_log_debug(`importing ES6 module '${module_name}' from '${module_url}'`);
        promise = import(/*! webpackIgnore: true */module_url);
        importedModulesPromises.set(module_name, promise);
    }

    return wrap_as_cancelable_promise(async () => {
        const module = await promise;
        if (newPromise) {
            importedModules.set(module_name, module);
            mono_log_debug(`imported ES6 module '${module_name}' from '${module_url}'`);
        }
        return module;
    });
}

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
        receiveWorkerHeapViews();
        setI32_unchecked(is_exception, 1);
    }
    return res;
}

export function wrap_error_root(is_exception: Int32Ptr | null, ex: any, result: WasmRoot<MonoObject>): void {
    const res = _wrap_error_flag(is_exception, ex);
    stringToMonoStringRoot(res, <any>result);
}

// to set out parameters of icalls
// TODO replace it with replace it with UTF8 char*, no GC root needed
// https://github.com/dotnet/runtime/issues/98365
export function wrap_no_error_root(is_exception: Int32Ptr | null, result?: WasmRoot<MonoObject>): void {
    if (is_exception) {
        receiveWorkerHeapViews();
        setI32_unchecked(is_exception, 0);
    }
    if (result) {
        result.clear();
    }
}

export function assert_js_interop(): void {
    loaderHelpers.assert_runtime_running();
    if (WasmEnableThreads) {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready && runtimeHelpers.proxyGCHandle, "Please use dedicated worker for working with JavaScript interop. See https://github.com/dotnet/runtime/blob/main/src/mono/wasm/threads.md#JS-interop-on-dedicated-threads");
    } else {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    }
}

export function assert_c_interop(): void {
    loaderHelpers.assert_runtime_running();
    if (WasmEnableThreads) {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "Please use dedicated worker for working with JavaScript interop. See https://github.com/dotnet/runtime/blob/main/src/mono/wasm/threads.md#JS-interop-on-dedicated-threads");
    } else {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    }
}
