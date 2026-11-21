// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {
    SignalState, OFFSET_WAKEUP_MAIN, OFFSET_WAKEUP_WORKER, OFFSET_WORKER_STATE, OFFSET_WORKER_ERROR_LENGTH,
    wakeMain, wakeWorker, readSharedError,
} from "./shared-signaling";
import type { AssetEntryInternal } from "./types";
import type { LocalRuntimeAPI } from "./worker-local";

const callCapacity = 16;
const capacity = callCapacity + 4; // Allocation, release and exception marshaling must survive call saturation.
const queueCapacity = 16; // Must divide 2^32 so dispatch indices remain distinct across counter rollover.
const errorCapacity = 4096;
const entrySize = 8 + errorCapacity;
const queueSize = 2 + queueCapacity + capacity * entrySize;
const bufferSize = 2 * queueSize;

export const enum NativeOperation {
    GetManagedStackTrace,
    CallDelegate,
    CompleteTask,
    ReleaseJSOwnedObject,
    BindAssemblyExports,
    CallJSExport,
    Malloc,
    Free,
    RunBackgroundTimers,
    GetRuntimeContractDescriptor,
    ConnectDSRouter,
    GetErrorSnapshot,
}

export const enum RuntimeOperation {
    BindJSImport,
    InvokeJSImport,
    ReleaseCSOwnedObject,
    ResolveOrRejectPromise,
    CancelPromise,
    InvokeJSFunction,
    ForceDisposeProxies,
    AbortInteropTimers,
    GetProxyCounts,
    GetErrorSnapshot,
}

export interface WorkerRuntimeContext {
    role: "ui" | "worker";
    invoke: (operation: NativeOperation | RuntimeOperation, first?: number, second?: number) => number;
    beforeRuntimeStart?: () => Promise<void>;
    memory?: WebAssembly.Memory;
    beforeTaskCompletion?: () => void;
    assertNativeCall?: () => void;
    exitError?: Error;
    loadResource?: (asset: AssetEntryInternal) => Promise<Response>;
    resolveModuleResource?: (asset: AssetEntryInternal) => Promise<string>;
    local?: LocalRuntimeAPI;
    downloadDiagnosticData?: (chunks: Uint8Array[]) => void;
    getErrorSnapshot?: (handle: number, output: number) => void;
    getRemoteError?: (handle: number) => Error;
}

export class NativeCallCapacityError extends Error { }

export function createNativeCallBuffer(): SharedArrayBuffer {
    return new SharedArrayBuffer(bufferSize * Int32Array.BYTES_PER_ELEMENT);
}

