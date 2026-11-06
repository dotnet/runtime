// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig, RuntimeAPI } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetApi, dotnetInternals, dotnetNativeBrowserExports, dotnetInteropJSExports, dotnetBrowserUtilsExports, Module } from "./cross-module";
import { mergeLoaderConfig } from "./config";
import { HostBuilder } from "./host-builder";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { createNativeCallBuffer, createNativeChannel, NativeOperation } from "./worker-native";
import { loadWorkerResource, resolveWorkerModule } from "./worker-assets";
import type { WorkerResourceRequest } from "./worker-assets";
import { loaderCallbacks } from "./callbacks";
import { fetchLazyAssembly, fetchSatelliteAssemblies } from "./assets";
import { serveWorkerLocal } from "./worker-local";
import type { WorkerLocalRequest } from "./worker-local";
import { deserializeWorkerError, serializeWorkerError } from "./worker-errors";
import type { WorkerError } from "./worker-errors";
import { restoreWorkerModules } from "./worker-config";
import { getAppSettingsFiles, getAppSettingsBytes } from "../appsettings";
import {
    createSignalingBuffer, SignalState, CommandId, OFFSET_COMMAND_RESULT, OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_PROMISE_ID, OFFSET_WORKER_STATE, OFFSET_WORKER_ERROR_LENGTH, writeSharedError, wakeMain,
} from "./shared-signaling";

export interface WorkerInitMessage { type: "init"; config: LoaderConfig; downloadOnly?: boolean }
export interface WorkerCommandMessage {
    type: "command"; commandId: CommandId; promiseId: number;
    mainAssemblyName?: string; args?: string[]; exitCode?: number; reason?: WorkerError;
}
export type WorkerToMainMessage =
    | { type: "attach", wasmMemory: WebAssembly.Memory, signalBuffer: SharedArrayBuffer, nativeBuffer: SharedArrayBuffer }
    | { type: "ready", runtimeBuildInfo: RuntimeAPI["runtimeBuildInfo"], workerProxyCounts: number[], appSettings: [string, Uint8Array][] }
    | { type: "preloaded", appSettings: [string, Uint8Array][] }
    | { type: "error", error: WorkerError }
    | WorkerResourceRequest
    | { type: "diagnostic-download"; chunks: Uint8Array[] }
    | { type: "download-progress"; loaded: number; total: number };

export function waitForInitMessage(): Promise<WorkerInitMessage> {
    return new Promise(resolve => {
        const handler = (event: MessageEvent<WorkerInitMessage>) => {
            if (event.data?.type !== "init") return;
            self.removeEventListener("message", handler);
            resolve(event.data);
        };
        self.addEventListener("message", handler);
    });
}

