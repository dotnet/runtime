// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { pthread_self } from "../../pthreads/worker";
import { controlCommandReceived } from "./event_pipe";
import { isDiagnosticMessage } from "../shared/types";


class DiagnosticServer {
    readonly websocketUrl: string;
    readonly ws: WebSocket;
    constructor(websocketUrl: string) {
        this.websocketUrl = websocketUrl;
        this.ws = new WebSocket(this.websocketUrl);
    }

    start(): void {
        console.log("starting diagnostic server");

        if (pthread_self) {
            pthread_self.addEventListenerFromBrowser(this.onMessage.bind(this));
            pthread_self.postMessageToBrowser({
                "type": "diagnostic_server",
                "cmd": "started",
                "thread_id": pthread_self.pthread_id
            });
        }
    }

    onMessage(event: MessageEvent<unknown>): void {
        const d = event.data;
        if (d && isDiagnosticMessage(d)) {
            controlCommandReceived(d);
        }
    }
}


/// Called by the runtime  to initialize the diagnostic server workers
export function mono_wasm_diagnostic_server_start(websocketUrl: string): void {
    console.debug("mono_wasm_diagnostic_server_start");
    const server = new DiagnosticServer(websocketUrl);
    server.start();
}
