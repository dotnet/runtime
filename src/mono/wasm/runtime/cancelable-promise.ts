// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_get_jsobj_from_js_handle } from "./gc-handles";
import { wrap_error_root } from "./method-calls";
import { mono_wasm_new_external_root } from "./roots";
import { JSHandle, MonoObject, MonoObjectRef, MonoString } from "./types";
import { Int32Ptr } from "./types/emscripten";

export const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");
export const promise_control_symbol = Symbol.for("wasm promise_control");

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function isThenable(js_obj: any): boolean {
    // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
    // to identify the object as a Promise.
    return Promise.resolve(js_obj) === js_obj ||
        ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
}

export function mono_wasm_cancel_promise_ref(thenable_js_handle: JSHandle, is_exception: Int32Ptr, result_address: MonoObjectRef): void | MonoString {
    const resultRoot = mono_wasm_new_external_root<MonoObject>(result_address);
    try {
        const promise = mono_wasm_get_jsobj_from_js_handle(thenable_js_handle);
        const promise_control = promise[promise_control_symbol];
        promise_control.reject("OperationCanceledException");
    }
    catch (ex) {
        wrap_error_root(is_exception, ex, resultRoot);
        return;
    }
    finally {
        resultRoot.release();
    }
}

export interface PromiseControl {
    isDone: boolean;
    resolve: (data?: any) => void;
    reject: (reason: any) => void;
}

export function _create_cancelable_promise(afterResolve?: () => void, afterReject?: () => void): {
    promise: Promise<any>, promise_control: PromiseControl
} {
    let promise_control: PromiseControl | null = null;
    const promise = new Promise(function (resolve, reject) {
        promise_control = {
            isDone: false,
            resolve: (data: any) => {
                if (!promise_control!.isDone) {
                    promise_control!.isDone = true;
                    resolve(data);
                    if (afterResolve) {
                        afterResolve();
                    }
                }
            },
            reject: (reason: any) => {
                if (!promise_control!.isDone) {
                    promise_control!.isDone = true;
                    reject(reason);
                    if (afterReject) {
                        afterReject();
                    }
                }
            }
        };
    });
    (<any>promise)[promise_control_symbol] = promise_control;
    return { promise, promise_control: promise_control! };
}