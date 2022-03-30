// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_wasm_new_root, WasmRoot } from "./roots";
import { prevent_timer_throttling } from "./scheduling";
import { Queue } from "./queue";
import { PromiseControl, _create_cancelable_promise } from "./cancelable-promise";
import { _mono_array_root_to_js_array, _wrap_delegate_root_as_function } from "./cs-to-js";
import { mono_wasm_get_jsobj_from_js_handle, mono_wasm_get_js_handle } from "./gc-handles";
import { _wrap_js_thenable_as_task } from "./js-to-cs";
import { wrap_error } from "./method-calls";
import { conv_string } from "./strings";
import { JSHandle, MonoArray, MonoObject, MonoObjectNull, MonoString } from "./types";
import { Module } from "./imports";
import { Int32Ptr } from "./types/emscripten";

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

export function mono_wasm_web_socket_open(uri: MonoString, subProtocols: MonoArray, on_close: MonoObject, web_socket_js_handle: Int32Ptr, thenable_js_handle: Int32Ptr, is_exception: Int32Ptr): MonoObject {
    const uri_root = mono_wasm_new_root(uri);
    const sub_root = mono_wasm_new_root(subProtocols);
    const on_close_root = mono_wasm_new_root(on_close);
    try {
        const js_uri = conv_string(uri_root.value);
        if (!js_uri) {
            return wrap_error(is_exception, "ERR12: Invalid uri '" + uri_root.value + "'");
        }

        const js_subs = _mono_array_root_to_js_array(sub_root);

        const js_on_close = _wrap_delegate_root_as_function(on_close_root)!;

        const ws = new globalThis.WebSocket(js_uri, <any>js_subs) as WebSocketExtension;
        const { promise, promise_control: open_promise_control } = _create_cancelable_promise();

        ws[wasm_ws_pending_receive_event_queue] = new Queue();
        ws[wasm_ws_pending_receive_promise_queue] = new Queue();
        ws[wasm_ws_pending_open_promise] = open_promise_control;
        ws[wasm_ws_pending_send_promises] = [];
        ws[wasm_ws_pending_close_promises] = [];
        ws.binaryType = "arraybuffer";
        const local_on_open = () => {
            if (ws[wasm_ws_is_aborted]) return;
            open_promise_control.resolve(null);
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
            js_on_close(ev.code, ev.reason);

            // this reject would not do anything if there was already "open" before it.
            open_promise_control.reject(ev.reason);

            for (const close_promise_control of ws[wasm_ws_pending_close_promises]) {
                close_promise_control.resolve();
            }

            // send close to any pending receivers, to wake them
            const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];
            receive_promise_queue.drain((receive_promise_control) => {
                const response_root = receive_promise_control.response_root;
                Module.setValue(<any>response_root.value + 0, 0, "i32");// count
                Module.setValue(<any>response_root.value + 4, 2, "i32");// type:close
                Module.setValue(<any>response_root.value + 8, 1, "i32");// end_of_message: true
                receive_promise_control.resolve(null);
            });
        };
        ws.addEventListener("message", local_on_message);
        ws.addEventListener("open", local_on_open, { once: true });
        ws.addEventListener("close", local_on_close, { once: true });

        const ws_js_handle = mono_wasm_get_js_handle(ws);
        Module.setValue(web_socket_js_handle, <any>ws_js_handle, "i32");

        const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
        // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
        Module.setValue(thenable_js_handle, <any>then_js_handle, "i32");

        return task_ptr;
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
    finally {
        uri_root.release();
        sub_root.release();
        on_close_root.release();
    }
}

