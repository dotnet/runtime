// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { VoidPtr } from "../../Common/JavaScript/types/emscripten";
import { dotnetApi, dotnetLogger } from "./cross-module";

export class DiagnosticConnectionBase {
    protected messagesToSend: Uint8Array[] = [];
    protected messagesReceived: Uint8Array[] = [];
    constructor(public clientSocket: number) {
    }

    store(message: Uint8Array): number {
        this.messagesToSend.push(message);
        return message.byteLength;
    }

    poll(): number {
        return this.messagesReceived.length;
    }

    recv(buffer: VoidPtr, bytesToRead: number): number {
        if (this.messagesReceived.length === 0) {
            return 0;
        }
        const message = this.messagesReceived[0]!;
        const bytesRead = Math.min(message.length, bytesToRead);
        const view = dotnetApi.localHeapViewU8();
        view.set(message.subarray(0, bytesRead), buffer as any >>> 0);
        if (bytesRead === message.length) {
            this.messagesReceived.shift();
        } else {
            this.messagesReceived[0] = message.subarray(bytesRead);
        }
        return bytesRead;
    }
}

export function downloadBlob(messages: Uint8Array[]) {
    if (!globalThis.document) return;

    const blob = new Blob(messages as BlobPart[], { type: "application/octet-stream" });
    const blobUrl = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.download = "trace." + (new Date()).valueOf() + ".nettrace";
    dotnetLogger.info(`Downloading trace ${link.download} - ${blob.size}  bytes`);
    link.href = blobUrl;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(blobUrl);
}
