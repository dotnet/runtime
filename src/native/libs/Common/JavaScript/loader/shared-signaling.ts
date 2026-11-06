// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const enum SignalState { IDLE, PENDING, DONE, ERROR, READY, EXECUTING }
export const enum CommandId { NONE, RUN_MAIN, EXIT, RUN_MAIN_AND_EXIT }

export const SIGNALING_VERSION = 17;
export const OFFSET_VERSION = 0;
export const OFFSET_MEMORY_GENERATION = 1;
export const OFFSET_WAKEUP_MAIN = 2;
export const OFFSET_WAKEUP_WORKER = 3;
export const OFFSET_COMMAND_SIGNAL = 4;
export const OFFSET_COMMAND_RESULT = 5;
export const OFFSET_COMMAND_PROMISE_ID = 6;
export const OFFSET_WORKER_STATE = 7;
export const OFFSET_WORKER_ERROR_LENGTH = 8;
const errorCapacity = 4096;
const bufferSize = OFFSET_WORKER_ERROR_LENGTH + 1 + errorCapacity;
let nextPromiseId = 1;

export function allocatePromiseId(): number {
    const result = nextPromiseId;
    nextPromiseId = (nextPromiseId + 1) | 0;
    return result;
}

export function createSignalingBuffer(): SharedArrayBuffer {
    const buffer = new SharedArrayBuffer(bufferSize * Int32Array.BYTES_PER_ELEMENT);
    Atomics.store(new Int32Array(buffer), OFFSET_VERSION, SIGNALING_VERSION);
    return buffer;
}

export function wakeMain(signal: Int32Array): void {
    Atomics.add(signal, OFFSET_WAKEUP_MAIN, 1);
    Atomics.notify(signal, OFFSET_WAKEUP_MAIN);
}

export function wakeWorker(signal: Int32Array): void {
    Atomics.add(signal, OFFSET_WAKEUP_WORKER, 1);
    Atomics.notify(signal, OFFSET_WAKEUP_WORKER);
}

export function writeSharedError(signal: Int32Array, offset: number, error: unknown, context: string): void {
    const text = error instanceof Error ? error.message : String(error);
    const message = text.length <= errorCapacity ? text : `runInWorker: ${context} error exceeds shared error capacity`;
    for (let index = 0; index < message.length; index++) signal[offset + 1 + index] = message.charCodeAt(index);
    Atomics.store(signal, offset, message.length);
}

export function readSharedError(signal: Int32Array, offset: number): Error {
    const length = Atomics.load(signal, offset);
    if (length < 0 || length > errorCapacity) throw new Error("runInWorker: invalid shared error length");
    let message = "";
    for (let index = 0; index < length; index++) message += String.fromCharCode(signal[offset + 1 + index]);
    return new Error(message);
}

export interface WasmMemoryViews {
    i8: Int8Array; u8: Uint8Array; i16: Int16Array; u16: Uint16Array;
    i32: Int32Array; u32: Uint32Array; f32: Float32Array; f64: Float64Array; i64: BigInt64Array;
}

export function createMemoryViews(buffer: ArrayBufferLike): WasmMemoryViews {
    return {
        i8: new Int8Array(buffer), u8: new Uint8Array(buffer),
        i16: new Int16Array(buffer), u16: new Uint16Array(buffer),
        i32: new Int32Array(buffer), u32: new Uint32Array(buffer),
        f32: new Float32Array(buffer), f64: new Float64Array(buffer), i64: new BigInt64Array(buffer),
    };
}

export class MemoryViewManager {
    private views: WasmMemoryViews;
    constructor(private readonly memory: WebAssembly.Memory, signal: Int32Array) {
        if (Atomics.load(signal, OFFSET_VERSION) !== SIGNALING_VERSION) throw new Error("runInWorker: signaling version mismatch");
        this.views = createMemoryViews(memory.buffer);
    }
    refreshIfNeeded(): WasmMemoryViews {
        if (this.views.u8.buffer.byteLength !== this.memory.buffer.byteLength) this.views = createMemoryViews(this.memory.buffer);
        return this.views;
    }
    getViews(): WasmMemoryViews {
        return this.refreshIfNeeded();
    }
}