// Only native opcodes, scalar ABI parameters and pointers cross this boundary.
export function createNativeChannel(
    signal: Int32Array, buffer: SharedArrayBuffer, worker: boolean,
    dispatch: (operation: number, first: number, second: number) => number,
) {
    if (buffer.byteLength !== bufferSize * 4) throw new Error("runInWorker: incompatible native call buffer");
    const calls = new Int32Array(buffer);
    const outgoing = worker ? 0 : queueSize;
    const incoming = worker ? queueSize : 0;
    const ownWakeup = worker ? OFFSET_WAKEUP_WORKER : OFFSET_WAKEUP_MAIN;
    const wakeOther = worker ? wakeMain : wakeWorker;
    const deferred: number[] = [];
    let stopped: Error | undefined;
    let nextId = 1;
    let dispatchDepth = 0;
    let sent = 0;
    let received = 0;

    function ensureRunning(): void {
        if (stopped) throw stopped;
        const state = Atomics.load(signal, OFFSET_WORKER_STATE);
        if (state === SignalState.ERROR) throw readSharedError(signal, OFFSET_WORKER_ERROR_LENGTH);
        if (state !== SignalState.IDLE) throw new Error("runInWorker: native runtime exited");
    }

    function assertCallAvailable(): void {
        ensureRunning();
        for (let slot = 0; slot < callCapacity; slot++) {
            if (Atomics.load(calls, entry(outgoing, slot)) === SignalState.IDLE) return;
        }
        throw new NativeCallCapacityError("runInWorker: native call capacity exceeded");
    }

    function entry(base: number, slot: number): number {
        return base + 2 + queueCapacity + slot * entrySize;
    }

    function execute(slot: number): void {
        const offset = entry(incoming, slot);
        if (Atomics.load(calls, offset) !== SignalState.PENDING) throw new Error("runInWorker: invalid native request state");
        dispatchDepth++;
        try {
            if (worker && calls[offset + 2] === NativeOperation.CompleteTask) {
                Atomics.store(calls, offset, SignalState.READY);
                wakeOther(signal);
                while (Atomics.load(calls, offset) === SignalState.READY) {
                    const generation = Atomics.load(signal, ownWakeup);
                    ensureRunning();
                    process(false);
                    if (Atomics.load(calls, offset) === SignalState.READY) Atomics.wait(signal, ownWakeup, generation);
                }
                if (Atomics.load(calls, offset) !== SignalState.EXECUTING) throw new Error("runInWorker: Task preparation failed");
                if (calls[offset + 7] !== 0) throw new Error("runInWorker: Task preparation failed");
            }
            const result = dispatch(calls[offset + 2], calls[offset + 3], calls[offset + 4]);
            Atomics.store(calls, offset + 5, result);
            Atomics.store(calls, offset, SignalState.DONE);
        } catch (error) {
            const text = error instanceof Error ? error.message : String(error);
            const message = text.length <= errorCapacity ? text : "runInWorker: native transport error exceeds shared error capacity";
            for (let index = 0; index < message.length; index++) calls[offset + 8 + index] = message.charCodeAt(index);
            Atomics.store(calls, offset + 6, message.length);
            Atomics.store(calls, offset, SignalState.ERROR);
        } finally {
            dispatchDepth--;
            received++;
            wakeOther(signal);
        }
    }

    function process(allowTaskCompletion = true): void {
        ensureRunning();
        if (allowTaskCompletion) {
            while (deferred.length) execute(deferred.shift()!);
        }
        for (;;) {
            const tail = Atomics.load(calls, incoming + 1) >>> 0;
            if (tail === (Atomics.load(calls, incoming) >>> 0)) return;
            const slot = Atomics.load(calls, incoming + 2 + tail % queueCapacity);
            if (slot < 0 || slot >= capacity) throw new Error("runInWorker: invalid native request slot");
            Atomics.store(calls, incoming + 1, (tail + 1) | 0);
            const operation = calls[entry(incoming, slot) + 2];
            if (worker && !allowTaskCompletion
                && (operation === NativeOperation.CompleteTask || operation === NativeOperation.RunBackgroundTimers)) {
                deferred.push(slot);
            } else execute(slot);
        }
    }

    function invoke(operation: number, first = 0, second = 0, beforeInvoke?: () => void): number {
        ensureRunning();
        if (!worker && operation === NativeOperation.CompleteTask && dispatchDepth !== 0) {
            throw new Error("runInWorker: Task completion must follow the initial native callback");
        }
        let slot = -1;
        const privileged = operation === NativeOperation.Malloc || operation === NativeOperation.Free
            || operation === NativeOperation.ReleaseJSOwnedObject || operation === NativeOperation.GetManagedStackTrace
            || operation === NativeOperation.RunBackgroundTimers || operation === NativeOperation.GetErrorSnapshot;
        const limit = worker || privileged ? capacity : callCapacity;
        for (let index = 0; index < limit; index++) {
            if (Atomics.compareExchange(calls, entry(outgoing, index), SignalState.IDLE, SignalState.PENDING) === SignalState.IDLE) {
                slot = index;
                break;
            }
        }
        if (slot === -1) throw new NativeCallCapacityError("runInWorker: native call capacity exceeded");
        const offset = entry(outgoing, slot);
        const id = nextId;
        let preparationError: unknown;
        nextId = (nextId + 1) | 0;
        try {
            const head = Atomics.load(calls, outgoing) >>> 0;
            const tail = Atomics.load(calls, outgoing + 1) >>> 0;
            if (((head - tail) >>> 0) >= queueCapacity) throw new Error("runInWorker: native dispatch queue is full");
            calls[offset + 1] = id;
            calls[offset + 2] = operation;
            calls[offset + 3] = first;
            calls[offset + 4] = second;
            calls[offset + 7] = 0;
            Atomics.store(calls, outgoing + 2 + head % queueCapacity, slot);
            Atomics.store(calls, outgoing, (head + 1) | 0);
            sent++;
            wakeOther(signal);
            for (;;) {
                const state = Atomics.load(calls, offset);
                if (state === SignalState.READY) {
                    try {
                        beforeInvoke?.();
                        Atomics.store(calls, offset, SignalState.EXECUTING);
                    } catch (error) {
                        preparationError = error;
                        calls[offset + 7] = 1;
                        Atomics.store(calls, offset, SignalState.EXECUTING);
                    }
                    wakeOther(signal);
                } else if (state !== SignalState.PENDING && state !== SignalState.EXECUTING) break;
                const generation = Atomics.load(signal, ownWakeup);
                ensureRunning();
                process(!worker);
                if (worker && Atomics.load(calls, offset) === SignalState.PENDING) {
                    Atomics.wait(signal, ownWakeup, generation);
                }
            }
            ensureRunning();
            if (calls[offset + 1] !== id) throw new Error("runInWorker: native completion identity mismatch");
            const state = Atomics.load(calls, offset);
            if (state === SignalState.ERROR) {
                if (preparationError !== undefined) throw preparationError;
                const length = calls[offset + 6];
                if (length < 0 || length > errorCapacity) throw new Error("runInWorker: invalid native error length");
                let message = "";
                for (let index = 0; index < length; index++) message += String.fromCharCode(calls[offset + 8 + index]);
                throw new Error(message);
            }
            if (state !== SignalState.DONE) throw new Error("runInWorker: invalid native completion");
            return calls[offset + 5];
        } finally {
            Atomics.store(calls, offset, SignalState.IDLE);
        }
    }

    function listen(onFailure: (error: Error) => void): void {
        if (stopped || Atomics.load(signal, OFFSET_WORKER_STATE) !== SignalState.IDLE) return;
        const generation = Atomics.load(signal, ownWakeup);
        try {
            process();
            const wait = Atomics.waitAsync(signal, ownWakeup, generation);
            if (wait.async) void wait.value.then(() => listen(onFailure));
            else setTimeout(() => listen(onFailure), 0);
        } catch (error) {
            onFailure(error instanceof Error ? error : new Error(String(error)));
        }
    }

    return {
        invoke, process, listen, assertCallAvailable,
        stop: (error: Error) => {
            stopped = error; wakeOther(signal);
        },
        getStats: () => ({ sent, received, dispatchDepth, deferred: deferred.length }),
    };
}
