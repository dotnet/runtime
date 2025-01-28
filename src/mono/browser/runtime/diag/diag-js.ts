// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnablePerfTracing from "consts:wasmEnablePerfTracing";

import { advert1, dotnet_IPC_V1, ServerCommandId, CommandSetId } from "./client-commands";
import { DiagConnectionBase, fnClientProvider, IDiagClient, IDiagConnection, IDiagServer, IDiagSession, schedule_diagnostic_server_loop, SessionId } from "./common";
import { mono_log_info } from "../logging";
import { GcDumpDiagClient } from "./dotnet-gcdump";
import { CountersClient } from "./dotnet-counters";
import { SampleProfilerClient } from "./dotnet-profiler";

let diagClient:IDiagClient|undefined = undefined as any;
let server:DiagServer = undefined as any;

// configure your application
// .withEnvironmentVariable("DOTNET_DiagnosticPorts", "download:gcdump")
// or implement function globalThis.dotnetDiagnosticClient with IDiagClient interface
export function createDiagConnectionJs (socket_handle:number, scenarioName:string):DiagConnectionJs {
    if (!WasmEnablePerfTracing) {
        return undefined as any;
    }
    if (diagClient === undefined) {
        diagClient = init_diag_client(scenarioName);
    }
    return new DiagConnectionJs(socket_handle);
}

function init_diag_client (scenarioName:string):IDiagClient {
    server = new DiagServer();
    if (scenarioName.startsWith("download:gcdump")) {
        return new GcDumpDiagClient();
    }
    if (scenarioName.startsWith("download:counters")) {
        return new CountersClient();
    }
    if (scenarioName.startsWith("download:samples")) {
        return new SampleProfilerClient();
    }
    const dotnetDiagnosticClient:fnClientProvider = (globalThis as any).dotnetDiagnosticClient;
    if (typeof dotnetDiagnosticClient === "function" ) {
        return dotnetDiagnosticClient(scenarioName);
    }
    throw new Error(`Unknown scenario: ${scenarioName}`);
}


// singleton wrapping the protocol with the diagnostic server in the Mono VM
// there could be multiple connection at the same time. Only the last which sent advert is receiving commands for all sessions
// DS:advert         ->1
//                     1<- DC1: command to start tracing session
// DS:OK, session ID ->1
// DS:advert         ->2
// DS:events         ->1
// DS:events         ->1
// DS:events         ->1
// DS:events         ->1
//                     2<- DC1: command to stop tracing session
// DS:close          ->1
class DiagServer implements IDiagServer {
    public commandConnection:DiagConnectionJs = undefined as any;
    sendCommand (message: Uint8Array): void {
        this.commandConnection.respond(message);
    }
    createSession (message: Uint8Array): void {
        this.commandConnection.diagClient = diagClient;
        this.commandConnection.respond(message);
    }
}

class DiagConnectionJs extends DiagConnectionBase implements IDiagConnection, IDiagSession {
    public session_id: SessionId = undefined as any;
    public diagClient?: IDiagClient;
    public stopDelayedAfterLastMessage:number|undefined = undefined;
    public resumedRuntime = false;

    constructor (public client_socket:number) {
        super(client_socket);
    }

    // this is message from the diagnostic server, which is Mono VM in this browser
    send (message:Uint8Array):number {
        schedule_diagnostic_server_loop();
        if (advert1.every((v, i) => v === message[i])) {
            // eslint-disable-next-line @typescript-eslint/no-this-alias
            server.commandConnection = this;
            diagClient?.onAdvertise(server);
        } else if (dotnet_IPC_V1.every((v, i) => v === message[i]) && message[16] == CommandSetId.Server) {
            if (message[17] == ServerCommandId.OK) {
                if (message.byteLength === 28) {
                    const view = message.subarray(20, 28);
                    const sessionIDLo = view[0] | (view[1] << 8) | (view[2] << 16) | (view[3] << 24);
                    const sessionIDHi = view[4] | (view[5] << 8) | (view[6] << 16) | (view[7] << 24);
                    const sessionId = [sessionIDHi, sessionIDLo] as SessionId;
                    this.session_id = sessionId;
                    diagClient?.onSessionStart(server, this);
                }
            } else {
                diagClient?.onError(server, this, message);
            }
        } else {
            if (this.diagClient)
                this.diagClient.onData(server, this, message);
            else {
                this.store(message);
            }
        }

        return message.length;
    }

    // this is message to the diagnostic server, which is Mono VM in this browser
    respond (message:Uint8Array) : void {
        this.messagesReceived.push(message);
        schedule_diagnostic_server_loop();
    }

    close (): number {
        if (this.messagesToSend.length === 0) {
            return 0;
        }
        const blob = new Blob(this.messagesToSend, { type: "application/octet-stream" });
        const blobUrl = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.download = "trace." + (new Date()).valueOf() + ".nettrace";
        mono_log_info(`Downloading trace ${link.download} - ${blob.size}  bytes`);
        link.href = blobUrl;
        document.body.appendChild(link);
        link.dispatchEvent(new MouseEvent("click", {
            bubbles: true, cancelable: true, view: window
        }));
        return 0;
    }
}
