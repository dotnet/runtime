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
    OFFSET_COMMAND_SIGNAL,
    OFFSET_COMMAND_RESULT,
    OFFSET_COMMAND_PROMISE_ID,
    OFFSET_JSEXPORT_HEAD,
    OFFSET_JSEXPORT_TAIL,
    OFFSET_WAKEUP_WORKER,
    OFFSET_FRAME_PTRS,
    FRAME_RING_CAPACITY,
    SignalState,
    CommandId,
    jsexportEntryOffset,
    RING_ENTRY_SIGNAL,
    wakeMain,
} from "./shared-signaling";

// -----------------------------------------------------------
// Init message types (exchanged via postMessage during Phase A)
// -----------------------------------------------------------

export interface WorkerInitMessage {
    type: "init";
    config: LoaderConfig;
}

export interface WorkerReadyMessage {
    type: "ready";
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

export type WorkerToMainMessage = WorkerReadyMessage | WorkerErrorMessage;

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

        runWorkerEventLoop(signalView, runtime);

        // Post "ready" back to main thread
        const readyMsg: WorkerReadyMessage = {
            type: "ready",
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

function runWorkerEventLoop(signalView: Int32Array, runtime: RuntimeAPI): void {
    let closing = false;
    let commandRunning = false;

    self.addEventListener("message", (ev: MessageEvent<WorkerCommandMessage>): void => {
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
                self.close();
            }
        }, error => {
            if (closing && command.commandId !== CommandId.EXIT) return;
            closing = true;
            const errorMsg: WorkerErrorMessage = {
                type: "error",
                message: error instanceof Error ? error.message : String(error),
            };
            self.postMessage(errorMsg);
            self.close();
        });
    });

    function processAllPending(): boolean {
        let didWork = false;

        // Drain JSExport ring
        // eslint-disable-next-line no-constant-condition
        while (true) {
            const head = Atomics.load(signalView, OFFSET_JSEXPORT_HEAD);
            const tail = Atomics.load(signalView, OFFSET_JSEXPORT_TAIL);
            if (head === tail) break;
            processJSExportEntry(signalView, tail);
            didWork = true;
        }

        return didWork;
    }

    function waitLoop(): void {
        if (closing) return;
        const lastSeen = Atomics.load(signalView, OFFSET_WAKEUP_WORKER);
        // Process anything already pending
        processAllPending();

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

function processJSExportEntry(signalView: Int32Array, tailIndex: number): void {
    const entryBase = jsexportEntryOffset(tailIndex);

    // TODO Phase 4: Read method_handle and args_ptr from ring entry,
    // invoke C# method via the method handle with args at argsPtr.
    // For now, mark as done and advance tail
    Atomics.store(signalView, entryBase + RING_ENTRY_SIGNAL, SignalState.DONE);
    Atomics.notify(signalView, entryBase + RING_ENTRY_SIGNAL);
    Atomics.add(signalView, OFFSET_JSEXPORT_TAIL, 1);
    wakeMain(signalView);
}
