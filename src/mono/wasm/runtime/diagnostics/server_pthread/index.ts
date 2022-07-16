// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { assertNever, mono_assert } from "../../types";
import { pthread_self } from "../../pthreads/worker";
import { Module } from "../../imports";
import cwraps from "../../cwraps";
import { EventPipeSessionIDImpl, isDiagnosticMessage } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import {
    DiagnosticServerControlCommand,
} from "../shared/controller-commands";

import { mockScript } from "./mock-remote";
import type { MockRemoteSocket } from "../mock";
import { createPromiseController } from "../../promise-controller";
import {
    isEventPipeCommand,
    isProcessCommand,
    ProtocolClientCommandBase,
    EventPipeClientCommandBase,
    ProcessClientCommandBase,
    isEventPipeCommandCollectTracing2,
    isEventPipeCommandStopTracing,
    isProcessCommandResumeRuntime,
} from "./protocol-client-commands";
import { makeEventPipeStreamingSession } from "./streaming-session";
import parseMockCommand from "./mock-command-parser";
import { CommonSocket } from "./common-socket";
import {
    createProtocolSocket, dotnetDiagnosticsServerProtocolCommandEvent,
    BinaryProtocolCommand,
    ProtocolCommandEvent,
    isBinaryProtocolCommand,
    parseBinaryProtocolCommand,
    ParseClientCommandResult,
} from "./protocol-socket";

function addOneShotMessageEventListener(src: EventTarget): Promise<MessageEvent<string | ArrayBuffer>> {
    return new Promise((resolve) => {
        const listener = (event: Event) => { resolve(event as MessageEvent<string | ArrayBuffer>); };
        src.addEventListener("message", listener, { once: true });
    });
}

function addOneShotProtocolCommandEventListener(src: EventTarget): Promise<ProtocolCommandEvent> {
    return new Promise((resolve) => {
        const listener = (event: Event) => { resolve(event as ProtocolCommandEvent); };
        src.addEventListener(dotnetDiagnosticsServerProtocolCommandEvent, listener, { once: true });
    });
}

function addOneShotOpenEventListenr(src: EventTarget): Promise<Event> {
    return new Promise((resolve) => {
        const listener = (event: Event) => { resolve(event); };
        src.addEventListener("open", listener, { once: true });
    });
}

export interface DiagnosticServer {
    stop(): void;
}

class DiagnosticServerImpl implements DiagnosticServer {
    readonly websocketUrl: string;
    readonly mocked: boolean;
    runtimeResumed = false;

    constructor(websocketUrl: string) {
        this.websocketUrl = websocketUrl;
        pthread_self.addEventListenerFromBrowser(this.onMessageFromMainThread.bind(this));
        this.mocked = websocketUrl.startsWith("mock:");
    }

    private startRequestedController = createPromiseController<void>().promise_control;
    private stopRequested = false;
    private stopRequestedController = createPromiseController<void>().promise_control;

    private attachToRuntimeController = createPromiseController<void>().promise_control;

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
            console.debug("diagnostic server: advertising and waiting for client");
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

    async openSocket(): Promise<CommonSocket> {
        if (this.mocked) {
            return mockScript.open();
        } else {
            const sock = new WebSocket(this.websocketUrl);
            await addOneShotOpenEventListenr(sock);
            return sock;
        }
    }

    async advertiseAndWaitForClient(): Promise<void> {
        try {
            const ws = await this.openSocket();
            let p: Promise<MessageEvent<string | ArrayBuffer>> | Promise<ProtocolCommandEvent>;
            if (this.mocked) {
                p = addOneShotMessageEventListener(ws);
            } else {
                p = addOneShotProtocolCommandEventListener(createProtocolSocket(ws));
            }
            this.sendAdvertise(ws);
            const message = await p;
            console.debug("received advertising response: ", message);
            queueMicrotask(() => this.parseAndDispatchMessage(ws, message));
        } finally {
            // if there were errors, resume the runtime anyway
            this.resumeRuntime();
        }
    }

    async parseAndDispatchMessage(ws: CommonSocket, message: MessageEvent<string | ArrayBuffer> | ProtocolCommandEvent): Promise<void> {
        try {
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

    sendAdvertise(ws: CommonSocket) {
        const BUF_LENGTH = 34;
        const buf = new ArrayBuffer(BUF_LENGTH);
        const view = new Uint8Array(buf);
        let pos = 0;
        const text = "ADVR_V1";
        for (let i = 0; i < text.length; i++) {
            view[pos++] = text.charCodeAt(i);
        }
        view[pos++] = 0; // nul terminator
        const guid = "C979E170-B538-475C-BCF1-B04A30DA1430";
        guid.split("-").forEach((part) => {
            // FIXME: I'm sure the endianness is wrong here
            for (let i = 0; i < part.length; i += 2) {
                const idx = part.length - i - 2; // go through the pieces backwards
                view[pos++] = parseInt(part.substring(idx, idx + 2), 16);
            }
        });
        // "process ID" in 2 32-bit parts
        const pid = [0, 1234]; // hi, lo
        for (let i = 0; i < pid.length; i++) {
            const j = pid[pid.length - i - 1]; //lo, hi
            view[pos++] = j & 0xFF;
            view[pos++] = (j >> 8) & 0xFF;
            view[pos++] = (j >> 16) & 0xFF;
            view[pos++] = (j >> 24) & 0xFF;
        }
        view[pos++] = 0;
        view[pos++] = 0; // two reserved zero bytes
        mono_assert(pos == BUF_LENGTH, "did not format ADVR_V1 correctly");
        ws.send(buf);

    }

    parseCommand(message: MessageEvent<string | ArrayBuffer> | ProtocolCommandEvent): ProtocolClientCommandBase | null {
        if (typeof message.data === "string") {
            return parseMockCommand(message.data);
        } else {
            console.debug("parsing byte command: ", message.data);
            const result = parseProtocolCommand(message.data);
            if (result.success) {
                return result.result;
            } else {
                console.warn("failed to parse command: ", result.error);
                return null;
            }
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
            const session = await makeEventPipeStreamingSession(ws, cmd);
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

function parseProtocolCommand(data: ArrayBuffer | BinaryProtocolCommand): ParseClientCommandResult<ProtocolClientCommandBase> {
    if (isBinaryProtocolCommand(data)) {
        return parseBinaryProtocolCommand(data);
    } else {
        throw new Error("binary blob from mock is not implemented");
    }
}

/// Called by the runtime  to initialize the diagnostic server workers
export function mono_wasm_diagnostic_server_on_server_thread_created(websocketUrlPtr: CharPtr): void {
    const websocketUrl = Module.UTF8ToString(websocketUrlPtr);
    console.debug(`mono_wasm_diagnostic_server_on_server_thread_created, url ${websocketUrl}`);
    const server = new DiagnosticServerImpl(websocketUrl);
    if (websocketUrl.startsWith("mock:")) {
        queueMicrotask(() => {
            mockScript.run();
        });
    }
    queueMicrotask(() => {
        server.serverLoop();
    });
}
