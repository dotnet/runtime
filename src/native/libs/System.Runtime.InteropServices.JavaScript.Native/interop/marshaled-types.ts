// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { GCHandle, TypedArray, VoidPtr } from "../types";
import { dotnetApi, dotnetAssert, dotnetLoaderExports, dotnetLogger } from "./cross-module";

import { jsOwnedGcHandleSymbol, promiseHolderSymbol, teardownManagedProxy } from "./gc-handles";
import { completeTask, getManagedStackTrace } from "./managed-exports";
import { GCHandleNull, IMemoryView, MarshalerToCs } from "./types";
import { isRuntimeRunning } from "./utils";

export const enum MemoryViewType {
    Byte = 0,
    Int32 = 1,
    Double = 2,
}

abstract class MemoryView implements IMemoryView {
    protected constructor(public _pointer: VoidPtr, public _length: number, public _viewType: MemoryViewType) {
    }

    abstract dispose(): void;
    abstract get isDisposed(): boolean;

    _unsafe_create_view(): TypedArray {
        // this view must be short lived so that it doesn't fail after wasm memory growth
        // for that reason we also don't give the view out to end user and provide set/slice/copyTo API instead
        const view = this._viewType == MemoryViewType.Byte ? new Uint8Array(dotnetApi.localHeapViewU8().buffer, this._pointer as any >>> 0, this._length)
            : this._viewType == MemoryViewType.Int32 ? new Int32Array(dotnetApi.localHeapViewI32().buffer, this._pointer as any >>> 0, this._length)
                : this._viewType == MemoryViewType.Double ? new Float64Array(dotnetApi.localHeapViewF64().buffer, this._pointer as any >>> 0, this._length)
                    : null;
        if (!view) throw new Error("NotImplementedException");
        return view;
    }

    set(source: TypedArray, targetOffset?: number): void {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        const targetView = this._unsafe_create_view();
        dotnetAssert.fastCheck(source && targetView && source.constructor === targetView.constructor, () => `Expected ${targetView.constructor}`);
        targetView.set(source, targetOffset || 0 >>> 0);
        // TODO consider memory write barrier
    }

    copyTo(target: TypedArray, sourceOffset?: number): void {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        dotnetAssert.fastCheck(target && sourceView && target.constructor === sourceView.constructor, () => `Expected ${sourceView.constructor}`);
        const trimmedSource = sourceView.subarray(sourceOffset || 0 >>> 0);
        // TODO consider memory read barrier
        target.set(trimmedSource);
    }

    slice(start?: number, end?: number): TypedArray {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        // TODO consider memory read barrier
        return sourceView.slice(start || 0 >>> 0, end ? end >>> 0 : undefined);
    }

    get length(): number {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        return this._length;
    }

    get byteLength(): number {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        return this._viewType == MemoryViewType.Byte ? this._length
            : this._viewType == MemoryViewType.Int32 ? this._length << 2
                : this._viewType == MemoryViewType.Double ? this._length << 3
                    : 0;
    }
}


export class Span extends MemoryView {
    private _isDisposed = false;
    public constructor(pointer: VoidPtr, length: number, viewType: MemoryViewType) {
        super(pointer, length, viewType);
    }
    dispose(): void {
        this._isDisposed = true;
    }
    get isDisposed(): boolean {
        return this._isDisposed;
    }
}

export class ArraySegment extends MemoryView {
    public constructor(pointer: VoidPtr, length: number, viewType: MemoryViewType) {
        super(pointer, length, viewType);
    }

    dispose(): void {
        teardownManagedProxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (this as any)[jsOwnedGcHandleSymbol] === GCHandleNull;
    }
}

export interface IDisposable {
    dispose(): void;
    get isDisposed(): boolean;
}

export class ManagedObject implements IDisposable {
    dispose(): void {
        teardownManagedProxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (this as any)[jsOwnedGcHandleSymbol] === GCHandleNull;
    }

    toString(): string {
        return `CsObject(gcHandle: ${(this as any)[jsOwnedGcHandleSymbol]})`;
    }
}

export class ManagedError extends Error implements IDisposable {
    private superStack: any;
    private managedStack: any;
    constructor(message: string) {
        super(message);
        this.superStack = Object.getOwnPropertyDescriptor(this, "stack"); // this works on Chrome
        Object.defineProperty(this, "stack", {
            get: this.getManageStack,
        });
    }

    getSuperStack() {
        if (this.superStack) {
            if (this.superStack.value !== undefined)
                return this.superStack.value;
            if (this.superStack.get !== undefined)
                return this.superStack.get.call(this);
        }
        return super.stack; // this works on FF
    }

    getManageStack() {
        if (this.managedStack) {
            return this.managedStack;
        }
        if (!isRuntimeRunning()) {
            this.managedStack = "... omitted managed stack trace.\n" + this.getSuperStack();
            return this.managedStack;
        }
        const gcHandle = (this as any)[jsOwnedGcHandleSymbol];
        if (gcHandle !== GCHandleNull) {
            const managedStack = getManagedStackTrace(gcHandle);
            if (managedStack) {
                this.managedStack = managedStack + "\n" + this.getSuperStack();
                return this.managedStack;
            }
        }
        return this.getSuperStack();
    }

    dispose(): void {
        teardownManagedProxy(this, GCHandleNull);
    }

    get isDisposed(): boolean {
        return (this as any)[jsOwnedGcHandleSymbol] === GCHandleNull;
    }
}

export class PromiseHolder extends ManagedObject {
    public isResolved = false;
    public isPosted = false;
    public isPostponed = false;
    public data: any = null;
    public reason: any = undefined;
    public constructor(public promise: Promise<any>,
        private gc_handle: GCHandle,
        private res_converter: MarshalerToCs) {
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
            if (!dotnetLoaderExports.isControllablePromise(promise)) {
                throw new Error("Expected a controllable promise.");
            }
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
            completeTask(this.gc_handle, reason, data, this.res_converter);
        } catch (ex) {
            // there is no point to propagate the exception into the unhandled promise rejection
        }
    }
}
