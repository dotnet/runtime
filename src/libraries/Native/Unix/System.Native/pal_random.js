// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var DotNetEntropyLib = {
    $DOTNETENTROPY: {
    },
    dotnet_browser_entropy : function (buffer, bufferLength) {
        // check that we have crypto available
        if (typeof crypto === 'object' && typeof crypto['getRandomValues'] === 'function') {
            // for modern web browsers
            // map the work array to the memory buffer passed with the length
            var wrkArray = new Uint8Array(Module.HEAPU8.buffer, buffer, bufferLength);
            crypto.getRandomValues(wrkArray);
            return 0;
        } else {
            // we couldn't find a proper implementation, as Math.random() is not suitable
            abort("no cryptographic support found for getRandomValues. Consider polyfilling it if you want to use something insecure like Math.random(), e.g. put this in a --pre-js: var crypto = { getRandomValues: function(array) { for (var i = 0; i < array.length; i++) array[i] = (Math.random()*256)|0 } };");
        }
    },
};

autoAddDeps(DotNetEntropyLib, '$DOTNETENTROPY')
mergeInto(LibraryManager.library, DotNetEntropyLib)
