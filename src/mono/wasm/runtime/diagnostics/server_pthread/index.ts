// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { pthread_self } from "../../pthreads/worker";
import { Module } from "../../imports";
import { isDiagnosticMessage } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import {
    DiagnosticServerControlCommand,
    makeDiagnosticServerControlReplyStartupResume
} from "../shared/controller-commands";

import { mockScript } from "./mock-remote";
import type { MockRemoteSocket } from "../mock";
import { PromiseController } from "../../promise-utils";

function addOneShotMessageEventListener(src: EventTarget): Promise<MessageEvent<string | ArrayBuffer>> {
    return new Promise((resolve) => {
        const listener = (event: Event) => { resolve(event as MessageEvent<string | ArrayBuffer>); };
        src.addEventListener("message", listener, { once: true });
    });
}

export interface DiagnosticServer {
    stop(): void;
}

interface ClientCommandBase {
    command_set: "EventPipe" | "Process";
    command: string;
}

interface EventPipeClientCommand extends ClientCommandBase {
    command_set: "EventPipe";
    command: "CollectTracing2" | "Stop";
    args: string;
}

interface ProcessClientCommand extends ClientCommandBase {
    command_set: "Process";
    command: "Resume";
}

type ClientCommand = EventPipeClientCommand | ProcessClientCommand;

class DiagnosticServerImpl implements DiagnosticServer {
    readonly websocketUrl: string;
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
        // TODO: mono_wasm_diagnostic_server_thread_attach ();
        this.attachToRuntimeController.resolve();
    }

    async serverLoop(this: DiagnosticServerImpl): Promise<void> {
        await this.startRequestedController.promise;
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
        const ws = mockScript.open();
        const p = addOneShotMessageEventListener(ws);
        ws.send("ADVR");
        const message = await p;
        const cmd = this.parseCommand(message);
        switch (cmd.command_set) {
            case "EventPipe":
                await this.dispatchEventPipeCommand(ws, cmd);
                break;
            case "Process":
                await this.dispatchProcessCommand(ws, cmd); // resume
                break;
            default:
                console.warn("Client sent unknown command", cmd);
                break;
        }
    }

    parseCommand(message: MessageEvent<string | ArrayBuffer>): ClientCommand {
        throw new Error("TODO");
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
    async dispatchEventPipeCommand(ws: WebSocket | MockRemoteSocket, cmd: EventPipeClientCommand): Promise<void> {
        switch (cmd.command) {
            case "CollectTracing2": {
                await this.attachToRuntimeController.promise; // can't start tracing until we've attached to the runtime
                const session = await createEventPipeStreamingSession(ws, cmd.args);
                this.postClientReply(ws, "OK", session.id);
                break;
            }
            case "Stop":
                await this.stopEventPipe(cmd.args);
                break;
            default:
                assertNever(cmd.command);
                break;
        }
    }

    // dispatch Process commands received from the diagnostic client
    async dispatchProcessCommand(ws: WebSocket | MockRemoteSocket, cmd: ProcessClientCommand): Promise<void> {
        switch (cmd.command) {
            case "Resume":
                pthread_self.postMessageToBrowser(makeDiagnosticServerControlReplyStartupResume());
                break;
            default:
                assertNever(cmd.command);
                break;
        }
    }
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

function assertNever(t: never): never {
    throw new Error("Unexpected unreachable result: " + t);
}
