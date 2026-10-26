// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// -----------------------------------------------------------
// Worker-side runtime bootstrap for withWorker() mode.
//
// When dotnet.js detects ENVIRONMENT_IS_WEB_WORKER, it calls
// waitForInitMessage() to receive config from the main thread,
// then bootstrapWorkerRuntime() boots the runtime and enters
// the signaling event loop.
// -----------------------------------------------------------

import type { LoaderConfig, RuntimeAPI } from "./types";

import { dotnetNativeBrowserExports, Module } from "./cross-module";
import { mergeLoaderConfig } from "./config";
import { HostBuilder } from "./host-builder";
import {
    createSignalingBuffer,
    notifyMemoryGrowth,
    MemoryViewManager,
    OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_RESULT,
    OFFSET_COMMAND_PROMISE_ID,
    OFFSET_WORKER_ERROR_LENGTH,
    OFFSET_WORKER_STATE,
    OFFSET_WAKEUP_WORKER,
    OFFSET_FRAME_PTRS,
    FRAME_RING_CAPACITY,
    SignalState,
    CommandId,
    wakeMain,
    writeSharedError,
} from "./shared-signaling";
import type { WorkerModuleImportsMessage } from "./worker-imports";
import { createWorkerJSImportInvoker } from "./worker-imports";
import type { WorkerExportDescriptor, WorkerExportsRequest, WorkerExportsResponse } from "./worker-exports";
import { createWorkerJSExportDispatcher } from "./worker-exports";
import type { WorkerPromiseMessage } from "./worker-promises";
import { createWorkerPromises } from "./worker-promises";

// -----------------------------------------------------------
// Init message types (exchanged via postMessage during Phase A)
// -----------------------------------------------------------

export interface WorkerInitMessage {
    type: "init";
    config: LoaderConfig;
}

export interface WorkerReadyMessage {
    type: "ready";
    wasmMemory: WebAssembly.Memory;
    signalBuffer: SharedArrayBuffer;
    runtimeBuildInfo: RuntimeAPI["runtimeBuildInfo"];
}

export interface WorkerCommandMessage {
    type: "command";
    commandId: CommandId;
    promiseId: number;
    mainAssemblyName?: string;
    args?: string[];
    exitCode?: number;
    reason?: unknown;
}

export interface WorkerErrorMessage {
    type: "error";
    message: string;
    stack?: string;
}

export type WorkerToMainMessage = WorkerReadyMessage | WorkerErrorMessage | WorkerExportsResponse | WorkerPromiseMessage;

// -----------------------------------------------------------
// Wait for init message from main thread
//
// Returns a Promise that resolves when the main thread posts
// the "init" message with config.
// -----------------------------------------------------------

export function waitForInitMessage(): Promise<WorkerInitMessage> {
    return new Promise<WorkerInitMessage>((resolve) => {
        const handler = (ev: MessageEvent): void => {
            const data = ev.data;
            if (data && data.type === "init") {
                self.removeEventListener("message", handler);
                resolve(data as WorkerInitMessage);
            }
        };
        self.addEventListener("message", handler);
    });
}

// -----------------------------------------------------------
// Bootstrap the runtime on the worker
//
// Boots the full runtime, allocates
// signaling + frame buffers, hooks memory growth, posts
// "ready" back, then enters the signaling event loop.
// -----------------------------------------------------------

export async function bootstrapWorkerRuntime(initMsg: WorkerInitMessage): Promise<void> {
    const { config } = initMsg;

    try {
        // Apply config from main thread
        mergeLoaderConfig(config);

        // Boot the runtime through the normal HostBuilder path.
        const builder = new HostBuilder();
        const runtime: RuntimeAPI = await builder.create();


        const wasmMemory = dotnetNativeBrowserExports.getWasmMemory();
        if (!(wasmMemory.buffer instanceof SharedArrayBuffer)) {
            throw new Error("withWorker(): runtime WASM memory is not shared");
        }

        // Allocate the signaling buffer (separate small SharedArrayBuffer)
        const signalBuffer = createSignalingBuffer();
        const signalView = new Int32Array(signalBuffer);

        // Pre-allocate frame slots in WASM heap for main→worker arg frames
        const mallocFn = (Module as any)._malloc as (size: number) => number;
        if (mallocFn) {
            const FRAME_SLOT_SIZE = 256; // 8 args × 32 bytes each
            for (let i = 0; i < FRAME_RING_CAPACITY; i++) {
                const ptr = mallocFn(FRAME_SLOT_SIZE);
                Atomics.store(signalView, OFFSET_FRAME_PTRS + i, ptr);
            }
        }

        // Hook memory growth to increment memory_generation
        const originalGrow = wasmMemory.grow.bind(wasmMemory);
        wasmMemory.grow = (delta: number): number => {
            const result = originalGrow(delta);
            notifyMemoryGrowth(signalView);

            return result;
        };

        runWorkerEventLoop(signalView, runtime, new MemoryViewManager(wasmMemory, signalView));

        // Post "ready" back to main thread
        const readyMsg: WorkerReadyMessage = {
            type: "ready",
            wasmMemory,
            signalBuffer,
            runtimeBuildInfo: runtime.runtimeBuildInfo,
        };
        self.postMessage(readyMsg);

    } catch (err: any) {
        // Post error back to main thread
        const errorMsg: WorkerErrorMessage = {
            type: "error",
            message: err?.message ?? String(err),
            stack: err?.stack,
        };
        self.postMessage(errorMsg);
    }
}

