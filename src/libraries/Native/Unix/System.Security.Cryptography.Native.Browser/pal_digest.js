// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var DotNetDigestLib = {
    $DOTNETDIGEST__postset: 'DOTNETDIGEST.pal_digest_intialize (Module);',
    $DOTNETDIGEST: {
        CRYPTO_ASM: "[System.Security.Cryptography.Algorithms]Interop/SubtleCrypto",
        pal_ctx_registry: [],
        pal_ref_counter: 0,
        pal_free_list: [],
        pal_digest_intialize: function (module) {
        },
        pal_digest_lazy_init: function () {
            if (this.digest_init)
                return;
            this.assembly_load = Module.cwrap ('mono_wasm_assembly_load', 'number', ['string']);
            this.find_class = Module.cwrap ('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
            this.find_method = Module.cwrap ('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);
            this.invoke_method = Module.cwrap ('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number'], { async: true });

            var crypto_digest_fqn_asm = this.CRYPTO_ASM.substring(this.CRYPTO_ASM.indexOf ("[") + 1, this.CRYPTO_ASM.indexOf ("]")).trim();
            var crypto_digest_fqn_class = this.CRYPTO_ASM.substring (this.CRYPTO_ASM.indexOf ("]") + 1).trim();

            this.crypto_module = this.assembly_load (crypto_digest_fqn_asm);
            if (!this.crypto_module)
                throw "Can't find bindings module assembly: " + crypto_digest_fqn_asm;

            if (crypto_digest_fqn_class !== null && typeof crypto_digest_fqn_class !== "undefined")
            {
                var namespace = "";
                var classname = crypto_digest_fqn_class.length > 0 ? crypto_digest_fqn_class : "Interop/SubtleCrypto";
            }

            var interop_class = this.find_class (this.crypto_module, namespace, classname)
            if (!interop_class)
                throw "Can't find " + crypto_digest_fqn_class + " class";

            var get_method = function(method_name) {
                var res = DOTNETDIGEST.find_method (interop_class, method_name, -1)
                if (!res)
                    throw "Can't find method " + namespace + "." + classname + ":" + method_name;
                return res;
            }
            this.hash_final_callback = get_method("HashFinalCallback");
            this.digest_init = true;
        },
        pal_register_ctx: function(obj) {

            var ctx_id = undefined;
            if (obj !== null && typeof obj !== "undefined")
            {
                ctx_id = this.pal_free_list.length ?
                            this.pal_free_list.pop() : this.pal_ref_counter++;
                this.pal_ctx_registry[ctx_id] = obj;
            }
            return ctx_id;
        },
        pal_require_ctx: function(ctx_id) {
            if (ctx_id >= 0)
                return this.pal_ctx_registry[ctx_id];
            return null;
        },
        pal_unregister_ctx: function(ctx_id) {
            var ctx = this.pal_ctx_registry[ctx_id];
            if (typeof ctx  !== "undefined" && ctx !== null) {
                this.pal_ctx_registry[ctx_id] = undefined;
                this.pal_free_list.push(ctx_id);
            }
            return ctx;
        },
        pal_reset_ctx: function(obj) {
            return DOTNETDIGEST.pal_register_ctx(obj);
        },
        pal_digest_callback : function (gc_handle, length) {
            DOTNETDIGEST.pal_digest_lazy_init();
            var result = BINDING.call_method(this.hash_final_callback, null, "io", [gc_handle, length]);
        }
    },
    dotnet_browser_digest : function (algo, digestLength) {

        //console.log("We are in dotnet_browser_digest javascript " + algo + " for length " + digestLength);
        // check that we have crypto available
        if (typeof crypto === 'object' && typeof crypto.subtle === 'object') {
            var cryptAlgo = 'UNKNOWN';
            switch(algo)
            {
            case 2:
                cryptAlgo = "SHA-1"
                break;
            case 3:
                cryptAlgo = "SHA-256"
                break;
            case 4:
                cryptAlgo = "SHA-384"
                break;
            case 5:
                cryptAlgo = "SHA-512"
                break;
            }
            return DOTNETDIGEST.pal_register_ctx(new Uint8Array());
        } else {
            return -1;
        }
    },
    dotnet_browser_digest_initialize : function () {

        //console.log("We are in dotnet_browser_digest_initialize javascript ");
        return DOTNETDIGEST.pal_reset_ctx(new Uint8Array());
    },
    dotnet_browser_digest_reset : function (ctxId) {

        //console.log("We are in dotnet_browser_reset javascript " + ctxId);
        var ctx = DOTNETDIGEST.pal_unregister_ctx(ctxId);
        if (typeof ctx  !== "undefined" && ctx !== null) {
            return 1;
        }
        return -1
    },
    dotnet_browser_digest_update : function (ctxId, pBuf, cbBuf, digestLength) {
        // console.log("We are in dotnet_browser_digest_update javascript " + ctxId + " for length " + digestLength );
        // console.log("pBuf " + pBuf + " pBuf length " + cbBuf);
        var data = DOTNETDIGEST.pal_require_ctx(ctxId);
        if (data) {
            var updatedArray = new Uint8Array(data.length + cbBuf);
            updatedArray.set(data);
            var heapBytes = new Uint8Array(Module.HEAPU8.buffer, pBuf, cbBuf);
            updatedArray.set(heapBytes, data.length);
            DOTNETDIGEST.pal_ctx_registry[ctxId] = updatedArray;
            return 1;
        }
        return -1;
    },
    dotnet_browser_digest_final : function (algo, ctxId, pOutput, cbOutput, digestLength, gc_handle) {
        // console.log("We are in dotnet_browser_digest_finalize javascript for: " + ctxId + " algo " + algo + " for length " + digestLength );
        // console.log("pOutput " + pOutput + " cbOutput length " + cbOutput);
        if (typeof crypto === 'object' && typeof crypto.subtle === 'object') {
            var cryptAlgo = 'UNKNOWN';
            switch(algo)
            {
            case 2:
                cryptAlgo = "SHA-1"
                break;
            case 3:
                cryptAlgo = "SHA-256"
                break;
            case 4:
                cryptAlgo = "SHA-384"
                break;
            case 5:
                cryptAlgo = "SHA-512"
                break;
            }

            const data = DOTNETDIGEST.pal_require_ctx(ctxId);

            crypto.subtle.digest(cryptAlgo, data).then(digestValue => {
                const hashArray = new Uint8Array(digestValue);     // convert ArrayBuffer buffer to byte array
                const heapBytes = new Uint8Array(Module.HEAPU8.buffer, pOutput, cbOutput); // Map an array to heap

                // Copy the bytes of the typed array to the heap.
                // This allows the hash to be accessable to the managed interface.
                heapBytes.set(hashArray);
                DOTNETDIGEST.pal_digest_callback(gc_handle, digestValue.byteLength);
                // Now we will reset the array
                DOTNETDIGEST.pal_reset_ctx(ctxId, new Uint8Array());
            });
            return 1;
        } else {
            return -1;
        }
    },
    dotnet_browser_digest_oneshot : function (algo, pBuf, cbBuf, pOutput, cbOutput, digestLength, gc_handle) {

        // console.log("We are in dotnet_browser_digest_oneshot javascript " + algo + " for length " + digestLength);
        // console.log("pBuf " + pBuf + " pBuf length " + cbBuf);
        // console.log("pOutput " + pOutput + " cbOutput length " + cbOutput);

        // check that we have crypto available
        if (typeof crypto === 'object' && typeof crypto.subtle === 'object') {
            var cryptAlgo = 'UNKNOWN';
            switch(algo)
            {
            case 2:
                cryptAlgo = "SHA-1"
                break;
            case 3:
                cryptAlgo = "SHA-256"
                break;
            case 4:
                cryptAlgo = "SHA-384"
                break;
            case 5:
                cryptAlgo = "SHA-512"
                break;
            }

            const data = new Uint8Array(Module.HEAPU8.buffer, pBuf, cbBuf);

            crypto.subtle.digest(cryptAlgo, data).then(digestValue => {
                const hashArray = new Uint8Array(digestValue);     // convert ArrayBuffer buffer to byte array
                const heapBytes = new Uint8Array(Module.HEAPU8.buffer, pOutput, cbOutput); // Map an array to heap

                // Copy the bytes of the typed array to the heap.
                // This allows the hash to be accessable to the managed interface.
                heapBytes.set(hashArray);
                DOTNETDIGEST.pal_digest_callback(gc_handle, digestValue.byteLength);
            });
            return 1;
        } else {
            return -1;
        }
    },
};

autoAddDeps(DotNetDigestLib, '$DOTNETDIGEST')
mergeInto(LibraryManager.library, DotNetDigestLib)
