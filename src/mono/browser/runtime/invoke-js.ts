// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";
import BuildConfiguration from "consts:configuration";

import { marshal_exception_to_cs, bind_arg_marshal_to_cs, marshal_task_to_cs } from "./marshal-to-cs";
import { get_signature_argument_count, bound_js_function_symbol, get_sig, get_signature_version, get_signature_type, imported_js_function_symbol, get_signature_handle, get_signature_function_name, get_signature_module_name, is_receiver_should_free, get_caller_native_tid, get_sync_done_semaphore_ptr, get_arg } from "./marshal";
import { forceThreadMemoryViewRefresh } from "./memory";
import { JSFunctionSignature, JSMarshalerArguments, BoundMarshalerToJs, JSFnHandle, BoundMarshalerToCs, JSHandle, MarshalerType, VoidPtrNull } from "./types/internal";
import { VoidPtr } from "./types/emscripten";
import { INTERNAL, Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { bind_arg_marshal_to_js } from "./marshal-to-js";
import { mono_log_debug, mono_wasm_symbolicate_string } from "./logging";
import { mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { wrap_as_cancelable_promise } from "./cancelable-promise";
import { threads_c_functions as tcwraps } from "./cwraps";
import { monoThreadInfo } from "./pthreads";
import { stringToUTF16Ptr } from "./strings";

export const js_import_wrapper_by_fn_handle: Function[] = <any>[null];// 0th slot is dummy, main thread we free them on shutdown. On web worker thread we free them when worker is detached.

export function mono_wasm_bind_js_import_ST (signature: JSFunctionSignature): VoidPtr {
    if (WasmEnableThreads) return VoidPtrNull;
    assert_js_interop();
    try {
        bind_js_import(signature);
        return VoidPtrNull;
    } catch (ex: any) {
        return stringToUTF16Ptr(normalize_exception(ex));
    }
}

export function mono_wasm_invoke_jsimport_MT (signature: JSFunctionSignature, args: JSMarshalerArguments) {
    if (!WasmEnableThreads) return;
    assert_js_interop();

    const function_handle = get_signature_handle(signature);

    let bound_fn = js_import_wrapper_by_fn_handle[function_handle];
    if (bound_fn == undefined) {
        // it was not bound yet, let's do it now
        try {
            bound_fn = bind_js_import(signature);
        } catch (ex: any) {
            // propagate the exception back to caller, which could be on different thread. Handle both sync and async signatures.
            try {
                const res_sig = get_sig(signature, 1);
                const res_type = get_signature_type(res_sig);
                if (res_type === MarshalerType.Task) {
                    const res = get_arg(args, 1);
                    marshal_task_to_cs(res, Promise.reject(ex));
                } else {
                    marshal_exception_to_cs(<any>args, ex);
                    if (monoThreadInfo.isUI) {
                        const done_semaphore = get_sync_done_semaphore_ptr(args);
                        tcwraps.mono_threads_wasm_sync_run_in_target_thread_done(done_semaphore);
                    }
                }
                return;
            } catch (ex: any) {
                loaderHelpers.mono_exit(1, ex);
                return;
            }
        }
    }
    mono_assert(bound_fn, () => `Imported function handle expected ${function_handle}`);

    bound_fn(args);
}

export function mono_wasm_invoke_jsimport_ST (function_handle: JSFnHandle, args: JSMarshalerArguments): void {
    if (WasmEnableThreads) return;
    loaderHelpers.assert_runtime_running();
    const bound_fn = js_import_wrapper_by_fn_handle[<any>function_handle];
    mono_assert(bound_fn, () => `Imported function handle expected ${function_handle}`);
    bound_fn(args);
}

function bind_js_import (signature: JSFunctionSignature): Function {
    assert_js_interop();
    const mark = startMeasure();

    const version = get_signature_version(signature);
    mono_assert(version === 2, () => `Signature version ${version} mismatch.`);

    const js_function_name = get_signature_function_name(signature)!;
    const js_module_name = get_signature_module_name(signature)!;
    const function_handle = get_signature_handle(signature);

    mono_log_debug(() => `Binding [JSImport] ${js_function_name} from ${js_module_name} module`);

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
    } else {
        if (args_count == 0 && !res_converter) {
            bound_fn = bind_fn_0V(closure);
        } else if (args_count == 1 && !res_converter) {
            bound_fn = bind_fn_1V(closure);
        } else if (args_count == 1 && res_converter) {
            bound_fn = bind_fn_1R(closure);
        } else if (args_count == 2 && res_converter) {
            bound_fn = bind_fn_2R(closure);
        } else {
            bound_fn = bind_fn(closure);
        }
    }

    function async_bound_fn (args: JSMarshalerArguments): void {
        forceThreadMemoryViewRefresh();
        bound_fn(args);
    }
    function sync_bound_fn (args: JSMarshalerArguments): void {
        const previous = runtimeHelpers.isPendingSynchronousCall;
        try {
            forceThreadMemoryViewRefresh();
            const caller_tid = get_caller_native_tid(args);
            runtimeHelpers.isPendingSynchronousCall = runtimeHelpers.managedThreadTID === caller_tid;
            bound_fn(args);
        } finally {
            runtimeHelpers.isPendingSynchronousCall = previous;
        }
    }
    function async_bound_fn_ui (args: JSMarshalerArguments): void {
        invoke_later_when_on_ui_thread_async(() => async_bound_fn(args));
    }
    function sync_bound_fn_ui (args: JSMarshalerArguments): void {
        invoke_later_when_on_ui_thread_sync(() => sync_bound_fn(args), args);
    }

    let wrapped_fn: WrappedJSFunction = bound_fn;
    if (WasmEnableThreads) {
        if (monoThreadInfo.isUI) {
            if (is_async || is_discard_no_wait) {
                wrapped_fn = async_bound_fn_ui;
            } else {
                wrapped_fn = sync_bound_fn_ui;
            }
        } else {
            if (is_async || is_discard_no_wait) {
                wrapped_fn = async_bound_fn;
            } else {
                wrapped_fn = sync_bound_fn;
            }
        }
    }

    // this is just to make debugging easier by naming the function in the stack trace.
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        try {
            const fname = js_function_name.replaceAll(".", "_");
            const url = `//# sourceURL=https://dotnet/JSImport/${fname}`;
            const body = `return (function JSImport_${fname}(){ return fn.apply(this, arguments)});`;
            wrapped_fn = new Function("fn", url + "\r\n" + body)(wrapped_fn);
        } catch (ex) {
            runtimeHelpers.cspPolicy = true;
        }
    }

    (<any>wrapped_fn)[imported_js_function_symbol] = closure;

    js_import_wrapper_by_fn_handle[function_handle] = wrapped_fn;

    endMeasure(mark, MeasuredBlock.bindJsFunction, js_function_name);

    return wrapped_fn;
}

