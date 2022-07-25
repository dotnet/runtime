// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { prevent_timer_throttling } from "./scheduling";
import { Queue } from "./queue";
import { PromiseControl, create_cancelable_promise } from "./cancelable-promise";
import { mono_assert } from "./types";
import { ArraySegment, IDisposable } from "./marshal";

const wasm_ws_pending_send_buffer = Symbol.for("wasm ws_pending_send_buffer");
const wasm_ws_pending_send_buffer_offset = Symbol.for("wasm ws_pending_send_buffer_offset");
const wasm_ws_pending_send_buffer_type = Symbol.for("wasm ws_pending_send_buffer_type");
const wasm_ws_pending_receive_event_queue = Symbol.for("wasm ws_pending_receive_event_queue");
const wasm_ws_pending_receive_promise_queue = Symbol.for("wasm ws_pending_receive_promise_queue");
const wasm_ws_pending_open_promise = Symbol.for("wasm ws_pending_open_promise");
const wasm_ws_pending_close_promises = Symbol.for("wasm ws_pending_close_promises");
const wasm_ws_pending_send_promises = Symbol.for("wasm ws_pending_send_promises");
const wasm_ws_is_aborted = Symbol.for("wasm ws_is_aborted");
let mono_wasm_web_socket_close_warning = false;
let _text_decoder_utf8: TextDecoder | undefined = undefined;
let _text_encoder_utf8: TextEncoder | undefined = undefined;
const ws_send_buffer_blocking_threshold = 65536;
const emptyBuffer = new Uint8Array();

export function ws_wasm_create(uri: string, sub_protocols: string[] | null, onClosed: (code: number, reason: string) => void): WebSocketExtension {
    mono_assert(uri && typeof uri === "string", () => `ERR12: Invalid uri ${typeof uri}`);

    const ws = new globalThis.WebSocket(uri, sub_protocols || undefined) as WebSocketExtension;
    const { promise_control: open_promise_control } = create_cancelable_promise();

    ws[wasm_ws_pending_receive_event_queue] = new Queue();
    ws[wasm_ws_pending_receive_promise_queue] = new Queue();
    ws[wasm_ws_pending_open_promise] = open_promise_control;
    ws[wasm_ws_pending_send_promises] = [];
    ws[wasm_ws_pending_close_promises] = [];
    ws.binaryType = "arraybuffer";
    const local_on_open = () => {
        if (ws[wasm_ws_is_aborted]) return;
        open_promise_control.resolve(ws);
        prevent_timer_throttling();
    };
    const local_on_message = (ev: MessageEvent) => {
        if (ws[wasm_ws_is_aborted]) return;
        _mono_wasm_web_socket_on_message(ws, ev);
        prevent_timer_throttling();
    };
    const local_on_close = (ev: CloseEvent) => {
        ws.removeEventListener("message", local_on_message);
        if (ws[wasm_ws_is_aborted]) return;
        if (onClosed) onClosed(ev.code, ev.reason);

        // this reject would not do anything if there was already "open" before it.
        open_promise_control.reject(ev.reason);

        for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
            close_promise_control.resolve();
        }

        // send close to any pending receivers, to wake them
        const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
        receive_promise_queue.drain((receive_promise_control) => {

            const response = new Int32Array([
                0,// count 
                2, // type:close
                1]);// end_of_message: true
            receive_promise_control.responseView.set(response);
            receive_promise_control.resolve(null);
        });
    };
    const local_on_error = (ev: any) => {
        open_promise_control.reject(ev.message || "WebSocket error");
    };
    ws.addEventListener("message", local_on_message);
    ws.addEventListener("open", local_on_open, { once: true });
    ws.addEventListener("close", local_on_close, { once: true });
    ws.addEventListener("error", local_on_error, { once: true });

    return ws;
}

export function ws_wasm_open(ws: WebSocketExtension): Promise<void> | null {
    mono_assert(!!ws, "ERR17: expected ws instance");
    const open_promise_control = ws[wasm_ws_pending_open_promise];
    return open_promise_control.promise;
}

export function ws_wasm_send(ws: WebSocketExtension, bufferView: ArraySegment, message_type: number, end_of_message: boolean): Promise<void> | null {
    mono_assert(!!ws, "ERR17: expected ws instance");

    const whole_buffer = _mono_wasm_web_socket_send_buffering(ws, bufferView, message_type, end_of_message);

    if (!end_of_message || !whole_buffer) {
        return null;
    }

    return _mono_wasm_web_socket_send_and_wait(ws, whole_buffer, bufferView);
}

