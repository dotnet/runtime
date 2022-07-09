// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { assertNever } from "../../types";
import { pthread_self } from "../../pthreads/worker";
import * as memory from "../../memory";
import { Module } from "../../imports";
import cwraps from "../../cwraps";
import { EventPipeSessionIDImpl, isDiagnosticMessage } from "../shared/types";
import { CharPtr, VoidPtr } from "../../types/emscripten";
import {
    DiagnosticServerControlCommand,
} from "../shared/controller-commands";

import { mockScript } from "./mock-remote";
import type { MockRemoteSocket } from "../mock";
import { PromiseController } from "../../promise-utils";
import { EventPipeSocketConnection, takeOverSocket } from "./event_pipe";
import { StreamQueue, allocateQueue } from "./stream-queue";
import {
    isEventPipeCommand,
    isProcessCommand,
    ProtocolClientCommandBase,
    EventPipeClientCommandBase,
    ProcessClientCommandBase,
    isEventPipeCommandCollectTracing2,
    isEventPipeCommandStopTracing,
    isProcessCommandResumeRuntime,
    EventPipeCommandCollectTracing2,
    EventPipeCollectTracingCommandProvider,
} from "./protocol-client-commands";
import parseMockCommand from "./mock-command-parser";

function addOneShotMessageEventListener(src: EventTarget): Promise<MessageEvent<string | ArrayBuffer>> {
    return new Promise((resolve) => {
        const listener = (event: Event) => { resolve(event as MessageEvent<string | ArrayBuffer>); };
        src.addEventListener("message", listener, { once: true });
    });
}

export interface DiagnosticServer {
    stop(): void;
}

class DiagnosticServerImpl implements DiagnosticServer {
    readonly websocketUrl: string;
    runtimeResumed = false;

    constructor(websocketUrl: string) {
        this.websocketUrl = websocketUrl;
        pthread_self.addEventListenerFromBrowser(this.onMessageFromMainThread.bind(this));
    }

    private startRequestedController = new PromiseController<void>();
    private stopRequested = false;
    private stopRequestedController = new PromiseController<void>();

    private attachToRuntimeController = new PromiseController<void>();

    start(): void {
        console.log(`starting diagnostic server with url: ${this.websocketUrl}`);
        this.startRequestedController.resolve();
    }
    stop(): void {
        this.stopRequested = true;
        this.stopRequestedController.resolve();
    }

    attachToRuntime(): void {
        cwraps.mono_wasm_diagnostic_server_thread_attach_to_runtime();
        this.attachToRuntimeController.resolve();
    }

    async serverLoop(this: DiagnosticServerImpl): Promise<void> {
        await this.startRequestedController.promise;
        await this.attachToRuntimeController.promise; // can't start tracing until we've attached to the runtime
        while (!this.stopRequested) {
            const p1: Promise<"first" | "second"> = this.advertiseAndWaitForClient().then(() => "first");
            const p2: Promise<"first" | "second"> = this.stopRequestedController.promise.then(() => "second");
            const result = await Promise.race([p1, p2]);
            switch (result) {
                case "first":
                    break;
                case "second":
                    console.debug("stop requested");
                    break;
                default:
                    assertNever(result);
            }
        }
    }


    async advertiseAndWaitForClient(): Promise<void> {
        try {
            const ws = mockScript.open();
            const p = addOneShotMessageEventListener(ws);
            ws.send("ADVR_V1");
            const message = await p;
            console.debug("received advertising response: ", message);
            const cmd = this.parseCommand(message);
            if (cmd === null) {
                console.error("unexpected message from client", message);
                return;
            } else if (isEventPipeCommand(cmd)) {
                await this.dispatchEventPipeCommand(ws, cmd);
            } else if (isProcessCommand(cmd)) {
                await this.dispatchProcessCommand(ws, cmd); // resume
            } else {
                console.warn("Client sent unknown command", cmd);
            }
        } finally {
            // if there were errors, resume the runtime anyway
            this.resumeRuntime();
        }
    }


    parseCommand(message: MessageEvent<string | ArrayBuffer>): ProtocolClientCommandBase | null {
        if (typeof message.data === "string") {
            return parseMockCommand(message.data);
        } else {
            console.debug("parsing byte command: ", message.data);
            throw new Error("TODO");
        }
    }

    onMessageFromMainThread(this: DiagnosticServerImpl, event: MessageEvent<unknown>): void {
        const d = event.data;
        if (d && isDiagnosticMessage(d)) {
            this.controlCommandReceived(d as DiagnosticServerControlCommand);
        }
    }

    /// dispatch commands received from the main thread
    controlCommandReceived(cmd: DiagnosticServerControlCommand): void {
        switch (cmd.cmd) {
            case "start":
                this.start();
                break;
            case "stop":
                this.stop();
                break;
            case "attach_to_runtime":
                this.attachToRuntime();
                break;
            default:
                console.warn("Unknown control command: ", <any>cmd);
                break;
        }
    }

