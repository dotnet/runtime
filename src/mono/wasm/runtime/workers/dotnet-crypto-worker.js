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
    var digest_type = "";
    switch(type) {
        case 0: digest_type = "SHA-1"; break;
        case 1: digest_type = "SHA-256"; break;
        case 2: digest_type = "SHA-384"; break;
        case 3: digest_type = "SHA-512"; break;
        default:
            throw "CRYPTO: Unknown digest: " + type;
    }

    // The 'crypto' API is not available in non-browser
    // environments (for example, v8 server).
    var digest = await crypto.subtle.digest(digest_type, data);
    return Array.from(new Uint8Array(digest));
}

// Operation to perform.
async function async_call(msg) {
    const req = JSON.parse(msg);

    if (req.func === "digest") {
        var digestArr = await call_digest(req.type, new Uint8Array(req.data));
        return JSON.stringify(digestArr);
    } else {
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