export async function bootstrapWorkerRuntime(message: WorkerInitMessage, bootConfig: LoaderConfig): Promise<void> {
    const signalBuffer = createSignalingBuffer();
    const signal = new Int32Array(signalBuffer);
    const nativeBuffer = createNativeCallBuffer();
    const attached = createPromiseCompletionSource<void>();
    let runtime: RuntimeAPI;
    let commandRunning = false;
    let closing = false;
    let failed = false;
    let starting = false;
    const builder = new HostBuilder();
    const channel = createNativeChannel(signal, nativeBuffer, true, (operation, first, second) => {
        const args = first as any;
        switch (operation) {
            case NativeOperation.GetManagedStackTrace: dotnetInteropJSExports.SystemInteropJS_GetManagedStackTrace(args); break;
            case NativeOperation.CallDelegate: dotnetInteropJSExports.SystemInteropJS_CallDelegate(args); break;
            case NativeOperation.CompleteTask: dotnetInteropJSExports.SystemInteropJS_CompleteTask(args); break;
            case NativeOperation.ReleaseJSOwnedObject: dotnetInteropJSExports.SystemInteropJS_ReleaseJSOwnedObjectByGCHandle(args); break;
            case NativeOperation.BindAssemblyExports: dotnetInteropJSExports.SystemInteropJS_BindAssemblyExports(args); break;
            case NativeOperation.CallJSExport: dotnetInteropJSExports.SystemInteropJS_CallJSExport(args, second as any); break;
            case NativeOperation.Malloc: return Number(Module._malloc(first));
            case NativeOperation.Free: Module._free(args); break;
            case NativeOperation.RunBackgroundTimers: dotnetBrowserUtilsExports.runBackgroundTimers(); break;
            case NativeOperation.GetRuntimeContractDescriptor: return dotnetApi.INTERNAL.GetDotNetRuntimeContractDescriptor();
            case NativeOperation.ConnectDSRouter: dotnetApi.INTERNAL.connectDSRouter(dotnetBrowserUtilsExports.utf16ToString(args, second)); break;
            case NativeOperation.GetErrorSnapshot:
                dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext]!.getErrorSnapshot!(first, second);
                break;
            default: throw new Error(`runInWorker: unknown native operation ${operation}`);
        }
        return 0;
    });

    function fail(error: unknown): void {
        if (failed) return;
        failed = true;
        const failure = error instanceof Error ? error : new Error(String(error));
        writeSharedError(signal, OFFSET_WORKER_ERROR_LENGTH, failure, "native worker");
        Atomics.store(signal, OFFSET_WORKER_STATE, SignalState.ERROR);
        attached.reject(failure);
        channel.stop(failure);
        wakeMain(signal);
        self.postMessage({ type: "error", error: serializeWorkerError(failure) });
    }

    self.addEventListener("message", (event: MessageEvent<WorkerCommandMessage | WorkerLocalRequest | { type: "attached" } | { type: "start" }>) => {
        const request = event.data;
        if (request.type === "start") {
            if (starting || closing || failed) return;
            void start().catch(fail);
            return;
        }
        if (request.type === "local") {
            if (closing || failed) {
                request.port.postMessage({ type: "error", error: serializeWorkerError(new Error("runInWorker: runtime exited")) });
                request.port.close();
                return;
            }
            void serveWorkerLocal(request, dotnetApi.INTERNAL).catch(fail);
            return;
        }
        if (request.type === "attached") {
            attached.resolve();
            return;
        }
        if (request.type !== "command" || closing) return;
        if (commandRunning && request.commandId !== CommandId.EXIT) {
            fail(new Error("runInWorker: another command is already running"));
            return;
        }
        commandRunning = true;
        void execute(request).then(result => {
            if (closing && request.commandId !== CommandId.EXIT) return;
            Atomics.store(signal, OFFSET_COMMAND_RESULT, result);
            Atomics.store(signal, OFFSET_COMMAND_PROMISE_ID, request.promiseId);
            Atomics.store(signal, OFFSET_COMMAND_SIGNAL, SignalState.DONE);
            commandRunning = false;
            if (request.commandId !== CommandId.RUN_MAIN) {
                closing = true;
                Atomics.store(signal, OFFSET_WORKER_STATE, SignalState.DONE);
                self.close();
            }
            wakeMain(signal);
        }, fail);
    });

    async function execute(command: WorkerCommandMessage): Promise<number> {
        if (command.commandId === CommandId.RUN_MAIN) return runtime.runMain(command.mainAssemblyName, command.args);
        if (command.commandId === CommandId.RUN_MAIN_AND_EXIT) return runtime.runMainAndExit(command.mainAssemblyName, command.args);
        if (command.commandId !== CommandId.EXIT) throw new Error("runInWorker: invalid lifecycle command");
        closing = true;
        const reason = command.reason ? deserializeWorkerError(command.reason) : undefined;
        const context = dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext];
        if (context && reason) context.exitError = reason;
        try {
            runtime.exit(command.exitCode ?? 0, reason);
        } catch (error) {
            if (error && typeof error === "object" && "status" in error && typeof error.status === "number") return error.status;
            throw error;
        }

        return command.exitCode ?? 0;
    }

    async function start(): Promise<void> {
        starting = true;
        runtime = await builder.create();
        self.postMessage({ type: "ready", runtimeBuildInfo: runtime.runtimeBuildInfo,
            workerProxyCounts: runtime.INTERNAL.nativeBoundary.getProxyCounts(), appSettings: appSettingsSnapshot() });
    }

    function appSettingsSnapshot(): [string, Uint8Array][] {
        return getAppSettingsFiles().map(fileName => [fileName, getAppSettingsBytes(fileName)!]);
    }

    dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext] = {
        role: "worker", invoke: channel.invoke, loadResource: loadWorkerResource, resolveModuleResource: resolveWorkerModule,
        downloadDiagnosticData: chunks => self.postMessage({ type: "diagnostic-download", chunks }),
        beforeRuntimeStart: async () => {
            const wasmMemory = dotnetNativeBrowserExports.getWasmMemory();
            if (!(wasmMemory.buffer instanceof SharedArrayBuffer)) throw new Error("runInWorker: native memory is not shared");
            self.postMessage({ type: "attach", wasmMemory, signalBuffer, nativeBuffer });
            await attached.promise;
        },
    };
    loaderCallbacks.downloadResourceProgress = (loaded, total) => self.postMessage({ type: "download-progress", loaded, total });
    channel.listen(fail);
    self.addEventListener("error", (event: ErrorEvent) => fail(event.error ?? new Error(event.message)));
    self.addEventListener("unhandledrejection", (event: PromiseRejectionEvent) => {
        event.preventDefault();
        event.stopImmediatePropagation();
        fail(event.reason);
    });
    try {
        Object.assign(dotnetApi.INTERNAL ??= {}, {
            loadLazyAssembly: fetchLazyAssembly,
            loadSatelliteAssemblies: fetchSatelliteAssemblies,
        });
        restoreWorkerModules(message.config, bootConfig);
        mergeLoaderConfig(message.config);
        if (message.downloadOnly) {
            await builder.download(false);
            self.postMessage({ type: "preloaded", appSettings: appSettingsSnapshot() });
        } else await start();
    } catch (error) {
        fail(error);
    }
}
