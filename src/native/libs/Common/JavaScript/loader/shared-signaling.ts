// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// -----------------------------------------------------------
// Shared signaling buffer layout, constants, and helpers
// for cross-thread communication between main thread and
// dedicated worker in withWorker() mode.
// -----------------------------------------------------------

import { WorkerMaxArgumentCount } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";

// Signal states for all signaling slots
export const enum SignalState {
    IDLE = 0,
    PENDING = 1,
    DONE = 2,
    ERROR = 3,
    ALLOCATE = 4,
    ALLOCATED = 5,
}

// Command identifiers for the command signaling slot
export const enum CommandId {
    NONE = 0,
    RUN_MAIN = 1,
    EXIT = 2,
    RUN_MAIN_AND_EXIT = 3,
}

// Layout version — bump when the signaling buffer layout changes
export const SIGNALING_VERSION = 7;

// Number of entries in the JSImport ring buffer (worker → main)
export const JSIMPORT_RING_CAPACITY = 16;
// Number of entries in the JSExport ring buffer (main → worker)
export const JSEXPORT_RING_CAPACITY = 16;
// Fields per ring entry
const RING_ENTRY_SIZE = 10 + WorkerMaxArgumentCount * 3;

// Number of pre-allocated arg frame pointers for main→worker calls
export const FRAME_RING_CAPACITY = 16;

// -----------------------------------------------------------
// Signaling buffer offsets (Int32Array view)
// -----------------------------------------------------------

// Control
export const OFFSET_VERSION = 0;
export const OFFSET_MEMORY_GENERATION = 1;

// Unified wakeup slots — both sides notify to wake the other thread.
// Main thread waits (waitAsync) on WAKEUP_MAIN; worker waits on WAKEUP_WORKER.
// Any command write, ring buffer write, or memory growth notifies the appropriate slot.
export const OFFSET_WAKEUP_MAIN = 2;
export const OFFSET_WAKEUP_WORKER = 3;

// Command
export const OFFSET_COMMAND_SIGNAL = 4;
export const OFFSET_COMMAND_ID = 5;
export const OFFSET_COMMAND_RESULT = 6;
export const OFFSET_COMMAND_PROMISE_ID = 7;

// JSImport ring (worker writes → main reads)
export const OFFSET_JSIMPORT_HEAD = 8;
export const OFFSET_JSIMPORT_TAIL = 9;
export const OFFSET_JSIMPORT_RING = 10;

// JSExport ring (main writes → worker reads)
export const OFFSET_JSEXPORT_HEAD = OFFSET_JSIMPORT_RING + JSIMPORT_RING_CAPACITY * RING_ENTRY_SIZE;
export const OFFSET_JSEXPORT_TAIL = OFFSET_JSEXPORT_HEAD + 1;
export const OFFSET_JSEXPORT_RING = OFFSET_JSEXPORT_HEAD + 2;

// Frame pointer ring (pre-allocated memory slots for main → worker arg frames)
export const OFFSET_FRAME_PTRS = OFFSET_JSEXPORT_RING + JSEXPORT_RING_CAPACITY * RING_ENTRY_SIZE;

// Dispatch queues contain call-slot indices; a claimed slot remains owned until
// its caller consumes the completion.
export const OFFSET_JSIMPORT_QUEUE = OFFSET_FRAME_PTRS + FRAME_RING_CAPACITY;
export const OFFSET_JSEXPORT_QUEUE = OFFSET_JSIMPORT_QUEUE + JSIMPORT_RING_CAPACITY;
export const JSIMPORT_ERROR_CAPACITY = 4096;
const ERROR_SLOT_SIZE = 1 + JSIMPORT_ERROR_CAPACITY;
export const OFFSET_JSIMPORT_ERROR_LENGTH = OFFSET_JSEXPORT_QUEUE + JSEXPORT_RING_CAPACITY;
export const OFFSET_JSEXPORT_ERROR_LENGTH = OFFSET_JSIMPORT_ERROR_LENGTH + JSIMPORT_RING_CAPACITY * ERROR_SLOT_SIZE;
export const OFFSET_WORKER_ERROR_LENGTH = OFFSET_JSEXPORT_ERROR_LENGTH + JSEXPORT_RING_CAPACITY * ERROR_SLOT_SIZE;
export const OFFSET_WORKER_STATE = OFFSET_WORKER_ERROR_LENGTH + ERROR_SLOT_SIZE;
export const SIGNALING_BUFFER_INT32_SIZE = OFFSET_WORKER_STATE + 1;

