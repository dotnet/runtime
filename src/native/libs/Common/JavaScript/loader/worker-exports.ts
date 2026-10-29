// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { PromiseCompletionSource } from "./types";
import { createPromiseCompletionSource } from "./promise-completion-source";
import type { WorkerPromiseMessage } from "./worker-promises";
import { captureWorkerPromise, createWorkerPromises } from "./worker-promises";
import type { WorkerValue, WorkerValueType, WorkerReturnType } from "./worker-values";
import {
    allocateWorkerString, freeWorkerString, readWorkerPrimitive, readWorkerString, validateWorkerValue, writeWorkerString,
    prepareWorkerArguments, allocateWorkerArgumentStrings, writeWorkerArgumentStrings, readWorkerArguments, readWorkerArgumentTypes,
} from "./worker-values";
import { isWorkerValueType, isWorkerReturnType, JavaScriptMarshalerArgSize, MarshalerType, WorkerMaxArgumentCount } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
import {
    allocatePromiseId, JSEXPORT_RING_CAPACITY, jsexportEntryOffset, jsexportErrorOffset, MemoryViewManager,
    OFFSET_FRAME_PTRS, OFFSET_WORKER_ERROR_LENGTH, OFFSET_JSEXPORT_HEAD, OFFSET_JSEXPORT_TAIL,
    OFFSET_JSEXPORT_QUEUE, OFFSET_JSEXPORT_RING, reserveCallSlot,
    OFFSET_WORKER_STATE,
    RING_ENTRY_ARGS_PTR, RING_ENTRY_HANDLE, RING_ENTRY_PROMISE_ID, RING_ENTRY_SIGNAL,
    RING_ENTRY_RESULT_KIND, WorkerResultKind,
    RING_ENTRY_RETURN_TYPE,
    RING_ENTRY_STRING_RESULT, RING_ENTRY_STRING_RESULT_LENGTH, RING_ENTRY_OPERATION, WorkerCallOperation,
    readSharedError, SignalState, spinWaitWithJSImportService, wakeMain, wakeWorker, writeSharedError,
} from "./shared-signaling";

export interface WorkerExportDescriptor {
    path: string[];
    handle: number;
    supported: boolean;
    isAsync?: boolean;
    argumentTypes: MarshalerType[];
    returnType: MarshalerType;
}
export interface WorkerExportsRequest {
    type: "exports";
    requestId: number;
    assemblyName: string;
}
export type WorkerExportsResponse = { type: "exports", requestId: number } & (
    { exports: WorkerExportDescriptor[] } | { error: string }
);

type ScalarExport = (...values: unknown[]) => WorkerValue | Promise<WorkerValue>;
interface AssemblyExports {
    [name: string]: AssemblyExports | ScalarExport;
}
interface PendingExports {
    assemblyName: string;
    completion: PromiseCompletionSource<AssemblyExports>;
}

