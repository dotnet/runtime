// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { VoidPtr } from "../types/emscripten";
import type { PromiseController } from "../types/internal";

import { runtimeHelpers } from "./globals";
import { loaderHelpers, Module } from "./globals";
import { mono_log_info } from "./logging";

let lastScheduledTimeoutId: any = undefined;

// run another cycle of the event loop, which is EP threads on MT runtime
export function diagnostic_server_loop () {
    lastScheduledTimeoutId = undefined;
    if (loaderHelpers.is_runtime_running()) {
        try {
            runtimeHelpers.mono_background_exec();// give GC chance to run
            runtimeHelpers.mono_wasm_ds_exec();
            schedule_diagnostic_server_loop(100);
        } catch (ex) {
            loaderHelpers.mono_exit(1, ex);
        }
    }
}

export function schedule_diagnostic_server_loop (delay = 0):void {
    if (!lastScheduledTimeoutId || delay === 0) {
        lastScheduledTimeoutId = Module.safeSetTimeout(diagnostic_server_loop, delay);
    }
}

export class DiagConnectionBase {
    protected messagesToSend: Uint8Array[] = [];
    protected messagesReceived: Uint8Array[] = [];
    constructor (public client_socket:number) {
    }

    store (message:Uint8Array):number {
        this.messagesToSend.push(message);
        return message.byteLength;
    }

    poll ():number {
        return this.messagesReceived.length;
    }

    recv (buffer:VoidPtr, bytes_to_read:number):number {
        if (this.messagesReceived.length === 0) {
            return 0;
        }
        const message = this.messagesReceived[0]!;
        const bytes_read = Math.min(message.length, bytes_to_read);
        Module.HEAPU8.set(message.subarray(0, bytes_read), buffer as any);
        if (bytes_read === message.length) {
            this.messagesReceived.shift();
        } else {
            this.messagesReceived[0] = message.subarray(bytes_read);
        }
        return bytes_read;
    }
}

export interface IDiagConnection {
    send (message: Uint8Array):number ;
    poll ():number ;
    recv (buffer:VoidPtr, bytes_to_read:number):number ;
    close ():number ;
}

// [hi,lo]
export type SessionId=[number, number];

export interface IDiagSession {
    session_id:SessionId;
    store(message: Uint8Array): number;
    sendCommand(message:Uint8Array):void;
}

export interface IDiagClient {
    skipDownload?:boolean;
    onClosePromise:PromiseController<Uint8Array[]>;
    commandOnAdvertise():Uint8Array;
    onSessionStart?(session:IDiagSession):void;
    onData?(session:IDiagSession, message:Uint8Array):void;
    onClose?(messages:Uint8Array[]):void;
    onError?(session:IDiagSession, message:Uint8Array):void;
}

export type fnClientProvider = (scenarioName:string) => IDiagClient;

export function downloadBlob (messages:Uint8Array[]) {
    const blob = new Blob(messages, { type: "application/octet-stream" });
    const blobUrl = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.download = "trace." + (new Date()).valueOf() + ".nettrace";
    mono_log_info(`Downloading trace ${link.download} - ${blob.size}  bytes`);
    link.href = blobUrl;
    document.body.appendChild(link);
    link.dispatchEvent(new MouseEvent("click", {
        bubbles: true, cancelable: true, view: window
    }));
}
