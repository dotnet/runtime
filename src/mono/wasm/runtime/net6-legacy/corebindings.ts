// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSHandle, GCHandle, MonoObjectRef, MonoMethod, MonoObject, WasmRoot } from "../types";
import { mono_bind_method, _create_primitive_converters } from "./method-binding";
import { mono_wasm_new_root } from "../roots";
import { Module, runtimeHelpers } from "../globals";
import cwraps from "../cwraps";
import { PromiseController } from "../promise-controller";
import { legacyHelpers, wasm_type_symbol } from "./globals";
import { find_corlib_class } from "../class-loader";
type SigLine = [lazy: boolean, jsname: string, csname: string, signature: string/*ArgsMarshalString*/];
const fn_signatures: SigLine[] = [
    [true, "_get_cs_owned_object_by_js_handle_ref", "GetCSOwnedObjectByJSHandleRef", "iim"],
    [true, "_get_cs_owned_object_js_handle_ref", "GetCSOwnedObjectJSHandleRef", "mi"],
    [true, "_try_get_cs_owned_object_js_handle_ref", "TryGetCSOwnedObjectJSHandleRef", "mi"],
    [true, "_create_cs_owned_proxy_ref", "CreateCSOwnedProxyRef", "iiim"],

    [true, "_get_js_owned_object_by_gc_handle_ref", "GetJSOwnedObjectByGCHandleRef", "im"],
    [true, "_get_js_owned_object_gc_handle_ref", "GetJSOwnedObjectGCHandleRef", "m"],

    [true, "_create_tcs", "CreateTaskSource", ""],
    [true, "_set_tcs_result_ref", "SetTaskSourceResultRef", "iR"],
    [true, "_set_tcs_failure", "SetTaskSourceFailure", "is"],
    [true, "_get_tcs_task_ref", "GetTaskSourceTaskRef", "im"],
    [true, "_setup_js_cont_ref", "SetupJSContinuationRef", "mo"],

    [true, "_object_to_string_ref", "ObjectToStringRef", "m"],
    [true, "_get_date_value_ref", "GetDateValueRef", "m"],
    [true, "_create_date_time_ref", "CreateDateTimeRef", "dm"],
    [true, "_create_uri_ref", "CreateUriRef", "sm"],
    [true, "_is_simple_array_ref", "IsSimpleArrayRef", "m"],
    [true, "_get_call_sig_ref", "GetCallSignatureRef", "im"],
];

export interface LegacyExports {
    // see src\libraries\System.Runtime.InteropServices.JavaScript\src\System\Runtime\InteropServices\JavaScript\Interop\LegacyExports.cs
    _get_cs_owned_object_by_js_handle_ref(jsHandle: JSHandle, shouldAddInflight: 0 | 1, result: MonoObjectRef): void;
    _get_cs_owned_object_js_handle_ref(obj: MonoObjectRef, shouldAddInflight: 0 | 1): JSHandle;
    _try_get_cs_owned_object_js_handle_ref(obj: MonoObjectRef, shouldAddInflight: 0 | 1): JSHandle;
    _create_cs_owned_proxy_ref(jsHandle: JSHandle, mappedType: number, shouldAddInflight: 0 | 1, result: MonoObjectRef): void;

    _get_js_owned_object_by_gc_handle_ref(gcHandle: GCHandle, result: MonoObjectRef): void;
    _get_js_owned_object_gc_handle_ref(obj: MonoObjectRef): GCHandle

    _create_tcs(): GCHandle;
    _set_tcs_result_ref(gcHandle: GCHandle, result: any): void
    _set_tcs_failure(gcHandle: GCHandle, result: string): void
    _get_tcs_task_ref(gcHandle: GCHandle, result: MonoObjectRef): void;
    _setup_js_cont_ref(task: MonoObjectRef, continuation: PromiseController): void;

    _object_to_string_ref(obj: MonoObjectRef): string;
    _get_date_value_ref(obj: MonoObjectRef): number;
    _create_date_time_ref(ticks: number, result: MonoObjectRef): void;
    _create_uri_ref(uri: string, result: MonoObjectRef): void;
    _is_simple_array_ref(obj: MonoObjectRef): boolean;
    _get_call_sig_ref(method: MonoMethod, obj: WasmRoot<MonoObject>): string;
}

export const legacyManagedExports: LegacyExports = <any>{};


export function bind_runtime_method(method_name: string, signature: string): Function {
    const method = get_method(method_name);
    return mono_bind_method(method, signature, false, "BINDINGS_" + method_name);
}

export function init_legacy_exports(): void {
    // please keep System.Runtime.InteropServices.JavaScript.JSHostImplementation.MappedType in sync
    (<any>Object.prototype)[wasm_type_symbol] = 0;
    (<any>Array.prototype)[wasm_type_symbol] = 1;
    (<any>ArrayBuffer.prototype)[wasm_type_symbol] = 2;
    (<any>DataView.prototype)[wasm_type_symbol] = 3;
    (<any>Function.prototype)[wasm_type_symbol] = 4;
    (<any>Uint8Array.prototype)[wasm_type_symbol] = 11;

    const box_buffer_size = 65536;
    legacyHelpers._unbox_buffer_size = 65536;
    legacyHelpers._box_buffer = Module._malloc(box_buffer_size);
    legacyHelpers._unbox_buffer = Module._malloc(legacyHelpers._unbox_buffer_size);
    legacyHelpers._class_int32 = find_corlib_class("System", "Int32");
    legacyHelpers._class_uint32 = find_corlib_class("System", "UInt32");
    legacyHelpers._class_double = find_corlib_class("System", "Double");
    legacyHelpers._class_boolean = find_corlib_class("System", "Boolean");
    legacyHelpers._null_root = mono_wasm_new_root<MonoObject>();
    _create_primitive_converters();

    legacyHelpers.runtime_legacy_exports_classname = "LegacyExports";
    legacyHelpers.runtime_legacy_exports_class = cwraps.mono_wasm_assembly_find_class(runtimeHelpers.runtime_interop_module, runtimeHelpers.runtime_interop_namespace, legacyHelpers.runtime_legacy_exports_classname);
    if (!legacyHelpers.runtime_legacy_exports_class)
        throw "Can't find " + runtimeHelpers.runtime_interop_namespace + "." + legacyHelpers.runtime_legacy_exports_classname + " class";

    for (const sig of fn_signatures) {
        const wf: any = legacyManagedExports;
        const [lazy, jsname, csname, signature] = sig;
        if (lazy) {
            // lazy init on first run
            wf[jsname] = function (...args: any[]) {
                const fce = bind_runtime_method(csname, signature);
                wf[jsname] = fce;
                return fce(...args);
            };
        }
        else {
            const fce = bind_runtime_method(csname, signature);
            wf[jsname] = fce;
        }
    }
}

export function get_method(method_name: string): MonoMethod {
    const res = cwraps.mono_wasm_assembly_find_method(legacyHelpers.runtime_legacy_exports_class, method_name, -1);
    if (!res)
        throw "Can't find method " + runtimeHelpers.runtime_interop_namespace + "." + legacyHelpers.runtime_legacy_exports_classname + "." + method_name;
    return res;
}