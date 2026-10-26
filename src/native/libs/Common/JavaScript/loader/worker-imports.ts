// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { JSMarshalerArguments } from "./types";
import { JavaScriptMarshalerArgSize, JSMarshalerArgumentOffsets, MarshalerType } from "../../../System.Runtime.InteropServices.JavaScript.Native/interop/types";
import {
    allocatePromiseId, JSIMPORT_RING_CAPACITY, jsimportEntryOffset,
    MemoryViewManager, OFFSET_JSIMPORT_ERROR_LENGTH, readSharedError, writeSharedError,
    OFFSET_JSIMPORT_HEAD, OFFSET_JSIMPORT_TAIL, OFFSET_WAKEUP_MAIN, OFFSET_WAKEUP_WORKER,
    RING_ENTRY_ARGS_PTR, RING_ENTRY_HANDLE, RING_ENTRY_PROMISE_ID, RING_ENTRY_SIGNAL,
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
    postImports: (message: WorkerModuleImportsMessage) => void,
    onFailure: (error: Error) => void,
) {
    const functions = new Map<number, UIImportFunction>();
    const modules = new Set<string>();
    let nextHandle = 1;
    let stopped = false;
    let dispatching = false;

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
        let tail = Atomics.load(signal, OFFSET_JSIMPORT_TAIL) >>> 0;
        while (tail !== (Atomics.load(signal, OFFSET_JSIMPORT_HEAD) >>> 0)) {
            const entry = jsimportEntryOffset(tail);
            if (Atomics.load(signal, entry + RING_ENTRY_SIGNAL) !== SignalState.PENDING) {
                throw new Error("withWorker(): invalid UI import queue state");
            }
            let state = SignalState.DONE;
            try {
                const invoke = functions.get(Atomics.load(signal, entry + RING_ENTRY_HANDLE));
                if (!invoke) throw new Error("withWorker(): unknown UI import handle");
                const pointer = Atomics.load(signal, entry + RING_ENTRY_ARGS_PTR) >>> 0;
                const views = memory.refreshIfNeeded();
                if (!pointer || pointer % 4 !== 0 || pointer + 3 * JavaScriptMarshalerArgSize > views.u8.byteLength) {
                    throw new Error("withWorker(): invalid UI import argument frame");
                }
                const value = views.i32[(pointer + 2 * JavaScriptMarshalerArgSize + JSMarshalerArgumentOffsets.Int32Value) / 4];
                let response: unknown;
                dispatching = true;
                try {
                    response = invoke(value);
                } finally {
                    dispatching = false;
                }
                const result = response;
                const isNull = result === null || result === undefined;
                if (!isNull && (typeof result !== "number" || !Number.isInteger(result) || result < -0x8000_0000 || result > 0x7fff_ffff)) {
                    throw new Error("withWorker(): UI import must return a synchronous Int32");
                }
                const output = memory.refreshIfNeeded();
                const resultPointer = pointer + JavaScriptMarshalerArgSize;
                output.u8[resultPointer + JSMarshalerArgumentOffsets.Type] = isNull ? MarshalerType.None : MarshalerType.Int32;
                output.i32[(resultPointer + JSMarshalerArgumentOffsets.Int32Value) / 4] = isNull ? 0 : result;
            } catch (error) {
                // One synchronous import is in flight. Nested calls need per-call error storage.
                writeSharedError(signal, OFFSET_JSIMPORT_ERROR_LENGTH, error, "UI import");
                state = SignalState.ERROR;
            }
            Atomics.store(signal, entry + RING_ENTRY_SIGNAL, state);
            tail = (tail + 1) >>> 0;
            Atomics.store(signal, OFFSET_JSIMPORT_TAIL, tail);
            wakeWorker(signal);
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
        wakeMain(signal);
    }

    listen();
    return { setModuleImports, processPending, stop, isDispatching: () => dispatching };
}

export function createWorkerJSImportInvoker(signal: Int32Array, memory: MemoryViewManager, handle: number) {
    return (args: JSMarshalerArguments): number | null => {
        const head = Atomics.load(signal, OFFSET_JSIMPORT_HEAD) >>> 0;
        const tail = Atomics.load(signal, OFFSET_JSIMPORT_TAIL) >>> 0;
        const entry = jsimportEntryOffset(head);
        if (((head - tail) >>> 0) >= JSIMPORT_RING_CAPACITY || Atomics.load(signal, entry + RING_ENTRY_SIGNAL) !== SignalState.IDLE) {
            throw new Error("withWorker(): UI import queue is full");
        }
        const id = allocatePromiseId();
        Atomics.store(signal, entry + RING_ENTRY_PROMISE_ID, id);
        Atomics.store(signal, entry + RING_ENTRY_HANDLE, handle);
        Atomics.store(signal, entry + RING_ENTRY_ARGS_PTR, Number(args));
        Atomics.store(signal, entry + RING_ENTRY_SIGNAL, SignalState.PENDING);
        Atomics.store(signal, OFFSET_JSIMPORT_HEAD, (head + 1) >>> 0);
        wakeMain(signal);
        try {
            let state: SignalState;
            for (;;) {
                const generation = Atomics.load(signal, OFFSET_WAKEUP_WORKER);
                state = Atomics.load(signal, entry + RING_ENTRY_SIGNAL);
                if (state !== SignalState.PENDING) break;
                Atomics.wait(signal, OFFSET_WAKEUP_WORKER, generation);
            }
            if (Atomics.load(signal, entry + RING_ENTRY_PROMISE_ID) !== id) throw new Error("withWorker(): UI import completion mismatch");
            if (state === SignalState.ERROR) {
                throw readSharedError(signal, OFFSET_JSIMPORT_ERROR_LENGTH);
            }
            if (state !== SignalState.DONE) throw new Error("withWorker(): invalid UI import completion");
            const views = memory.refreshIfNeeded();
            const resultPointer = Number(args) + JavaScriptMarshalerArgSize;
            const resultType = views.u8[resultPointer + JSMarshalerArgumentOffsets.Type];
            if (resultType === MarshalerType.None) return null;
            if (resultType !== MarshalerType.Int32) throw new Error("withWorker(): invalid UI import result type");
            return views.i32[(resultPointer + JSMarshalerArgumentOffsets.Int32Value) / 4];
        } finally {
            Atomics.store(signal, entry + RING_ENTRY_SIGNAL, SignalState.IDLE);
        }
    };
}
