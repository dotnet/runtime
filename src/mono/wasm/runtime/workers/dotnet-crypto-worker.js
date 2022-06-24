// @ts-check
/// <reference no-default-lib="true"/>
/// <reference lib="esnext" />
/// <reference lib="webworker" />
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var ChannelWorker = {
    _impl: class {
        // LOCK states
        get LOCK_UNLOCKED() { return 0; } // 0 means the lock is unlocked
        get LOCK_OWNED() { return 2; } // 2 means the ChannelWorker owns the lock

        // BEGIN ChannelOwner contract - shared constants.
        get STATE_IDX() { return 0; }
        get MSG_SIZE_IDX() { return 1; }
        get LOCK_IDX() { return 2; }

        // Communication states.
        get STATE_SHUTDOWN() { return -1; } // Shutdown
        get STATE_IDLE() { return 0; }
        get STATE_REQ() { return 1; }
        get STATE_RESP() { return 2; }
        get STATE_REQ_P() { return 3; } // Request has multiple parts
        get STATE_RESP_P() { return 4; } // Response has multiple parts
        get STATE_AWAIT() { return 5; } // Awaiting the next part
        // END ChannelOwner contract - shared constants.

        constructor(comm_buf, msg_buf, msg_char_len) {
            this.comm = new Int32Array(comm_buf);
            this.msg = new Uint16Array(msg_buf);
            this.msg_char_len = msg_char_len;
        }

        async await_request(async_call) {
            for (;;) {
                // Wait for signal to perform operation
                Atomics.wait(this.comm, this.STATE_IDX, this.STATE_IDLE);

                // Read in request
                var req = this._read_request();
                if (req === this.STATE_SHUTDOWN)
                    break;

                var resp = null;
                try {
                    // Perform async action based on request
                    resp = await async_call(req);
                }
                catch (err) {
                    console.log("Request error: " + err);
                    resp = JSON.stringify(err);
                }

                // Send response
                this._send_response(resp);
            }
        }

        _read_request() {
            var request = "";
            for (;;) {
                this._acquire_lock();

                // Get the current state and message size
                var state = Atomics.load(this.comm, this.STATE_IDX);
                var size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

                // Append the latest part of the message.
                request += this._read_from_msg(0, size_to_read);

                // The request is complete.
                if (state === this.STATE_REQ) {
                    this._release_lock();
                    break;
                }

                // Shutdown the worker.
                if (state === this.STATE_SHUTDOWN) {
                    this._release_lock();
                    return this.STATE_SHUTDOWN;
                }

                // Reset the size and transition to await state.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
                Atomics.store(this.comm, this.STATE_IDX, this.STATE_AWAIT);
                this._release_lock();

                Atomics.wait(this.comm, this.STATE_IDX, this.STATE_AWAIT);
            }

            return request;
        }

        _read_from_msg(begin, end) {
            return String.fromCharCode.apply(null, this.msg.slice(begin, end));
        }

        _send_response(msg) {
            if (Atomics.load(this.comm, this.STATE_IDX) !== this.STATE_REQ)
                throw "WORKER: Invalid sync communication channel state.";

            var state; // State machine variable
            const msg_len = msg.length;
            var msg_written = 0;

            for (;;) {
                this._acquire_lock();

                // Write the message and return how much was written.
                var wrote = this._write_to_msg(msg, msg_written, msg_len);
                msg_written += wrote;

                // Indicate how much was written to the this.msg buffer.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, wrote);

                // Indicate if this was the whole message or part of it.
                state = msg_written === msg_len ? this.STATE_RESP : this.STATE_RESP_P;

                // Update the state
                Atomics.store(this.comm, this.STATE_IDX, state);

                this._release_lock();

                // Wait for the transition to know the main thread has
                // received the response by moving onto a new state.
                Atomics.wait(this.comm, this.STATE_IDX, state);

                // Done sending response.
                if (state === this.STATE_RESP)
                    break;
            }
        }

        _write_to_msg(input, start, input_len) {
            var mi = 0;
            var ii = start;
            while (mi < this.msg_char_len && ii < input_len) {
                this.msg[mi] = input.charCodeAt(ii);
                ii++; // Next character
                mi++; // Next buffer index
            }
            return ii - start;
        }

        _acquire_lock() {
            while (Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_UNLOCKED, this.LOCK_OWNED) !== this.LOCK_UNLOCKED) {
                // empty
            }
        }

        _release_lock() {
            const result = Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_OWNED, this.LOCK_UNLOCKED);
            if (result !== this.LOCK_OWNED) {
                throw "CRYPTO: ChannelWorker tried to release a lock that wasn't acquired: " + result;
            }
        }
    },

    create: function (comm_buf, msg_buf, msg_char_len) {
        return new this._impl(comm_buf, msg_buf, msg_char_len);
    }
};

