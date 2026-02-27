// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { GCHandle, GCHandleNull, JSExportHandle, JSMarshalerArguments, JSThreadBlockingMode, MarshalerToCs, MarshalerToJs, MarshalerType } from "./types/internal";
import cwraps from "./cwraps";
import { runtimeHelpers, Module, loaderHelpers, mono_assert } from "./globals";
import { JavaScriptMarshalerArgSize, alloc_stack_frame, get_arg, get_arg_gc_handle, is_args_exception, set_arg_i32, set_arg_intptr, set_arg_type, set_gc_handle } from "./marshal";
import { marshal_array_to_cs, marshal_exception_to_cs, marshal_string_to_cs } from "./marshal-to-cs";
import { marshal_int32_to_js, end_marshal_task_to_js, marshal_string_to_js, begin_marshal_task_to_js, marshal_exception_to_js } from "./marshal-to-js";
import { assert_c_interop, assert_js_interop } from "./invoke-js";
import { monoThreadInfo, mono_wasm_main_thread_ptr } from "./pthreads";
import { _zero_region } from "./memory";
import { mono_log_error } from "./logging";

export function init_managed_exports (): void {
    const exports_fqn_asm = "System.Runtime.InteropServices.JavaScript";
    // TODO https://github.com/dotnet/runtime/issues/98366
    runtimeHelpers.runtime_interop_module = cwraps.mono_wasm_assembly_load(exports_fqn_asm);
    if (!runtimeHelpers.runtime_interop_module)
        throw "Can't find bindings module assembly: " + exports_fqn_asm;

    runtimeHelpers.runtime_interop_namespace = exports_fqn_asm;
    runtimeHelpers.runtime_interop_exports_classname = "JavaScriptExports";
    // TODO https://github.com/dotnet/runtime/issues/98366
    runtimeHelpers.runtime_interop_exports_class = cwraps.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, runtimeHelpers.runtime_interop_exports_classname);
    if (!runtimeHelpers.runtime_interop_exports_class)
        throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + " class";
}

// the marshaled signature is: void LoadSatelliteAssembly(byte[] dll)
export function load_satellite_assembly (dll: Uint8Array): void {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = alloc_stack_frame(size);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Array);
        marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
        invoke_jsexport_UCO(cwraps.SystemInteropJS_LoadSatelliteAssembly, args);
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// the marshaled signature is: void LoadLazyAssembly(byte[] dll, byte[] pdb)
export function load_lazy_assembly (dll: Uint8Array, pdb: Uint8Array | null): void {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 4;
        const args = alloc_stack_frame(size);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        set_arg_type(arg1, MarshalerType.Array);
        set_arg_type(arg2, MarshalerType.Array);
        marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
        marshal_array_to_cs(arg2, pdb, MarshalerType.Byte);
        invoke_jsexport_UCO(cwraps.SystemInteropJS_LoadLazyAssembly, args);
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
export function release_js_owned_object_by_gc_handle (gc_handle: GCHandle) {
    mono_assert(gc_handle, "Must be valid gc_handle");
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = alloc_stack_frame(size);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Object);
        set_gc_handle(arg1, gc_handle);
        invoke_jsexport_UCO(cwraps.SystemInteropJS_ReleaseJSOwnedObjectByGCHandle, args);
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
export function complete_task (holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs) {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 5;
        const args = alloc_stack_frame(size);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Object);
        set_gc_handle(arg1, holder_gc_handle);
        const arg2 = get_arg(args, 3);
        if (!error) {
            set_arg_type(arg2, MarshalerType.None);
            const arg3 = get_arg(args, 4);
            mono_assert(res_converter, "res_converter missing");
            try {
                res_converter(arg3, data);
            } catch (ex) {
                error = ex;
            }
        }
        if (error) {
            marshal_exception_to_cs(arg2, error);
        }
        invoke_jsexport_UCO(cwraps.SystemInteropJS_CompleteTask, args);
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// the marshaled signature is: TRes? CallDelegate<T1,T2,T3,TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
export function call_delegate (callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs) {
    loaderHelpers.assert_runtime_running();
    if (WasmEnableThreads) {
        if (monoThreadInfo.isUI) {
            if (runtimeHelpers.config.jsThreadBlockingMode == JSThreadBlockingMode.PreventSynchronousJSExport) {
                throw new Error("Cannot call synchronous C# methods.");
            } else if (runtimeHelpers.isPendingSynchronousCall) {
                throw new Error("Cannot call synchronous C# method from inside a synchronous call to a JS method.");
            }
        }
    }
    const sp = Module.stackSave();
    try {
        const size = 6;
        const args = alloc_stack_frame(size);

        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Object);
        set_gc_handle(arg1, callback_gc_handle);
        // payload arg numbers are shifted by one, the real first is a gc handle of the callback

        if (arg1_converter) {
            const arg2 = get_arg(args, 3);
            arg1_converter(arg2, arg1_js);
        }
        if (arg2_converter) {
            const arg3 = get_arg(args, 4);
            arg2_converter(arg3, arg2_js);
        }
        if (arg3_converter) {
            const arg4 = get_arg(args, 5);
            arg3_converter(arg4, arg3_js);
        }

        invoke_jsexport_UCO(cwraps.SystemInteropJS_CallDelegate, args);

        if (res_converter) {
            const res = get_arg(args, 1);
            return res_converter(res);
        }
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// the marshaled signature is: string GetManagedStackTrace(GCHandle exception)
export function get_managed_stack_trace (exception_gc_handle: GCHandle) {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = alloc_stack_frame(size);

        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Exception);
        set_gc_handle(arg1, exception_gc_handle);

        invoke_jsexport_UCO(cwraps.SystemInteropJS_GetManagedStackTrace, args);
        const res = get_arg(args, 1);
        return marshal_string_to_js(res);
    } finally {
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);

    }
}

