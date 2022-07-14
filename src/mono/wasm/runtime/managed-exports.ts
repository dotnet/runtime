// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { GCHandle, MonoMethod, mono_assert } from "./types";
import cwraps from "./cwraps";
import { Module, runtimeHelpers } from "./imports";
import { alloc_stack_frame, get_arg, get_arg_gc_handle, JSMarshalerArguments, set_gc_handle } from "./marshal";
import { invoke_method_and_handle_exception } from "./invoke-cs";

// in all the exported internals methods, we use the same data structures for stack frame as normal full blow interop
export interface JavaScriptExports {
    // see src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\Interop\JavaScriptExports.cs
    _release_js_owned_object_by_gc_handle(gc_handle: GCHandle): void;//ReleaseJSOwnedObjectByGCHandle
    _create_task_callback(): GCHandle;// CreateTaskCallback
    _complete_task(args: JSMarshalerArguments): void;// CompleteTask
    _call_delegate(args: JSMarshalerArguments): void;// CallDelegate
}

export const javaScriptExports: JavaScriptExports = <any>{};

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


    const release_js_owned_object_by_gc_handle_method = get_method("ReleaseJSOwnedObjectByGCHandle");
    mono_assert(release_js_owned_object_by_gc_handle_method, "Can't find ReleaseJSOwnedObjectByGCHandle method");
    const create_task_callback_method = get_method("CreateTaskCallback");
    mono_assert(create_task_callback_method, "Can't find CreateTaskCallback method");
    const complete_task_method = get_method("CompleteTask");
    mono_assert(complete_task_method, "Can't find CompleteTask method");
    const call_delegate_method = get_method("CallDelegate");
    mono_assert(call_delegate_method, "Can't find CallDelegate method");

    javaScriptExports._release_js_owned_object_by_gc_handle = (gc_handle: GCHandle) => {
        if (!gc_handle) {
            Module.printErr("Must be valid gc_handle");
        }
        mono_assert(gc_handle, "Must be valid gc_handle");
        const sp = anyModule.stackSave();
        try {
            const args = alloc_stack_frame(3);
            const arg1 = get_arg(args, 2);
            set_gc_handle(arg1, gc_handle);
            invoke_method_and_handle_exception(release_js_owned_object_by_gc_handle_method, args);
        } finally {
            anyModule.stackRestore(sp);
        }
    };
    javaScriptExports._create_task_callback = () => {
        const sp = anyModule.stackSave();
        try {
            const args = alloc_stack_frame(3);
            invoke_method_and_handle_exception(create_task_callback_method, args);
            const res = get_arg(args, 1);
            return get_arg_gc_handle(res);
        } finally {
            anyModule.stackRestore(sp);
        }
    };
    javaScriptExports._complete_task = (args: JSMarshalerArguments) => {
        invoke_method_and_handle_exception(complete_task_method, args);
    };
    javaScriptExports._call_delegate = (args: JSMarshalerArguments) => {
        invoke_method_and_handle_exception(call_delegate_method, args);
    };
}

export function get_method(method_name: string): MonoMethod {
    const res = cwraps.mono_wasm_assembly_find_method(runtimeHelpers.runtime_interop_exports_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + runtimeHelpers.runtime_interop_exports_classname + "." + method_name;
    return res;
}