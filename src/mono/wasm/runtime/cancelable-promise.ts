// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _lookup_js_owned_object } from "./gc-handles";
import { TaskCallbackHolder } from "./marshal-to-cs";
import { mono_assert, GCHandle } from "./types";

export const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");
export const promise_control_symbol = Symbol.for("wasm promise_control");

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function isThenable(js_obj: any): boolean {
    // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
    // to identify the object as a Promise.
    return Promise.resolve(js_obj) === js_obj ||
        ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
}

export interface PromiseControl {
    isDone: boolean;
    resolve: (data?: any) => void;
    reject: (reason: any) => void;
    promise: Promise<any>;
}

export function create_cancelable_promise(afterResolve?: () => void, afterReject?: () => void): {
    promise: Promise<any>, promise_control: PromiseControl
} {
    let promise_control: PromiseControl = <any>null;
    const promise = new Promise(function (resolve, reject) {
        promise_control = {
            promise: <any>null,
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
    promise_control.promise = promise;
    (<any>promise)[promise_control_symbol] = promise_control;
    return { promise, promise_control: promise_control! };
}

export function wrap_as_cancelable_promise<T>(fn: () => Promise<T>): Promise<T> {
    const { promise, promise_control } = create_cancelable_promise();
    const inner = fn();
    inner.then((data) => promise_control.resolve(data)).catch((reason) => promise_control.reject(reason));
    return promise;
}

export function mono_wasm_cancel_promise(task_holder_gc_handle: GCHandle): void {
    const holder = _lookup_js_owned_object(task_holder_gc_handle) as TaskCallbackHolder;
    if (!holder) return; // probably already GC collected

    const promise = holder.promise;
    mono_assert(!!promise, () => `Expected Promise for GCHandle ${task_holder_gc_handle}`);
    const promise_control = (<any>promise)[promise_control_symbol];
    mono_assert(!!promise_control, () => `Expected promise_control for GCHandle ${task_holder_gc_handle}`);
    promise_control.reject("OperationCanceledException");
}

