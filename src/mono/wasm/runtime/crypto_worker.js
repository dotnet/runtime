// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var ChannelWorker = {
    _impl: class {
        // BEGIN ChannelOwner contract - shared constants.
        STATE_IDX = 0;
        MSG_SIZE_IDX = 1;

        STATE_IDLE = 0;
        STATE_REQ = 1;
        STATE_RESP = 2;
        STATE_REQ_P = 3; // Request has multiple parts
        STATE_RESP_P = 4; // Response has multiple parts
        STATE_AWAIT = 5; // Awaiting the next part
        // END ChannelOwner contract - shared constants.

        constructor(comm_buf, msg_buf, msg_char_len) {
            this.comm = new Int32Array(comm_buf);
            this.msg = new Uint16Array(msg_buf);
            this.msg_char_len = msg_char_len;
        }

        async await_request(async_call) {
            console.log("await_request()");

            for (;;) {
                // Wait for signal to perform operation
                Atomics.wait(this.comm, this.STATE_IDX, this.STATE_IDLE);

                // Read in request
                var req = this._read_request();
                console.log("Request: " + req);

                // Perform async action based on request
                var resp = await async_call(req);

                // Send response
                this._send_response(resp);
            }
        }

        _read_request() {
            var request = "";
            for (;;) {
                // Get the current state and message size
                var state = Atomics.load(this.comm, this.STATE_IDX);
                var size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

                // Append the latest part of the message.
                request += this._read_from_msg(0, size_to_read);

                // The request is complete.
                if (state === this.STATE_REQ)
                    break;

                // Reset the size and transition to await state.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
                Atomics.store(this.comm, this.STATE_IDX, this.STATE_AWAIT);
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
                // Write the message and return how much was written.
                var wrote = this._write_to_msg(msg, msg_written, msg_len);
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
    },

    create: function (comm_buf, msg_buf, msg_char_len) {
        return new this._impl(comm_buf, msg_buf, msg_char_len);
    }
};

//
// [TODO] Handle crypto calls that uses Promises below.
//

// Operation to perform.
async function async_call(msg) {
    var keyPair = await self.crypto.subtle.generateKey(
        {
            name: "RSA-OAEP",
            modulusLength: 2048,
            publicExponent: new Uint8Array([1, 0, 1]),
            hash: "SHA-256",
        },
        true,
        ["encrypt", "decrypt"]
    );

    return msg.split("").reverse().join("");
}

var s_channel;

// Initialize WebWorker
onmessage = function (p) {
    console.log(p.data.salutation);
    s_channel = ChannelWorker.create(p.data.comm_buf, p.data.msg_buf, p.data.msg_char_len);

    s_channel.await_request(async_call);
}
