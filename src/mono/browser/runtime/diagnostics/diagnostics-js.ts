// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { advert1, CommandSetId, dotnet_IPC_V1, ServerCommandId } from "./client-commands";
import { DiagnosticConnectionBase, downloadBlob, fnClientProvider, IDiagnosticClient, IDiagnosticConnection, IDiagnosticSession, scheduleDiagnosticServerEventLoop, SessionId } from "./common";
import { PromiseAndController } from "../types/internal";
import { loaderHelpers } from "./globals";
import { mono_log_warn } from "./logging";
import { collectCpuSamples } from "./dotnet-cpu-profiler";
import { collectMetrics } from "./dotnet-counters";
import { collectGcDump } from "./dotnet-gcdump";

//let diagClient:IDiagClient|undefined = undefined as any;
//let server:DiagServer = undefined as any;

// configure your application
// .withEnvironmentVariable("DOTNET_DiagnosticPorts", "download:gcdump")
// or implement function globalThis.dotnetDiagnosticClient with IDiagClient interface

let nextJsClient:PromiseAndController<IDiagnosticClient>;
let fromScenarioNameOnce = false;

// Only the last which sent advert is receiving commands for all sessions
export let serverSession:DiagnosticSession|undefined = undefined;

// singleton wrapping the protocol with the diagnostic server in the Mono VM
// there could be multiple connection at the same time.
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

class DiagnosticSession extends DiagnosticConnectionBase implements IDiagnosticConnection, IDiagnosticSession {
    public session_id: SessionId = undefined as any;
    public diagClient?: IDiagnosticClient;
    public stopDelayedAfterLastMessage:number|undefined = undefined;
    public resumedRuntime = false;

    constructor (public client_socket:number) {
        super(client_socket);
    }

    sendCommand (message: Uint8Array): void {
        if (!serverSession) {
            mono_log_warn("no server yet");
            return;
        }
        serverSession.respond(message);
    }

    async connectNewClient () {
        this.diagClient = await nextJsClient.promise;
        cleanupClient();
        const firstCommand = this.diagClient.commandOnAdvertise();
        this.respond(firstCommand);
    }

    is_advert_message (message:Uint8Array):boolean {
        return advert1.every((v, i) => v === message[i]);
    }

    is_response_message (message:Uint8Array):boolean {
        return dotnet_IPC_V1.every((v, i) => v === message[i]) && message[16] == CommandSetId.Server;
    }

    is_response_ok_with_session (message:Uint8Array):boolean {
        return message.byteLength === 28 && message[17] == ServerCommandId.OK;
    }

    parse_session_id (message:Uint8Array):SessionId {
        const view = message.subarray(20, 28);
        const sessionIDLo = view[0] | (view[1] << 8) | (view[2] << 16) | (view[3] << 24);
        const sessionIDHi = view[4] | (view[5] << 8) | (view[6] << 16) | (view[7] << 24);
        return [sessionIDHi, sessionIDLo] as SessionId;
    }

    // this is message from the diagnostic server, which is Mono VM in this browser
    send (message:Uint8Array):number {
        scheduleDiagnosticServerEventLoop();
        if (this.is_advert_message(message)) {
            // eslint-disable-next-line @typescript-eslint/no-this-alias
            serverSession = this;
            this.connectNewClient();
        } else if (this.is_response_message(message)) {
            if (this.is_response_ok_with_session(message)) {
                this.session_id = this.parse_session_id(message);
                if (this.diagClient?.onSessionStart) {
                    this.diagClient.onSessionStart(this);
                }
            } else {
                if (this.diagClient?.onError) {
                    this.diagClient.onError(this, message);
                } else {
                    mono_log_warn("Diagnostic session " + this.session_id + " error : " + message.toString());
                }
            }
        } else {
            if (this.diagClient?.onData)
                this.diagClient.onData(this, message);
            else {
                this.store(message);
            }
        }

        return message.length;
    }

    // this is message to the diagnostic server, which is Mono VM in this browser
    respond (message:Uint8Array) : void {
        this.messagesReceived.push(message);
        scheduleDiagnosticServerEventLoop();
    }

    close (): number {
        if (this.diagClient?.onClose) {
            this.diagClient.onClose(this.messagesToSend);
        }
        if (this.diagClient?.onClosePromise) {
            this.diagClient.onClosePromise.resolve(this.messagesToSend);
        }
        if (this.messagesToSend.length === 0) {
            return 0;
        }
        if (this.diagClient && !this.diagClient.skipDownload) {
            downloadBlob(this.messagesToSend);
        }
        this.messagesToSend = [];
        return 0;
    }
}

export function cleanupClient () {
    nextJsClient = loaderHelpers.createPromiseController<IDiagnosticClient>();
}

export function setupJsClient (client:IDiagnosticClient) {
    if (nextJsClient.promise_control.isDone) {
        throw new Error("multiple clients in parallel are not allowed");
    }
    nextJsClient.promise_control.resolve(client);
}

export function createDiagConnectionJs (socket_handle:number, scenarioName:string):DiagnosticSession {
    if (!fromScenarioNameOnce) {
        fromScenarioNameOnce = true;
        if (scenarioName.startsWith("js://gcdump")) {
            collectGcDump({});
        }
        if (scenarioName.startsWith("js://counters")) {
            collectMetrics({});
        }
        if (scenarioName.startsWith("js://cpu-samples")) {
            collectCpuSamples({});
        }
        const dotnetDiagnosticClient:fnClientProvider = (globalThis as any).dotnetDiagnosticClient;
        if (typeof dotnetDiagnosticClient === "function" ) {
            nextJsClient.promise_control.resolve(dotnetDiagnosticClient(scenarioName));
        }
    }
    return new DiagnosticSession(socket_handle);
}
