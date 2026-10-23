// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// -----------------------------------------------------------
// Main-thread worker proxy for withWorker() mode.
//
// Spawns a dedicated worker running the same dotnet.js module
// and returns a RuntimeAPI
// that proxies runMain/runMainAndExit to the worker via shared
// memory signaling.
//
// Full JSImport/JSExport interop is deferred to a future phase.
// -----------------------------------------------------------

import type { LoaderConfig, RuntimeAPI } from "./types";

import {
    SignalState,
    CommandId,
    OFFSET_WAKEUP_MAIN,
    OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_ID,
    OFFSET_COMMAND_RESULT,
    OFFSET_COMMAND_PROMISE_ID,
    allocatePromiseId,
    wakeMain,
    MemoryViewManager,
} from "./shared-signaling";

import type { WorkerCommandMessage, WorkerInitMessage, WorkerToMainMessage } from "./worker-runtime";
import { scriptUrl } from "./bootstrap";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { dotnetLogger, Module } from "./cross-module";

const DEFAULT_TIMEOUT_MS = 30_000;

export async function createWorkerProxy(config: LoaderConfig): Promise<RuntimeAPI> {

    const worker = new globalThis.Worker(scriptUrl, { type: "module" });

    const { promise: apiPromise, resolve, reject } = createPromiseCompletionSource<RuntimeAPI>();
    let timeoutId: ReturnType<typeof setTimeout> | undefined;
    let rejectCommand: ((error: Error) => void) | undefined;
    let terminated = false;

    function terminate(error: Error): void {
        if (terminated) return;
        terminated = true;
        if (timeoutId !== undefined) clearTimeout(timeoutId);
        worker.onmessage = null;
        worker.onerror = null;
        worker.onmessageerror = null;
        worker.terminate();
        rejectCommand?.(error);
        reject(error);
    }

    const timeoutMs = config.workerOptions?.timeoutMs ?? DEFAULT_TIMEOUT_MS;
    if (timeoutMs > 0) {
        timeoutId = setTimeout(() => {
            terminate(new Error(`withWorker(): worker did not respond within ${timeoutMs}ms`));
        }, timeoutMs);
    }

    worker.onmessage = (ev: MessageEvent<WorkerToMainMessage>): void => {
        const msg = ev.data;

        if (msg.type === "ready") {
            if (!(msg.wasmMemory?.buffer instanceof SharedArrayBuffer)) {
                terminate(new Error("withWorker(): worker WASM memory is missing or not shared"));
                return;
            }
            if (timeoutId !== undefined) {
                clearTimeout(timeoutId);
            }

            const signalView = new Int32Array(msg.signalBuffer);
            const memoryViews = new MemoryViewManager(msg.wasmMemory, signalView);
            const api = buildProxyRuntimeAPI(config, signalView, memoryViews, msg.runtimeBuildInfo, worker, terminate, handler => {
                rejectCommand = handler;
            });
            resolve(api);
        } else if (msg.type === "error") {
            terminate(new Error(`withWorker(): worker failed: ${msg.message}`));
        }
    };

    worker.onerror = (ev: ErrorEvent): void => {
        terminate(new Error(`withWorker(): worker error: ${ev.message}`));
    };
    worker.onmessageerror = (): void => {
        terminate(new Error("withWorker(): could not deserialize a worker message"));
    };

    const initMsg: WorkerInitMessage = { type: "init", config };
    try {
        worker.postMessage(initMsg);
    } catch (error) {
        terminate(error instanceof Error ? error : new Error(String(error)));
    }

    return await apiPromise;
}

// -----------------------------------------------------------
// Build the proxy RuntimeAPI returned to the caller
//
// Only runMain, runMainAndExit, exit, getConfig, and
// runtimeBuildInfo are functional. All other APIs throw
// "not supported in worker proxy mode" for now.
// -----------------------------------------------------------