// -----------------------------------------------------------
// Worker event loop
//
// Uses Atomics.waitAsync when no synchronous call is on stack
// (keeps the JS event loop alive for async managed code like
// runMainAndExit). Falls back to Atomics.wait only during
// synchronous JSExport processing where blocking is needed.
//
// Command messages carry arguments; JSExport ring writes notify
// OFFSET_WAKEUP_WORKER to wake the shared-memory loop.
// -----------------------------------------------------------

function runWorkerEventLoop(signalView: Int32Array, runtime: RuntimeAPI, memoryViews: MemoryViewManager): void {
    let closing = false;
    let commandRunning = false;
    const promises = createWorkerPromises();
    const processExports = createWorkerJSExportDispatcher(signalView, memoryViews, (handle, pointer) => {
        return runtime.INTERNAL.invokeWorkerJSExport(handle, pointer);
    }, message => {
        if (!closing) self.postMessage(message);
    }, reportFailure);
    function reportFailure(error: unknown): void {
        promises.stop(error instanceof Error ? error : new Error(String(error)));
        writeSharedError(signalView, OFFSET_WORKER_ERROR_LENGTH, error, "worker");
        Atomics.store(signalView, OFFSET_WORKER_STATE, SignalState.ERROR);
        wakeMain(signalView);
    }
    self.addEventListener("error", (event: ErrorEvent) => reportFailure(event.error ?? new Error(event.message)));
    function pumpExports(): void {
        try {
            processExports();
        } catch (error) {
            reportFailure(error);
            self.postMessage({ type: "error", message: error instanceof Error ? error.message : String(error) });
            throw error;
        }
    }

    self.addEventListener("message", (ev: MessageEvent<WorkerCommandMessage | WorkerModuleImportsMessage | WorkerExportsRequest | WorkerPromiseMessage>): void => {
        if (ev.data.type === "async-import" && !closing) {
            promises.complete(ev.data);
            return;
        }
        if (ev.data.type === "exports" && !closing) {
            const request = ev.data;
            void runtime.INTERNAL.getWorkerAssemblyExports(request.assemblyName).then(
                (exports: WorkerExportDescriptor[]) => self.postMessage({ type: "exports", requestId: request.requestId, exports }),
                (error: unknown) => self.postMessage({
                    type: "exports", requestId: request.requestId, error: error instanceof Error ? error.message : String(error),
                }),
            );
            return;
        }
        if (ev.data.type === "imports" && !closing) {
            for (const entry of ev.data.functions) {
                runtime.INTERNAL.registerWorkerJSImport(ev.data.moduleName, entry.name, createWorkerJSImportInvoker(signalView, memoryViews, entry.handle, pumpExports, promises));
            }
            return;
        }
        if (ev.data.type !== "command" || closing) return;
        const command = ev.data;
        if (commandRunning && command.commandId !== CommandId.EXIT) {
            const errorMsg: WorkerErrorMessage = { type: "error", message: "Another worker command is already running" };
            self.postMessage(errorMsg);
            return;
        }
        commandRunning = true;
        if (command.commandId === CommandId.EXIT) closing = true;
        void processCommand(command, runtime).then(result => {
            if (closing && command.commandId !== CommandId.EXIT) return;
            Atomics.store(signalView, OFFSET_COMMAND_RESULT, result);
            Atomics.store(signalView, OFFSET_COMMAND_PROMISE_ID, command.promiseId);
            Atomics.store(signalView, OFFSET_COMMAND_SIGNAL, SignalState.DONE);
            commandRunning = false;
            wakeMain(signalView);
            if (command.commandId !== CommandId.RUN_MAIN) {
                closing = true;
                promises.stop(new Error("withWorker(): worker has exited"));
                Atomics.store(signalView, OFFSET_WORKER_STATE, SignalState.DONE);
                self.close();
            }
        }, error => {
            if (closing && command.commandId !== CommandId.EXIT) return;
            closing = true;
            reportFailure(error);
            const errorMsg: WorkerErrorMessage = {
                type: "error",
                message: error instanceof Error ? error.message : String(error),
            };
            self.postMessage(errorMsg);
            self.close();
        });
    });

    function waitLoop(): void {
        if (closing) return;
        const lastSeen = Atomics.load(signalView, OFFSET_WAKEUP_WORKER);
        // Process anything already pending
        pumpExports();

        // Wait asynchronously for next wakeup — keeps event loop alive
        // so that async operations (runMainAndExit, Promises) can resolve.
        const result = Atomics.waitAsync(signalView, OFFSET_WAKEUP_WORKER, lastSeen);
        if (result.async) {
            result.value.then(() => waitLoop());
        } else {
            // "not-equal" — value already changed, process immediately
            setTimeout(waitLoop, 0);
        }
    }

    waitLoop();
}

async function processCommand(command: WorkerCommandMessage, runtime: RuntimeAPI): Promise<number> {
    switch (command.commandId) {
        case CommandId.RUN_MAIN:
            return await runtime.runMain(command.mainAssemblyName, command.args);
        case CommandId.RUN_MAIN_AND_EXIT:
            return await runtime.runMainAndExit(command.mainAssemblyName, command.args);
        case CommandId.EXIT: {
            const exitCode = command.exitCode ?? 0;
            try {
                runtime.exit(exitCode, command.reason);
            } catch (error) {
                if (!error || typeof error !== "object" || !("status" in error) || typeof error.status !== "number") {
                    throw error;
                }
                return error.status;
            }
            return exitCode;
        }
        default:
            throw new Error(`Unknown worker command ${command.commandId}`);
    }
}