export function mono_wasm_web_socket_send(webSocket_js_handle: JSHandle, buffer_ptr: MonoObject, offset: number, length: number, message_type: number, end_of_message: boolean, thenable_js_handle: Int32Ptr, is_exception: Int32Ptr): MonoObject {
    const buffer_root = mono_wasm_new_root(buffer_ptr);
    try {
        const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
        if (!ws)
            throw new Error("ERR17: Invalid JS object handle " + webSocket_js_handle);

        if (ws.readyState != WebSocket.OPEN) {
            throw new Error("InvalidState: The WebSocket is not connected.");
        }

        const whole_buffer = _mono_wasm_web_socket_send_buffering(ws, buffer_root, offset, length, message_type, end_of_message);

        if (!end_of_message || !whole_buffer) {
            return MonoObjectNull; // we are done buffering synchronously, no promise
        }
        return _mono_wasm_web_socket_send_and_wait(ws, whole_buffer, thenable_js_handle);
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
    finally {
        buffer_root.release();
    }
}

export function mono_wasm_web_socket_receive(webSocket_js_handle: JSHandle, buffer_ptr: MonoObject, offset: number, length: number, response_ptr: MonoObject, thenable_js_handle: Int32Ptr, is_exception: Int32Ptr): MonoObject {
    const buffer_root = mono_wasm_new_root(buffer_ptr);
    const response_root = mono_wasm_new_root(response_ptr);
    const release_buffer = () => {
        buffer_root.release();
        response_root.release();
    };

    try {
        const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
        if (!ws)
            throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);
        const receive_event_queue = ws[wasm_ws_pending_receive_event_queue];
        const receive_promise_queue = ws[wasm_ws_pending_receive_promise_queue];

        const readyState = ws.readyState;
        if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
            throw new Error("InvalidState: The WebSocket is not connected.");
        }

        if (receive_event_queue.getLength()) {
            if (receive_promise_queue.getLength() != 0) {
                throw new Error("ERR20: Invalid WS state");// assert
            }
            // finish synchronously
            _mono_wasm_web_socket_receive_buffering(receive_event_queue, buffer_root, offset, length, response_root);
            release_buffer();

            Module.setValue(thenable_js_handle, 0, "i32");
            return MonoObjectNull;
        }
        const { promise, promise_control } = _create_cancelable_promise(release_buffer, release_buffer);
        const receive_promise_control = promise_control as ReceivePromiseControl;
        receive_promise_control.buffer_root = buffer_root;
        receive_promise_control.buffer_offset = offset;
        receive_promise_control.buffer_length = length;
        receive_promise_control.response_root = response_root;
        receive_promise_queue.enqueue(receive_promise_control);

        const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
        // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
        Module.setValue(thenable_js_handle, <any>then_js_handle, "i32");
        return task_ptr;
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
}

export function mono_wasm_web_socket_close(webSocket_js_handle: JSHandle, code: number, reason: MonoString, wait_for_close_received: boolean, thenable_js_handle: Int32Ptr, is_exception: Int32Ptr): MonoObject {
    const reason_root = mono_wasm_new_root(reason);
    try {
        const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle);
        if (!ws)
            throw new Error("ERR19: Invalid JS object handle " + webSocket_js_handle);

        if (ws.readyState == WebSocket.CLOSED) {
            return MonoObjectNull;// no promise
        }

        const js_reason = conv_string(reason_root.value);

        if (wait_for_close_received) {
            const { promise, promise_control } = _create_cancelable_promise();
            ws[wasm_ws_pending_close_promises].push(promise_control);

            if (js_reason) {
                ws.close(code, js_reason);
            } else {
                ws.close(code);
            }

            const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
            // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
            Module.setValue(thenable_js_handle, <any>then_js_handle, "i32");

            return task_ptr;
        }
        else {
            if (!mono_wasm_web_socket_close_warning) {
                mono_wasm_web_socket_close_warning = true;
                console.warn("WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.");
            }
            if (js_reason) {
                ws.close(code, js_reason);
            } else {
                ws.close(code);
            }
            Module.setValue(thenable_js_handle, 0, "i32");
            return MonoObjectNull;// no promise
        }
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
    finally {
        reason_root.release();
    }
}

export function mono_wasm_web_socket_abort(webSocket_js_handle: JSHandle, is_exception: Int32Ptr): MonoObject {
    try {
        const ws = mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle) as WebSocketExtension;
        if (!ws)
            throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);

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

        return MonoObjectNull;
    }
    catch (ex) {
        return wrap_error(is_exception, ex);
    }
}

