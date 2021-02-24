// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var DotNetEntropyLib = {
    $DOTNETENTROPY: {
        // batchedQuotaMax is the max number of bytes as specified by the api spec.
        // If the byteLength of array is greater than 65536, throw a QuotaExceededError and terminate the algorithm.
        // https://www.w3.org/TR/WebCryptoAPI/#Crypto-method-getRandomValues
        batchedQuotaMax: 65536,
        getBatchedRandomValues: function (buffer, bufferLength) {
            // for modern web browsers
            // map the work array to the memory buffer passed with the length
            var wrkArray = new Uint8Array(Module.HEAPU8.buffer, buffer, bufferLength);
            while (bufferLength > 0) {
                var sliceEm = bufferLength % this.batchedQuotaMax;
                sliceEm = sliceEm > 0 ? sliceEm : this.batchedQuotaMax;
                var diceEmSegment = new Uint8Array(sliceEm);
                crypto.getRandomValues(diceEmSegment);
                bufferLength -= sliceEm;
                wrkArray.set(diceEmSegment, bufferLength);
            }
        }
    },
    dotnet_browser_entropy : function (buffer, bufferLength) {
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