export function createWorkerJSExportClient(
    signal: Int32Array,
    memory: MemoryViewManager,
    worker: Worker,
    processImports: () => void,
    assertRunning: () => void,
    onFailure: (error: Error) => void,
) {
    const pending = new Map<number, PendingExports>();
    const assemblies = new Map<string, Promise<AssemblyExports>>();
    const promises = createWorkerPromises();
    let stopped: Error | undefined;
    let callDepth = 0;

    function ensureRunning(): void {
        assertRunning();
        if (stopped) throw stopped;
        const state = Atomics.load(signal, OFFSET_WORKER_STATE);
        if (state !== SignalState.IDLE) {
            const error = state === SignalState.ERROR
                ? readSharedError(signal, OFFSET_WORKER_ERROR_LENGTH)
                : new Error("withWorker(): worker has exited");
            onFailure(error);
            throw error;
        }
    }

    function invoke(handle: number, values: unknown[], isAsync: boolean, argumentTypes: WorkerValueType[], returnType: WorkerReturnType): WorkerValue | Promise<WorkerValue> {
        ensureRunning();
        for (let index = 0; index < argumentTypes.length; index++) validateWorkerValue(values[index], argumentTypes[index]);
        const hasStrings = argumentTypes.includes(MarshalerType.String);
        const head = Atomics.load(signal, OFFSET_JSEXPORT_HEAD) >>> 0;
        const tail = Atomics.load(signal, OFFSET_JSEXPORT_TAIL) >>> 0;
        if (((head - tail) >>> 0) >= JSEXPORT_RING_CAPACITY) {
            throw new Error("withWorker(): export queue is full");
        }
        const slot = reserveCallSlot(signal, OFFSET_JSEXPORT_RING, JSEXPORT_RING_CAPACITY, "export");
        const entry = jsexportEntryOffset(slot);
        let active = false;
        let published = false;
        const id = promises.reserve();
        let pendingPromise = false;
        let stringsReleased = false;
        function releaseStrings(): void {
            if (!stringsReleased && active && !stopped && Atomics.load(signal, OFFSET_WORKER_STATE) === SignalState.IDLE
                && (hasStrings || returnType === MarshalerType.String)) {
                dispatch(WorkerCallOperation.Release);
                stringsReleased = true;
            }
        }
        function dispatch(operation: WorkerCallOperation): SignalState {
            ensureRunning();
            const head = Atomics.load(signal, OFFSET_JSEXPORT_HEAD) >>> 0;
            const tail = Atomics.load(signal, OFFSET_JSEXPORT_TAIL) >>> 0;
            if (((head - tail) >>> 0) >= JSEXPORT_RING_CAPACITY) throw new Error("withWorker(): export queue is full");
            Atomics.store(signal, entry + RING_ENTRY_OPERATION, operation);
            Atomics.store(signal, entry + RING_ENTRY_SIGNAL, SignalState.PENDING);
            Atomics.store(signal, OFFSET_JSEXPORT_QUEUE + head % JSEXPORT_RING_CAPACITY, slot);
            Atomics.store(signal, OFFSET_JSEXPORT_HEAD, (head + 1) >>> 0);
            published = true;
            wakeWorker(signal);
            const state = spinWaitWithJSImportService(signal, entry + RING_ENTRY_SIGNAL, SignalState.PENDING, memory, () => {
                ensureRunning();
                processImports();
            });
            ensureRunning();
            if (Atomics.load(signal, entry + RING_ENTRY_PROMISE_ID) !== id) throw new Error("withWorker(): export completion mismatch");
            if (state === SignalState.ERROR) throw readSharedError(signal, jsexportErrorOffset(slot));
            if (state !== SignalState.DONE) throw new Error("withWorker(): invalid export completion");
            return state;
        }
        try {
            const pointer = Atomics.load(signal, OFFSET_FRAME_PTRS + slot) >>> 0;
            const views = memory.refreshIfNeeded();
            const frameSize = (argumentTypes.length + 2) * JavaScriptMarshalerArgSize;
            if (!pointer || pointer % 8 !== 0 || pointer + frameSize > views.u8.byteLength) {
                throw new Error("withWorker(): invalid export argument frame");
            }
            views.u8.fill(0, pointer, pointer + frameSize);
            callDepth++;
            active = true;
            Atomics.store(signal, entry + RING_ENTRY_HANDLE, handle);
            Atomics.store(signal, entry + RING_ENTRY_ARGS_PTR, pointer);
            Atomics.store(signal, entry + RING_ENTRY_PROMISE_ID, id);
            prepareWorkerArguments(signal, entry, memory, pointer, values, argumentTypes);
            Atomics.store(signal, entry + RING_ENTRY_RETURN_TYPE, returnType);
            Atomics.store(signal, entry + RING_ENTRY_STRING_RESULT, 0);
            Atomics.store(signal, entry + RING_ENTRY_STRING_RESULT_LENGTH, -1);
            if (hasStrings) {
                dispatch(WorkerCallOperation.Allocate);
                writeWorkerArgumentStrings(signal, entry, memory, values);
            }
            Atomics.store(signal, entry + RING_ENTRY_RESULT_KIND, isAsync ? WorkerResultKind.Promise : WorkerResultKind.Scalar);
            dispatch(WorkerCallOperation.Invoke);
            if (isAsync) {
                const kind = Atomics.load(signal, entry + RING_ENTRY_RESULT_KIND);
                if (kind === WorkerResultKind.Null) return null;
                if (kind !== WorkerResultKind.Promise) throw new Error("withWorker(): invalid asynchronous export completion");
                releaseStrings();
                const promise = promises.promise(id, returnType);
                pendingPromise = true;
                return promise;
            }
            if (returnType === MarshalerType.String) {
                return readWorkerString(memory, Atomics.load(signal, entry + RING_ENTRY_STRING_RESULT) >>> 0,
                    Atomics.load(signal, entry + RING_ENTRY_STRING_RESULT_LENGTH));
            }
            if (returnType === MarshalerType.Void) return undefined;
            const resultPointer = pointer + JavaScriptMarshalerArgSize;
            return readWorkerPrimitive(memory, resultPointer, returnType) ?? (returnType === MarshalerType.Boolean ? false : 0);
        } catch (error) {
            if (published && Atomics.load(signal, entry + RING_ENTRY_SIGNAL) === SignalState.PENDING) {
                onFailure(error instanceof Error ? error : new Error(String(error)));
            }
            throw error;
        } finally {
            try {
                releaseStrings();
            } finally {
                if (!pendingPromise) promises.release(id);
                Atomics.store(signal, entry + RING_ENTRY_SIGNAL, SignalState.IDLE);
                if (active) callDepth--;
            }
        }
    }

    function buildExports(descriptors: WorkerExportDescriptor[]): AssemblyExports {
        const result: AssemblyExports = {};
        const functions = new Map<number, ScalarExport>();
        for (const descriptor of descriptors) {
            if (!descriptor.path.length || descriptor.path.some(part => !part)) throw new Error("withWorker(): invalid export path");
            let fn = functions.get(descriptor.handle);
            if (!fn) {
                const argumentTypes = descriptor.argumentTypes;
                const returnType = descriptor.returnType;
                if (descriptor.supported && (!Array.isArray(argumentTypes) || argumentTypes.length > WorkerMaxArgumentCount
                    || !argumentTypes.every(isWorkerValueType) || !isWorkerReturnType(returnType))) {
                    throw new Error("withWorker(): invalid export value types");
                }
                fn = descriptor.supported && argumentTypes.every(isWorkerValueType) && isWorkerReturnType(returnType)
                    ? (...values) => invoke(descriptor.handle, values, descriptor.isAsync === true, argumentTypes, returnType) : () => {
                        throw new Error(`withWorker(): exports support at most ${WorkerMaxArgumentCount} primitive/String arguments and a supported result or Task`);
                    };
                functions.set(descriptor.handle, fn);
            }
            let scope = result;
            for (const part of descriptor.path.slice(0, -1)) {
                if (!Object.prototype.hasOwnProperty.call(scope, part)) {
                    Object.defineProperty(scope, part, { value: {}, writable: true, configurable: true, enumerable: true });
                }
                const child = scope[part];
                if (typeof child === "function") throw new Error("withWorker(): conflicting export path");
                scope = child;
            }
            Object.defineProperty(scope, descriptor.path[descriptor.path.length - 1], { value: fn, writable: true, configurable: true, enumerable: true });
        }
        return result;
    }

    function onMessage(event: MessageEvent<WorkerExportsResponse | WorkerPromiseMessage>): void {
        const message = event.data;
        if (message.type === "async-export") {
            promises.complete(message);
            return;
        }
        if (message.type !== "exports") return;
        const request = pending.get(message.requestId);
        if (!request) return;
        pending.delete(message.requestId);
        try {
            if ("error" in message) throw new Error(message.error);
            request.completion.resolve(buildExports(message.exports));
        } catch (error) {
            assemblies.delete(request.assemblyName);
            request.completion.reject(error);
        }
    }

    async function getAssemblyExports(assemblyName: string): Promise<AssemblyExports> {
        ensureRunning();
        if (callDepth !== 0) throw new Error("withWorker(): bind exports outside synchronous calls");
        const name = assemblyName.endsWith(".dll") ? assemblyName.slice(0, -4) : assemblyName;
        if (!name) throw new Error("withWorker(): assembly name is required");
        const cached = assemblies.get(name);
        if (cached) return cached;
        const completion = createPromiseCompletionSource<AssemblyExports>();
        const requestId = allocatePromiseId();
        pending.set(requestId, { assemblyName: name, completion });
        assemblies.set(name, completion.promise);
        const request: WorkerExportsRequest = { type: "exports", requestId, assemblyName: name };
        try {
            worker.postMessage(request);
        } catch (error) {
            pending.delete(requestId);
            assemblies.delete(name);
            completion.reject(error);
        }
        return await completion.promise;
    }

    function stop(error: Error): void {
        stopped = error;
        promises.stop(error);
        worker.removeEventListener("message", onMessage);
        for (const request of pending.values()) request.completion.reject(error);
        pending.clear();
        assemblies.clear();
    }
    worker.addEventListener("message", onMessage);
    return { getAssemblyExports, stop, isCalling: () => callDepth !== 0 };
}