// Total signaling buffer size in bytes (Int32Array element = 4 bytes)
export const SIGNALING_BUFFER_BYTE_SIZE = SIGNALING_BUFFER_INT32_SIZE * 4;

// -----------------------------------------------------------
// Ring entry field offsets (relative to ring entry base)
// -----------------------------------------------------------
export const RING_ENTRY_SIGNAL = 0;
export const RING_ENTRY_PROMISE_ID = 1;
export const RING_ENTRY_HANDLE = 2;
export const RING_ENTRY_ARGS_PTR = 3;
export const RING_ENTRY_RESULT_KIND = 4;
export const RING_ENTRY_ARGUMENT_COUNT = 5;
export const RING_ENTRY_RETURN_TYPE = 6;
export const RING_ENTRY_STRING_RESULT = 7;
export const RING_ENTRY_STRING_RESULT_LENGTH = 8;
export const RING_ENTRY_OPERATION = 9;
export const RING_ENTRY_ARGUMENTS = 10;
export const ARGUMENT_ENTRY_TYPE = 0;
export const ARGUMENT_ENTRY_STRING = 1;
export const ARGUMENT_ENTRY_STRING_LENGTH = 2;

export function argumentEntryOffset(entry: number, index: number): number {
    if (!Number.isInteger(index) || index < 0 || index >= WorkerMaxArgumentCount) {
        throw new Error("withWorker(): invalid argument index");
    }
    return entry + RING_ENTRY_ARGUMENTS + index * 3;
}

export const enum WorkerCallOperation {
    Invoke = 0,
    Allocate = 1,
    Release = 2,
}

export const enum WorkerResultKind {
    Scalar = 0,
    Promise = 1,
    Null = 2,
}

// -----------------------------------------------------------
// Promise ID generation
// -----------------------------------------------------------

let nextPromiseId = 1;

export function allocatePromiseId(): number {
    const id = nextPromiseId;
    nextPromiseId = (nextPromiseId + 1) | 0;
    return id;
}

// -----------------------------------------------------------
// Signaling buffer creation
// -----------------------------------------------------------

export function createSignalingBuffer(): SharedArrayBuffer {
    const buffer = new SharedArrayBuffer(SIGNALING_BUFFER_BYTE_SIZE);
    const view = new Int32Array(buffer);
    Atomics.store(view, OFFSET_VERSION, SIGNALING_VERSION);
    Atomics.store(view, OFFSET_MEMORY_GENERATION, 0);
    Atomics.store(view, OFFSET_WAKEUP_MAIN, 0);
    Atomics.store(view, OFFSET_WAKEUP_WORKER, 0);
    Atomics.store(view, OFFSET_COMMAND_SIGNAL, SignalState.IDLE);
    Atomics.store(view, OFFSET_JSIMPORT_HEAD, 0);
    Atomics.store(view, OFFSET_JSIMPORT_TAIL, 0);
    Atomics.store(view, OFFSET_JSEXPORT_HEAD, 0);
    Atomics.store(view, OFFSET_JSEXPORT_TAIL, 0);

    return buffer;
}

// -----------------------------------------------------------
// Unified wakeup helpers
//
// Both sides use a monotonically increasing counter as the
// wakeup slot value. The waiter stores the "last seen" value
// and waits while the slot still equals that value.
// The notifier increments the counter and calls Atomics.notify.
// -----------------------------------------------------------

export function wakeMain(signalView: Int32Array): void {
    Atomics.add(signalView, OFFSET_WAKEUP_MAIN, 1);
    Atomics.notify(signalView, OFFSET_WAKEUP_MAIN);
}

export function wakeWorker(signalView: Int32Array): void {
    Atomics.add(signalView, OFFSET_WAKEUP_WORKER, 1);
    Atomics.notify(signalView, OFFSET_WAKEUP_WORKER);
}

export function writeSharedError(signal: Int32Array, offset: number, error: unknown, context: string): void {
    let message = error instanceof Error ? error.message : String(error);
    if (message.length > JSIMPORT_ERROR_CAPACITY) {
        message = `withWorker(): ${context} error message exceeds ${JSIMPORT_ERROR_CAPACITY} UTF-16 code units`;
    }
    for (let i = 0; i < message.length; i++) signal[offset + 1 + i] = message.charCodeAt(i);
    Atomics.store(signal, offset, message.length);
}

export function readSharedError(signal: Int32Array, offset: number): Error {
    const length = Atomics.load(signal, offset);
    if (length < 0 || length > JSIMPORT_ERROR_CAPACITY) throw new Error("withWorker(): invalid shared error length");
    let message = "";
    for (let i = 0; i < length; i++) message += String.fromCharCode(signal[offset + 1 + i]);
    return new Error(message);
}

