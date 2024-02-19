// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import WasmEnableThreads from "consts:wasmEnableThreads";

import { Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { bind_arg_marshal_to_cs } from "./marshal-to-cs";
import { bind_arg_marshal_to_js, end_marshal_task_to_js } from "./marshal-to-js";
import {
    get_sig, get_signature_argument_count,
    bound_cs_function_symbol, get_signature_version, alloc_stack_frame, get_signature_type,
} from "./marshal";
import { MonoMethod, JSFunctionSignature, BoundMarshalerToCs, BoundMarshalerToJs, MarshalerType } from "./types/internal";
import { assert_js_interop } from "./invoke-js";
import { startMeasure, MeasuredBlock, endMeasure } from "./profiler";
import { bind_assembly_exports, invoke_async_jsexport, invoke_sync_jsexport } from "./managed-exports";
import { mono_log_debug } from "./logging";

export function mono_wasm_bind_cs_function(method: MonoMethod, assemblyName: string, namespaceName: string, shortClassName: string, methodName: string, signatureHash: number, signature: JSFunctionSignature): void {
    const fullyQualifiedName = `[${assemblyName}] ${namespaceName}.${shortClassName}:${methodName}`;
    const mark = startMeasure();
    mono_log_debug(`Binding [JSExport] ${namespaceName}.${shortClassName}:${methodName} from ${assemblyName} assembly`);
    const version = get_signature_version(signature);
    mono_assert(version === 2, () => `Signature version ${version} mismatch.`);


    const args_count = get_signature_argument_count(signature);

    const arg_marshalers: (BoundMarshalerToCs)[] = new Array(args_count);
    for (let index = 0; index < args_count; index++) {
        const sig = get_sig(signature, index + 2);
        const marshaler_type = get_signature_type(sig);
        const arg_marshaler = bind_arg_marshal_to_cs(sig, marshaler_type, index + 2);
        mono_assert(arg_marshaler, "ERR43: argument marshaler must be resolved");
        arg_marshalers[index] = arg_marshaler;
    }

    const res_sig = get_sig(signature, 1);
    let res_marshaler_type = get_signature_type(res_sig);

    // hack until we have public API for JSType.DiscardNoWait
    if (WasmEnableThreads && shortClassName === "DefaultWebAssemblyJSRuntime"
        && namespaceName === "Microsoft.AspNetCore.Components.WebAssembly.Services"
        && (methodName === "BeginInvokeDotNet" || methodName === "EndInvokeJS")) {
        res_marshaler_type = MarshalerType.DiscardNoWait;
    }

    const is_async = res_marshaler_type == MarshalerType.Task;
    const is_discard_no_wait = res_marshaler_type == MarshalerType.DiscardNoWait;
    if (is_async) {
        res_marshaler_type = MarshalerType.TaskPreCreated;
    }
    const res_converter = bind_arg_marshal_to_js(res_sig, res_marshaler_type, 1);

    const closure: BindingClosure = {
        method,
        fullyQualifiedName,
        args_count,
        arg_marshalers,
        res_converter,
        is_async,
        is_discard_no_wait,
        isDisposed: false,
    };
    let bound_fn: Function;

    if (is_async) {
        if (args_count == 1 && res_converter) {
            bound_fn = bind_fn_1RA(closure);
        }
        else if (args_count == 2 && res_converter) {
            bound_fn = bind_fn_2RA(closure);
        }
        else {
            bound_fn = bind_fn(closure);
        }
    } else if (is_discard_no_wait) {
        bound_fn = bind_fn(closure);
    } else {
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
        }
        else {
            bound_fn = bind_fn(closure);
        }
    }

    // this is just to make debugging easier. 
    // It's not CSP compliant and possibly not performant, that's why it's only enabled in debug builds
    // in Release configuration, it would be a trimmed by rollup
    if (BuildConfiguration === "Debug" && !runtimeHelpers.cspPolicy) {
        try {
            const url = `//# sourceURL=https://dotnet/JSExport/${methodName}`;
            const body = `return (function JSExport_${methodName}(){ return fn.apply(this, arguments)});`;
            bound_fn = new Function("fn", url + "\r\n" + body)(bound_fn);
        }
        catch (ex) {
            runtimeHelpers.cspPolicy = true;
        }
    }

    (<any>bound_fn)[bound_cs_function_symbol] = closure;

    _walk_exports_to_set_function(assemblyName, namespaceName, shortClassName, methodName, signatureHash, bound_fn);
    endMeasure(mark, MeasuredBlock.bindCsFunction, fullyQualifiedName);
}