// GCHandle InstallMainSynchronizationContext(nint jsNativeTID, JSThreadBlockingMode jsThreadBlockingMode)
export function install_main_synchronization_context (jsThreadBlockingMode: JSThreadBlockingMode): GCHandle {
    if (!WasmEnableThreads) return GCHandleNull;
    assert_c_interop();

    try {
        // this block is like alloc_stack_frame() but without set_args_context()
        const bytes = JavaScriptMarshalerArgSize * 4;
        const args = Module.stackAlloc(bytes) as any;
        _zero_region(args, bytes);

        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        set_arg_intptr(arg1, mono_wasm_main_thread_ptr() as any);

        // sync with JSHostImplementation.Types.cs
        switch (jsThreadBlockingMode) {
            case JSThreadBlockingMode.PreventSynchronousJSExport:
                set_arg_i32(arg2, 0);
                break;
            case JSThreadBlockingMode.ThrowWhenBlockingWait:
                set_arg_i32(arg2, 1);
                break;
            case JSThreadBlockingMode.WarnWhenBlockingWait:
                set_arg_i32(arg2, 2);
                break;
            case JSThreadBlockingMode.DangerousAllowBlockingWait:
                set_arg_i32(arg2, 100);
                break;
            default:
                throw new Error("Invalid jsThreadBlockingMode");
        }

        // this block is like invoke_sync_jsexport() but without assert_js_interop()
        invoke_jsexport_UCO(cwraps.SystemInteropJS_InstallMainSynchronizationContext, args);
        if (is_args_exception(args)) {
            const exc = get_arg(args, 0);
            throw marshal_exception_to_js(exc);
        }
        return get_arg_gc_handle(res) as any;
    } catch (e) {
        mono_log_error("install_main_synchronization_context failed", e);
        throw e;
    }
}

export function invoke_jsexport_UCO (method: (args: JSMarshalerArguments) => void, args: JSMarshalerArguments): void {
    if (WasmEnableThreads) throw new Error("TODO-WASM: unification");
    assert_js_interop();
    method(args);
    if (is_args_exception(args)) {
        const exc = get_arg(args, 0);
        throw marshal_exception_to_js(exc);
    }
}

export function invoke_jsexport_handle (method: JSExportHandle, args: JSMarshalerArguments): void {
    if (WasmEnableThreads) throw new Error("TODO-WASM: unification");
    assert_js_interop();

    cwraps.SystemInteropJS_CallJSExport(method, args);

    if (is_args_exception(args)) {
        const exc = get_arg(args, 0);
        throw marshal_exception_to_js(exc);
    }
}

// the marshaled signature is: Task BindAssemblyExports(string assemblyName)
export function bind_assembly_exports (assemblyName: string): Promise<void> {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const size = 3;
        const args = alloc_stack_frame(size);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        marshal_string_to_cs(arg1, assemblyName);

        // because this is async, we could pre-allocate the promise
        let promise = begin_marshal_task_to_js(res, MarshalerType.TaskPreCreated);

        invoke_jsexport_UCO(cwraps.SystemInteropJS_BindAssemblyExports, args);

        // in case the C# side returned synchronously
        promise = end_marshal_task_to_js(args, marshal_int32_to_js, promise);

        if (promise === null || promise === undefined) {
            promise = Promise.resolve();
        }
        return promise;
    } finally {
        // synchronously
        if (loaderHelpers.is_runtime_running()) Module.stackRestore(sp);
    }
}
