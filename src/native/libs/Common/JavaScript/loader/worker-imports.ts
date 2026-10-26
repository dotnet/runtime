// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JSMarshalerArguments } from "./types";
import type { WorkerPromiseMessage } from "./worker-promises";
import { captureWorkerPromise, createWorkerPromises } from "./worker-promises";
import { JavaScriptMarshalerArgSize, JSMarshalerArgumentOffsets, MarshalerType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
import {
    JSIMPORT_RING_CAPACITY, jsimportEntryOffset, jsimportErrorOffset,
    MemoryViewManager, readSharedError, writeSharedError, reserveCallSlot, OFFSET_JSIMPORT_RING, OFFSET_JSIMPORT_QUEUE,
    OFFSET_JSIMPORT_HEAD, OFFSET_JSIMPORT_TAIL, OFFSET_WAKEUP_MAIN, OFFSET_WAKEUP_WORKER,
    RING_ENTRY_ARGS_PTR, RING_ENTRY_HANDLE, RING_ENTRY_PROMISE_ID, RING_ENTRY_SIGNAL,
    RING_ENTRY_RESULT_KIND, WorkerResultKind,
    SignalState, wakeMain, wakeWorker,
} from "./shared-signaling";

export interface WorkerModuleImportsMessage {
    type: "imports";
    moduleName: string;
    functions: { name: string, handle: number }[];
}

type UIImportFunction = (value: number) => unknown;

export function createWorkerJSImportDispatcher(
    signal: Int32Array,
    memory: MemoryViewManager,
    postImports: (message: WorkerModuleImportsMessage | WorkerPromiseMessage) => void,
    onFailure: (error: Error) => void,
) {
    const functions = new Map<number, UIImportFunction>();
    const modules = new Set<string>();
    const active = new Set<number>();
    let nextHandle = 1;
    let stopped = false;
    let dispatchDepth = 0;

    function setModuleImports(moduleName: string, moduleImports: unknown): void {
        if (stopped) throw new Error("withWorker(): runtime has exited");
        if (!moduleName || typeof moduleName !== "string") throw new Error("withWorker(): module name must be a nonempty string");
        if (!moduleImports || typeof moduleImports !== "object") throw new Error("withWorker(): module imports must be an object");
        if (modules.has(moduleName)) throw new Error(`withWorker(): replacing module imports is not yet supported: ${moduleName}`);
        const entries: { name: string, handle: number, invoke: (value: number) => unknown }[] = [];
        const ancestors = new Set<object>();
        function collect(scope: object, prefix: string): void {
            if (ancestors.has(scope)) throw new Error("withWorker(): module imports contain a cycle");
            ancestors.add(scope);
            for (const [name, value] of Object.entries(scope)) {
                const path = prefix ? `${prefix}.${name}` : name;
                if (typeof value === "function") {
                    entries.push({ name: path, handle: nextHandle++, invoke: arg => Reflect.apply(value, scope, [arg]) });
                } else if (value && typeof value === "object") {
                    collect(value, path);
                }
            }
            ancestors.delete(scope);
        }
        collect(moduleImports, "");
        postImports({ type: "imports", moduleName, functions: entries.map(({ name, handle }) => ({ name, handle })) });
        for (const entry of entries) functions.set(entry.handle, entry.invoke);
        modules.add(moduleName);
    }

    function processPending(): void {
        if (stopped) return;
        for (;;) {
            const tail = Atomics.load(signal, OFFSET_JSIMPORT_TAIL) >>> 0;
            if (tail === (Atomics.load(signal, OFFSET_JSIMPORT_HEAD) >>> 0)) return;
            const slot = Atomics.load(signal, OFFSET_JSIMPORT_QUEUE + tail % JSIMPORT_RING_CAPACITY);
            if (slot < 0 || slot >= JSIMPORT_RING_CAPACITY) throw new Error("withWorker(): invalid UI import slot");
            const entry = jsimportEntryOffset(slot);
            if (Atomics.load(signal, entry + RING_ENTRY_SIGNAL) !== SignalState.PENDING) {
                throw new Error("withWorker(): invalid UI import queue state");
            }
            Atomics.store(signal, OFFSET_JSIMPORT_TAIL, (tail + 1) >>> 0);
            let state = SignalState.DONE;
            dispatchDepth++;
            try {
                const invoke = functions.get(Atomics.load(signal, entry + RING_ENTRY_HANDLE));
                if (!invoke) throw new Error("withWorker(): unknown UI import handle");
                const pointer = Atomics.load(signal, entry + RING_ENTRY_ARGS_PTR) >>> 0;
                const views = memory.refreshIfNeeded();
                if (!pointer || pointer % 4 !== 0 || pointer + 3 * JavaScriptMarshalerArgSize > views.u8.byteLength) {
                    throw new Error("withWorker(): invalid UI import argument frame");
                }
                const value = views.i32[(pointer + 2 * JavaScriptMarshalerArgSize + JSMarshalerArgumentOffsets.Int32Value) / 4];
                const result = invoke(value);
                if (Atomics.load(signal, entry + RING_ENTRY_RESULT_KIND) === WorkerResultKind.Promise) {
                    const id = Atomics.load(signal, entry + RING_ENTRY_PROMISE_ID);
                    const kind = captureWorkerPromise(result, id, "async-import", active, postImports, onFailure);
                    Atomics.store(signal, entry + RING_ENTRY_RESULT_KIND, kind);
                    continue;
                }
                const isNull = result === null || result === undefined;
                if (!isNull && (typeof result !== "number" || !Number.isInteger(result) || result < -0x8000_0000 || result > 0x7fff_ffff)) {
                    throw new Error("withWorker(): UI import must return a synchronous Int32");
                }
                const output = memory.refreshIfNeeded();
                const resultPointer = pointer + JavaScriptMarshalerArgSize;
                output.u8[resultPointer + JSMarshalerArgumentOffsets.Type] = isNull ? MarshalerType.None : MarshalerType.Int32;
                output.i32[(resultPointer + JSMarshalerArgumentOffsets.Int32Value) / 4] = isNull ? 0 : result;
            } catch (error) {
                writeSharedError(signal, jsimportErrorOffset(slot), error, "UI import");
                state = SignalState.ERROR;
            } finally {
                dispatchDepth--;
                Atomics.store(signal, entry + RING_ENTRY_SIGNAL, state);
                wakeWorker(signal);
            }
        }
    }

    function listen(): void {
        if (stopped) return;
        const generation = Atomics.load(signal, OFFSET_WAKEUP_MAIN);
        try {
            processPending();
        } catch (error) {
            onFailure(error instanceof Error ? error : new Error(String(error)));
            return;
        }
        const wait = Atomics.waitAsync(signal, OFFSET_WAKEUP_MAIN, generation);
        if (wait.async) {
            void wait.value.then(listen);
        } else {
            setTimeout(listen, 0);
        }
    }

    function stop(): void {
        stopped = true;
        functions.clear();
        modules.clear();
        active.clear();
        wakeMain(signal);
    }

    listen();
    return { setModuleImports, processPending, stop, isDispatching: () => dispatchDepth !== 0 };
}

export function createWorkerJSImportInvoker(
    signal: Int32Array, memory: MemoryViewManager, handle: number, processExports: () => void,
    promises = createWorkerPromises(),
) {
    return (args: JSMarshalerArguments, isAsync = false): unknown => {
        const head = Atomics.load(signal, OFFSET_JSIMPORT_HEAD) >>> 0;
        const tail = Atomics.load(signal, OFFSET_JSIMPORT_TAIL) >>> 0;
        if (((head - tail) >>> 0) >= JSIMPORT_RING_CAPACITY) {
            throw new Error("withWorker(): UI import queue is full");
        }
        const slot = reserveCallSlot(signal, OFFSET_JSIMPORT_RING, JSIMPORT_RING_CAPACITY, "UI import");
        const entry = jsimportEntryOffset(slot);
        const id = promises.reserve();
        let pendingPromise = false;
        Atomics.store(signal, entry + RING_ENTRY_PROMISE_ID, id);
        Atomics.store(signal, entry + RING_ENTRY_HANDLE, handle);
        Atomics.store(signal, entry + RING_ENTRY_ARGS_PTR, Number(args));
        Atomics.store(signal, entry + RING_ENTRY_RESULT_KIND, isAsync ? WorkerResultKind.Promise : WorkerResultKind.Scalar);
        Atomics.store(signal, OFFSET_JSIMPORT_QUEUE + head % JSIMPORT_RING_CAPACITY, slot);
        Atomics.store(signal, OFFSET_JSIMPORT_HEAD, (head + 1) >>> 0);
        wakeMain(signal);
        try {
            let state: SignalState;
            for (;;) {
                const generation = Atomics.load(signal, OFFSET_WAKEUP_WORKER);
                state = Atomics.load(signal, entry + RING_ENTRY_SIGNAL);
                if (state !== SignalState.PENDING) break;
                processExports();
                state = Atomics.load(signal, entry + RING_ENTRY_SIGNAL);
                if (state !== SignalState.PENDING) break;
                Atomics.wait(signal, OFFSET_WAKEUP_WORKER, generation);
            }
            if (Atomics.load(signal, entry + RING_ENTRY_PROMISE_ID) !== id) throw new Error("withWorker(): UI import completion mismatch");
            if (state === SignalState.ERROR) {
                throw readSharedError(signal, jsimportErrorOffset(slot));
            }
            if (state !== SignalState.DONE) throw new Error("withWorker(): invalid UI import completion");
            if (isAsync) {
                const kind = Atomics.load(signal, entry + RING_ENTRY_RESULT_KIND);
                if (kind === WorkerResultKind.Null) return null;
                if (kind === WorkerResultKind.Promise) {
                    const promise = promises.promise(id);
                    pendingPromise = true;
                    return promise;
                }
                throw new Error("withWorker(): invalid asynchronous UI import completion");
            }
            const views = memory.refreshIfNeeded();
            const resultPointer = Number(args) + JavaScriptMarshalerArgSize;
            const resultType = views.u8[resultPointer + JSMarshalerArgumentOffsets.Type];
            if (resultType === MarshalerType.None) return null;
            if (resultType !== MarshalerType.Int32) throw new Error("withWorker(): invalid UI import result type");
            return views.i32[(resultPointer + JSMarshalerArgumentOffsets.Int32Value) / 4];
        } finally {
            if (!pendingPromise) promises.release(id);
            Atomics.store(signal, entry + RING_ENTRY_SIGNAL, SignalState.IDLE);
        }
    };
}