function buildProxyRuntimeAPI(
    config: LoaderConfig,
    signalView: Int32Array,
    memoryViews: MemoryViewManager,
    runtimeBuildInfo: RuntimeAPI["runtimeBuildInfo"],
    worker: Worker,
    terminate: (error: Error) => void,
    registerFailureHandler: (handler: (error: Error) => void) => void,
): RuntimeAPI {
    let pendingCommand: AbortController | undefined;
    let exited: Error | undefined;
    let exiting = false;
    let exitRequested = false;

    registerFailureHandler(error => {
        exited = error;
        pendingCommand?.abort(error);
        wakeMain(signalView);
    });

    async function sendCommandAndWait(commandId: CommandId, mainAssemblyName?: string, args?: string[], exitCode?: number, reason?: unknown): Promise<number> {
        if (exited) throw exited;
        if (pendingCommand) throw new Error("withWorker(): another command is already running");
        const controller = new AbortController();
        pendingCommand = controller;
        const promiseId = allocatePromiseId();
        const message: WorkerCommandMessage = { type: "command", commandId, promiseId, mainAssemblyName, args, exitCode, reason };
        Atomics.store(signalView, OFFSET_COMMAND_ID, commandId);
        Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.PENDING);
        try {
            // Strings are structured-cloned until the shared-heap marshaler is implemented.
            worker.postMessage(message);
            return await waitForCommandCompletion(signalView, promiseId, controller.signal);
        } finally {
            if (pendingCommand === controller) pendingCommand = undefined;
        }
    }

    const notSupported = (name: string) => (): never => {
        throw new Error(`${name}() is not supported in withWorker() proxy mode`);
    };

    const api: RuntimeAPI = {
        // Functional APIs
        runMain: (mainAssemblyName?: string, args?: string[]): Promise<number> => {
            if (exiting) return Promise.reject(exited ?? new Error("withWorker(): runtime is exiting"));
            return sendCommandAndWait(CommandId.RUN_MAIN, mainAssemblyName, args);
        },
        runMainAndExit: async (mainAssemblyName?: string, args?: string[]): Promise<number> => {
            if (exiting) throw exited ?? new Error("withWorker(): runtime is exiting");
            if (pendingCommand) throw new Error("withWorker(): another command is already running");
            exiting = true;
            try {
                return await sendCommandAndWait(CommandId.RUN_MAIN_AND_EXIT, mainAssemblyName, args);
            } finally {
                if (!exitRequested) terminate(new Error("withWorker(): runtime has exited"));
            }
        },
        exit: (code: number, reason?: unknown): void => {
            if (exitRequested || exited) return;
            exitRequested = true;
            exiting = true;
            pendingCommand?.abort(new Error(`withWorker(): runtime exited with code ${code}`));
            pendingCommand = undefined;
            wakeMain(signalView);
            const timeout = setTimeout(() => {
                const error = new Error("withWorker(): worker did not acknowledge exit");
                dotnetLogger.error(error.message, error);
                terminate(error);
            }, DEFAULT_TIMEOUT_MS);
            void sendCommandAndWait(CommandId.EXIT, undefined, undefined, code, reason).catch(error => {
                dotnetLogger.error("withWorker(): worker shutdown failed", error);
            }).finally(() => {
                clearTimeout(timeout);
                terminate(new Error(`withWorker(): runtime exited with code ${code}`));
            });
        },
        getConfig: (): LoaderConfig => config,
        runtimeBuildInfo,

        // Stubs — not yet supported in proxy mode
        INTERNAL: { workerMemoryViews: memoryViews },
        Module,
        runtimeId: 0,
        getAssemblyExports: notSupported("getAssemblyExports"),
        setModuleImports: notSupported("setModuleImports"),
        invokeLibraryInitializers: notSupported("invokeLibraryInitializers"),

        // Memory APIs — not supported in proxy mode
        setHeapB32: notSupported("setHeapB32"),
        setHeapB8: notSupported("setHeapB8"),
        setHeapU8: notSupported("setHeapU8"),
        setHeapU16: notSupported("setHeapU16"),
        setHeapU32: notSupported("setHeapU32"),
        setHeapI8: notSupported("setHeapI8"),
        setHeapI16: notSupported("setHeapI16"),
        setHeapI32: notSupported("setHeapI32"),
        setHeapI52: notSupported("setHeapI52"),
        setHeapU52: notSupported("setHeapU52"),
        setHeapI64Big: notSupported("setHeapI64Big"),
        setHeapF32: notSupported("setHeapF32"),
        setHeapF64: notSupported("setHeapF64"),
        getHeapB32: notSupported("getHeapB32"),
        getHeapB8: notSupported("getHeapB8"),
        getHeapU8: notSupported("getHeapU8"),
        getHeapU16: notSupported("getHeapU16"),
        getHeapU32: notSupported("getHeapU32"),
        getHeapI8: notSupported("getHeapI8"),
        getHeapI16: notSupported("getHeapI16"),
        getHeapI32: notSupported("getHeapI32"),
        getHeapI52: notSupported("getHeapI52"),
        getHeapU52: notSupported("getHeapU52"),
        getHeapI64Big: notSupported("getHeapI64Big"),
        getHeapF32: notSupported("getHeapF32"),
        getHeapF64: notSupported("getHeapF64"),
        localHeapViewI8: notSupported("localHeapViewI8"),
        localHeapViewI16: notSupported("localHeapViewI16"),
        localHeapViewI32: notSupported("localHeapViewI32"),
        localHeapViewI64Big: notSupported("localHeapViewI64Big"),
        localHeapViewU8: notSupported("localHeapViewU8"),
        localHeapViewU16: notSupported("localHeapViewU16"),
        localHeapViewU32: notSupported("localHeapViewU32"),
        localHeapViewF32: notSupported("localHeapViewF32"),
        localHeapViewF64: notSupported("localHeapViewF64"),

        // Diagnostics APIs — not supported in proxy mode
        collectCpuSamples: notSupported("collectCpuSamples"),
        collectMetrics: notSupported("collectMetrics"),
        collectGcDump: notSupported("collectGcDump"),
        collectPgoTrace: notSupported("collectPgoTrace"),
        connectDSRouter: notSupported("connectDSRouter"),
    };

    return api;
}

