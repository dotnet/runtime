// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig, RuntimeAPI } from "./types";
import { InternalExchangeIndex } from "../types";
import { dotnetInternals, dotnetNativeBrowserExports, dotnetInteropJSExports, dotnetBrowserUtilsExports, Module } from "./cross-module";
import { mergeLoaderConfig } from "./config";
import { HostBuilder } from "./host-builder";
import { createPromiseCompletionSource } from "./promise-completion-source";
import { createNativeCallBuffer, createNativeChannel, NativeOperation } from "./worker-native";
import {
    createSignalingBuffer, SignalState, CommandId, OFFSET_COMMAND_RESULT, OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_PROMISE_ID, OFFSET_WORKER_STATE, OFFSET_WORKER_ERROR_LENGTH, writeSharedError, wakeMain,
} from "./shared-signaling";

export interface WorkerInitMessage { type: "init"; config: LoaderConfig }
export interface WorkerCommandMessage {
    type: "command"; commandId: CommandId; promiseId: number;
    mainAssemblyName?: string; args?: string[]; exitCode?: number; reason?: unknown;
}
export type WorkerToMainMessage =
    | { type: "attach", wasmMemory: WebAssembly.Memory, signalBuffer: SharedArrayBuffer, nativeBuffer: SharedArrayBuffer }
    | { type: "ready", runtimeBuildInfo: RuntimeAPI["runtimeBuildInfo"], workerProxyCounts: number[] }
    | { type: "error", message: string };

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

export async function bootstrapWorkerRuntime(message: WorkerInitMessage): Promise<void> {
    const signalBuffer = createSignalingBuffer();
    const signal = new Int32Array(signalBuffer);
    const nativeBuffer = createNativeCallBuffer();
    const attached = createPromiseCompletionSource<void>();
    let runtime: RuntimeAPI;
    let commandRunning = false;
    let closing = false;
    let failed = false;
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
            default: throw new Error(`withWorker(): unknown native operation ${operation}`);
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
        self.postMessage({ type: "error", message: failure.message });
    }

    self.addEventListener("message", (event: MessageEvent<WorkerCommandMessage | { type: "attached" }>) => {
        const request = event.data;
        if (request.type === "attached") {
            attached.resolve();
            return;
        }
        if (request.type !== "command" || closing) return;
        if (commandRunning && request.commandId !== CommandId.EXIT) {
            fail(new Error("withWorker(): another command is already running"));
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
        if (command.commandId !== CommandId.EXIT) throw new Error("withWorker(): invalid lifecycle command");
        closing = true;
        try {
            runtime.exit(command.exitCode ?? 0, command.reason);
        } catch (error) {
            if (error && typeof error === "object" && "status" in error && typeof error.status === "number") return error.status;
            throw error;
        }
        return command.exitCode ?? 0;
    }

    dotnetInternals[InternalExchangeIndex.WorkerRuntimeContext] = {
        role: "worker", invoke: channel.invoke,
        beforeRuntimeStart: async () => {
            const wasmMemory = dotnetNativeBrowserExports.getWasmMemory();
            if (!(wasmMemory.buffer instanceof SharedArrayBuffer)) throw new Error("withWorker(): native memory is not shared");
            self.postMessage({ type: "attach", wasmMemory, signalBuffer, nativeBuffer });
            await attached.promise;
        },
    };
    channel.listen(fail);
    self.addEventListener("error", (event: ErrorEvent) => fail(event.error ?? new Error(event.message)));
    self.addEventListener("unhandledrejection", (event: PromiseRejectionEvent) => {
        event.preventDefault();
        event.stopImmediatePropagation();
        fail(event.reason);
    });
    try {
        mergeLoaderConfig(message.config);
        runtime = await new HostBuilder().create();
        self.postMessage({ type: "ready", runtimeBuildInfo: runtime.runtimeBuildInfo,
            workerProxyCounts: runtime.INTERNAL.nativeBoundary.getProxyCounts() });
    } catch (error) {
        fail(error);
    }
}
