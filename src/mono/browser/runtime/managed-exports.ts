// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { GCHandle, MarshalerToCs, MarshalerToJs, MarshalerType, MonoMethod } from "./types/internal";
import cwraps from "./cwraps";
import { runtimeHelpers, Module, loaderHelpers, mono_assert } from "./globals";
import { alloc_stack_frame, get_arg, set_arg_type, set_gc_handle } from "./marshal";
import { invoke_method_and_handle_exception, invoke_method_raw } from "./invoke-cs";
import { marshal_array_to_cs, marshal_array_to_cs_impl, marshal_exception_to_cs, marshal_intptr_to_cs } from "./marshal-to-cs";
import { marshal_int32_to_js, end_marshal_task_to_js, marshal_string_to_js, begin_marshal_task_to_js } from "./marshal-to-js";
import { do_not_force_dispose } from "./gc-handles";

export function init_managed_exports(): void {
    const exports_fqn_asm = "System.Runtime.InteropServices.JavaScript";
    runtimeHelpers.runtime_interop_module = cwraps.mono_wasm_assembly_load(exports_fqn_asm);
    if (!runtimeHelpers.runtime_interop_module)
        throw "Can't find bindings module assembly: " + exports_fqn_asm;

    runtimeHelpers.runtime_interop_namespace = "System.Runtime.InteropServices.JavaScript";
    runtimeHelpers.runtime_interop_exports_classname = "JavaScriptExports";
    runtimeHelpers.runtime_interop_exports_class = cwraps.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, runtimeHelpers.runtime_interop_exports_classname);
    if (!runtimeHelpers.runtime_interop_exports_class)
        throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + " class";

    const install_main_synchronization_context = WasmEnableThreads ? get_method("InstallMainSynchronizationContext") : undefined;
    mono_assert(!WasmEnableThreads || install_main_synchronization_context, "Can't find InstallMainSynchronizationContext method");
    const call_entry_point = get_method("CallEntrypoint");
    mono_assert(call_entry_point, "Can't find CallEntrypoint method");
    const release_js_owned_object_by_gc_handle_method = get_method("ReleaseJSOwnedObjectByGCHandle");
    mono_assert(release_js_owned_object_by_gc_handle_method, "Can't find ReleaseJSOwnedObjectByGCHandle method");
    const complete_task_method = get_method("CompleteTask");
    mono_assert(complete_task_method, "Can't find CompleteTask method");
    const call_delegate_method = get_method("CallDelegate");
    mono_assert(call_delegate_method, "Can't find CallDelegate method");
    const get_managed_stack_trace_method = get_method("GetManagedStackTrace");
    mono_assert(get_managed_stack_trace_method, "Can't find GetManagedStackTrace method");
    const load_satellite_assembly_method = get_method("LoadSatelliteAssembly");
    mono_assert(load_satellite_assembly_method, "Can't find LoadSatelliteAssembly method");
    const load_lazy_assembly_method = get_method("LoadLazyAssembly");
    mono_assert(load_lazy_assembly_method, "Can't find LoadLazyAssembly method");

    runtimeHelpers.javaScriptExports.call_entry_point = async (entry_point: MonoMethod, program_args?: string[]): Promise<number> => {
        loaderHelpers.assert_runtime_running();
        const sp = Module.stackSave();
        try {
            Module.runtimeKeepalivePush();
            const args = alloc_stack_frame(4);
            const res = get_arg(args, 1);
            const arg1 = get_arg(args, 2);
            const arg2 = get_arg(args, 3);
            marshal_intptr_to_cs(arg1, entry_point);
            if (program_args && program_args.length == 0) {
                program_args = undefined;
            }
            marshal_array_to_cs_impl(arg2, program_args, MarshalerType.String);

            // because this is async, we could pre-allocate the promise
            let promise = begin_marshal_task_to_js(res, MarshalerType.TaskPreCreated, marshal_int32_to_js);

            // NOTE: at the moment this is synchronous call on the same thread and therefore we could marshal (null) result synchronously
            invoke_method_and_handle_exception(call_entry_point, args);

            // in case the C# side returned synchronously
            promise = end_marshal_task_to_js(args, marshal_int32_to_js, promise);

            if (promise === null || promise === undefined) {
                promise = Promise.resolve(0);
            }
            (promise as any)[do_not_force_dispose] = true; // prevent disposing the task in forceDisposeProxies()
            return await promise;
        } finally {
            Module.runtimeKeepalivePop();// after await promise !
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.load_satellite_assembly = (dll: Uint8Array): void => {
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);
            const arg1 = get_arg(args, 2);
            set_arg_type(arg1, MarshalerType.Array);
            marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
            invoke_method_and_handle_exception(load_satellite_assembly_method, args);
        } finally {
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.load_lazy_assembly = (dll: Uint8Array, pdb: Uint8Array | null): void => {
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(4);
            const arg1 = get_arg(args, 2);
            const arg2 = get_arg(args, 3);
            set_arg_type(arg1, MarshalerType.Array);
            set_arg_type(arg2, MarshalerType.Array);
            marshal_array_to_cs(arg1, dll, MarshalerType.Byte);
            marshal_array_to_cs(arg2, pdb, MarshalerType.Byte);
            invoke_method_and_handle_exception(load_lazy_assembly_method, args);
        } finally {
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle = (gc_handle: GCHandle) => {
        mono_assert(gc_handle, "Must be valid gc_handle");
        loaderHelpers.assert_runtime_running();
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);
            const arg1 = get_arg(args, 2);
            set_arg_type(arg1, MarshalerType.Object);
            set_gc_handle(arg1, gc_handle);
            invoke_method_and_handle_exception(release_js_owned_object_by_gc_handle_method, args);
        } finally {
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.complete_task = (holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs) => {
        loaderHelpers.assert_runtime_running();
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(5);
            const arg1 = get_arg(args, 2);
            set_arg_type(arg1, MarshalerType.Object);
            set_gc_handle(arg1, holder_gc_handle);
            const arg2 = get_arg(args, 3);
            if (error) {
                marshal_exception_to_cs(arg2, error);
            } else {
                set_arg_type(arg2, MarshalerType.None);
                const arg3 = get_arg(args, 4);
                mono_assert(res_converter, "res_converter missing");
                res_converter(arg3, data);
            }
            invoke_method_and_handle_exception(complete_task_method, args);
        } finally {
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.call_delegate = (callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs) => {
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

            invoke_method_and_handle_exception(call_delegate_method, args);

            if (res_converter) {
                const res = get_arg(args, 1);
                return res_converter(res);
            }
        } finally {
            Module.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.get_managed_stack_trace = (exception_gc_handle: GCHandle) => {
        loaderHelpers.assert_runtime_running();
        const sp = Module.stackSave();
        try {
            const args = alloc_stack_frame(3);

            const arg1 = get_arg(args, 2);
            set_arg_type(arg1, MarshalerType.Exception);
            set_gc_handle(arg1, exception_gc_handle);

            invoke_method_and_handle_exception(get_managed_stack_trace_method, args);
            const res = get_arg(args, 1);
            return marshal_string_to_js(res);
        } finally {
            Module.stackRestore(sp);
        }
    };
    if (WasmEnableThreads && install_main_synchronization_context) {
        runtimeHelpers.javaScriptExports.install_main_synchronization_context = () => invoke_method_raw(install_main_synchronization_context);
    }
}

export function get_method(method_name: string): MonoMethod {
    const res = cwraps.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + "." + method_name;
    return res;
}
