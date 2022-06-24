// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { EventPipeSessionDiagnosticServerID } from "../../types";
import cwraps from "../../cwraps";
import { withStackAlloc, getI32 } from "../../memory";
import { getThread, Thread } from "../../pthreads/browser";

// interface ServerReadyResult {
//     sessions?: (EventPipeSessionOptions & EventPipeSessionIPCOptions)[]; // provider configs
// }

// interface ServerConfigureResult {
//     serverStarted: boolean;
//     serverReady?: Promise<ServerReadyResult>;
// }

// async function configureServer(options: DiagnosticOptions): Promise<ServerConfigureResult> {
//     if (options.server !== undefined && options.server) {
//         // TODO start the server
//         let serverReady: Promise<ServerReadyResult>;
//         if (options.server == "wait") {
//             //TODO: make a promise to wait for the connection
//             serverReady = Promise.resolve({});
//         } else {
//             // server is ready now, no need to wait
//             serverReady = Promise.resolve({});
//         }
//         // TODO: start the server and wait for a connection
//         return { serverStarted: false, serverReady: serverReady };
//     } else
//         return { serverStarted: false };
// }

// function postIPCStreamingSessionStarted(/*diagnosticSessionID: EventPipeSessionDiagnosticServerID, sessionID: EventPipeSessionIDImpl*/): void {
//     // TODO: For IPC streaming sessions this is the place to send back an acknowledgement with the session ID
// }

/// An object that can be used to control the diagnostic server.
export interface ServerController {
    wait_for_resume(): Promise<{ sessions: EventPipeSessionDiagnosticServerID[] }>;
    post_diagnostic_server_attach_to_runtime(): void;
    // configureServer(options: DiagnosticOptions): Promise<ServerConfigureResult>;
    // postIPCStreamingSessionStarted(diagnosticSessionID: EventPipeSessionDiagnosticServerID, sessionID: EventPipeSessionIDImpl): void;
}

class ServerControllerImpl implements ServerController {
    constructor(private server: Thread) { }
    async wait_for_resume(): Promise<{ sessions: EventPipeSessionDiagnosticServerID[] }> {
        console.debug("waiting for the diagnostic server to allow us to resume");
        const promise = new Promise<void>((resolve, /*reject*/) => {
            setTimeout(() => { resolve(); }, 1000);
        });
        await promise;
        // let req = this.server.allocateRequest({ type: "diagnostic_server", cmd: "wait_for_resume" });
        // let respone = await this.server.sendAndWait(req);
        // if (respone.type !== "diagnostic_server" || respone.cmd !== "wait_for_resume_response") {
        //     throw new Error("unexpected response");
        // }
        return { sessions: [] };
    }
    post_diagnostic_server_attach_to_runtime(): void {
        console.debug("signal the diagnostic server to attach to the runtime");
    }
}


let serverController: ServerController | null = null;

export function getController(): ServerController {
    if (serverController)
        return serverController;
    throw new Error("unexpected no server controller");
}

export function startDiagnosticServer(websocket_url: string): ServerController | null {
    const sizeOfPthreadT = 4;
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
    const thread = getThread(result);
    if (thread === undefined) {
        throw new Error("unexpected diagnostic server thread not found");
    }
    serverController = new ServerControllerImpl(thread);
    return serverController;
}

