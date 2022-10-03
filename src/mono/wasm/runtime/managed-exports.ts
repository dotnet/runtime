// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { GCHandle, MarshalerToCs, MarshalerToJs, MonoMethod, mono_assert } from "./types";
import cwraps from "./cwraps";
import { Module, runtimeHelpers } from "./imports";
import { alloc_stack_frame, get_arg, get_arg_gc_handle, MarshalerType, set_arg_type, set_gc_handle } from "./marshal";
import { invoke_method_and_handle_exception } from "./invoke-cs";
import { marshal_array_to_cs_impl, marshal_exception_to_cs, marshal_intptr_to_cs } from "./marshal-to-cs";
import { marshal_int32_to_js, marshal_task_to_js } from "./marshal-to-js";

// in all the exported internals methods, we use the same data structures for stack frame as normal full blow interop
// see src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\Interop\JavaScriptExports.cs
export interface JavaScriptExports {
    // the marshaled signature is: void ReleaseJSOwnedObjectByGCHandle(GCHandle gcHandle)
    release_js_owned_object_by_gc_handle(gc_handle: GCHandle): void;
    // the marshaled signature is: GCHandle CreateTaskCallback()
    create_task_callback(): GCHandle;
    // the marshaled signature is: void CompleteTask<T>(GCHandle holder, Exception? exceptionResult, T? result)
    complete_task(holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs): void;
    // the marshaled signature is: TRes? CallDelegate<T1,T2,T3TRes>(GCHandle callback, T1? arg1, T2? arg2, T3? arg3)
    call_delegate(callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any,
        res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs): any;
    // the marshaled signature is: Task<int>? CallEntrypoint(MonoMethod* entrypointPtr, string[] args)
    call_entry_point(entry_point: MonoMethod, args?: string[]): Promise<number>;
}

export function init_managed_exports(): void {
    const anyModule = Module as any;
    const exports_fqn_asm = "System.Runtime.InteropServices.JavaScript";
    runtimeHelpers.runtime_interop_module = cwraps.mono_wasm_assembly_load(exports_fqn_asm);
    if (!runtimeHelpers.runtime_interop_module)
        throw "Can't find bindings module assembly: " + exports_fqn_asm;

    runtimeHelpers.runtime_interop_namespace = "System.Runtime.InteropServices.JavaScript";
    runtimeHelpers.runtime_interop_exports_classname = "JavaScriptExports";
    runtimeHelpers.runtime_interop_exports_class = cwraps.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, runtimeHelpers.runtime_interop_exports_classname);
    if (!runtimeHelpers.runtime_interop_exports_class)
        throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + " class";


    const call_entry_point = get_method("CallEntrypoint");
    mono_assert(call_entry_point, "Can't find CallEntrypoint method");
    const release_js_owned_object_by_gc_handle_method = get_method("ReleaseJSOwnedObjectByGCHandle");
    mono_assert(release_js_owned_object_by_gc_handle_method, "Can't find ReleaseJSOwnedObjectByGCHandle method");
    const create_task_callback_method = get_method("CreateTaskCallback");
    mono_assert(create_task_callback_method, "Can't find CreateTaskCallback method");
    const complete_task_method = get_method("CompleteTask");
    mono_assert(complete_task_method, "Can't find CompleteTask method");
    const call_delegate_method = get_method("CallDelegate");
    mono_assert(call_delegate_method, "Can't find CallDelegate method");
    runtimeHelpers.javaScriptExports.call_entry_point = (entry_point: MonoMethod, program_args?: string[]) => {
        const sp = anyModule.stackSave();
        try {
            const args = alloc_stack_frame(4);
            const res = get_arg(args, 1);
            const arg1 = get_arg(args, 2);
            const arg2 = get_arg(args, 3);
            marshal_intptr_to_cs(arg1, entry_point);
            if (program_args && program_args.length == 0) {
                program_args = undefined;
            }
            marshal_array_to_cs_impl(arg2, program_args, MarshalerType.String);
            invoke_method_and_handle_exception(call_entry_point, args);
            const promise = marshal_task_to_js(res, undefined, marshal_int32_to_js);
            if (!promise) {
                return Promise.resolve(0);
            }
            return promise;
        } finally {
            anyModule.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.release_js_owned_object_by_gc_handle = (gc_handle: GCHandle) => {
        mono_assert(gc_handle, "Must be valid gc_handle");
        const sp = anyModule.stackSave();
        try {
            const args = alloc_stack_frame(3);
            const arg1 = get_arg(args, 2);
            set_arg_type(arg1, MarshalerType.Object);
            set_gc_handle(arg1, gc_handle);
            invoke_method_and_handle_exception(release_js_owned_object_by_gc_handle_method, args);
        } finally {
            anyModule.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.create_task_callback = () => {
        const sp = anyModule.stackSave();
        try {
            const args = alloc_stack_frame(2);
            invoke_method_and_handle_exception(create_task_callback_method, args);
            const res = get_arg(args, 1);
            return get_arg_gc_handle(res);
        } finally {
            anyModule.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.complete_task = (holder_gc_handle: GCHandle, error?: any, data?: any, res_converter?: MarshalerToCs) => {
        const sp = anyModule.stackSave();
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
            anyModule.stackRestore(sp);
        }
    };
    runtimeHelpers.javaScriptExports.call_delegate = (callback_gc_handle: GCHandle, arg1_js: any, arg2_js: any, arg3_js: any, res_converter?: MarshalerToJs, arg1_converter?: MarshalerToCs, arg2_converter?: MarshalerToCs, arg3_converter?: MarshalerToCs) => {
        const sp = anyModule.stackSave();
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
            anyModule.stackRestore(sp);
        }
    };
}

export function get_method(method_name: string): MonoMethod {
    const res = cwraps.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + "." + method_name;
    return res;
}