async function call_digest(type, data) {
    const digest_type = get_hash_name(type);

    // The 'crypto' API is not available in non-browser
    // environments (for example, v8 server).
    const digest = await crypto.subtle.digest(digest_type, data);
    return Array.from(new Uint8Array(digest));
}

async function sign(type, key, data) {
    const hash_name = get_hash_name(type);

    if (key.length === 0) {
        // crypto.subtle.importKey will raise an error for an empty key.
        // To prevent an error, reset it to a key with just a `0x00` byte. This is equivalent
        // since HMAC keys get zero-extended up to the block size of the algorithm.
        key = new Uint8Array([0]);
    }

    const cryptoKey = await crypto.subtle.importKey("raw", key, {name: "HMAC", hash: hash_name}, false /* extractable */, ["sign"]);
    const signResult = await crypto.subtle.sign("HMAC", cryptoKey, data);
    return Array.from(new Uint8Array(signResult));
}

function get_hash_name(type) {
    switch (type) {
        case 0: return "SHA-1";
        case 1: return "SHA-256";
        case 2: return "SHA-384";
        case 3: return "SHA-512";
        default:
            throw "CRYPTO: Unknown digest: " + type;
    }
}

const AesBlockSizeBytes = 16; // 128 bits

async function encrypt_decrypt(isEncrypting, key, iv, data) {
    const algorithmName = "AES-CBC";
    const keyUsage = isEncrypting ? ["encrypt"] : ["encrypt", "decrypt"];
    const cryptoKey = await importKey(key, algorithmName, keyUsage);
    const algorithm = {
        name: algorithmName,
        iv: new Uint8Array(iv)
    };

    const result = await (isEncrypting ?
        crypto.subtle.encrypt(
            algorithm,
            cryptoKey,
            new Uint8Array(data)) :
        decrypt(
            algorithm,
            cryptoKey,
            data));

    let resultByteArray = new Uint8Array(result);
    if (isEncrypting) {
        // trim off the last block, which is always a padding block.
        resultByteArray = resultByteArray.slice(0, resultByteArray.length - AesBlockSizeBytes);
    }
    return Array.from(resultByteArray);
}

async function decrypt(algorithm, cryptoKey, data) {
    // crypto.subtle AES-CBC will only allow a PaddingMode of PKCS7, but we need to use
    // PaddingMode None. To simulate this, we only decrypt full blocks of data, with an extra full
    // padding block of 0x10 (16) bytes appended to data. crypto.subtle will see that padding block and return
    // the fully decrypted message. To create the encrypted padding block, we encrypt the padding block
    // using the last block of the cipher text as the IV.

    const paddingBlockIV = new Uint8Array(data).slice(data.length - AesBlockSizeBytes);
    const paddingBlock = new Uint8Array([0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10]);
    const encryptedPaddingBlockResult = await crypto.subtle.encrypt(
        {
            name: algorithm.name,
            iv: paddingBlockIV
        },
        cryptoKey,
        paddingBlock
    );

    const encryptedPaddingBlock = new Uint8Array(encryptedPaddingBlockResult).slice(0, AesBlockSizeBytes);
    for (var i = 0; i < encryptedPaddingBlock.length; i++) {
        data.push(encryptedPaddingBlock[i]);
    }

    return await crypto.subtle.decrypt(
        algorithm,
        cryptoKey,
        new Uint8Array(data));
}

function importKey(key, algorithmName, keyUsage) {
    return crypto.subtle.importKey(
        "raw",
        new Uint8Array(key),
        {
            name: algorithmName
        },
        false /* extractable */,
        keyUsage);
}

// Operation to perform.
async function async_call(msg) {
    const req = JSON.parse(msg);

    if (req.func === "digest") {
        const digestArr = await call_digest(req.type, new Uint8Array(req.data));
        return JSON.stringify(digestArr);
    } 
    else if (req.func === "sign") {
        const signResult = await sign(req.type, new Uint8Array(req.key), new Uint8Array(req.data));
        return JSON.stringify(signResult);
    }
    else if (req.func === "encrypt_decrypt") {
        const signResult = await encrypt_decrypt(req.isEncrypting, req.key, req.iv, req.data);
        return JSON.stringify(signResult);
    }
    else {
        throw "CRYPTO: Unknown request: " + req.func;
    }
}

var s_channel;

// Initialize WebWorker
onmessage = function (p) {
    var data = p;
    if (p.data !== undefined) {
        data = p.data;
    }
    s_channel = ChannelWorker.create(data.comm_buf, data.msg_buf, data.msg_char_len);
    s_channel.await_request(async_call);
};
