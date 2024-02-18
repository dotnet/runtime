// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { GCHandle, GCHandleNull, JSMarshalerArguments, MarshalerToCs, MarshalerToJs, MarshalerType, MonoMethod } from "./types/internal";
import cwraps from "./cwraps";
import { runtimeHelpers, Module, loaderHelpers, mono_assert } from "./globals";
import { JavaScriptMarshalerArgSize, alloc_stack_frame, get_arg, get_arg_gc_handle, is_args_exception, set_arg_intptr, set_arg_type, set_gc_handle } from "./marshal";
import { marshal_array_to_cs, marshal_array_to_cs_impl, marshal_bool_to_cs, marshal_exception_to_cs, marshal_intptr_to_cs, marshal_string_to_cs } from "./marshal-to-cs";
import { marshal_int32_to_js, end_marshal_task_to_js, marshal_string_to_js, begin_marshal_task_to_js, marshal_exception_to_js } from "./marshal-to-js";
import { do_not_force_dispose } from "./gc-handles";
import { assert_c_interop, assert_js_interop } from "./invoke-js";
import { mono_wasm_main_thread_ptr } from "./pthreads/shared";
import { _zero_region } from "./memory";
import { stringToUTF8Ptr } from "./strings";

const managedExports: ManagedExports = {} as any;

export function init_managed_exports(): void {
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

    managedExports.InstallMainSynchronizationContext = WasmEnableThreads ? get_method("InstallMainSynchronizationContext") : undefined;
    managedExports.CallEntrypoint = get_method("CallEntrypoint");
    managedExports.BindAssemblyExports = get_method("BindAssemblyExports");
    managedExports.ReleaseJSOwnedObjectByGCHandle = get_method("ReleaseJSOwnedObjectByGCHandle");
    managedExports.CompleteTask = get_method("CompleteTask");
    managedExports.CallDelegate = get_method("CallDelegate");
    managedExports.GetManagedStackTrace = get_method("GetManagedStackTrace");
    managedExports.LoadSatelliteAssembly = get_method("LoadSatelliteAssembly");
    managedExports.LoadLazyAssembly = get_method("LoadLazyAssembly");
}

// the marshaled signature is: Task<int>? CallEntrypoint(char* mainAssemblyName, string[] args)
export function call_entry_point(main_assembly_name: string, program_args: string[] | undefined, waitForDebugger: boolean): Promise<number> {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(5);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        const arg3 = get_arg(args, 4);
        const main_assembly_name_ptr = stringToUTF8Ptr(main_assembly_name);
        marshal_intptr_to_cs(arg1, main_assembly_name_ptr);
        marshal_array_to_cs_impl(arg2, program_args && !program_args.length ? undefined : program_args, MarshalerType.String);
        marshal_bool_to_cs(arg3, waitForDebugger);

        // because this is async, we could pre-allocate the promise
        let promise = begin_marshal_task_to_js(res, MarshalerType.TaskPreCreated, marshal_int32_to_js);

        invoke_async_jsexport(managedExports.CallEntrypoint, args, 5);

        // in case the C# side returned synchronously
        promise = end_marshal_task_to_js(args, marshal_int32_to_js, promise);

        if (promise === null || promise === undefined) {
            promise = Promise.resolve(0);
        }
        (promise as any)[do_not_force_dispose] = true; // prevent disposing the task in forceDisposeProxies()

        return promise;
    } finally {
        Module.stackRestore(sp); // synchronously
    }
}

// the marshaled signature is: void LoadSatelliteAssembly(byte[] dll)
export function load_satellite_assembly(dll: Uint8Array): void {
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(3);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Array);
        marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
        invoke_sync_jsexport(managedExports.LoadSatelliteAssembly, args);
    } finally {
        Module.stackRestore(sp);
    }
}

// the marshaled signature is: void LoadLazyAssembly(byte[] dll, byte[] pdb)
export function load_lazy_assembly(dll: Uint8Array, pdb: Uint8Array | null): void {
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(4);
        const arg1 = get_arg(args, 2);
        const arg2 = get_arg(args, 3);
        set_arg_type(arg1, MarshalerType.Array);
        set_arg_type(arg2, MarshalerType.Array);
        marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
        marshal_array_to_cs(arg2, pdb, MarshalerType.Byte);
        invoke_sync_jsexport(managedExports.LoadLazyAssembly, args);
    } finally {
        Module.stackRestore(sp);
    }
}

// the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
export function release_js_owned_object_by_gc_handle(gc_handle: GCHandle) {
    mono_assert(gc_handle, "Must be valid gc_handle");
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(3);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Object);
        set_gc_handle(arg1, gc_handle);
        // this must stay synchronous for free_gcv_handle sake
        invoke_sync_jsexport(managedExports.ReleaseJSOwnedObjectByGCHandle, args);
    } finally {
        Module.stackRestore(sp);
    }
}

// the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
export function complete_task(holder_gc_handle: GCHandle, isCanceling: boolean, error?: any, data?: any, res_converter?: MarshalerToCs) {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(5);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Object);
        set_gc_handle(arg1, holder_gc_handle);
        const arg2 = get_arg(args, 3);
        if (error) {
            marshal_exception_to_cs(arg2, error);
            if (isCanceling) {
                set_arg_type(res, MarshalerType.Discard);
            }
        } else {
            set_arg_type(arg2, MarshalerType.None);
            const arg3 = get_arg(args, 4);
            mono_assert(res_converter, "res_converter missing");
            res_converter(arg3, data);
        }
        invoke_async_jsexport(managedExports.CompleteTask, args, 4);
    } finally {
        Module.stackRestore(sp);
    }
}

