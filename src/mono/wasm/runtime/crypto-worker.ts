// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./imports";
import { mono_assert } from "./types";

let mono_wasm_crypto: {
    channel: LibraryChannel
    worker: Worker
} | null = null;

export function dotnet_browser_can_use_simple_digest_hash(): number {
    return mono_wasm_crypto === null ? 0 : 1;
}

export function dotnet_browser_simple_digest_hash(ver: number, input_buffer: number, input_len: number, output_buffer: number, output_len: number): number {
    mono_assert(!!mono_wasm_crypto, "subtle crypto not initialized");

    const msg = {
        func: "digest",
        type: ver,
        data: Array.from(Module.HEAPU8.subarray(input_buffer, input_buffer + input_len))
    };

    const response = mono_wasm_crypto.channel.send_msg(JSON.stringify(msg));
    const digest = JSON.parse(response);
    if (digest.length > output_len) {
        console.info("call_digest: about to throw!");
        throw "DIGEST HASH: Digest length exceeds output length: " + digest.length + " > " + output_len;
    }

    Module.HEAPU8.set(digest, output_buffer);
    return 1;
}

export function init_crypto(): void {
    if (typeof globalThis.crypto !== "undefined" && typeof globalThis.crypto.subtle !== "undefined"
        && typeof SharedArrayBuffer !== "undefined"
        && typeof Worker !== "undefined"
    ) {
        console.debug("MONO_WASM: Initializing Crypto WebWorker");

        const chan = LibraryChannel.create(1024); // 1024 is the buffer size in char units.
        const worker = new Worker("dotnet-crypto-worker.js");
        mono_wasm_crypto = {
            channel: chan,
            worker: worker,
        };
        worker.postMessage({
            comm_buf: chan.get_comm_buffer(),
            msg_buf: chan.get_msg_buffer(),
            msg_char_len: chan.get_msg_len()
        });
        worker.onerror = event => {
            console.warn(`MONO_WASM: Error in Crypto WebWorker. Cryptography digest calls will fallback to managed implementation. Error: ${event.message}`);
            mono_wasm_crypto = null;
        };
    }
}

class LibraryChannel {
    private msg_char_len: number;
    private comm_buf: SharedArrayBuffer;
    private msg_buf: SharedArrayBuffer;
    private comm: Int32Array;
    private msg: Uint16Array;

    // LOCK states
    private get LOCK_UNLOCKED(): number { return 0; }  // 0 means the lock is unlocked
    private get LOCK_OWNED(): number { return 1; } // 1 means the LibraryChannel owns the lock

    // Index constants for the communication buffer.
    private get STATE_IDX(): number { return 0; }
    private get MSG_SIZE_IDX(): number { return 1; }
    private get LOCK_IDX(): number { return 2; }
    private get COMM_LAST_IDX(): number { return this.LOCK_IDX; }

    // Communication states.
    private get STATE_SHUTDOWN(): number { return -1; } // Shutdown
    private get STATE_IDLE(): number { return 0; }
    private get STATE_REQ(): number { return 1; }
    private get STATE_RESP(): number { return 2; }
    private get STATE_REQ_P(): number { return 3; } // Request has multiple parts
    private get STATE_RESP_P(): number { return 4; } // Response has multiple parts
    private get STATE_AWAIT(): number { return 5; } // Awaiting the next part

    private constructor(msg_char_len: number) {
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

    public get_msg_len(): number { return this.msg_char_len; }
    public get_msg_buffer(): SharedArrayBuffer { return this.msg_buf; }
    public get_comm_buffer(): SharedArrayBuffer { return this.comm_buf; }

    public send_msg(msg: string): string {
        if (Atomics.load(this.comm, this.STATE_IDX) !== this.STATE_IDLE) {
            throw "OWNER: Invalid sync communication channel state. " + Atomics.load(this.comm, this.STATE_IDX);
        }
        this.send_request(msg);
        return this.read_response();
    }

    public shutdown(): void {
        if (Atomics.load(this.comm, this.STATE_IDX) !== this.STATE_IDLE) {
            throw "OWNER: Invalid sync communication channel state. " + Atomics.load(this.comm, this.STATE_IDX);
        }

        // Notify webworker
        Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
        Atomics.store(this.comm, this.STATE_IDX, this.STATE_SHUTDOWN);
        Atomics.notify(this.comm, this.STATE_IDX);
    }

    private send_request(msg: string): void {
        let state;
        const msg_len = msg.length;
        let msg_written = 0;

        for (; ;) {
            this.acquire_lock();

            // Write the message and return how much was written.
            const wrote = this.write_to_msg(msg, msg_written, msg_len);
            msg_written += wrote;

            // Indicate how much was written to the this.msg buffer.
            Atomics.store(this.comm, this.MSG_SIZE_IDX, wrote);

            // Indicate if this was the whole message or part of it.
            state = msg_written === msg_len ? this.STATE_REQ : this.STATE_REQ_P;

            // Notify webworker
            Atomics.store(this.comm, this.STATE_IDX, state);

            this.release_lock();

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

    private write_to_msg(input: string, start: number, input_len: number): number {
        let mi = 0;
        let ii = start;
        while (mi < this.msg_char_len && ii < input_len) {
            this.msg[mi] = input.charCodeAt(ii);
            ii++; // Next character
            mi++; // Next buffer index
        }
        return ii - start;
    }

    private read_response(): string {
        let state;
        let response = "";
        for (; ;) {
            // Wait for webworker response.
            //  - Atomics.wait() is not permissible on the main thread.
            do {
                state = Atomics.load(this.comm, this.STATE_IDX);
            } while (state !== this.STATE_RESP && state !== this.STATE_RESP_P);

            this.acquire_lock();

            const size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

            // Append the latest part of the message.
            response += this.read_from_msg(0, size_to_read);

            // The response is complete.
            if (state === this.STATE_RESP) {
                this.release_lock();
                break;
            }

            // Reset the size and transition to await state.
            Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
            Atomics.store(this.comm, this.STATE_IDX, this.STATE_AWAIT);

            this.release_lock();

            Atomics.notify(this.comm, this.STATE_IDX);
        }

        // Reset the communication channel's state and let the
        // webworker know we are done.
        Atomics.store(this.comm, this.STATE_IDX, this.STATE_IDLE);
        Atomics.notify(this.comm, this.STATE_IDX);

        return response;
    }

    private read_from_msg(begin: number, end: number): string {
        const slicedMessage: number[] = [];
        this.msg.slice(begin, end).forEach((value, index) => slicedMessage[index] = value);
        return String.fromCharCode.apply(null, slicedMessage);
    }

    private acquire_lock() {
        while (Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_UNLOCKED, this.LOCK_OWNED) !== this.LOCK_UNLOCKED) {
            // empty
        }
    }

    private release_lock() {
        const result = Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_OWNED, this.LOCK_UNLOCKED);
        if (result !== this.LOCK_OWNED) {
            throw "CRYPTO: LibraryChannel tried to release a lock that wasn't acquired: " + result;
        }
    }

    public static create(msg_char_len: number): LibraryChannel {
        if (msg_char_len === undefined) {
            msg_char_len = 1024; // Default size is arbitrary but is in 'char' units (i.e. UTF-16 code points).
        }
        return new LibraryChannel(msg_char_len);
    }
}
