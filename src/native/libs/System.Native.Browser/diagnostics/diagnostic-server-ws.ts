// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DiagnosticConnectionBase } from "./common";
import { dotnetBrowserUtilsExports, dotnetLogger, dotnetNativeBrowserExports } from "./cross-module";
import { IDiagnosticConnection } from "./types";

export function createDiagConnectionWs(socketHandle: number, url: string): IDiagnosticConnection {
    return new DiagnosticConnectionWS(socketHandle, url);
}

// this is used together with `dotnet-dsrouter` which will create IPC pipe on your local machine
// 1. run `dotnet-dsrouter server-websocket` this will print process ID and websocket URL
// 2. configure your wasm dotnet application `.withEnvironmentVariable("DOTNET_DiagnosticPorts", "ws://127.0.0.1:8088/diagnostics")`
// 3. run your wasm application
// 4. run `dotnet-gcdump -p <process ID>` or `dotnet-trace collect -p <process ID>`
class DiagnosticConnectionWS extends DiagnosticConnectionBase implements IDiagnosticConnection {
    private ws: WebSocket;

    constructor(clientSocket: number, url: string) {
        super(clientSocket);
        const ws = this.ws = new WebSocket(url);
        const onMessage = async (evt: MessageEvent<Blob>) => {
            const buffer = await evt.data.arrayBuffer();
            const message = new Uint8Array(buffer);
            this.messagesReceived.push(message);
            dotnetBrowserUtilsExports.runBackgroundTimers();
        };
        ws.addEventListener("open", () => {
            for (const data of this.messagesToSend) {
                ws.send(data as any);
            }
            this.messagesToSend = [];
            dotnetBrowserUtilsExports.runBackgroundTimers();
        }, { once: true });
        ws.addEventListener("message", onMessage);
        ws.addEventListener("error", () => {
            dotnetLogger.warn("Diagnostic server WebSocket connection was closed unexpectedly.");
            ws.removeEventListener("message", onMessage);
        }, { once: true });
    }

    send(message: Uint8Array): number {
        dotnetNativeBrowserExports.SystemJS_ScheduleDiagnosticServer();
        // copy the message
        if (this.ws!.readyState == WebSocket.CLOSED || this.ws!.readyState == WebSocket.CLOSING) {
            return -1;
        }
        if (this.ws!.readyState == WebSocket.CONNECTING) {
            return super.store(message);
        }

        this.ws!.send(message as any);

        return message.length;
    }

    close(): number {
        dotnetNativeBrowserExports.SystemJS_ScheduleDiagnosticServer();
        this.ws.close();
        return 0;
    }
}
