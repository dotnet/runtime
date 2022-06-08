// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticOptions, EventPipeSessionOptions, EventPipeSessionIPCOptions } from "./types";

interface ServerReadyResult {
    sessions?: (EventPipeSessionOptions & EventPipeSessionIPCOptions)[]; // provider configs
}

interface ServerConfigureResult {
    serverStarted: boolean;
    serverReady?: Promise<ServerReadyResult>;
}

async function configureServer(options: DiagnosticOptions): Promise<ServerConfigureResult> {
    if (options.server !== undefined && options.server) {
        // TODO start the server
        let serverReady: Promise<ServerReadyResult>;
        if (options.server == "wait") {
            //TODO: make a promise to wait for the connection
            serverReady = Promise.resolve({});
        } else {
            // server is ready now, no need to wait
            serverReady = Promise.resolve({});
        }
        // TODO: start the server and wait for a connection
        return { serverStarted: false, serverReady: serverReady };
    } else
        return { serverStarted: false };
}

function postIPCStreamingSessionStarted(/*sessionID: EventPipeSessionID*/): void {
    // TODO: For IPC streaming sessions this is the place to send back an acknowledgement with the session ID
}

const serverController = {
    configureServer,
    postIPCStreamingSessionStarted,
};

export default serverController;
