// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { JSHandle, GCHandle, MonoObject, MonoObjectRef } from "./types";
import { PromiseControl } from "./cancelable-promise";
import { runtimeHelpers } from "./imports";

const fn_signatures: [jsname: string, csname: string, signature: string/*ArgsMarshalString*/][] = [
    ["_get_cs_owned_object_by_js_handle_ref", "GetCSOwnedObjectByJSHandleRef", "iim"],
    ["_get_cs_owned_object_js_handle_ref", "GetCSOwnedObjectJSHandleRef", "mi"],
    ["_try_get_cs_owned_object_js_handle_ref", "TryGetCSOwnedObjectJSHandleRef", "mi"],
    ["_create_cs_owned_proxy_ref", "CreateCSOwnedProxyRef", "iiim"],

    ["_get_js_owned_object_by_gc_handle_ref", "GetJSOwnedObjectByGCHandleRef", "im"],
    ["_get_js_owned_object_gc_handle_ref", "GetJSOwnedObjectGCHandleRef", "m"],
    ["_release_js_owned_object_by_gc_handle", "ReleaseJSOwnedObjectByGCHandle", "i"],

    ["_create_tcs", "CreateTaskSource", ""],
    ["_set_tcs_result", "SetTaskSourceResult", "io"],
    ["_set_tcs_failure", "SetTaskSourceFailure", "is"],
    ["_get_tcs_task", "GetTaskSourceTask", "i!"],
    ["_task_from_result", "TaskFromResult", "o!"],
    ["_setup_js_cont", "SetupJSContinuation", "mo"],

    ["_object_to_string_ref", "ObjectToStringRef", "m"],
    ["_get_date_value_ref", "GetDateValueRef", "m"],
    ["_create_date_time", "CreateDateTime", "d!"],
    ["_create_uri", "CreateUri", "s!"],
    ["_is_simple_array_ref", "IsSimpleArrayRef", "m"],
];

export interface t_CSwraps {
    // BINDING
    _get_cs_owned_object_by_js_handle_ref(jsHandle: JSHandle, shouldAddInflight: 0 | 1, result: MonoObjectRef): void;
    _get_cs_owned_object_js_handle_ref(obj: MonoObjectRef, shouldAddInflight: 0 | 1): JSHandle;
    _try_get_cs_owned_object_js_handle_ref(obj: MonoObjectRef, shouldAddInflight: 0 | 1): JSHandle;
    _create_cs_owned_proxy_ref(jsHandle: JSHandle, mappedType: number, shouldAddInflight: 0 | 1, result: MonoObjectRef): void;

    _get_js_owned_object_by_gc_handle_ref(gcHandle: GCHandle, result: MonoObjectRef): void;
    _get_js_owned_object_gc_handle_ref(obj: MonoObjectRef): GCHandle
    _release_js_owned_object_by_gc_handle(gcHandle: GCHandle): void;

    _create_tcs(): GCHandle;
    // FIXME: We currently rely on marshaling to convert result types
    _set_tcs_result(gcHandle: GCHandle, result: any): void
    _set_tcs_failure(gcHandle: GCHandle, result: string): void
    _get_tcs_task(gcHandle: GCHandle): MonoObject;
    _task_from_result(result: MonoObject): MonoObject
    _setup_js_cont(task: MonoObject, continuation: PromiseControl): MonoObject

    _object_to_string_ref(obj: MonoObjectRef): string;
    _get_date_value_ref(obj: MonoObjectRef): number;
    _create_date_time(ticks: number): MonoObject;
    _create_uri(uri: string): MonoObject;
    _is_simple_array_ref(obj: MonoObjectRef): boolean;
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
