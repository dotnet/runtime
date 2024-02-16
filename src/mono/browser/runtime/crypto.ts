import { isSharedArrayBuffer, localHeapViewU8 } from "./memory";

// batchedQuotaMax is the max number of bytes as specified by the api spec.
// If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
// https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
const batchedQuotaMax = 65536;

export function mono_wasm_browser_entropy(bufferPtr: number, bufferLength: number): number {
    if (!globalThis.crypto || !globalThis.crypto.getRandomValues) {
        return -1;
    }

    const memoryView = localHeapViewU8();
    const targetView = memoryView.subarray(bufferPtr, bufferPtr + bufferLength);

    // When threading is enabled, Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
    const needsCopy = isSharedArrayBuffer(memoryView.buffer);
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