function bind_fn_0V (closure: BindingClosure) {
    const fn = closure.fn;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_0V (args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            // call user function
            fn();
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1V (closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1V (args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            const arg1 = marshaler1(args);
            // call user function
            fn(arg1);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1R (closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1R (args: JSMarshalerArguments) {
        const mark = startMeasure();
        try {
            mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
            const arg1 = marshaler1(args);
            // call user function
            const js_result = fn(arg1);
            res_converter(args, js_result);
        } catch (ex) {
            marshal_exception_to_cs(<any>args, ex);
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_2R (closure: BindingClosure) {
    const fn = closure.fn;
    const marshaler1 = closure.arg_marshalers[0]!;
    const marshaler2 = closure.arg_marshalers[1]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_2R (args: JSMarshalerArguments) {
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
        } finally {
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn (closure: BindingClosure) {
    const args_count = closure.args_count;
    const arg_marshalers = closure.arg_marshalers;
    const res_converter = closure.res_converter;
    const arg_cleanup = closure.arg_cleanup;
    const has_cleanup = closure.has_cleanup;
    const fn = closure.fn;
    const fqn = closure.fqn;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn (args: JSMarshalerArguments) {
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
        } finally {
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

export function mono_wasm_invoke_js_function (bound_function_js_handle: JSHandle, args: JSMarshalerArguments): void {
    invoke_later_when_on_ui_thread_sync(() => mono_wasm_invoke_js_function_impl(bound_function_js_handle, args), args);
}

export function mono_wasm_invoke_js_function_impl (bound_function_js_handle: JSHandle, args: JSMarshalerArguments): void {
    loaderHelpers.assert_runtime_running();
    const bound_fn = mono_wasm_get_jsobj_from_js_handle(bound_function_js_handle);
    mono_assert(bound_fn && typeof (bound_fn) === "function" && bound_fn[bound_js_function_symbol], () => `Bound function handle expected ${bound_function_js_handle}`);
    bound_fn(args);
}

export function mono_wasm_set_module_imports (module_name: string, moduleImports: any) {
    importedModules.set(module_name, moduleImports);
    mono_log_debug(() => `added module imports '${module_name}'`);
}

function mono_wasm_lookup_js_import (function_name: string, js_module_name: string | null): Function {
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
    } else if (parts[0] === "INTERNAL") {
        scope = INTERNAL;
        parts.shift();
    } else if (parts[0] === "globalThis") {
        scope = globalThis;
        parts.shift();
    }

    for (let i = 0; i < parts.length - 1; i++) {
        const part = parts[i];
        const newscope = scope[part];
        if (!newscope) {
            throw new Error(`${part} not found while looking up ${function_name}`);
        }
        scope = newscope;
    }

    const fname = parts[parts.length - 1];
    const fn = scope[fname];

    if (typeof (fn) !== "function") {
        throw new Error(`${function_name} must be a Function but was ${typeof fn}`);
    }

    // if the function was already bound to some object it would stay bound to original object. That's good.
    return fn.bind(scope);
}

export function set_property (self: any, name: string, value: any): void {
    mono_check(self, "Null reference");
    self[name] = value;
}

export function get_property (self: any, name: string): any {
    mono_check(self, "Null reference");
    return self[name];
}

export function has_property (self: any, name: string): boolean {
    mono_check(self, "Null reference");
    return name in self;
}

export function get_typeof_property (self: any, name: string): string {
    mono_check(self, "Null reference");
    return typeof self[name];
}

export function get_global_this (): any {
    return globalThis;
}

export const importedModulesPromises: Map<string, Promise<any>> = new Map();
export const importedModules: Map<string, Promise<any>> = new Map();

export function dynamic_import (module_name: string, module_url: string): Promise<any> {
    assert_js_interop();
    mono_assert(module_name && typeof module_name === "string", "module_name must be string");
    mono_assert(module_url && typeof module_url === "string", "module_url must be string");
    let promise = importedModulesPromises.get(module_name);
    const newPromise = !promise;
    if (newPromise) {
        mono_log_debug(() => `importing ES6 module '${module_name}' from '${module_url}'`);
        promise = import(/*! webpackIgnore: true */module_url);
        importedModulesPromises.set(module_name, promise);
    }

    return wrap_as_cancelable_promise(async () => {
        const module = await promise;
        if (newPromise) {
            importedModules.set(module_name, module);
            mono_log_debug(() => `imported ES6 module '${module_name}' from '${module_url}'`);
        }
        return module;
    });
}

export function normalize_exception (ex: any) {
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
    return res;
}

export function assert_js_interop (): void {
    loaderHelpers.assert_runtime_running();
    if (WasmEnableThreads) {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready && runtimeHelpers.proxyGCHandle, "Please use dedicated worker for working with JavaScript interop. See https://github.com/dotnet/runtime/blob/main/src/mono/wasm/threads.md#JS-interop-on-dedicated-threads");
    } else {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    }
}

export function assert_c_interop (): void {
    loaderHelpers.assert_runtime_running();
    if (WasmEnableThreads) {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "Please use dedicated worker for working with JavaScript interop. See https://github.com/dotnet/runtime/blob/main/src/mono/wasm/threads.md#JS-interop-on-dedicated-threads");
    } else {
        mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    }
}

// make sure we are not blocking em_task_queue_execute up the call stack
// so that when we call back to managed, the FS calls could still be processed by the UI thread
// see also emscripten_yield which can process the FS calls inside the spin wait
export function invoke_later_when_on_ui_thread_sync (fn: Function, args: JSMarshalerArguments) {
    if (WasmEnableThreads && monoThreadInfo.isUI) {
        Module.safeSetTimeout(() => {
            fn();
            // see also mono_threads_wasm_sync_run_in_target_thread_vii_cb
            const done_semaphore = get_sync_done_semaphore_ptr(args);
            tcwraps.mono_threads_wasm_sync_run_in_target_thread_done(done_semaphore);
        }, 0);
    } else {
        fn();
    }
}

// make sure we are not blocking em_task_queue_execute up the call stack
// so that when we call back to managed, the FS calls could still be processed by the UI thread
export function invoke_later_when_on_ui_thread_async (fn: Function) {
    if (WasmEnableThreads && monoThreadInfo.isUI) {
        Module.safeSetTimeout(fn, 0);
    } else {
        fn();
    }
}