export function createWorkerJSExportDispatcher(
    signal: Int32Array, memory: MemoryViewManager,
    invoke: (handle: number, pointer: number, values: WorkerValue[]) => unknown,
    post: (message: WorkerPromiseMessage) => void = () => {
        throw new Error("withWorker(): asynchronous export transport is missing");
    },
    onFailure: (error: Error) => void = error => {
        throw error;
    },
) {
    const active = new Set<number>();
    const buffers = Array.from({ length: JSEXPORT_RING_CAPACITY }, () => new Set<number>());
    return (): void => {
        for (;;) {
            const tail = Atomics.load(signal, OFFSET_JSEXPORT_TAIL) >>> 0;
            if (tail === (Atomics.load(signal, OFFSET_JSEXPORT_HEAD) >>> 0)) return;
            const slot = Atomics.load(signal, OFFSET_JSEXPORT_QUEUE + tail % JSEXPORT_RING_CAPACITY);
            if (slot < 0 || slot >= JSEXPORT_RING_CAPACITY) throw new Error("withWorker(): invalid export slot");
            const entry = jsexportEntryOffset(slot);
            if (Atomics.load(signal, entry + RING_ENTRY_SIGNAL) !== SignalState.PENDING) throw new Error("withWorker(): invalid export queue state");
            Atomics.store(signal, OFFSET_JSEXPORT_TAIL, (tail + 1) >>> 0);
            let state = SignalState.DONE;
            try {
                const operation = Atomics.load(signal, entry + RING_ENTRY_OPERATION);
                const owned = buffers[slot];
                if (operation === WorkerCallOperation.Release) {
                    for (const pointer of owned) freeWorkerString(pointer);
                    owned.clear();
                    Atomics.store(signal, entry + RING_ENTRY_STRING_RESULT, 0);
                } else if (operation === WorkerCallOperation.Allocate) {
                    if (owned.size !== 0) throw new Error("withWorker(): string allocation already owned");
                    allocateWorkerArgumentStrings(signal, entry, pointer => owned.add(pointer));
                } else if (operation === WorkerCallOperation.Invoke) {
                    const pointer = Atomics.load(signal, entry + RING_ENTRY_ARGS_PTR) >>> 0;
                    const argumentTypes = readWorkerArgumentTypes(signal, entry);
                    if (!pointer || pointer % 8 !== 0 || pointer + (argumentTypes.length + 2) * JavaScriptMarshalerArgSize > memory.refreshIfNeeded().u8.byteLength) {
                        throw new Error("withWorker(): invalid export frame");
                    }
                    const returnType = Atomics.load(signal, entry + RING_ENTRY_RETURN_TYPE);
                    if (!isWorkerReturnType(returnType)) throw new Error("withWorker(): invalid export result type");
                    const values = readWorkerArguments(signal, entry, memory, pointer);
                    const result = invoke(Atomics.load(signal, entry + RING_ENTRY_HANDLE), pointer, values);
                    if (Atomics.load(signal, entry + RING_ENTRY_RESULT_KIND) === WorkerResultKind.Promise) {
                        const id = Atomics.load(signal, entry + RING_ENTRY_PROMISE_ID);
                        const kind = captureWorkerPromise(result, id, "async-export", active, post, onFailure, returnType);
                        Atomics.store(signal, entry + RING_ENTRY_RESULT_KIND, kind);
                    } else if (returnType === MarshalerType.String) {
                        const value = validateWorkerValue(result, returnType);
                        const text = typeof value === "string" ? value : null;
                        const resultPointer = allocateWorkerString(text?.length ?? 0);
                        if (resultPointer) owned.add(resultPointer);
                        if (text !== null) writeWorkerString(memory, resultPointer, text);
                        Atomics.store(signal, entry + RING_ENTRY_STRING_RESULT, resultPointer);
                        Atomics.store(signal, entry + RING_ENTRY_STRING_RESULT_LENGTH, text?.length ?? -1);
                    }
                } else throw new Error("withWorker(): invalid export operation");
            } catch (error) {
                writeSharedError(signal, jsexportErrorOffset(slot), error, "managed export");
                state = SignalState.ERROR;
            }
            Atomics.store(signal, entry + RING_ENTRY_SIGNAL, state);
            wakeMain(signal);
        }
    };
}
