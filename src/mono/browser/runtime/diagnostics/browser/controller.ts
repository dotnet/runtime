// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { threads_c_functions as cwraps } from "../../cwraps";
import { INTERNAL, mono_assert } from "../../globals";
import { mono_log_info, mono_log_debug, mono_log_warn } from "../../logging";
import { withStackAlloc, getI32 } from "../../memory";
import { Thread, waitForThread } from "../../pthreads/browser";
import { isDiagnosticMessage, makeDiagnosticServerControlCommand } from "../shared/controller-commands";
import monoDiagnosticsMock from "consts:monoDiagnosticsMock";
import { PThreadPtr } from "../../pthreads/shared/types";

/// An object that can be used to control the diagnostic server.
export interface ServerController {
    postServerAttachToRuntime(): void;
}

class ServerControllerImpl implements ServerController {
    constructor(private server: Thread) {
        server.port.addEventListener("message", this.onServerReply.bind(this));
    }
    start(): void {
        mono_log_debug("signaling the diagnostic server to start");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("start"));
    }
    stop(): void {
        mono_log_debug("signaling the diagnostic server to stop");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("stop"));
    }
    postServerAttachToRuntime(): void {
        mono_log_debug("signal the diagnostic server to attach to the runtime");
        this.server.postMessageToWorker(makeDiagnosticServerControlCommand("attach_to_runtime"));
    }

    onServerReply(event: MessageEvent): void {
        const d = event.data;
        if (isDiagnosticMessage(d)) {
            switch (d.cmd) {
                default:
                    mono_log_warn("Unknown control reply command: ", <any>d);
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
    mono_assert(WasmEnableThreads, "The diagnostic server requires threads to be enabled during build time.");
    const sizeOfPthreadT = 4;
    mono_log_info(`starting the diagnostic server url: ${websocket_url}`);
    const result: PThreadPtr | undefined = withStackAlloc(sizeOfPthreadT, (pthreadIdPtr) => {
        if (!cwraps.mono_wasm_diagnostic_server_create_thread(websocket_url, pthreadIdPtr))
            return undefined;
        const pthreadId = getI32(pthreadIdPtr) as any as PThreadPtr;
        return pthreadId;
    });
    if (result === undefined) {
        mono_log_warn("diagnostic server failed to start");
        return null;
    }
    // have to wait until the message port is created
    const thread = await waitForThread(result);
    if (monoDiagnosticsMock) {
        INTERNAL.diagnosticServerThread = thread;
    }
    if (thread === undefined) {
        throw new Error("unexpected diagnostic server thread not found");
    }
    const serverControllerImpl = new ServerControllerImpl(thread);
    serverController = serverControllerImpl;
    serverControllerImpl.start();
    return serverControllerImpl;
}

