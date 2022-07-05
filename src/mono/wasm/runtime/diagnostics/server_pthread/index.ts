// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { pthread_self } from "../../pthreads/worker";
import { Module } from "../../imports";
import { isDiagnosticMessage } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import type {
    DiagnosticServerControlCommand,
    /*DiagnosticServerControlCommandStart, DiagnosticServerControlCommandSetSessionID*/
} from "../shared/controller-commands";

import { mockScript } from "./mock-remote";
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

class DiagnosticServerImpl implements DiagnosticServer {
    readonly websocketUrl: string;
    readonly ws: WebSocket | null;
    constructor(websocketUrl: string) {
        this.websocketUrl = websocketUrl;
        this.ws = null; // new WebSocket(this.websocketUrl);
        pthread_self.addEventListenerFromBrowser(this.onMessageFromMainThread.bind(this));
    }

    private startRequestedController = new PromiseController<void>();
    private stopRequested = false;
    private stopRequestedController = new PromiseController<void>();

    start(): void {
        console.log(`starting diagnostic server with url: ${this.websocketUrl}`);
        this.startRequestedController.resolve();
    }
    stop(): void {
        this.stopRequested = true;
        this.stopRequestedController.resolve();
    }

    async serverLoop(this: DiagnosticServerImpl): Promise<void> {
        await this.startRequestedController.promise;
        while (!this.stopRequested) {
            const firstPromise: Promise<["first", string] | ["second", undefined]> = this.advertiseAndWaitForClient().then((r) => ["first", r]);
            const secondPromise: Promise<["first", string] | ["second", undefined]> = this.stopRequestedController.promise.then(() => ["second", undefined]);
            const clientCommandState = await Promise.race([firstPromise, secondPromise]);
            // dispatchClientCommand(clientCommandState);
            if (clientCommandState[0] === "first") {
                console.debug("command received: ", clientCommandState[1]);
            } else if (clientCommandState[0] === "second") {
                console.debug("stop requested");
                break;
            }
        }
    }

    async advertiseAndWaitForClient(): Promise<string> {
        const sock = mockScript.open();
        const p = addOneShotMessageEventListener(sock);
        sock.send("ADVR");
        const message = await p;
        return message.data.toString();
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
            default:
                console.warn("Unknown control command: ", <any>cmd);
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