// -----------------------------------------------------------
// Async wait for command completion using Atomics.waitAsync
//
// Waits on OFFSET_WAKEUP_MAIN for notifications from the worker.
// On each wakeup, checks whether the command signal has moved
// from PENDING to DONE or ERROR.
// -----------------------------------------------------------

function waitForCommandCompletion(signalView: Int32Array, promiseId: number, abortSignal: AbortSignal): Promise<number> {
    return new Promise<number>((resolve, reject) => {
        function check(): void {
            if (abortSignal.aborted) {
                reject(abortSignal.reason);
                return;
            }
            const lastSeen = Atomics.load(signalView, OFFSET_WAKEUP_MAIN);
            const signal = Atomics.load(signalView, OFFSET_COMMAND_PROMISE_ID) === promiseId
                ? Atomics.load(signalView, OFFSET_COMMAND_SIGNAL)
                : SignalState.PENDING;
            if (signal === SignalState.DONE) {
                const result = Atomics.load(signalView, OFFSET_COMMAND_RESULT);
                Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.IDLE);
                resolve(result);

                return;
            }
            if (signal === SignalState.ERROR) {
                const result = Atomics.load(signalView, OFFSET_COMMAND_RESULT);
                Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.IDLE);
                reject(new Error(`Worker command failed with exit code ${result}`));

                return;
            }

            // Not yet complete — wait for next wakeup
            const waitResult = Atomics.waitAsync(signalView, OFFSET_WAKEUP_MAIN, lastSeen);
            if (waitResult.async) {
                waitResult.value.then(check);
            } else {
                // "not-equal" — value already changed, check immediately
                setTimeout(check, 0);
            }
        }

        check();
    });
}
