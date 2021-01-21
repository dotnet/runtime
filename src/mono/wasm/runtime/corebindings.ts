// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSHandle, GCHandle, MonoObject, MonoType, MonoMethod, MarshalSignatureInfo } from "./types";
import { ArgsMarshalString } from "./method-binding";
import { PromiseControl } from "./cancelable-promise";
import { runtimeHelpers } from "./imports";

const fn_signatures: [jsname: string, csname: string, signature: ArgsMarshalString][] = [
    ["_get_cs_owned_object_by_js_handle", "GetCSOwnedObjectByJSHandle", "ii!"],
    ["_get_cs_owned_object_js_handle", "GetCSOwnedObjectJSHandle", "mi"],
    ["_try_get_cs_owned_object_js_handle", "TryGetCSOwnedObjectJSHandle", "mi"],
    ["_create_cs_owned_proxy", "CreateCSOwnedProxy", "iii!"],

    ["_get_js_owned_object_by_gc_handle", "GetJSOwnedObjectByGCHandle", "i!"],
    ["_get_js_owned_object_gc_handle", "GetJSOwnedObjectGCHandle", "m"],
    ["_release_js_owned_object_by_gc_handle", "ReleaseJSOwnedObjectByGCHandle", "i"],

    ["_create_tcs", "CreateTaskSource", ""],
    ["_set_tcs_result", "SetTaskSourceResult", "io"],
    ["_set_tcs_failure", "SetTaskSourceFailure", "is"],
    ["_get_tcs_task", "GetTaskSourceTask", "i!"],
    ["_task_from_result", "TaskFromResult", "o!"],
    ["_setup_js_cont", "SetupJSContinuation", "mo"],

    ["_object_to_string", "ObjectToString", "m"],
    ["_is_simple_array", "IsSimpleArray", "m"],

    ["make_marshal_signature_info", "MakeMarshalSignatureInfo", "ii"],
    ["get_custom_marshaler_info", "GetCustomMarshalerInfoForType", "is"],
];

export interface t_CSwraps {
    // BINDING
    _get_cs_owned_object_by_js_handle(jsHandle: JSHandle, shouldAddInflight: 0 | 1): MonoObject;
    _get_cs_owned_object_js_handle(jsHandle: JSHandle, shouldAddInflight: 0 | 1): JSHandle;
    _try_get_cs_owned_object_js_handle(obj: MonoObject, shouldAddInflight: 0 | 1): JSHandle;
    _create_cs_owned_proxy(jsHandle: JSHandle, mappedType: number, shouldAddInflight: 0 | 1): MonoObject;

    _get_js_owned_object_by_gc_handle(gcHandle: GCHandle): MonoObject;
    _get_js_owned_object_gc_handle(obj: MonoObject): GCHandle
    _release_js_owned_object_by_gc_handle(gcHandle: GCHandle): void;

    _create_tcs(): GCHandle;
    _set_tcs_result(gcHandle: GCHandle, result: MonoObject): void
    _set_tcs_failure(gcHandle: GCHandle, result: string): void
    _get_tcs_task(gcHandle: GCHandle): MonoObject;
    _task_from_result(result: MonoObject): MonoObject
    _setup_js_cont(task: MonoObject, continuation: PromiseControl): MonoObject

    _object_to_string(obj: MonoObject): string;
    _is_simple_array(obj: MonoObject): boolean;

    make_marshal_signature_info(typePtr: MonoType, methodPtr: MonoMethod): string;
    get_custom_marshaler_info(typePtr: MonoType, marshalerFullName: string | null): string;

    generate_args_marshaler(signature: string, methodPtr: MonoMethod): string;
}

const wrapped_cs_functions: t_CSwraps = <any>{};
for (const sig of fn_signatures) {
    const wf: any = wrapped_cs_functions;
    // lazy init on first run
    wf[sig[0]] = function (...args: any[]) {
        const fce = runtimeHelpers.bind_runtime_method(sig[1], sig[2]);
        wf[sig[0]] = fce;
        return fce(...args);
    };
}

export default wrapped_cs_functions;