    // dispatch EventPipe commands received from the diagnostic client
    async dispatchEventPipeCommand(ws: WebSocket | MockRemoteSocket, cmd: EventPipeClientCommandBase): Promise<void> {
        if (isEventPipeCommandCollectTracing2(cmd)) {
            const session = await createEventPipeStreamingSession(ws, cmd);
            this.postClientReply(ws, "OK", session.sessionID);
            console.debug("created session, now streaming: ", session);
            cwraps.mono_wasm_event_pipe_session_start_streaming(session.sessionID);
        } else if (isEventPipeCommandStopTracing(cmd)) {
            await this.stopEventPipe(cmd.sessionID);
        } else {
            console.warn("unknown EventPipe command: ", cmd);
        }
    }

    postClientReply(ws: WebSocket | MockRemoteSocket, status: "OK", rest?: string | number): void {
        ws.send(JSON.stringify([status, rest]));
    }

    async stopEventPipe(sessionID: EventPipeSessionIDImpl): Promise<void> {
        console.debug("stopEventPipe", sessionID);
        cwraps.mono_wasm_event_pipe_session_disable(sessionID);
    }

    // dispatch Process commands received from the diagnostic client
    async dispatchProcessCommand(ws: WebSocket | MockRemoteSocket, cmd: ProcessClientCommandBase): Promise<void> {
        if (isProcessCommandResumeRuntime(cmd)) {
            this.resumeRuntime();
        } else {
            console.warn("unknown Process command", cmd);
        }
    }

    resumeRuntime(): void {
        if (!this.runtimeResumed) {
            console.debug("resuming runtime startup");
            cwraps.mono_wasm_diagnostic_server_post_resume_runtime();
            this.runtimeResumed = true;
        }
    }
}

class EventPipeStreamingSession {

    constructor(readonly sessionID: EventPipeSessionIDImpl, readonly ws: WebSocket | MockRemoteSocket,
        readonly queue: StreamQueue, readonly connection: EventPipeSocketConnection) { }
}

async function createEventPipeStreamingSession(ws: WebSocket | MockRemoteSocket, cmd: EventPipeCommandCollectTracing2): Promise<EventPipeStreamingSession> {
    // First, create the native IPC stream and get its queue.
    const ipcStreamAddr = cwraps.mono_wasm_diagnostic_server_create_stream(); // FIXME: this should be a wrapped in a JS object so we can free it when we're done.
    const queueAddr = mono_wasm_diagnostic_server_get_stream_queue(ipcStreamAddr);
    // then take over the websocket connection
    const conn = takeOverSocket(ws);
    // and set up queue notifications
    const queue = allocateQueue(queueAddr, conn.write.bind(conn));
    // create the event pipe session
    const sessionID = createEventPipeStreamingSessionNative(ipcStreamAddr, cmd);
    if (sessionID === null)
        throw new Error("failed to create event pipe session");
    return new EventPipeStreamingSession(sessionID, ws, queue, conn);
}

function createEventPipeStreamingSessionNative(ipcStreamAddr: VoidPtr, options: EventPipeCommandCollectTracing2): EventPipeSessionIDImpl | null {

    const sizeOfInt32 = 4;

    const success = memory.withStackAlloc(sizeOfInt32, createStreamingSessionWithPtrCB, options, ipcStreamAddr);

    if (success === false)
        return null;
    const sessionID = success;

    return sessionID;
}

function createStreamingSessionWithPtrCB(sessionIdOutPtr: VoidPtr, options: EventPipeCommandCollectTracing2, ipcStreamAddr: VoidPtr): false | number {
    const providers = providersStringFromObject(options.providers);
    memory.setI32(sessionIdOutPtr, 0);
    if (!cwraps.mono_wasm_event_pipe_enable(null, ipcStreamAddr, options.circularBufferMB, providers, options.requestRundown, sessionIdOutPtr)) {
        return false;
    } else {
        return memory.getI32(sessionIdOutPtr);
    }
}

function providersStringFromObject(providers: EventPipeCollectTracingCommandProvider[]) {
    const providersString = providers.map(providerToString).join(",");
    return providersString;

    function providerToString(provider: EventPipeCollectTracingCommandProvider): string {
        const keyword_str = provider.keywords === 0 ? "" : provider.keywords.toString();
        const args_str = provider.filter_data === "" ? "" : ":" + provider.filter_data;
        return provider.provider_name + ":" + keyword_str + ":" + provider.logLevel + args_str;
    }
}

const IPC_STREAM_QUEUE_OFFSET = 4; /* keep in sync with mono_wasm_diagnostic_server_create_stream() in C */
function mono_wasm_diagnostic_server_get_stream_queue(streamAddr: VoidPtr): VoidPtr {
    // TODO: this can probably be in JS if we put the queue at a known address in the stream. (probably offset 4);
    return <any>streamAddr + IPC_STREAM_QUEUE_OFFSET;
}

/// Called by the runtime  to initialize the diagnostic server workers
export function mono_wasm_diagnostic_server_on_server_thread_created(websocketUrlPtr: CharPtr): void {
    const websocketUrl = Module.UTF8ToString(websocketUrlPtr);
    console.debug(`mono_wasm_diagnostic_server_on_server_thread_created, url ${websocketUrl}`);
    const server = new DiagnosticServerImpl(websocketUrl);
    queueMicrotask(() => {
        mockScript.run();
    });
    queueMicrotask(() => {
        server.serverLoop();
    });
}
