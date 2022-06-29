// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { pthread_self } from "../../pthreads/worker";
import { Module } from "../../imports";
import { controlCommandReceived } from "./event_pipe";
import { isDiagnosticMessage } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import { DiagnosticServer } from "./event_pipe";

class DiagnosticServerImpl implements DiagnosticServer {
    readonly websocketUrl: string;
    readonly ws: WebSocket | null;
    constructor(websocketUrl: string) {
        this.websocketUrl = websocketUrl;
        this.ws = null; // new WebSocket(this.websocketUrl);
    }

    start(): void {
        console.log(`starting diagnostic server with url: ${this.websocketUrl}`);
        // XXX FIXME: we started before the runtime is ready, so we don't get a port because it gets created on attach.

        if (pthread_self) {
            pthread_self.addEventListenerFromBrowser(this.onMessage.bind(this));
            pthread_self.postMessageToBrowser({
                "type": "diagnostic_server",
                "cmd": "started",
                "thread_id": pthread_self.pthread_id
            });
            this.installTimeoutHandler();
        }
    }

    private stopRequested = false;

    stop(): void {
        this.stopRequested = true;
    }

    private installTimeoutHandler(): void {
        if (!this.stopRequested) {
            setTimeout(this.timeoutHandler.bind(this), 500);
        }
    }

    private timeoutHandler(this: DiagnosticServerImpl): void {
        console.debug("ping from diagnostic server");
        this.installTimeoutHandler();
    }

    onMessage(this: DiagnosticServerImpl, event: MessageEvent<unknown>): void {
        const d = event.data;
        if (d && isDiagnosticMessage(d)) {
            controlCommandReceived(this, d);
        }
    }
}


/// Called by the runtime  to initialize the diagnostic server workers
export function mono_wasm_diagnostic_server_on_server_thread_created(websocketUrlPtr: CharPtr): void {
    const websocketUrl = Module.UTF8ToString(websocketUrlPtr);
    console.debug(`mono_wasm_diagnostic_server_on_server_thread_created, url ${websocketUrl}`);
    const server = new DiagnosticServerImpl(websocketUrl);
    server.start();
}