function _mono_wasm_web_socket_send_and_wait(ws: WebSocketExtension, buffer: Uint8Array | string, thenable_js_handle: Int32Ptr): MonoObject {
    // send and return promise
    ws.send(buffer);
    ws[wasm_ws_pending_send_buffer] = null;

    // if the remaining send buffer is small, we don't block so that the throughput doesn't suffer. 
    // Otherwise we block so that we apply some backpresure to the application sending large data.
    // this is different from Managed implementation
    if (ws.bufferedAmount < ws_send_buffer_blocking_threshold) {
        return MonoObjectNull; // no promise
    }

    // block the promise/task until the browser passed the buffer to OS
    const { promise, promise_control } = _create_cancelable_promise();
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

    const { task_ptr, then_js_handle } = _wrap_js_thenable_as_task(promise);
    // task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
    Module.setValue(thenable_js_handle, <any>then_js_handle, "i32");

    return task_ptr;
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
            promise_control.buffer_root, promise_control.buffer_offset, promise_control.buffer_length,
            promise_control.response_root);
        promise_control.resolve(null);
    }
    prevent_timer_throttling();
}

function _mono_wasm_web_socket_receive_buffering(event_queue: Queue<any>, buffer_root: WasmRoot<MonoObject>, buffer_offset: number, buffer_length: number, response_root: WasmRoot<MonoObject>) {
    const event = event_queue.peek();

    const count = Math.min(buffer_length, event.data.length - event.offset);
    if (count > 0) {
        const targetView = Module.HEAPU8.subarray(<any>buffer_root.value + buffer_offset, <any>buffer_root.value + buffer_offset + buffer_length);
        const sourceView = event.data.subarray(event.offset, event.offset + count);
        targetView.set(sourceView, 0);
        event.offset += count;
    }
    const end_of_message = event.data.length === event.offset ? 1 : 0;
    if (end_of_message) {
        event_queue.dequeue();
    }
    Module.setValue(<any>response_root.value + 0, count, "i32");
    Module.setValue(<any>response_root.value + 4, event.type, "i32");
    Module.setValue(<any>response_root.value + 8, end_of_message, "i32");
}

function _mono_wasm_web_socket_send_buffering(ws: WebSocketExtension, buffer_root: WasmRoot<MonoObject>, buffer_offset: number, length: number, message_type: number, end_of_message: boolean): Uint8Array | string | null {
    let buffer = ws[wasm_ws_pending_send_buffer];
    let offset = 0;
    const message_ptr = <any>buffer_root.value + buffer_offset;

    if (buffer) {
        offset = ws[wasm_ws_pending_send_buffer_offset];
        // match desktop WebSocket behavior by copying message_type of the first part
        message_type = ws[wasm_ws_pending_send_buffer_type];
        // if not empty message, append to existing buffer
        if (length !== 0) {
            const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
            if (offset + length > buffer.length) {
                const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
                newbuffer.set(buffer, 0);// copy previous buffer
                newbuffer.set(view, offset);// append copy at the end
                ws[wasm_ws_pending_send_buffer] = buffer = newbuffer;
            }
            else {
                buffer.set(view, offset);// append copy at the end
            }
            offset += length;
            ws[wasm_ws_pending_send_buffer_offset] = offset;
        }
    }
    else if (!end_of_message) {
        // create new buffer
        if (length !== 0) {
            const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
            buffer = new Uint8Array(view); // copy
            offset = length;
            ws[wasm_ws_pending_send_buffer_offset] = offset;
            ws[wasm_ws_pending_send_buffer] = buffer;
        }
        ws[wasm_ws_pending_send_buffer_type] = message_type;
    }
    else {
        // use the buffer only localy
        if (length !== 0) {
            const memoryView = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
            buffer = memoryView; // send will make a copy
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
    response_root: WasmRoot<MonoObject>
    buffer_root: WasmRoot<MonoObject>
    buffer_offset: number
    buffer_length: number
}

type Message = {
    type: number,// WebSocketMessageType
    data: Uint8Array,
    offset: number
}
