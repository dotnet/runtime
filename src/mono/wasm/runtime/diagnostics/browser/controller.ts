// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "../../cwraps";
import { withStackAlloc, getI32 } from "../../memory";
import { PromiseController } from "../../promise-utils";
import { Thread, waitForThread } from "../../pthreads/browser";
import { makeDiagnosticServerControlCommand } from "../shared/controller-commands";
import { isDiagnosticMessage } from "../shared/types";

/// An object that can be used to control the diagnostic server.
export interface ServerController {
    waitForStartupResume(): Promise<void>;
    postServerAttachToRuntime(): void;
}

class ServerControllerImpl implements ServerController {
    private readonly startupResumePromise: PromiseController<void> = new PromiseController();
    constructor(private server: Thread) {
        server.port.addEventListener("message", this.onServerReply.bind(this));
    }
    start(): void {
        console.debug("signaling the diagnostic server to start");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("start"));
    }
    stop(): void {
        console.debug("signaling the diagnostic server to stop");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("stop"));
    }
    async waitForStartupResume(): Promise<void> {
        await this.startupResumePromise.promise;
    }
    postServerAttachToRuntime(): void {
        console.debug("signal the diagnostic server to attach to the runtime");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("attach_to_runtime"));
    }

    onServerReply(event: MessageEvent): void {
        const d = event.data;
        if (isDiagnosticMessage(d)) {
            switch (d.cmd) {
                case "startup_resume":
                    console.debug("diagnostic server startup resume");
                    this.startupResumePromise.resolve();
                    break;
                default:
                    console.warn("Unknown control command: ", <any>d);
                    break;
            }
        }
    }
}

let serverController: ServerController | null = null;

export function getController(): ServerController {
    if (serverController)
        return serverController;
    throw new Error("unexpected no server controller");
}

export async function startDiagnosticServer(websocket_url: string): Promise<ServerController | null> {
    const sizeOfPthreadT = 4;
    console.debug(`starting the diagnostic server url: ${websocket_url}`);
    const result: number | undefined = withStackAlloc(sizeOfPthreadT, (pthreadIdPtr) => {
        if (!cwraps.mono_wasm_diagnostic_server_create_thread(websocket_url, pthreadIdPtr))
            return undefined;
        const pthreadId = getI32(pthreadIdPtr);
        return pthreadId;
    });
    if (result === undefined) {
        console.warn("diagnostic server failed to start");
        return null;
    }
    // have to wait until the message port is created
    const thread = await waitForThread(result);
    if (thread === undefined) {
        throw new Error("unexpected diagnostic server thread not found");
    }
    const serverControllerImpl = new ServerControllerImpl(thread);
    serverController = serverControllerImpl;
    serverControllerImpl.start();
    return serverControllerImpl;
}

