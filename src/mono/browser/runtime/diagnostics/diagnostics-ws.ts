// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { IDiagnosticConnection, DiagnosticConnectionBase, diagnosticServerEventLoop, scheduleDiagnosticServerEventLoop } from "./common";
import { mono_log_warn } from "./logging";

export function createDiagConnectionWs (socket_handle:number, url:string):IDiagnosticConnection {
    return new DiagnosticConnectionWS(socket_handle, url);
}

// this is used together with `dotnet-dsrouter` which will create IPC pipe on your local machine
// 1. run `dotnet-dsrouter server-websocket` this will print process ID and websocket URL
// 2. configure your wasm dotnet application `.withEnvironmentVariable("DOTNET_DiagnosticPorts", "ws://127.0.0.1:8088/diagnostics")`
// 3. run your wasm application
// 4. run `dotnet-gcdump -p <process ID>` or `dotnet-trace collect -p <process ID>`
class DiagnosticConnectionWS extends DiagnosticConnectionBase implements IDiagnosticConnection {
    private ws: WebSocket;

    constructor (client_socket:number, url:string) {
        super(client_socket);
        const ws = this.ws = new WebSocket(url);
        const onMessage = async (evt:MessageEvent<Blob>) => {
            const buffer = await evt.data.arrayBuffer();
            const message = new Uint8Array(buffer);
            this.messagesReceived.push(message);
            diagnosticServerEventLoop();
        };
        ws.addEventListener("open", () => {
            for (const data of this.messagesToSend) {
                ws.send(data);
            }
            this.messagesToSend = [];
            diagnosticServerEventLoop();
        }, { once: true });
        ws.addEventListener("message", onMessage);
        ws.addEventListener("error", () => {
            mono_log_warn("Diagnostic server WebSocket connection was closed unexpectedly.");
            ws.removeEventListener("message", onMessage);
        }, { once: true });
    }

    send (message:Uint8Array):number {
        scheduleDiagnosticServerEventLoop();
        // copy the message
        if (this.ws!.readyState == WebSocket.CLOSED) {
            return -1;
        }
        if (this.ws!.readyState == WebSocket.CONNECTING) {
            return super.store(message);
        }

        this.ws!.send(message);

        return message.length;
    }

    close ():number {
        scheduleDiagnosticServerEventLoop();
        this.ws.close();
        return 0;
    }
}