// the marshaled signature is: TRes? CallDelegate<T1,T2,T3,TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
export function call_delegate(callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs) {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(6);

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

        invoke_sync_jsexport(managedExports.CallDelegate, args);

        if (res_converter) {
            const res = get_arg(args, 1);
            return res_converter(res);
        }
    } finally {
        Module.stackRestore(sp);
    }
}

// the marshaled signature is: string GetManagedStackTrace(GCHandle exception)
export function get_managed_stack_trace(exception_gc_handle: GCHandle) {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(3);

        const arg1 = get_arg(args, 2);
        set_arg_type(arg1, MarshalerType.Exception);
        set_gc_handle(arg1, exception_gc_handle);

        invoke_sync_jsexport(managedExports.GetManagedStackTrace, args);
        const res = get_arg(args, 1);
        return marshal_string_to_js(res);
    } finally {
        Module.stackRestore(sp);
    }
}

// GCHandle InstallMainSynchronizationContext(nint jsNativeTID)
export function install_main_synchronization_context(): GCHandle {
    if (!WasmEnableThreads) return GCHandleNull;
    assert_c_interop();

    const sp = Module.stackSave();
    try {
        // this block is like alloc_stack_frame() but without set_args_context()
        const bytes = JavaScriptMarshalerArgSize * 3;
        const args = Module.stackAlloc(bytes) as any;
        _zero_region(args, bytes);

        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        set_arg_intptr(arg1, mono_wasm_main_thread_ptr() as any);

        // this block is like invoke_sync_jsexport() but without assert_js_interop()
        cwraps.mono_wasm_invoke_jsexport(managedExports.InstallMainSynchronizationContext!, args);
        if (is_args_exception(args)) {
            const exc = get_arg(args, 0);
            throw marshal_exception_to_js(exc);
        }
        return get_arg_gc_handle(res) as any;
    } finally {
        Module.stackRestore(sp);
    }
}

export function invoke_async_jsexport(method: MonoMethod, args: JSMarshalerArguments, size: number): void {
    assert_js_interop();
    if (!WasmEnableThreads || runtimeHelpers.isCurrentThread) {
        cwraps.mono_wasm_invoke_jsexport(method, args as any);
        if (is_args_exception(args)) {
            const exc = get_arg(args, 0);
            throw marshal_exception_to_js(exc);
        }
    } else {
        throw new Error("Should be unreachable until we implement deputy." + size);
        /*
        set_receiver_should_free(args);
        const bytes = JavaScriptMarshalerArgSize * size;
        const cpy = Module._malloc(bytes) as any;
        copyBytes(args as any, cpy, bytes);
        twraps.mono_wasm_invoke_jsexport_async_post(runtimeHelpers.managedThreadTID, method, cpy);
        */
    }
}

export function invoke_sync_jsexport(method: MonoMethod, args: JSMarshalerArguments): void {
    assert_js_interop();
    if (!WasmEnableThreads || runtimeHelpers.isCurrentThread) {
        cwraps.mono_wasm_invoke_jsexport(method, args as any);
    } else {
        throw new Error("Should be unreachable until we implement deputy.");
        /*
        if (!runtimeHelpers.isCurrentThread && runtimeHelpers.isPendingSynchronousCall) {
            throw new Error("Cannot call synchronous C# method from inside a synchronous call to a JS method.");
        }
        // this is blocking too
        twraps.mono_wasm_invoke_jsexport_sync_send(runtimeHelpers.managedThreadTID, method, args as any);
        */
    }
    if (is_args_exception(args)) {
        const exc = get_arg(args, 0);
        throw marshal_exception_to_js(exc);
    }
}

// the marshaled signature is: Task BindAssemblyExports(string assemblyName)
export function bind_assembly_exports(assemblyName: string): Promise<void> {
    loaderHelpers.assert_runtime_running();
    const sp = Module.stackSave();
    try {
        const args = alloc_stack_frame(3);
        const res = get_arg(args, 1);
        const arg1 = get_arg(args, 2);
        marshal_string_to_cs(arg1, assemblyName);

        // because this is async, we could pre-allocate the promise
        let promise = begin_marshal_task_to_js(res, MarshalerType.TaskPreCreated);

        invoke_async_jsexport(managedExports.BindAssemblyExports, args, 3);

        // in case the C# side returned synchronously
        promise = end_marshal_task_to_js(args, marshal_int32_to_js, promise);

        if (promise === null || promise === undefined) {
            promise = Promise.resolve();
        }
        return promise;
    } finally {
        Module.stackRestore(sp); // synchronously
    }
}


function get_method(method_name: string): MonoMethod {
    // TODO https://github.com/dotnet/runtime/issues/98366
    const res = cwraps.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + "." + method_name;
    return res;
}

type ManagedExports = {
    InstallMainSynchronizationContext: MonoMethod | undefined,
    entry_point: MonoMethod,
    CallEntrypoint: MonoMethod,
    BindAssemblyExports: MonoMethod,
    ReleaseJSOwnedObjectByGCHandle: MonoMethod,
    CompleteTask: MonoMethod,
    CallDelegate: MonoMethod,
    GetManagedStackTrace: MonoMethod,
    LoadSatelliteAssembly: MonoMethod,
    LoadLazyAssembly: MonoMethod,
}
