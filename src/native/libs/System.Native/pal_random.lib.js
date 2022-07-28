// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const DotNetEntropyLib = {
    $DOTNETENTROPY: {
        // batchedQuotaMax is the max number of bytes as specified by the api spec.
        // If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
        // https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
        batchedQuotaMax: 65536,
        getBatchedRandomValues: function (buffer, bufferLength) {
            // Chrome doesn't want SharedArrayBuffer to be passed to crypto APIs
            const needTempBuf = typeof SharedArrayBuffer !== 'undefined' && Module.HEAPU8.buffer instanceof SharedArrayBuffer;
            // if we need a temporary buffer, make one that is big enough and write into it from the beginning
            // otherwise, use the wasm instance memory and write at the given 'buffer' pointer offset.
            const buf = needTempBuf ? new ArrayBuffer(bufferLength) : Module.HEAPU8.buffer;
            const offset = needTempBuf ? 0 : buffer;
            // for modern web browsers
            // map the work array to the memory buffer passed with the length
            for (let i = 0; i < bufferLength; i += this.batchedQuotaMax) {
                const view = new Uint8Array(buf, offset + i, Math.min(bufferLength - i, this.batchedQuotaMax));
                crypto.getRandomValues(view)
            }
            if (needTempBuf) {
                // copy data out of the temporary buffer into the wasm instance memory
                const heapView = new Uint8Array(Module.HEAPU8.buffer, buffer, bufferLength);
                heapView.set(new Uint8Array (buf));
            }
        }
    },
    dotnet_browser_entropy: function (buffer, bufferLength) {
        // check that we have crypto available
        if (typeof crypto === 'object' && typeof crypto['getRandomValues'] === 'function') {
            DOTNETENTROPY.getBatchedRandomValues(buffer, bufferLength)
            return 0;
        } else {
            // we couldn't find a proper implementation, as Math.random() is not suitable
            // instead of aborting here we will return and let managed code handle the message
            return -1;
        }
    },
};

autoAddDeps(DotNetEntropyLib, '$DOTNETENTROPY')
mergeInto(LibraryManager.library, DotNetEntropyLib)