export function ws_wasm_receive(ws: WebSocketExtension, bufferView: ArraySegment, responseView: ArraySegment): Promise<void> | null {
    mono_assert(!!ws, "ERR18: expected ws instance");

    const receive_event_queue = ws[wasm_ws_pending_receive_event_queue];
    const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];

    const readyState = ws.readyState;
    if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
        throw new Error("InvalidState: The WebSocket is not connected.");
    }

    if (receive_event_queue.getLength()) {
        mono_assert(receive_promise_queue.getLength() == 0, "ERR20: Invalid WS state");

        // finish synchronously
        _mono_wasm_web_socket_receive_buffering(receive_event_queue, bufferView, responseView);

        return null;
    }
    const { promise, promise_control } = create_cancelable_promise(undefined, undefined);
    const receive_promise_control = promise_control as ReceivePromiseControl;
    receive_promise_control.bufferView = bufferView;
    receive_promise_control.responseView = responseView;
    receive_promise_queue.enqueue(receive_promise_control);

    return promise;
}

export function ws_wasm_close(ws: WebSocketExtension, code: number, reason: string | null, wait_for_close_received: boolean): Promise<void> | null {
    mono_assert(!!ws, "ERR19: expected ws instance");


    if (ws.readyState == WebSocket.CLOSED) {
        return null;
    }

    if (wait_for_close_received) {
        const { promise, promise_control } = create_cancelable_promise();
        ws[wasm_ws_pending_close_promises].push(promise_control);

        if (typeof reason === "string") {
            ws.close(code, reason);
        } else {
            ws.close(code);
        }
        return promise;
    }
    else {
        if (!mono_wasm_web_socket_close_warning) {
            mono_wasm_web_socket_close_warning = true;
            console.warn("WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.");
        }
        if (typeof reason === "string") {
            ws.close(code, reason);
        } else {
            ws.close(code);
        }
        return null;
    }
}

export function ws_wasm_abort(ws: WebSocketExtension): void {
    mono_assert(!!ws, "ERR18: expected ws instance");

    ws[wasm_ws_is_aborted] = true;
    const open_promise_control = ws[wasm_ws_pending_open_promise];
    if (open_promise_control) {
        open_promise_control.reject("OperationCanceledException");
    }
    for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
        close_promise_control.reject("OperationCanceledException");
    }
    for (const send_promise_control of ws[wasm_ws_pending_send_promises]) {
        send_promise_control.reject("OperationCanceledException");
    }

    ws[wasm_ws_pending_receive_promise_queue].drain(receive_promise_control => {
        receive_promise_control.reject("OperationCanceledException");
    });

    // this is different from Managed implementation
    ws.close(1000, "Connection was aborted.");
}

function _mono_wasm_web_socket_send_and_wait(ws: WebSocketExtension, buffer: Uint8Array | string, managedBuffer: IDisposable): Promise<void> | null {
    // send and return promise
    ws.send(buffer);
    managedBuffer.dispose();
    ws[wasm_ws_pending_send_buffer] = null;

    // if the remaining send buffer is small, we don't block so that the throughput doesn't suffer.
    // Otherwise we block so that we apply some backpresure to the application sending large data.
    // this is different from Managed implementation
    if (ws.bufferedAmount < ws_send_buffer_blocking_threshold) {
        return null;
    }

    // block the promise/task until the browser passed the buffer to OS
    const { promise, promise_control } = create_cancelable_promise();
    const pending = ws[wasm_ws_pending_send_promises];
    pending.push(promise_control);

    let nextDelay = 1;
    const polling_check = () => {
        // was it all sent yet ?
        if (ws.bufferedAmount === 0) {
            promise_control.resolve(null);
        }
        else if (ws.readyState != WebSocket.OPEN) {
            // only reject if the data were not sent
            // bufferedAmount does not reset to zero once the connection closes
            promise_control.reject("InvalidState: The WebSocket is not connected.");
        }
        else if (!promise_control.isDone) {
            globalThis.setTimeout(polling_check, nextDelay);
            // exponentially longer delays, up to 1000ms
            nextDelay = Math.min(nextDelay * 1.5, 1000);
            return;
        }
        // remove from pending
        const index = pending.indexOf(promise_control);
        if (index > -1) {
            pending.splice(index, 1);
        }
    };

    globalThis.setTimeout(polling_check, 0);

    return promise;
}

function _mono_wasm_web_socket_on_message(ws: WebSocketExtension, event: MessageEvent) {
    const event_queue = ws[wasm_ws_pending_receive_event_queue];
    const promise_queue = ws[wasm_ws_pending_receive_promise_queue];

    if (typeof event.data === "string") {
        if (_text_encoder_utf8 === undefined) {
            _text_encoder_utf8 = new TextEncoder();
        }
        event_queue.enqueue({
            type: 0,// WebSocketMessageType.Text
            // according to the spec https://encoding.spec.whatwg.org/
            // - Unpaired surrogates will get replaced with 0xFFFD
            // - utf8 encode specifically is defined to never throw
            data: _text_encoder_utf8.encode(event.data),
            offset: 0
        });
    }
    else {
        if (event.data.constructor.name !== "ArrayBuffer") {
            throw new Error("ERR19: WebSocket receive expected ArrayBuffer");
        }
        event_queue.enqueue({
            type: 1,// WebSocketMessageType.Binary
            data: new Uint8Array(event.data),
            offset: 0
        });
    }
    if (promise_queue.getLength() && event_queue.getLength() > 1) {
        throw new Error("ERR21: Invalid WS state");// assert
    }
    while (promise_queue.getLength() && event_queue.getLength()) {
        const promise_control = promise_queue.dequeue()!;
        _mono_wasm_web_socket_receive_buffering(event_queue,
            promise_control.bufferView, promise_control.responseView);
        promise_control.resolve(null);
    }
    prevent_timer_throttling();
}

