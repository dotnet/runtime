// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

const CryptoWebWorkerLib = {
    $CRYPTOWEBWORKER: {
        call_digest: function (hash, input_buffer, input_len, output_buffer, output_len) {
            if (this.can_call_digest() !== 1) {
                return 0; // Not supported. Caller should have validated this first.
            }

            var msg = {
                func: "digest",
                type: hash,
                data: Array.from(Module.HEAPU8.subarray (input_buffer, input_buffer + input_len))
            };
            var response = globalThis.mono_wasm_crypto.channel.send_msg (JSON.stringify (msg));
            var digest = JSON.parse (response);
            if (digest.length > output_len) {
                console.info("call_digest: about to throw!");
                throw "DIGEST HASH: Digest length exceeds output length: " + digest.length + " > " + output_len;
            }

            Module.HEAPU8.set (digest, output_buffer);
            return 1;
        },
        can_call_digest: function () {
            console.log("can_call_digest WasmCrypto:", JSON.stringify(globalThis.mono_wasm_crypto));
            console.log("can_call_digest SharedArrayBuffer:", JSON.stringify(typeof SharedArrayBuffer));
            if (!!globalThis.mono_wasm_crypto?.channel || typeof SharedArrayBuffer === "undefined") {
                return 0; // Not supported
            }
            return 1;
        }
    },
    dotnet_browser_simple_digest_hash: function (hash, input_buffer, input_len, output_buffer, output_len) {
        return CRYPTOWEBWORKER.call_digest (hash, input_buffer, input_len, output_buffer, output_len);
    },
    dotnet_browser_can_use_simple_digest_hash: function () {
        return CRYPTOWEBWORKER.can_call_digest ();
    },
};

autoAddDeps(CryptoWebWorkerLib, '$CRYPTOWEBWORKER')
mergeInto(LibraryManager.library, CryptoWebWorkerLib)
