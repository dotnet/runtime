// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ManagedObject } from "./marshaled-types";
import { ControllablePromise, GCHandle, MarshalerToCs } from "./types";
import { isRuntimeRunning } from "./utils";
import { lookupJsOwnedObject, teardownManagedProxy } from "./gc-handles";
import { marshalCsObjectToCs } from "./marshal-to-cs";
import { completeTask } from "./managed-exports";

const promiseHolderSymbol = Symbol.for("wasm promise_holder");

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
    Module.safeSetTimeout(() => {
        if (!isRuntimeRunning()) {
            dotnetLogger.debug("This promise can't be canceled, mono runtime already exited.");
            return;
        }
        const holder = lookupJsOwnedObject(task_holder_gc_handle) as PromiseHolder;
        dotnetAssert.check(!!holder, () => `Expected Promise for GCHandle ${task_holder_gc_handle}`);
        holder.cancel();
    }, 0);
}

export class PromiseHolder extends ManagedObject {
    public isResolved = false;
    public isPosted = false;
    public isPostponed = false;
    public data: any = null;
    public reason: any = undefined;
    public constructor(public promise: Promise<any>,
        private gc_handle: GCHandle,
        private res_converter?: MarshalerToCs) {
        super();
    }

    resolve(data: any) {
        if (!isRuntimeRunning()) {
            dotnetLogger.debug("This promise resolution can't be propagated to managed code, runtime already exited.");
            return;
        }
        dotnetAssert.check(!this.isResolved, "resolve could be called only once");
        dotnetAssert.check(!this.isDisposed, "resolve is already disposed.");
        this.isResolved = true;
        this.completeTaskWrapper(data, null);
    }

    reject(reason: any) {
        if (!isRuntimeRunning()) {
            dotnetLogger.debug("This promise rejection can't be propagated to managed code, runtime already exited.");
            return;
        }
        if (!reason) {
            reason = new Error() as any;
        }
        dotnetAssert.check(!this.isResolved, "reject could be called only once");
        dotnetAssert.check(!this.isDisposed, "resolve is already disposed.");
        this.isResolved = true;
        this.completeTaskWrapper(null, reason);
    }

    cancel() {
        if (!isRuntimeRunning()) {
            dotnetLogger.debug("This promise cancelation can't be propagated to managed code, runtime already exited.");
            return;
        }
        dotnetAssert.check(!this.isResolved, "cancel could be called only once");
        dotnetAssert.check(!this.isDisposed, "resolve is already disposed.");

        if (this.isPostponed) {
            // there was racing resolve/reject which was postponed, to retain valid GCHandle
            // in this case we just finish the original resolve/reject
            // and we need to use the postponed data/reason
            this.isResolved = true;
            if (this.reason !== undefined) {
                this.completeTaskWrapper(null, this.reason);
            } else {
                this.completeTaskWrapper(this.data, null);
            }
        } else {
            // there is no racing resolve/reject, we can reject/cancel the promise
            const promise = this.promise;
            assertIsControllablePromise(promise);
            const pcs = dotnetLoaderExports.getPromiseCompletionSource(promise);

            const reason = new Error("OperationCanceledException") as any;
            reason[promiseHolderSymbol] = this;
            pcs.reject(reason);
        }
    }

    // we can do this just once, because it will be dispose the GCHandle
    completeTaskWrapper(data: any, reason: any) {
        try {
            dotnetAssert.check(!this.isPosted, "Promise is already posted to managed.");
            this.isPosted = true;

            // we can unregister the GC handle just on JS side
            teardownManagedProxy(this, this.gc_handle, /*skipManaged: */ true);
            // order of operations with teardown_managed_proxy matters
            // so that managed user code running in the continuation could allocate the same GCHandle number and the local registry would be already ok with that
            completeTask(this.gc_handle, reason, data, this.res_converter || marshalCsObjectToCs);
        } catch (ex) {
            // there is no point to propagate the exception into the unhandled promise rejection
        }
    }
}

export function assertIsControllablePromise(promise: Promise<any>): asserts promise is ControllablePromise<any> {
    if (!dotnetLoaderExports.isControllablePromise(promise)) {
        throw new Error("Expected a controllable promise.");
    }
}
