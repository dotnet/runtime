// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

class ChannelWorker {
    private comm: Int32Array;
    private msg: Uint16Array;
    private msg_char_len: number;

    // BEGIN ChannelOwner contract - shared constants.
    private get STATE_IDX() { return 0; }
    private get MSG_SIZE_IDX() { return 1; }

    // Communication states.
    private get STATE_SHUTDOWN() { return -1; } // Shutdown
    private get STATE_IDLE() { return 0; }
    private get STATE_REQ() { return 1; }
    private get STATE_RESP() { return 2; }
    private get STATE_REQ_P() { return 3; } // Request has multiple parts
    private get STATE_RESP_P() { return 4; } // Response has multiple parts
    private get STATE_AWAIT() { return 5; } // Awaiting the next part
    // END ChannelOwner contract - shared constants.

    private constructor(comm_buf: number[], msg_buf: number[], msg_char_len: number) {
        this.comm = new Int32Array(comm_buf);
        this.msg = new Uint16Array(msg_buf);
        this.msg_char_len = msg_char_len;
    }

    public async await_request(async_call: Function) {
        for (;;) {
            // Wait for signal to perform operation
            Atomics.wait(this.comm, this.STATE_IDX, this.STATE_IDLE);

            // Read in request
            const req = this.read_request();
            if (req === this.STATE_SHUTDOWN) {
                break;
            }

            let resp = null;
            try {
                // Perform async action based on request
                resp = await async_call(req);
            }
            catch (err) {
                console.error("Request error: " + err);
                resp = JSON.stringify(err);
            }

            // Send response
            this.send_response(resp);
        }
    }

    private read_request() {
        let request = "";
        for (;;) {
            // Get the current state and message size
            const state = Atomics.load(this.comm, this.STATE_IDX);
            const size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

            // Append the latest part of the message.
            request += this.read_from_msg(0, size_to_read);

            // The request is complete.
            if (state === this.STATE_REQ)
                break;

            // Shutdown the worker.
            if (state === this.STATE_SHUTDOWN)
                return this.STATE_SHUTDOWN;

            // Reset the size and transition to await state.
            Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
            Atomics.store(this.comm, this.STATE_IDX, this.STATE_AWAIT);
            Atomics.wait(this.comm, this.STATE_IDX, this.STATE_AWAIT);
        }

        return request;
    }

    private read_from_msg(begin: number, end: number) {
        const slicedMessage: number[] = [];
        this.msg.slice(begin, end).forEach((value, index) => slicedMessage[index] = value);
        return String.fromCharCode.apply(null, slicedMessage);
    }

    private send_response(msg: string) {
        if (Atomics.load(this.comm, this.STATE_IDX) !== this.STATE_REQ)
            throw "WORKER: Invalid sync communication channel state.";

        let state; // State machine variable
        const msg_len = msg.length;
        let msg_written = 0;

        for (;;) {
            // Write the message and return how much was written.
            const wrote = this.write_to_msg(msg, msg_written, msg_len);
            msg_written += wrote;

            // Indicate how much was written to the this.msg buffer.
            Atomics.store(this.comm, this.MSG_SIZE_IDX, wrote);

            // Indicate if this was the whole message or part of it.
            state = msg_written === msg_len ? this.STATE_RESP : this.STATE_RESP_P;

            // Update the state
            Atomics.store(this.comm, this.STATE_IDX, state);

            // Wait for the transition to know the main thread has
            // received the response by moving onto a new state.
            Atomics.wait(this.comm, this.STATE_IDX, state);

            // Done sending response.
            if (state === this.STATE_RESP)
                break;
        }
    }

    private write_to_msg(input: string, start: number, input_len: number) {
        let mi = 0;
        let ii = start;
        while (mi < this.msg_char_len && ii < input_len) {
            this.msg[mi] = input.charCodeAt(ii);
            ii++; // Next character
            mi++; // Next buffer index
        }
        return ii - start;
    }

    public static create(comm_buf: number[], msg_buf: number[], msg_char_len: number) {
        return new ChannelWorker(comm_buf, msg_buf, msg_char_len);
    }
}

async function call_digest(type: number, data: BufferSource) {
    let digest_type = "";
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
    const digest = await crypto.subtle.digest(digest_type, data);
    return Array.from(new Uint8Array(digest));
}

// Operation to perform.
async function async_call(msg: string) {
    const req = JSON.parse(msg);

    if (req.func === "digest") {
        const digestArr = await call_digest(req.type, new Uint8Array(req.data));
        return JSON.stringify(digestArr);
    } else {
        throw "CRYPTO: Unknown request: " + req.func;
    }
}

let s_channel: ChannelWorker;

// Initialize WebWorker
onmessage = function (p: any) {
    let data = p;
    if (p.data !== undefined) {
        data = p.data;
    }
    s_channel = ChannelWorker.create(data.comm_buf, data.msg_buf, data.msg_char_len);
    s_channel.await_request(async_call);
};
