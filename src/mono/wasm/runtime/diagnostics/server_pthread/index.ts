// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/// <reference lib="webworker" />

import { pthread_self } from "../../pthreads/worker";
import { Module } from "../../imports";
import { controlCommandReceived } from "./event_pipe";
import { isDiagnosticMessage } from "../shared/types";
import { CharPtr } from "../../types/emscripten";
import { DiagnosticServer } from "./event_pipe";

import { mockScript } from "./mock-remote";
import PromiseController from "./promise-controller";

//function delay(ms: number): Promise<void> {
//    return new Promise(resolve => setTimeout(resolve, ms));
//}

function addOneShotMessageEventListener(src: EventTarget): Promise<MessageEvent<string | ArrayBuffer>> {
    return new Promise((resolve) => {
        const listener: (event: Event) => void = ((event: MessageEvent<string | ArrayBuffer>) => {
            src.removeEventListener("message", listener);
            resolve(event);
        }) as (event: Event) => void;
        src.addEventListener("message", listener);
    });
}

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
        }
    }

    private stopRequested = false;
    private stopRequestedController = new PromiseController<void>();

    stop(): void {
        this.stopRequested = true;
        this.stopRequestedController.resolve();
    }

    async serverLoop(this: DiagnosticServerImpl): Promise<void> {
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

    // async eventPipeSessionLoop(): Promise<void> {
    // await runtimeStarted();
    // const eventPipeFlushThread = await enableEventPipeSessionAndSignalResume();
    // while (!this.stopRequested) {
    //     const outcome = await oneOfStoppedOrMessageReceived(eventPipeFlushThread);
    //     if (outcome === "stopped") {
    //         break;
    //     } else {
    //         sendEPBufferToWebSocket(outcome);
    //     }
    // }
    // await closeWebSocket();
    // }

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
    queueMicrotask(() => {
        mockScript.run();
    });
    queueMicrotask(() => {
        server.serverLoop();
    });
}
