// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, runtimeHelpers } from "./imports";
import { mono_assert } from "./types";

class OperationFailedError extends Error { }

const ERR_ARGS = -1;
const ERR_WORKER_FAILED = -2;
const ERR_OP_FAILED = -3;
const ERR_UNKNOWN = -100;

let mono_wasm_crypto: {
    channel: LibraryChannel
    worker: Worker
} | null = null;

export function dotnet_browser_can_use_subtle_crypto_impl(): number {
    return mono_wasm_crypto === null ? 0 : 1;
}

export function dotnet_browser_simple_digest_hash(ver: number, input_buffer: number, input_len: number, output_buffer: number, output_len: number): number {
    const msg = {
        func: "digest",
        type: ver,
        data: Array.from(Module.HEAPU8.subarray(input_buffer, input_buffer + input_len))
    };

    return _send_simple_msg(msg, "DIGEST HASH", output_buffer, output_len);
}

export function dotnet_browser_sign(hashAlgorithm: number, key_buffer: number, key_len: number, input_buffer: number, input_len: number, output_buffer: number, output_len: number): number {
    const msg = {
        func: "sign",
        type: hashAlgorithm,
        key: Array.from(Module.HEAPU8.subarray(key_buffer, key_buffer + key_len)),
        data: Array.from(Module.HEAPU8.subarray(input_buffer, input_buffer + input_len))
    };

    return _send_simple_msg(msg, "SIGN HASH", output_buffer, output_len);
}

const AesBlockSizeBytes = 16; // 128 bits

export function dotnet_browser_encrypt_decrypt(isEncrypting: boolean, key_buffer: number, key_len: number, iv_buffer: number, iv_len: number, input_buffer: number, input_len: number, output_buffer: number, output_len: number): number {
    if (input_len <= 0 || input_len % AesBlockSizeBytes !== 0) {
        throw "ENCRYPT DECRYPT: data was not a full block: " + input_len;
    }

    const msg = {
        func: "encrypt_decrypt",
        isEncrypting: isEncrypting,
        key: Array.from(Module.HEAPU8.subarray(key_buffer, key_buffer + key_len)),
        iv: Array.from(Module.HEAPU8.subarray(iv_buffer, iv_buffer + iv_len)),
        data: Array.from(Module.HEAPU8.subarray(input_buffer, input_buffer + input_len))
    };

    const result = _send_msg_worker(msg);
    if (typeof result === "number") {
        return result;
    }

    if (result.length > output_len) {
        console.error(`MONO_WASM_ENCRYPT_DECRYPT: Encrypt/Decrypt length exceeds output length: ${result.length} > ${output_len}`);
        return ERR_ARGS;
    }

    Module.HEAPU8.set(result, output_buffer);
    return result.length;
}

export function dotnet_browser_derive_bits(password_buffer: number, password_len: number, salt_buffer: number, salt_len: number, iterations: number, hashAlgorithm: number, output_buffer: number, output_len: number): number {
    const msg = {
        func: "derive_bits",
        password: Array.from(Module.HEAPU8.subarray(password_buffer, password_buffer + password_len)),
        salt: Array.from(Module.HEAPU8.subarray(salt_buffer, salt_buffer + salt_len)),
        iterations: iterations,
        hashAlgorithm: hashAlgorithm,
        lengthInBytes: output_len
    };

    return _send_simple_msg(msg, "DERIVE BITS", output_buffer, output_len);
}

