// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import monoDiagnosticsMock from "consts:monoDiagnosticsMock";
import { PromiseAndController, assertNever } from "../../types/internal";
import { pthread_self } from "../../pthreads/worker";
import { createPromiseController } from "../../globals";
import cwraps from "../../cwraps";
import { EventPipeSessionIDImpl } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import {
    DiagnosticServerControlCommand,
    isDiagnosticMessage
} from "../shared/controller-commands";

import { importAndInstantiateMock } from "./mock-remote";
import type { Mock, MockRemoteSocket } from "../mock";
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
} from "./protocol-client-commands";
import { makeEventPipeStreamingSession } from "./streaming-session";
import { CommonSocket } from "./common-socket";
import {
    createProtocolSocket, dotnetDiagnosticsServerProtocolCommandEvent,
    ProtocolCommandEvent,
} from "./protocol-socket";
import {
    BinaryProtocolCommand,
    isBinaryProtocolCommand,
} from "./ipc-protocol/types";
import {
    parseBinaryProtocolCommand,
    ParseClientCommandResult,
} from "./ipc-protocol/parser";
import {
    createAdvertise,
    createBinaryCommandOKReply,
} from "./ipc-protocol/serializer";
import { mono_log_error, mono_log_info, mono_log_debug, mono_log_warn } from "../../logging";
import { utf8ToString } from "../../strings";

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
    readonly mocked: Promise<Mock> | undefined;
    runtimeResumed = false;

    constructor(websocketUrl: string, mockPromise?: Promise<Mock>) {
        this.websocketUrl = websocketUrl;
        pthread_self.addEventListenerFromBrowser(this.onMessageFromMainThread.bind(this));
        this.mocked = monoDiagnosticsMock ? mockPromise : undefined;
    }

    private startRequestedController = createPromiseController<void>().promise_control;
    private stopRequested = false;
    private stopRequestedController = createPromiseController<void>().promise_control;

    private attachToRuntimeController = createPromiseController<void>().promise_control;

    start(): void {
        mono_log_info(`starting diagnostic server with url: ${this.websocketUrl}`);
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
            mono_log_debug("diagnostic server: advertising and waiting for client");
            const p1: Promise<"first" | "second"> = this.advertiseAndWaitForClient().then(() => "first");
            const p2: Promise<"first" | "second"> = this.stopRequestedController.promise.then(() => "second");
            const result = await Promise.race([p1, p2]);
            switch (result) {
                case "first":
                    break;
                case "second":
                    mono_log_debug("stop requested");
                    break;
                default:
                    assertNever(result);
            }
        }
    }

    async openSocket(): Promise<CommonSocket> {
        if (monoDiagnosticsMock && this.mocked) {
            return (await this.mocked).open();
        } else {
            const sock = new WebSocket(this.websocketUrl);
            // TODO: add an "error" handler here - if we get readyState === 3, the connection failed.
            await addOneShotOpenEventListenr(sock);
            return sock;
        }
    }

    private openCount = 0;

    async advertiseAndWaitForClient(): Promise<void> {
        try {
            const connNum = this.openCount++;
            mono_log_debug("opening websocket and sending ADVR_V1", connNum);
            const ws = await this.openSocket();
            const p = addOneShotProtocolCommandEventListener(createProtocolSocket(ws));
            this.sendAdvertise(ws);
            const message = await p;
            mono_log_debug("received advertising response: ", message, connNum);
            queueMicrotask(() => this.parseAndDispatchMessage(ws, connNum, message));
        } finally {
            // if there were errors, resume the runtime anyway
            this.resumeRuntime();
        }
    }

    async parseAndDispatchMessage(ws: CommonSocket, connNum: number, message: ProtocolCommandEvent): Promise<void> {
        try {
            const cmd = this.parseCommand(message, connNum);
            if (cmd === null) {
                mono_log_error("unexpected message from client", message, connNum);
                return;
            } else if (isEventPipeCommand(cmd)) {
                await this.dispatchEventPipeCommand(ws, cmd);
            } else if (isProcessCommand(cmd)) {
                await this.dispatchProcessCommand(ws, cmd); // resume
            } else {
                mono_log_warn("MONO_WASM Client sent unknown command", cmd);
            }
        } finally {
            // if there were errors, resume the runtime anyway
            this.resumeRuntime();
        }
    }

    sendAdvertise(ws: CommonSocket) {
        /* FIXME: don't use const fake guid and fake process id. In dotnet-dsrouter the pid is used
         * as a dictionary key,so if we ever supprt multiple runtimes, this might need to change.
        */
        const guid = "C979E170-B538-475C-BCF1-B04A30DA1430";
        const processIdLo = 0;
        const processIdHi = 1234;
        const buf = createAdvertise(guid, [processIdLo, processIdHi]);
        ws.send(buf);
    }

    parseCommand(message: ProtocolCommandEvent, connNum: number): ProtocolClientCommandBase | null {
        mono_log_debug("parsing byte command: ", message.data, connNum);
        const result = parseProtocolCommand(message.data);
        mono_log_debug("parsed byte command: ", result, connNum);
        if (result.success) {
            return result.result;
        } else {
            mono_log_warn("failed to parse command: ", result.error, connNum);
            return null;
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
                mono_log_warn("Unknown control command: ", <any>cmd);
                break;
        }
    }

    // dispatch EventPipe commands received from the diagnostic client
    async dispatchEventPipeCommand(ws: CommonSocket, cmd: EventPipeClientCommandBase): Promise<void> {
        if (isEventPipeCommandCollectTracing2(cmd)) {
            await this.collectTracingEventPipe(ws, cmd);
        } else if (isEventPipeCommandStopTracing(cmd)) {
            await this.stopEventPipe(ws, cmd.sessionID);
        } else {
            mono_log_warn("unknown EventPipe command: ", cmd);
        }
    }

    postClientReplyOK(ws: CommonSocket, payload?: Uint8Array): void {
        // FIXME: send a binary response for non-mock sessions!
        ws.send(createBinaryCommandOKReply(payload));
    }

    async stopEventPipe(ws: WebSocket | MockRemoteSocket, sessionID: EventPipeSessionIDImpl): Promise<void> {
        mono_log_debug("stopEventPipe", sessionID);
        cwraps.mono_wasm_event_pipe_session_disable(sessionID);
        // we might send OK before the session is actually stopped since the websocket is async
        // but the client end should be robust to that.
        this.postClientReplyOK(ws);
    }

    async collectTracingEventPipe(ws: WebSocket | MockRemoteSocket, cmd: EventPipeCommandCollectTracing2): Promise<void> {
        const session = await makeEventPipeStreamingSession(ws, cmd);
        const sessionIDbuf = new Uint8Array(8); // 64 bit
        sessionIDbuf[0] = session.sessionID & 0xFF;
        sessionIDbuf[1] = (session.sessionID >> 8) & 0xFF;
        sessionIDbuf[2] = (session.sessionID >> 16) & 0xFF;
        sessionIDbuf[3] = (session.sessionID >> 24) & 0xFF;
        // sessionIDbuf[4..7] is 0 because all our session IDs are 32-bit
        this.postClientReplyOK(ws, sessionIDbuf);
        mono_log_debug("created session, now streaming: ", session);
        cwraps.mono_wasm_event_pipe_session_start_streaming(session.sessionID);
    }

    // dispatch Process commands received from the diagnostic client
    async dispatchProcessCommand(ws: WebSocket | MockRemoteSocket, cmd: ProcessClientCommandBase): Promise<void> {
        if (isProcessCommandResumeRuntime(cmd)) {
            this.processResumeRuntime(ws);
        } else {
            mono_log_warn("unknown Process command", cmd);
        }
    }

    processResumeRuntime(ws: WebSocket | MockRemoteSocket): void {
        this.postClientReplyOK(ws);
        this.resumeRuntime();
    }

    resumeRuntime(): void {
        if (!this.runtimeResumed) {
            mono_log_debug("resuming runtime startup");
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
    const websocketUrl = utf8ToString(websocketUrlPtr);
    mono_log_debug(`mono_wasm_diagnostic_server_on_server_thread_created, url ${websocketUrl}`);
    let mock: PromiseAndController<Mock> | undefined = undefined;
    if (monoDiagnosticsMock && websocketUrl.startsWith("mock:")) {
        mock = createPromiseController<Mock>();
        queueMicrotask(async () => {
            const m = await importAndInstantiateMock(websocketUrl);
            mock!.promise_control.resolve(m);
            m.run();
        });
    }
    const server = new DiagnosticServerImpl(websocketUrl, mock?.promise);
    queueMicrotask(() => {
        server.serverLoop();
    });
}
