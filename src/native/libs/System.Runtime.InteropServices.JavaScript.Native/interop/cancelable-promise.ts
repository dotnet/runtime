// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetAssert, dotnetLoaderExports, dotnetLogger } from "./cross-module";

import { ControllablePromise, GCHandle } from "./types";
import { isRuntimeRunning } from "./utils";
import { lookupJsOwnedObject } from "./gc-handles";
import { PromiseHolder } from "./marshaled-types";

export function isThenable(js_obj: any): boolean {
    // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
    // to identify the object as a Promise.
    return Promise.resolve(js_obj) === js_obj ||
        ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
}

export function wrapAsCancelablePromise<T>(fn: () => Promise<T>): ControllablePromise<T> {
    const pcs = dotnetLoaderExports.createPromiseCompletionSource<T>();
    const inner = fn();
    inner.then((data) => pcs.resolve(data)).catch((reason) => pcs.reject(reason));
    return pcs.promise;
}

export function wrapAsCancelable<T>(inner: Promise<T>): ControllablePromise<T> {
    const pcs = dotnetLoaderExports.createPromiseCompletionSource<T>();
    inner.then((data) => pcs.resolve(data)).catch((reason) => pcs.reject(reason));
    return pcs.promise;
}

export function cancelPromise(task_holder_gc_handle: GCHandle): void {
    // cancelation should not arrive earlier than the promise created by marshaling in SystemInteropJS_InvokeJSImportSync
    if (!isRuntimeRunning()) {
        dotnetLogger.debug("This promise can't be canceled, mono runtime already exited.");
        return;
    }
    const holder = lookupJsOwnedObject(task_holder_gc_handle) as PromiseHolder;
    dotnetAssert.fastCheck(!!holder, () => `Expected Promise for GCHandle ${task_holder_gc_handle}`);
    holder.cancel();
}