function _send_simple_msg(msg: any, prefix: string, output_buffer: number, output_len: number): number {
    const result = _send_msg_worker(msg);

    if (typeof result === "number") {
        return result;
    }

    if (result.length > output_len) {
        console.error(`MONO_WASM_ENCRYPT_DECRYPT: ${prefix}: Result length exceeds output length: ${result.length} > ${output_len}`);
        return ERR_ARGS;
    }

    Module.HEAPU8.set(result, output_buffer);
    return 0;
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
        const messageData: InitCryptoMessageData = {
            config: JSON.stringify(runtimeHelpers.config),
            comm_buf: chan.get_comm_buffer(),
            msg_buf: chan.get_msg_buffer(),
            msg_char_len: chan.get_msg_len()
        };
        worker.postMessage(messageData);
        worker.onerror = event => {
            console.warn(`MONO_WASM: Error in Crypto WebWorker. Cryptography digest calls will fallback to managed implementation. Error: ${event.message}`);
            mono_wasm_crypto = null;
        };
    }
}

function _send_msg_worker(msg: any): number | any {
    mono_assert(!!mono_wasm_crypto, "subtle crypto not initialized");

    try {
        const response = mono_wasm_crypto.channel.send_msg(JSON.stringify(msg));
        const responseJson = JSON.parse(response);

        if (responseJson.error !== undefined) {
            console.error(`MONO_WASM_ENCRYPT_DECRYPT: Worker failed with: ${responseJson.error}`);
            if (responseJson.error_type == "ArgumentsError")
                return ERR_ARGS;
            if (responseJson.error_type == "WorkerFailedError")
                return ERR_WORKER_FAILED;

            return ERR_UNKNOWN;
        }

        return responseJson.result;
    } catch (err) {
        if (err instanceof Error && err.stack !== undefined)
            console.error(`MONO_WASM_ENCRYPT_DECRYPT: ${err.stack}`);
        else
            console.error(`MONO_WASM_ENCRYPT_DECRYPT: _send_msg_worker failed: ${err}`);
        return ERR_OP_FAILED;
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
    private get STATE_REQ_FAILED(): number { return 6; } // The Request failed
    private get STATE_RESET(): number { return 7; } // Reset to a known state

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
        try {
            let state = Atomics.load(this.comm, this.STATE_IDX);
            // FIXME: this console write is possibly serializing the access and prevents a deadlock
            if (state !== this.STATE_IDLE) console.debug(`MONO_WASM_ENCRYPT_DECRYPT: send_msg, waiting for idle now, ${state}`);
            state = this.wait_for_state(pstate => pstate == this.STATE_IDLE, "waiting");

            this.send_request(msg);
            return this.read_response();
        } catch (err) {
            this.reset(LibraryChannel._stringify_err(err));
            throw err;
        }
        finally {
            const state = Atomics.load(this.comm, this.STATE_IDX);
            // FIXME: this console write is possibly serializing the access and prevents a deadlock
            if (state !== this.STATE_IDLE) console.debug(`MONO_WASM_ENCRYPT_DECRYPT: state at end of send_msg: ${state}`);
        }
    }

    public shutdown(): void {
        console.debug("MONO_WASM_ENCRYPT_DECRYPT: Shutting down crypto");
        const state = Atomics.load(this.comm, this.STATE_IDX);
        if (state !== this.STATE_IDLE)
            throw new Error(`OWNER: Invalid sync communication channel state: ${state}`);

        // Notify webworker
        Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
        this._change_state_locked(this.STATE_SHUTDOWN);
        Atomics.notify(this.comm, this.STATE_IDX);
    }

    private reset(reason: string): void {
        console.debug(`MONO_WASM_ENCRYPT_DECRYPT: reset: ${reason}`);
        const state = Atomics.load(this.comm, this.STATE_IDX);
        if (state === this.STATE_SHUTDOWN)
            return;

        if (state === this.STATE_RESET || state === this.STATE_IDLE) {
            console.debug(`MONO_WASM_ENCRYPT_DECRYPT: state is already RESET or idle: ${state}`);
            return;
        }

        Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
        this._change_state_locked(this.STATE_RESET);
        Atomics.notify(this.comm, this.STATE_IDX);
    }

    private send_request(msg: string): void {
        let state;
        const msg_len = msg.length;
        let msg_written = 0;

        for (; ;) {
            this.acquire_lock();

            try {
                // Write the message and return how much was written.
                const wrote = this.write_to_msg(msg, msg_written, msg_len);
                msg_written += wrote;

                // Indicate how much was written to the this.msg buffer.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, wrote);

                // Indicate if this was the whole message or part of it.
                state = msg_written === msg_len ? this.STATE_REQ : this.STATE_REQ_P;

                // Notify webworker
                this._change_state_locked(state);
            } finally {
                this.release_lock();
            }

            Atomics.notify(this.comm, this.STATE_IDX);

            // The send message is complete.
            if (state === this.STATE_REQ)
                break;

            this.wait_for_state(state => state == this.STATE_AWAIT, "send_request");
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
        let response = "";
        for (; ;) {
            const state = this.wait_for_state(state => state == this.STATE_RESP || state == this.STATE_RESP_P, "read_response");
            this.acquire_lock();

            try {
                const size_to_read = Atomics.load(this.comm, this.MSG_SIZE_IDX);

                // Append the latest part of the message.
                response += this.read_from_msg(0, size_to_read);

                // The response is complete.
                if (state === this.STATE_RESP) {
                    Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
                    break;
                }

                // Reset the size and transition to await state.
                Atomics.store(this.comm, this.MSG_SIZE_IDX, 0);
                this._change_state_locked(this.STATE_AWAIT);
            } finally {
                this.release_lock();
            }
            Atomics.notify(this.comm, this.STATE_IDX);
        }

        // Reset the communication channel's state and let the
        // webworker know we are done.
        this._change_state_locked(this.STATE_IDLE);
        Atomics.notify(this.comm, this.STATE_IDX);

        return response;
    }

    private _change_state_locked(newState: number): void {
        Atomics.store(this.comm, this.STATE_IDX, newState);
    }

    private wait_for_state(is_ready: (state: number) => boolean, msg: string): number {
        // Wait for webworker
        //  - Atomics.wait() is not permissible on the main thread.
        for (; ;) {
            const lock_state = Atomics.load(this.comm, this.LOCK_IDX);
            if (lock_state !== this.LOCK_UNLOCKED)
                continue;

            const state = Atomics.load(this.comm, this.STATE_IDX);
            if (state == this.STATE_REQ_FAILED)
                throw new OperationFailedError(`Worker failed during ${msg} with state=${state}`);

            if (is_ready(state))
                return state;
        }
    }

    private read_from_msg(begin: number, end: number): string {
        const slicedMessage: number[] = [];
        this.msg.slice(begin, end).forEach((value, index) => slicedMessage[index] = value);
        return String.fromCharCode.apply(null, slicedMessage);
    }

    private acquire_lock() {
        for (; ;) {
            const lock_state = Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_UNLOCKED, this.LOCK_OWNED);

            if (lock_state === this.LOCK_UNLOCKED) {
                const state = Atomics.load(this.comm, this.STATE_IDX);
                if (state === this.STATE_REQ_FAILED)
                    throw new OperationFailedError("Worker failed");
                return;
            }
        }
    }

    private release_lock() {
        const result = Atomics.compareExchange(this.comm, this.LOCK_IDX, this.LOCK_OWNED, this.LOCK_UNLOCKED);
        if (result !== this.LOCK_OWNED) {
            throw new Error("CRYPTO: LibraryChannel tried to release a lock that wasn't acquired: " + result);
        }
    }

    private static _stringify_err(err: any) {
        return (err instanceof Error && err.stack !== undefined) ? err.stack : err;
    }

    public static create(msg_char_len: number): LibraryChannel {
        if (msg_char_len === undefined) {
            msg_char_len = 1024; // Default size is arbitrary but is in 'char' units (i.e. UTF-16 code points).
        }
        return new LibraryChannel(msg_char_len);
    }
}

export type InitCryptoMessageData = {
    config: string,// serialized to avoid passing non-clonable objects
    comm_buf: SharedArrayBuffer,
    msg_buf: SharedArrayBuffer,
    msg_char_len: number
}