function bind_fn_0V(closure: BindingClosure) {
    const method = closure.method;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_0V() {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(2);
            // call C# side
            invoke_sync_jsexport(method, args);
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1V(closure: BindingClosure) {
    const method = closure.method;
    const marshaler1 = closure.arg_marshalers[0]!;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1V(arg1: any) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);
            marshaler1(args, arg1);

            // call C# side
            invoke_sync_jsexport(method, args);
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1R(closure: BindingClosure) {
    const method = closure.method;
    const marshaler1 = closure.arg_marshalers[0]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_1R(arg1: any) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);
            marshaler1(args, arg1);

            // call C# side
            invoke_sync_jsexport(method, args);

            const js_result = res_converter(args);
            return js_result;
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_1RA(closure: BindingClosure) {
    const method = closure.method;
    const marshaler1 = closure.arg_marshalers[0]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bind_fn_1RA(arg1: any) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);
            marshaler1(args, arg1);

            // pre-allocate the promise
            let promise = res_converter(args);

            // call C# side
            invoke_async_jsexport(method, args, 3);

            // in case the C# side returned synchronously
            promise = end_marshal_task_to_js(args, undefined, promise);

            return promise;
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_2R(closure: BindingClosure) {
    const method = closure.method;
    const marshaler1 = closure.arg_marshalers[0]!;
    const marshaler2 = closure.arg_marshalers[1]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn_2R(arg1: any, arg2: any) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(4);
            marshaler1(args, arg1);
            marshaler2(args, arg2);

            // call C# side
            invoke_sync_jsexport(method, args);

            const js_result = res_converter(args);
            return js_result;
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn_2RA(closure: BindingClosure) {
    const method = closure.method;
    const marshaler1 = closure.arg_marshalers[0]!;
    const marshaler2 = closure.arg_marshalers[1]!;
    const res_converter = closure.res_converter!;
    const fqn = closure.fullyQualifiedName;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bind_fn_2RA(arg1: any, arg2: any) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(4);
            marshaler1(args, arg1);
            marshaler2(args, arg2);

            // pre-allocate the promise
            let promise = res_converter(args);

            // call C# side
            invoke_async_jsexport(method, args, 4);

            // in case the C# side returned synchronously
            promise = end_marshal_task_to_js(args, undefined, promise);

            return promise;
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

function bind_fn(closure: BindingClosure) {
    const args_count = closure.args_count;
    const arg_marshalers = closure.arg_marshalers;
    const res_converter = closure.res_converter;
    const method = closure.method;
    const fqn = closure.fullyQualifiedName;
    const is_async = closure.is_async;
    const is_discard_no_wait = closure.is_discard_no_wait;
    if (!WasmEnableThreads) (<any>closure) = null;
    return function bound_fn(...js_args: any[]) {
        const mark = startMeasure();
        loaderHelpers.assert_runtime_running();
        mono_assert(!WasmEnableThreads || !closure.isDisposed, "The function was already disposed");
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(2 + args_count);
            for (let index = 0; index < args_count; index++) {
                const marshaler = arg_marshalers[index];
                if (marshaler) {
                    const js_arg = js_args[index];
                    marshaler(args, js_arg);
                }
            }
            let js_result = undefined;
            if (is_async) {
                // pre-allocate the promise
                js_result = res_converter!(args);
            }

            // call C# side
            if (is_async) {
                invoke_async_jsexport(method, args, 2 + args_count);
                // in case the C# side returned synchronously
                js_result = end_marshal_task_to_js(args, undefined, js_result);
            }
            else if (is_discard_no_wait) {
                // call C# side, fire and forget
                invoke_async_jsexport(method, args, 2 + args_count);
            }
            else {
                invoke_sync_jsexport(method, args);
                if (res_converter) {
                    js_result = res_converter(args);
                }
            }
            return js_result;
        } finally {
            Module.stackRestore(sp);
            endMeasure(mark, MeasuredBlock.callCsFunction, fqn);
        }
    };
}

type BindingClosure = {
    fullyQualifiedName: string,
    args_count: number,
    method: MonoMethod,
    arg_marshalers: (BoundMarshalerToCs)[],
    res_converter: BoundMarshalerToJs | undefined,
    is_async: boolean,
    is_discard_no_wait: boolean,
    isDisposed: boolean,
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
    assert_js_interop();
    const result = exportsByAssembly.get(assembly);
    if (!result) {
        await bind_assembly_exports(assembly);
    }

    return exportsByAssembly.get(assembly) || {};
}
