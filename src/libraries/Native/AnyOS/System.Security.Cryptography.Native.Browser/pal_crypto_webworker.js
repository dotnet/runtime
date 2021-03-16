// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var CryptoWebWorkerLib = {
    $CRYPTOWEBWORKER: {
        call_sha_hash: function (sha_hash, input_buffer, input_len, output_buffer, output_len) {
            var msg = {
                func: "sha",
                type: sha_hash,
                data: Array.from(Module.HEAPU8.subarray (input_buffer, input_buffer + input_len))
            };
            var response = MONO.mono_wasm_crypto.channel.send_msg (JSON.stringify (msg));
            var digest = JSON.parse (response);
            if (digest.length > output_len) {
                throw "SHA HASH: Digest length exceeds output length: " + digest.length + " > " + output_len;
            }

            Module.HEAPU8.set (digest, output_buffer);
            return 1;
        }
    },
    dotnet_browser_sha_hash: function (sha_hash, input_buffer, input_len, output_buffer, output_len) {
        return CRYPTOWEBWORKER.call_sha_hash (sha_hash, input_buffer, input_len, output_buffer, output_len);
    },
};

autoAddDeps(CryptoWebWorkerLib, '$CRYPTOWEBWORKER')
mergeInto(LibraryManager.library, CryptoWebWorkerLib)
