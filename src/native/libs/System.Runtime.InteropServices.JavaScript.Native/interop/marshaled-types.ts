// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { TypedArray, VoidPtr } from "../types";
import { jsOwnedGcHandleSymbol, teardownManagedProxy } from "./gc-handles";
import { getManagedStackTrace } from "./managed-exports";
import { GCHandleNull, IMemoryView } from "./types";
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
        dotnetAssert.check(source && targetView && source.constructor === targetView.constructor, () => `Expected ${targetView.constructor}`);
        targetView.set(source, targetOffset || 0 >>> 0);
        // TODO consider memory write barrier
    }

    copyTo(target: TypedArray, sourceOffset?: number): void {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        dotnetAssert.check(target && sourceView && target.constructor === sourceView.constructor, () => `Expected ${sourceView.constructor}`);
        const trimmedSource = sourceView.subarray(sourceOffset || 0 >>> 0);
        // TODO consider memory read barrier
        target.set(trimmedSource);
    }

    slice(start?: number, end?: number): TypedArray {
        dotnetAssert.check(!this.isDisposed, "ObjectDisposedException");
        const sourceView = this._unsafe_create_view();
        // TODO consider memory read barrier
        return sourceView.slice(start || 0 >>> 0, end || 0 >>> 0);
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
