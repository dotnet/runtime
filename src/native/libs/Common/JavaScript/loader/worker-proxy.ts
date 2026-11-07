// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig, RuntimeAPI, VoidPtr } from "./types";
import { InternalExchangeIndex } from "../types";
import { scriptUrl } from "./bootstrap";
import { dotnetApi, dotnetInternals, dotnetRuntimeExports, dotnetBrowserUtilsExports, dotnetLogger, Module } from "./cross-module";
import { runtimeState } from "./exit";
import { loaderCallbacks } from "./callbacks";
import { loadDotnetModule, loadJSModule, callLibraryInitializerOnRuntimeReady, updateWorkerDownloadProgress } from "./assets";
import { initializeLoaderConfig } from "./run";
import { storeAppSettingsBytes } from "../appsettings";
import { serveWorkerResource } from "./worker-assets";
import { createWorkerLocal } from "./worker-local";
import { createWorkerBootConfig } from "./worker-config";
import { deserializeWorkerError, serializeWorkerError } from "./worker-errors";
import type { WorkerError } from "./worker-errors";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { createNativeChannel, NativeOperation, RuntimeOperation } from "./worker-native";
import {
    SignalState, CommandId, MemoryViewManager, allocatePromiseId, OFFSET_VERSION, SIGNALING_VERSION,
    OFFSET_COMMAND_SIGNAL, OFFSET_COMMAND_PROMISE_ID, OFFSET_COMMAND_RESULT, OFFSET_WAKEUP_MAIN,
    OFFSET_WORKER_STATE, OFFSET_WORKER_ERROR_LENGTH, readSharedError, wakeMain,
} from "./shared-signaling";
import type { WorkerToMainMessage, WorkerCommandMessage } from "./worker-runtime";

interface WorkerSession {
    ready: Promise<RuntimeAPI>;
    downloaded: Promise<RuntimeAPI>;
    start(): void;
}

let workerSession: Promise<WorkerSession> | undefined;

export async function createWorkerProxy(config: LoaderConfig, downloadOnly = false): Promise<RuntimeAPI> {
    const session = await (workerSession ??= createWorkerSession(config, downloadOnly));
    if (downloadOnly) return session.downloaded;
    session.start();
    return session.ready;
}