function _mono_wasm_web_socket_receive_buffering(event_queue: Queue<any>, bufferView: ArraySegment, responseView: ArraySegment) {
    const event = event_queue.peek();

    const count = Math.min(bufferView.length, event.data.length - event.offset);
    if (count > 0) {
        const sourceView = event.data.subarray(event.offset, event.offset + count);
        bufferView.set(sourceView, 0);
        event.offset += count;
    }
    const end_of_message = event.data.length === event.offset ? 1 : 0;
    if (end_of_message) {
        event_queue.dequeue();
    }

    const response = new Int32Array([count, event.type, end_of_message]);
    responseView.set(response);

    bufferView.dispose();
    responseView.dispose();
}

function _mono_wasm_web_socket_send_buffering(ws: WebSocketExtension, bufferView: ArraySegment, message_type: number, end_of_message: boolean): Uint8Array | string | null {
    let buffer = ws[wasm_ws_pending_send_buffer];
    let offset = 0;
    const length = bufferView.length;

    if (buffer) {
        offset = ws[wasm_ws_pending_send_buffer_offset];
        // match desktop WebSocket behavior by copying message_type of the first part
        message_type = ws[wasm_ws_pending_send_buffer_type];
        // if not empty message, append to existing buffer
        if (length !== 0) {
            if (offset + length > buffer.length) {
                const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
                newbuffer.set(buffer, 0);// copy previous buffer
                bufferView.copyTo(newbuffer.subarray(offset));// append copy at the end
                ws[wasm_ws_pending_send_buffer] = buffer = newbuffer;
            }
            else {
                bufferView.copyTo(buffer.subarray(offset));// append copy at the end
            }
            offset += length;
            ws[wasm_ws_pending_send_buffer_offset] = offset;
        }
    }
    else if (!end_of_message) {
        // create new buffer
        if (length !== 0) {
            buffer = <Uint8Array>bufferView.slice(); // copy
            offset = length;
            ws[wasm_ws_pending_send_buffer_offset] = offset;
            ws[wasm_ws_pending_send_buffer] = buffer;
        }
        ws[wasm_ws_pending_send_buffer_type] = message_type;
    }
    else {
        if (length !== 0) {
            // we could use the unsafe view, because it will be immediately used in ws.send()
            buffer = <Uint8Array>bufferView._unsafe_create_view();
            offset = length;
        }
    }
    // buffer was updated, do we need to trim and convert it to final format ?
    if (end_of_message) {
        if (offset == 0 || buffer == null) {
            return emptyBuffer;
        }
        if (message_type === 0) {
            // text, convert from UTF-8 bytes to string, because of bad browser API
            if (_text_decoder_utf8 === undefined) {
                // we do not validate outgoing data https://github.com/dotnet/runtime/issues/59214
                _text_decoder_utf8 = new TextDecoder("utf-8", { fatal: false });
            }

            // See https://github.com/whatwg/encoding/issues/172
            const bytes = typeof SharedArrayBuffer !== "undefined" && buffer instanceof SharedArrayBuffer
                ? (<any>buffer).slice(0, offset)
                : buffer.subarray(0, offset);
            return _text_decoder_utf8.decode(bytes);
        } else {
            // binary, view to used part of the buffer
            return buffer.subarray(0, offset);
        }
    }
    return null;
}

type WebSocketExtension = WebSocket & {
    [wasm_ws_pending_receive_event_queue]: Queue<Message>;
    [wasm_ws_pending_receive_promise_queue]: Queue<ReceivePromiseControl>;
    [wasm_ws_pending_open_promise]: PromiseControl
    [wasm_ws_pending_send_promises]: PromiseControl[]
    [wasm_ws_pending_close_promises]: PromiseControl[]
    [wasm_ws_is_aborted]: boolean
    [wasm_ws_pending_send_buffer_offset]: number
    [wasm_ws_pending_send_buffer_type]: number
    [wasm_ws_pending_send_buffer]: Uint8Array | null
}

type ReceivePromiseControl = PromiseControl & {
    bufferView: ArraySegment,
    responseView: ArraySegment
}

type Message = {
    type: number,// WebSocketMessageType
    data: Uint8Array,
    offset: number
}
