// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { _lookup_js_owned_object } from "./gc-handles";
import { createPromiseController, loaderHelpers } from "./globals";
import { TaskCallbackHolder } from "./marshal-to-cs";
import { ControllablePromise, GCHandle } from "./types/internal";

export const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");

export function isThenable(js_obj: any): boolean {
    // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
    // to identify the object as a Promise.
    return Promise.resolve(js_obj) === js_obj ||
        ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
}

export function wrap_as_cancelable_promise<T>(fn: () => Promise<T>): ControllablePromise<T> {
    const { promise, promise_control } = createPromiseController<T>();
    const inner = fn();
    inner.then((data) => promise_control.resolve(data)).catch((reason) => promise_control.reject(reason));
    return promise;
}

export function mono_wasm_cancel_promise(task_holder_gc_handle: GCHandle): void {
    const holder = _lookup_js_owned_object(task_holder_gc_handle) as TaskCallbackHolder;
    if (!holder) return; // probably already GC collected

    const promise = holder.promise;
    mono_assert(!!promise, () => `Expected Promise for GCHandle ${task_holder_gc_handle}`);
    loaderHelpers.assertIsControllablePromise(promise);
    const promise_control = loaderHelpers.getPromiseController(promise);
    promise_control.reject("OperationCanceledException");
}