async function createWorkerSession(config: LoaderConfig, preload: boolean): Promise<WorkerSession> {
    const initializers = await initializeLoaderConfig();
    const resources = config.resources;
    if (!resources) throw new Error("runInWorker: runtime resources are missing");
    const boot = createWorkerBootConfig(config);
    const worker = new Worker(scriptUrl, { type: "module" });
    const ready = createPromiseCompletionSource<RuntimeAPI>();
    const downloaded = createPromiseCompletionSource<RuntimeAPI>();
    void ready.promise.catch(error => dotnetLogger.debug(() => `Worker readiness failed: ${error}`));
    void downloaded.promise.catch(error => dotnetLogger.debug(() => `Worker preload failed: ${error}`));
    let startRequested = !preload;
    let signal: Int32Array | undefined;
    let channel: ReturnType<typeof createNativeChannel> | undefined;
    let exited: Error | undefined;
    let pending: AbortController | undefined;
    let exitRequested = false;
    let uiAttached = false;
    const resourceAbort = new AbortController();
    const timeoutMs = config.workerOptions?.timeoutMs ?? 30000;
    const armTimeout = () => timeoutMs > 0 ? setTimeout(() => terminate(new Error("runInWorker: worker startup timed out")), timeoutMs) : undefined;
    let timeout = armTimeout();

    function terminate(error: Error): void {
        if (exited) return;
        exited = error;
        resourceAbort.abort(error);
        if (timeout !== undefined) clearTimeout(timeout);
        runtimeState.exitCode ??= 1;
        const context = dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext];
        if (context) context.exitError = error;
        channel?.stop(error);
        if (typeof dotnetRuntimeExports.forceDisposeProxies === "function") {
            try {
                dotnetRuntimeExports.forceDisposeProxies(false, false);
            } catch (cleanupError) {
                dotnetLogger.error("runInWorker: UI proxy cleanup failed", cleanupError);
            }
        }
        pending?.abort(error);
        worker.onmessage = null;
        worker.onerror = null;
        worker.onmessageerror = null;
        worker.terminate();
        ready.reject(error);
        downloaded.reject(error);
        if (signal) wakeMain(signal);
    }

    async function attach(message: Extract<WorkerToMainMessage, { type: "attach" }>): Promise<void> {
        if (!(message.wasmMemory.buffer instanceof SharedArrayBuffer)) throw new Error("runInWorker: memory is not shared");
        signal = new Int32Array(message.signalBuffer);
        if (Atomics.load(signal, OFFSET_VERSION) !== SIGNALING_VERSION) throw new Error("runInWorker: signaling version mismatch");
        channel = createNativeChannel(signal, message.nativeBuffer, false, (operation, first, second) => {
            switch (operation) {
                case RuntimeOperation.BindJSImport: return Number(dotnetRuntimeExports.bindJSImportST(first as any));
                case RuntimeOperation.InvokeJSImport: dotnetRuntimeExports.invokeJSImportST(first as any, second as any); break;
                case RuntimeOperation.ReleaseCSOwnedObject: dotnetRuntimeExports.releaseCSOwnedObject(first as any); break;
                case RuntimeOperation.ResolveOrRejectPromise: dotnetRuntimeExports.resolveOrRejectPromise(first as any); break;
                case RuntimeOperation.CancelPromise: dotnetRuntimeExports.cancelPromise(first as any); break;
                case RuntimeOperation.InvokeJSFunction: dotnetRuntimeExports.invokeJSFunction(first as any, second as any); break;
                case RuntimeOperation.ForceDisposeProxies: dotnetRuntimeExports.forceDisposeProxies(!!first, !!second); break;
                case RuntimeOperation.AbortInteropTimers: dotnetRuntimeExports.abortInteropTimers(); break;
                case RuntimeOperation.GetProxyCounts:
                    dotnetApi.INTERNAL.TARGET.getProxyCounts().forEach((count: number, index: number) => {
                        dotnetApi.localHeapViewI32()[(first >>> 2) + index] = count;
                    });
                    break;
                case RuntimeOperation.GetErrorSnapshot:
                    dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext]!.getErrorSnapshot!(first, second);
                    break;
                default: throw new Error("runInWorker: unknown runtime callback");
            }
            return 0;
        });
        const native = channel;
        const frames: VoidPtr[] = [];
        Object.assign(Module, {
            _malloc: (size: number) => {
                const pointer = native.invoke(NativeOperation.Malloc, size) >>> 0;
                if (!pointer && size !== 0) throw new Error("runInWorker: native allocation failed");
                return pointer as any as VoidPtr;
            },
            _free: (pointer: VoidPtr) => {
                if (Number(pointer)) native.invoke(NativeOperation.Free, Number(pointer));
            },
            stackSave: () => frames.length as any as VoidPtr,
            stackAlloc: (size: number) => {
                const pointer = Module._malloc(Math.ceil(size / 16) * 16);
                frames.push(pointer);
                return pointer;
            },
            stackRestore: (saved: VoidPtr) => {
                const count = Number(saved);
                if (!Number.isInteger(count) || count < 0 || count > frames.length) throw new Error("runInWorker: invalid scratch stack restore");
                while (frames.length > count) Module._free(frames.pop()!);
            },
            safeSetTimeout: (callback: () => void, delay: number) => setTimeout(callback, delay),
            runtimeKeepalivePush: () => {},
            runtimeKeepalivePop: () => {},
        });
        const call = (operation: NativeOperation, first = 0, second = 0) => {
            const context = dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext];
            native.invoke(operation, first, second, operation === NativeOperation.CompleteTask ? context?.beforeTaskCompletion : undefined);
        };
        dotnetInternals[InternalExchangeIndex.InteropJavaScriptExportsTable] = [
            args => call(NativeOperation.GetManagedStackTrace, Number(args)),
            args => call(NativeOperation.CallDelegate, Number(args)),
            args => call(NativeOperation.CompleteTask, Number(args)),
            args => call(NativeOperation.ReleaseJSOwnedObject, Number(args)),
            args => call(NativeOperation.BindAssemblyExports, Number(args)),
            (handle, args) => call(NativeOperation.CallJSExport, Number(handle), Number(args)),
        ];
        dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext] = {
            role: "ui", invoke: native.invoke, memory: message.wasmMemory, assertNativeCall: native.assertCallAvailable,
            local: createWorkerLocal(worker, resourceAbort.signal, {
                GetDotNetRuntimeContractDescriptor: () => native.invoke(NativeOperation.GetRuntimeContractDescriptor) >>> 0,
                GetDotNetRuntimeHeap: (pointer, length) => dotnetApi.localHeapViewU8().subarray(pointer >>> 0, (pointer >>> 0) + length),
                connectDSRouter: url => {
                    const pointer = dotnetBrowserUtilsExports.stringToUTF16Ptr(url);
                    try {
                        native.invoke(NativeOperation.ConnectDSRouter, Number(pointer), Number(pointer) + url.length * 2);
                    } finally {
                        Module._free(pointer as any);
                    }
                },
            }),
        };
        Object.assign(dotnetApi, {
            runMain: (assembly?: string, args?: string[]) => send(CommandId.RUN_MAIN, assembly, args),
            runMainAndExit: async (assembly?: string, args?: string[]) => {
                if (exited) throw exited;
                if (pending) throw new Error("runInWorker: another command is already running");
                try {
                    const code = await send(CommandId.RUN_MAIN_AND_EXIT, assembly, args);
                    runtimeState.exitCode = code;
                    return code;
                } finally {
                    terminate(new Error("runInWorker: runtime exited"));
                }
            },
            exit: (code: number, reason?: unknown) => {
                if (exited || exitRequested) return;
                exitRequested = true;
                const error = reason instanceof Error ? reason
                    : new Error(reason === undefined ? `runInWorker: runtime exited with code ${code}` : String(reason));
                const context = dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext];
                if (context) context.exitError = error;
                pending?.abort(error);
                pending = undefined;
                void send(CommandId.EXIT, undefined, undefined, code, serializeWorkerError(error)).then(() => {
                    runtimeState.exitCode = code;
                    terminate(error);
                }, failure => {
                    dotnetLogger.error("runInWorker: exit failed", failure);
                    terminate(error);
                });
            },
        });
        runtimeState.nativeReady = true;
        const runtimeModule = await loadDotnetModule(resources!.jsModuleRuntime[0]);
        await runtimeModule.dotnetInitializeModule<void>(dotnetInternals);
        dotnetApi.INTERNAL.workerMemoryViews = new MemoryViewManager(message.wasmMemory, signal);
        dotnetApi.INTERNAL.nativeBoundary = { role: "ui", getStats: native.getStats, getScratchCount: () => frames.length };
        native.listen(terminate);
        uiAttached = true;
        worker.postMessage({ type: "attached" });
    }

    async function send(commandId: CommandId, mainAssemblyName?: string, args?: string[], exitCode?: number, reason?: WorkerError): Promise<number> {
        if (exited) throw exited;
        if (!signal) throw new Error("runInWorker: native transport is not attached");
        if (pending) throw new Error("runInWorker: another command is already running");
        const activeSignal = signal;
        const controller = new AbortController();
        pending = controller;
        const promiseId = allocatePromiseId();
        Atomics.store(activeSignal, OFFSET_COMMAND_SIGNAL, SignalState.PENDING);
        const command: WorkerCommandMessage = { type: "command", commandId, promiseId, mainAssemblyName, args, exitCode, reason };
        try {
            worker.postMessage(command);
            return await new Promise<number>((resolve, reject) => {
                const check = () => {
                    if (controller.signal.aborted) {
                        reject(controller.signal.reason); return;
                    }
                    const generation = Atomics.load(activeSignal, OFFSET_WAKEUP_MAIN);
                    if (Atomics.load(activeSignal, OFFSET_COMMAND_PROMISE_ID) === promiseId
                        && Atomics.load(activeSignal, OFFSET_COMMAND_SIGNAL) === SignalState.DONE) {
                        resolve(Atomics.load(activeSignal, OFFSET_COMMAND_RESULT));
                        return;
                    }
                    if (Atomics.load(activeSignal, OFFSET_WORKER_STATE) === SignalState.ERROR) {
                        reject(readSharedError(activeSignal, OFFSET_WORKER_ERROR_LENGTH));
                        return;
                    }
                    const wait = Atomics.waitAsync(activeSignal, OFFSET_WAKEUP_MAIN, generation);
                    if (wait.async) void wait.value.then(check);
                    else setTimeout(check, 0);
                };
                controller.signal.addEventListener("abort", check, { once: true });
                check();
            });
        } finally {
            if (pending === controller) pending = undefined;
        }
    }

    worker.onmessage = event => {
        if (exited) return;
        const message = event.data as WorkerToMainMessage;
        if (message.type === "diagnostic-download") {
            try {
                const download = dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext]?.downloadDiagnosticData;
                if (!download) throw new Error("runInWorker: UI diagnostic download handler is not initialized");
                download(message.chunks);
            } catch (error) {
                terminate(error instanceof Error ? error : new Error(String(error)));
            }
        } else if (message.type === "resource") {
            void serveWorkerResource(message, resourceAbort.signal, boot.assets).catch(error => terminate(error instanceof Error ? error : new Error(String(error))));
        } else if (message.type === "download-progress") {
            try {
                updateWorkerDownloadProgress(message.loaded, message.total);
            } catch (error) {
                terminate(error instanceof Error ? error : new Error(String(error)));
            }
        } else if (message.type === "attach") {
            if (signal) {
                terminate(new Error("runInWorker: duplicate native attachment")); return;
            }
            void attach(message).catch(error => terminate(error instanceof Error ? error : new Error(String(error))));
        } else if (message.type === "error") {
            const error = deserializeWorkerError(message.error);
            dotnetLogger.error("runInWorker: native worker failed", error);
            terminate(error);
        } else if (message.type === "preloaded") {
            if (!uiAttached) {
                terminate(new Error("runInWorker: worker preloaded before UI attachment"));
                return;
            }
            if (timeout !== undefined) clearTimeout(timeout);
            for (const [fileName, bytes] of message.appSettings) storeAppSettingsBytes(fileName, bytes);
            downloaded.resolve(dotnetApi);
        } else if (message.type === "ready") {
            if (!uiAttached) {
                terminate(new Error("runInWorker: worker became ready before UI attachment")); return;
            }
            if (timeout !== undefined) clearTimeout(timeout);
            for (const [fileName, bytes] of message.appSettings) storeAppSettingsBytes(fileName, bytes);
            runtimeState.dotnetReady = true;
            Object.assign(dotnetApi.runtimeBuildInfo, message.runtimeBuildInfo);
            dotnetApi.INTERNAL.nativeBoundary.workerProxyCounts = message.workerProxyCounts;
            void (async () => {
                const afterReady = (resources.modulesAfterRuntimeReady ?? []).map(asset => [asset, loadJSModule(asset)] as const);
                await loaderCallbacks.dotnetReady?.();
                await Promise.all([...initializers, ...afterReady].map(([asset, promise]) => callLibraryInitializerOnRuntimeReady([asset, promise])));
                ready.resolve(dotnetApi);
                downloaded.resolve(dotnetApi);
            })().catch(error => terminate(error instanceof Error ? error : new Error(String(error))));
        }
    };
    worker.onerror = event => terminate(new Error(event.message));
    worker.onmessageerror = () => terminate(new Error("runInWorker: invalid worker message"));
    try {
        worker.postMessage({ type: "init", config: boot.config, downloadOnly: preload });
    } catch (error) {
        terminate(error instanceof Error ? error : new Error(String(error)));
    }
    return {
        ready: ready.promise,
        downloaded: downloaded.promise,
        start: () => {
            if (startRequested) return;
            startRequested = true;
            void downloaded.promise.then(() => {
                if (exited) throw exited;
                timeout = armTimeout();
                worker.postMessage({ type: "start" });
            }).catch(error => terminate(error instanceof Error ? error : new Error(String(error))));
        },
    };
}
