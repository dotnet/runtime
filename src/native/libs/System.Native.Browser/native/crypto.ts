// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { netPublicApi, Logger, netBrowserHostExports } from "../cross-module";

export function SystemJS_RandomBytes(bufferPtr: number, bufferLength: number): number {
    // batchedQuotaMax is the max number of bytes as specified by the api spec.
    // If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
    // https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
    const batchedQuotaMax = 65536;

    if (!globalThis.crypto || !globalThis.crypto.getRandomValues) {
        if (!(globalThis as any)["cryptoWarnOnce"]) {
            Logger.warn("This engine doesn't support crypto.getRandomValues. Please use a modern version or provide polyfill for 'globalThis.crypto.getRandomValues'.");
            (globalThis as any)["cryptoWarnOnce"] = true;
        }
        return -1;
    }

    const memoryView = netPublicApi.localHeapViewU8();
    const targetView = memoryView.subarray(bufferPtr, bufferPtr + bufferLength);

    // When threading is enabled, Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
    const needsCopy = netBrowserHostExports.isSharedArrayBuffer(memoryView.buffer);
    const targetBuffer = needsCopy
        ? new Uint8Array(bufferLength)
        : targetView;

    // fill the targetBuffer in batches of batchedQuotaMax
    for (let i = 0; i < bufferLength; i += batchedQuotaMax) {
        const targetBatch = targetBuffer.subarray(i, i + Math.min(bufferLength - i, batchedQuotaMax));
        globalThis.crypto.getRandomValues(targetBatch);
    }

    if (needsCopy) {
        targetView.set(targetBuffer);
    }

    return 0;
}
