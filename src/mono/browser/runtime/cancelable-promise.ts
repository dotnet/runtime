// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { _lookup_js_owned_object, teardown_managed_proxy, upgrade_managed_proxy_to_strong_ref } from "./gc-handles";
import { createPromiseController, loaderHelpers, mono_assert } from "./globals";
import { ControllablePromise, GCHandle, MarshalerToCs } from "./types/internal";
import { ManagedObject } from "./marshal";
import { compareExchangeI32, forceThreadMemoryViewRefresh } from "./memory";
import { mono_log_debug } from "./logging";
import { complete_task } from "./managed-exports";
import { marshal_cs_object_to_cs } from "./marshal-to-cs";
import { invoke_later_when_on_ui_thread_async } from "./invoke-js";

export const _are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");

export function isThenable (js_obj: any): boolean {
    // When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
    // to identify the object as a Promise.
    return Promise.resolve(js_obj) === js_obj ||
        ((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function");
}

export function wrap_as_cancelable_promise<T> (fn: () => Promise<T>): ControllablePromise<T> {
    const { promise, promise_control } = createPromiseController<T>();
    const inner = fn();
    inner.then((data) => promise_control.resolve(data)).catch((reason) => promise_control.reject(reason));
    return promise;
}

export function wrap_as_cancelable<T> (inner: Promise<T>): ControllablePromise<T> {
    const { promise, promise_control } = createPromiseController<T>();
    inner.then((data) => promise_control.resolve(data)).catch((reason) => promise_control.reject(reason));
    return promise;
}

export function mono_wasm_cancel_promise (task_holder_gc_handle: GCHandle): void {
    // cancelation should not arrive earlier than the promise created by marshaling in mono_wasm_invoke_jsimport_MT
    invoke_later_when_on_ui_thread_async(() => mono_wasm_cancel_promise_impl(task_holder_gc_handle));
}

export function mono_wasm_cancel_promise_impl (task_holder_gc_handle: GCHandle): void {
    if (!loaderHelpers.is_runtime_running()) {
        mono_log_debug("This promise can't be canceled, mono runtime already exited.");
        return;
    }
    const holder = _lookup_js_owned_object(task_holder_gc_handle) as PromiseHolder;
    mono_assert(!!holder, () => `Expected Promise for GCHandle ${task_holder_gc_handle}`);
    holder.cancel();
}

// NOTE: layout has to match PromiseHolderState in JSHostImplementation.Types.cs
const enum PromiseHolderState {
    IsResolving = 0,
}

const promise_holder_symbol = Symbol.for("wasm promise_holder");

export class PromiseHolder extends ManagedObject {
    public isResolved = false;
    public isPosted = false;
    public isPostponed = false;
    public data: any = null;
    public reason: any = undefined;
    public constructor (public promise: Promise<any>,
        private gc_handle: GCHandle,
        private promiseHolderPtr: number, // could be null for GCV_handle
        private res_converter?: MarshalerToCs) {
        super();
    }

    // returns false if the promise is being canceled by another thread in managed code
    setIsResolving (): boolean {
        if (!WasmEnableThreads || this.promiseHolderPtr === 0) {
            return true;
        }
        forceThreadMemoryViewRefresh();
        if (compareExchangeI32(this.promiseHolderPtr + PromiseHolderState.IsResolving, 1, 0) === 0) {
            return true;
        }
        return false;
    }

    resolve (data: any) {
        if (!loaderHelpers.is_runtime_running()) {
            mono_log_debug("This promise resolution can't be propagated to managed code, mono runtime already exited.");
            return;
        }
        mono_assert(!this.isResolved, "resolve could be called only once");
        mono_assert(!this.isDisposed, "resolve is already disposed.");
        if (WasmEnableThreads && !this.setIsResolving()) {
            // we know that cancelation is in flight
            // because we need to keep the GCHandle alive until until the cancelation arrives
            // we skip the this resolve and let the cancelation to reject the Task
            // we store the original data and use it later
            this.data = data;
            this.isPostponed = true;

            // but after the promise is resolved, nothing holds the weak reference to the PromiseHolder anymore
            // we know that cancelation is in flight, so we upgrade the weak reference to strong for the meantime
            upgrade_managed_proxy_to_strong_ref(this, this.gc_handle);
            return;
        }
        this.isResolved = true;
        this.complete_task_wrapper(data, null);
    }

    reject (reason: any) {
        if (!loaderHelpers.is_runtime_running()) {
            mono_log_debug("This promise rejection can't be propagated to managed code, mono runtime already exited.");
            return;
        }
        if (!reason) {
            reason = new Error() as any;
        }
        mono_assert(!this.isResolved, "reject could be called only once");
        mono_assert(!this.isDisposed, "resolve is already disposed.");
        const isCancelation = reason[promise_holder_symbol] === this;
        if (WasmEnableThreads && !isCancelation && !this.setIsResolving()) {
            // we know that cancelation is in flight
            // because we need to keep the GCHandle alive until until the cancelation arrives
            // we skip the this reject and let the cancelation to reject the Task
            // we store the original reason and use it later
            this.reason = reason;
            this.isPostponed = true;

            // but after the promise is resolved, nothing holds the weak reference to the PromiseHolder anymore
            // we know that cancelation is in flight, so we upgrade the weak reference to strong for the meantime
            upgrade_managed_proxy_to_strong_ref(this, this.gc_handle);
            return;
        }
        this.isResolved = true;
        this.complete_task_wrapper(null, reason);
    }

    cancel () {
        if (!loaderHelpers.is_runtime_running()) {
            mono_log_debug("This promise cancelation can't be propagated to managed code, mono runtime already exited.");
            return;
        }
        mono_assert(!this.isResolved, "cancel could be called only once");
        mono_assert(!this.isDisposed, "resolve is already disposed.");

        if (this.isPostponed) {
            // there was racing resolve/reject which was postponed, to retain valid GCHandle
            // in this case we just finish the original resolve/reject
            // and we need to use the postponed data/reason
            this.isResolved = true;
            if (this.reason !== undefined) {
                this.complete_task_wrapper(null, this.reason);
            } else {
                this.complete_task_wrapper(this.data, null);
            }
        } else {
            // there is no racing resolve/reject, we can reject/cancel the promise
            const promise = this.promise;
            loaderHelpers.assertIsControllablePromise(promise);
            const promise_control = loaderHelpers.getPromiseController(promise);

            const reason = new Error("OperationCanceledException") as any;
            reason[promise_holder_symbol] = this;
            promise_control.reject(reason);
        }
    }

    // we can do this just once, because it will be dispose the GCHandle
    complete_task_wrapper (data: any, reason: any) {
        try {
            mono_assert(!this.isPosted, "Promise is already posted to managed.");
            this.isPosted = true;
            forceThreadMemoryViewRefresh();

            // we can unregister the GC handle just on JS side
            teardown_managed_proxy(this, this.gc_handle, /*skipManaged: */ true);
            // order of operations with teardown_managed_proxy matters
            // so that managed user code running in the continuation could allocate the same GCHandle number and the local registry would be already ok with that
            complete_task(this.gc_handle, reason, data, this.res_converter || marshal_cs_object_to_cs);
        } catch (ex) {
            try {
                loaderHelpers.mono_exit(1, ex);
            } catch (ex2) {
                // there is no point to propagate the exception into the unhandled promise rejection
            }
        }
    }
}