// -----------------------------------------------------------
// Ring buffer helpers
// -----------------------------------------------------------

export function ringEntryOffset(ringBaseOffset: number, index: number, capacity: number): number {
    const slot = index % capacity;

    return ringBaseOffset + slot * RING_ENTRY_SIZE;
}

export function jsimportEntryOffset(index: number): number {
    return ringEntryOffset(OFFSET_JSIMPORT_RING, index, JSIMPORT_RING_CAPACITY);
}

export function jsexportEntryOffset(index: number): number {
    return ringEntryOffset(OFFSET_JSEXPORT_RING, index, JSEXPORT_RING_CAPACITY);
}

export function jsimportErrorOffset(slot: number): number {
    return OFFSET_JSIMPORT_ERROR_LENGTH + slot * ERROR_SLOT_SIZE;
}

export function jsexportErrorOffset(slot: number): number {
    return OFFSET_JSEXPORT_ERROR_LENGTH + slot * ERROR_SLOT_SIZE;
}

export function reserveCallSlot(signal: Int32Array, base: number, capacity: number, context: string): number {
    for (let slot = 0; slot < capacity; slot++) {
        const entry = ringEntryOffset(base, slot, capacity);
        if (Atomics.compareExchange(signal, entry + RING_ENTRY_SIGNAL, SignalState.IDLE, SignalState.PENDING) === SignalState.IDLE) {
            return slot;
        }
    }
    throw new Error(`withWorker(): ${context} queue is full`);
}

// -----------------------------------------------------------
// Memory view refresh
// -----------------------------------------------------------

export interface WasmMemoryViews {
    i8: Int8Array;
    u8: Uint8Array;
    i16: Int16Array;
    u16: Uint16Array;
    i32: Int32Array;
    u32: Uint32Array;
    f32: Float32Array;
    f64: Float64Array;
    i64: BigInt64Array;
}

export function createMemoryViews(buffer: ArrayBufferLike): WasmMemoryViews {
    return {
        i8: new Int8Array(buffer),
        u8: new Uint8Array(buffer),
        i16: new Int16Array(buffer),
        u16: new Uint16Array(buffer),
        i32: new Int32Array(buffer),
        u32: new Uint32Array(buffer),
        f32: new Float32Array(buffer),
        f64: new Float64Array(buffer),
        i64: new BigInt64Array(buffer),
    };
}

export class MemoryViewManager {
    private knownGeneration = 0;
    private views: WasmMemoryViews;
    private readonly wasmMemory: WebAssembly.Memory;
    private readonly signalView: Int32Array;

    constructor(wasmMemory: WebAssembly.Memory, signalView: Int32Array) {
        this.wasmMemory = wasmMemory;
        this.signalView = signalView;
        this.views = createMemoryViews(wasmMemory.buffer);
    }

    refreshIfNeeded(): WasmMemoryViews {
        const current = Atomics.load(this.signalView, OFFSET_MEMORY_GENERATION);
        if (current !== this.knownGeneration || this.views.u8.buffer.byteLength !== this.wasmMemory.buffer.byteLength) {
            this.knownGeneration = current;
            this.views = createMemoryViews(this.wasmMemory.buffer);
        }

        return this.views;
    }

    getViews(): WasmMemoryViews {
        return this.views;
    }
}

// -----------------------------------------------------------
// Spin-wait with JSImport servicing
//
// Used by main thread during synchronous JSExport calls and
// proxied _malloc. Spins on a target signal slot while also
// checking for incoming JSImport requests in the JSImport ring.
//
// serviceJSImport is a callback provided by the main-thread
// runtime to handle a single JSImport request.
// -----------------------------------------------------------

export function spinWaitWithJSImportService(
    signalView: Int32Array,
    targetOffset: number,
    waitWhileValue: number,
    memoryViewManager: MemoryViewManager,
    serviceJSImport: ((signalView: Int32Array, memoryViewManager: MemoryViewManager) => void) | null,
): number {
    // eslint-disable-next-line no-constant-condition
    while (true) {
        const value = Atomics.load(signalView, targetOffset);
        if (value !== waitWhileValue) {
            return value;
        }

        memoryViewManager.refreshIfNeeded();

        if (serviceJSImport) {
            serviceJSImport(signalView, memoryViewManager);
        }
    }
}

// -----------------------------------------------------------
// Worker-side: increment memory generation after grow()
// -----------------------------------------------------------

export function notifyMemoryGrowth(signalView: Int32Array): void {
    Atomics.add(signalView, OFFSET_MEMORY_GENERATION, 1);
}
