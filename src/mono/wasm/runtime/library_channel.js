// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

Module [ 'channel' ] = {
    _impl: class {
        // Index constants for the communication buffer.
        get STATE_IDX() { return 0; }
        get MSG_SIZE_IDX() { return 1; }
        get COMM_LAST_IDX() { return this.MSG_SIZE_IDX; }

        // Communication states.
        get STATE_IDLE() { return 0; }
        get STATE_REQ() { return 1; }
        get STATE_RESP() { return 2; }
        get STATE_REQ_P() { return 3; } // Request has multiple parts
        get STATE_RESP_P() { return 4; } // Response has multiple parts
        get STATE_AWAIT() { return 5; } // Awaiting the next part

        constructor(msg_char_len) {
            this.msg_char_len = msg_char_len;

            const int_bytes = 4;
            const comm_byte_len = int_bytes * (this.COMM_LAST_IDX + 1);
            this.comm_buf = new SharedArrayBuffer(comm_byte_len);

            // JavaScript character encoding is UTF-16.
            const char_bytes = 2;
            const msg_byte_len = char_bytes * this.msg_char_len;
            this.msg_buf = new SharedArrayBuffer(msg_byte_len);

            // Create the local arrays to use.
            this.comm = new Int32Array(this.comm_buf);
            this.msg = new Uint16Array(this.msg_buf);
        }

        get_msg_len() { return this.msg_char_len; }
        get_msg_buffer() { return this.msg_buf; }
        get_comm_buffer() { return this.comm_buf; }

        send_msg(msg) {
            if (Atomics.load(this.comm, this.STATE_IDX) !== this.STATE_IDLE) {
                throw "OWNER: Invalid sync communication channel state.";
            }
            this._send_request(msg);
            return this._read_response();
        }

        _send_request(msg) {
            var state;
            const msg_len = msg.length;
            var msg_written = 0;

            for (;;) {
                // Write the message and return how much was written.
                var wrote = this._write_to_msg(msg, msg_written, msg_len);
                msg_written += wrote;

                // Indicate how much was written to the this.msg buffer.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, wrote);

                // Indicate if this was the whole message or part of it.
                state = msg_written === msg_len ? this.STATE_REQ : this.STATE_REQ_P;

                // Notify webworker
                Atomics.store(this.comm, this.STATE_IDX, state);
                Atomics.notify(this.comm, this.STATE_IDX);

                // The send message is complete.
                if (state === this.STATE_REQ)
                    break;

                // Wait for the worker to be ready for the next part.
                //  - Atomics.wait() is not permissible on the main thread.
                do {
                    state = Atomics.load(this.comm, this.STATE_IDX);
                } while (state !== this.STATE_AWAIT);
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

        _read_response() {
            var state;
            var response = "";
            for (;;) {
                // Wait for webworker response.
                //  - Atomics.wait() is not permissible on the main thread.
                do {
                    state = Atomics.load(this.comm, this.STATE_IDX);
                } while (state !== this.STATE_RESP && state !== this.STATE_RESP_P);

                var size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

                // Append the latest part of the message.
                response += this._read_from_msg(0, size_to_read);

                // The response is complete.
                if (state === this.STATE_RESP)
                    break;

                // Reset the size and transition to await state.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
                Atomics.store(this.comm, this.STATE_IDX, this.STATE_AWAIT);
                Atomics.notify(this.comm, this.STATE_IDX);
            }

            // Reset the communication channel's state and let the
            // webworker know we are done.
            Atomics.store(this.comm, this.STATE_IDX, this.STATE_IDLE);
            Atomics.notify(this.comm, this.STATE_IDX);

            return response;
        }

        _read_from_msg(begin, end) {
            return String.fromCharCode.apply(null, this.msg.slice(begin, end));
        }
    },

    create: function (msg_char_len) {
        if (msg_char_len === undefined) {
            msg_char_len = 1024; // Default size is arbitrary but is in 'char' units (i.e. UTF-16 code points).
        }
        return new this._impl(msg_char_len);
    }
};
