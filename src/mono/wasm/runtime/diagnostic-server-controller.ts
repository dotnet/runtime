// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DiagnosticOptions, EventPipeSessionID } from "./types";

interface ServerConfigureResult {
    serverStarted: boolean;
    serverReady?: Promise<void>;
}

async function configureServer(options: DiagnosticOptions): Promise<ServerConfigureResult> {
    if (options.waitForConnection) {
        // TODO: start the server and wait for a connection
        return { serverStarted: false, serverReady: Promise.resolve() };
    } else {
        // TODO: maybe still start the server if there's an option specified
        return { serverStarted: false };
    }
}

function postIPCStreamingSessionStarted(sessionID: EventPipeSessionID): void {
    // TODO: For IPC streaming sessions this is the place to send back an acknowledgement with the session ID
}

const serverController = {
    configureServer,
    postIPCStreamingSessionStarted,
};

export default serverController;
