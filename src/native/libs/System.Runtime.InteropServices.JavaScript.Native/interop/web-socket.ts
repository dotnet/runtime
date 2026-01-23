// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { EmscriptenModuleInternal, PromiseCompletionSource, VoidPtr } from "./types";

import { preventTimerThrottling } from "./scheduling";
import { Queue } from "./queue";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL } from "./per-module";
import { assertJsInterop, utf8ToStringRelaxed } from "./utils";
import { fixupPointer } from "./utils";
import { dotnetApi, dotnetAssert, dotnetBrowserUtilsExports, dotnetLoaderExports, dotnetLogger } from "./cross-module";
import { wrapAsCancelable } from "./cancelable-promise";

const wasmWsPendingSendBuffer = Symbol.for("wasm ws_pending_send_buffer");
const wasmWsPendingSendBufferOffset = Symbol.for("wasm ws_pending_send_buffer_offset");
const wasmWsPendingSendBufferType = Symbol.for("wasm ws_pending_send_buffer_type");
const wasmWsPendingReceiveEventQueue = Symbol.for("wasm ws_pending_receive_event_queue");
const wasmWsPendingReceivePromiseQueue = Symbol.for("wasm ws_pending_receive_promise_queue");
const wasmWsPendingOpenPromise = Symbol.for("wasm ws_pending_open_promise");
const wasmWsPendingOpenPromiseUsed = Symbol.for("wasm wasm_ws_pending_open_promise_used");
const wasmWsPendingError = Symbol.for("wasm wasm_ws_pending_error");
const wasmWsPendingClosePromises = Symbol.for("wasm ws_pending_close_promises");
const wasmWsPendingSendPromises = Symbol.for("wasm ws_pending_send_promises");
const wasmWsIsAborted = Symbol.for("wasm ws_is_aborted");
const wasmWsCloseSent = Symbol.for("wasm wasm_ws_close_sent");
const wasmWsCloseReceived = Symbol.for("wasm wasm_ws_close_received");
const wasmWsReceiveStatusPtr = Symbol.for("wasm ws_receive_status_ptr");

const wsSendBufferBlockingThreshold = 65536;
const emptyBuffer = new Uint8Array();

export function wsGetState(ws: WebSocketExtension): number {
    if (ws.readyState != WebSocket.CLOSED) {
        return ws.readyState ?? -1;
    }
    const receiveEventQueue = ws[wasmWsPendingReceiveEventQueue];
    const queuedEventsCount = receiveEventQueue.getLength();
    if (queuedEventsCount == 0) {
        return ws.readyState ?? -1;
    }
    return ws[wasmWsCloseSent] ? WebSocket.CLOSING : WebSocket.OPEN;
}

export function wsCreate(uri: string, subProtocols: string[] | null, receiveStatusPtr: VoidPtr): WebSocketExtension {
    verifyEnvironment();
    assertJsInterop();
    dotnetAssert.fastCheck(uri && typeof uri === "string", () => `ERR12: Invalid uri ${typeof uri}`);
    let ws: WebSocketExtension;
    try {
        ws = new globalThis.WebSocket(uri, subProtocols || undefined) as WebSocketExtension;
    } catch (error: any) {
        dotnetLogger.warn("WebSocket error in ws_wasm_create: " + error.toString());
        throw error;
    }
    const openPromiseControl = dotnetLoaderExports.createPromiseCompletionSource<WebSocketExtension>();

    ws[wasmWsPendingReceiveEventQueue] = new Queue();
    ws[wasmWsPendingReceivePromiseQueue] = new Queue();
    ws[wasmWsPendingOpenPromise] = openPromiseControl;
    ws[wasmWsPendingSendPromises] = [];
    ws[wasmWsPendingClosePromises] = [];
    ws[wasmWsReceiveStatusPtr] = fixupPointer(receiveStatusPtr, 0);
    ws.binaryType = "arraybuffer";
    const localOnOpen = () => {
        try {
            if (ws[wasmWsIsAborted]) return;
            if (!dotnetLoaderExports.isRuntimeRunning()) return;
            openPromiseControl.resolve(ws);
            preventTimerThrottling();
        } catch (error: any) {
            dotnetLogger.warn("failed to propagate WebSocket open event: " + error.toString());
        }
    };
    const localOnMessage = (ev: MessageEvent) => {
        try {
            if (ws[wasmWsIsAborted]) return;
            if (!dotnetLoaderExports.isRuntimeRunning()) return;
            webSocketOnMessage(ws, ev);
            preventTimerThrottling();
        } catch (error: any) {
            dotnetLogger.warn("failed to propagate WebSocket message event: " + error.toString());
        }
    };
    const localOnClose = (ev: CloseEvent) => {
        try {
            ws.removeEventListener("message", localOnMessage);
            if (ws[wasmWsIsAborted]) return;
            if (!dotnetLoaderExports.isRuntimeRunning()) return;

            ws[wasmWsCloseReceived] = true;
            // do not mangle names, maps to BrowserWebSockets\BrowserInterop.cs
            ws["closeStatus"] = ev.code;
            ws["closeStatusDescription"] = ev.reason;

            if (ws[wasmWsPendingOpenPromiseUsed]) {
                openPromiseControl.reject(new Error(ev.reason));
            }

            for (const closePromiseControl of ws[wasmWsPendingClosePromises]) {
                closePromiseControl.resolve();
            }

            (dotnetApi.Module as EmscriptenModuleInternal).safeSetTimeout(() => {
                const receivePromiseQueue = ws[wasmWsPendingReceivePromiseQueue];
                receivePromiseQueue.drain((receivePromiseControl: ReceivePromiseControl) => {
                    dotnetApi.setHeapI32(receiveStatusPtr, 0); // count
                    dotnetApi.setHeapI32(<any>receiveStatusPtr + 4, 2); // type:close
                    dotnetApi.setHeapI32(<any>receiveStatusPtr + 8, 1); // end_of_message: true
                    receivePromiseControl.resolve();
                });
            }, 0);
        } catch (error: any) {
            dotnetLogger.warn("failed to propagate WebSocket close event: " + error.toString());
        }
    };
    const localOnError = (ev: any) => {
        try {
            if (ws[wasmWsIsAborted]) return;
            if (!dotnetLoaderExports.isRuntimeRunning()) return;
            ws.removeEventListener("message", localOnMessage);
            const message = ev.message
                ? "WebSocket error: " + ev.message
                : "WebSocket error";
            dotnetLogger.warn(message);
            ws[wasmWsPendingError] = message;
            rejectPromises(ws, new Error(message));
        } catch (error: any) {
            dotnetLogger.warn("failed to propagate WebSocket error event: " + error.toString());
        }
    };
    ws.addEventListener("message", localOnMessage);
    ws.addEventListener("open", localOnOpen, { once: true });
    ws.addEventListener("close", localOnClose, { once: true });
    ws.addEventListener("error", localOnError, { once: true });
    ws.dispose = () => {
        ws.removeEventListener("message", localOnMessage);
        ws.removeEventListener("open", localOnOpen);
        ws.removeEventListener("close", localOnClose);
        ws.removeEventListener("error", localOnError);
        wsAbort(ws);
    };

    return ws;
}

export function wsOpen(ws: WebSocketExtension): Promise<WebSocketExtension> | null {
    dotnetAssert.check(!!ws, "ERR17: expected ws instance");
    if (ws[wasmWsPendingError]) {
        return rejectedPromise(ws[wasmWsPendingError]);
    }
    const openPromiseControl = ws[wasmWsPendingOpenPromise];
    ws[wasmWsPendingOpenPromiseUsed] = true;
    return openPromiseControl.promise;
}

export function wsSend(ws: WebSocketExtension, bufferPtr: VoidPtr, bufferLength: number, messageType: number, endOfMessage: boolean): Promise<void> | null {
    dotnetAssert.check(!!ws, "ERR17: expected ws instance");

    if (ws[wasmWsPendingError]) {
        return rejectedPromise(ws[wasmWsPendingError]);
    }
    if (ws[wasmWsIsAborted] || ws[wasmWsCloseSent]) {
        return rejectedPromise("InvalidState: The WebSocket is not connected.");
    }
    if (ws.readyState == WebSocket.CLOSED) {
        // this is server initiated close but not partial close
        // because CloseOutputAsync_ServerInitiated_CanSend expectations, we don't fail here
        return resolvedPromise();
    }

    const bufferView = new Uint8Array(dotnetApi.localHeapViewU8().buffer, fixupPointer(bufferPtr, 0), bufferLength);
    const wholeBuffer = webSocketSendBuffering(ws, bufferView, messageType, endOfMessage);

    if (!endOfMessage || !wholeBuffer) {
        return resolvedPromise();
    }

    return webSocketSendAndWait(ws, wholeBuffer);
}

export function wsReceive(ws: WebSocketExtension, bufferPtr: VoidPtr, bufferLength: number): Promise<void> | null {
    dotnetAssert.check(!!ws, "ERR18: expected ws instance");

    if (ws[wasmWsPendingError]) {
        return rejectedPromise(ws[wasmWsPendingError]);
    }

    if (ws[wasmWsIsAborted]) {
        const receiveStatusPtr = ws[wasmWsReceiveStatusPtr];
        dotnetApi.setHeapI32(receiveStatusPtr, 0);
        dotnetApi.setHeapI32(<any>receiveStatusPtr + 4, 2);
        dotnetApi.setHeapI32(<any>receiveStatusPtr + 8, 1);
        return resolvedPromise();
    }

    const receiveEventQueue = ws[wasmWsPendingReceiveEventQueue];
    const receivePromiseQueue = ws[wasmWsPendingReceivePromiseQueue];

    if (receiveEventQueue.getLength()) {
        dotnetAssert.check(receivePromiseQueue.getLength() == 0, "ERR20: Invalid WS state");

        webSocketReceiveBuffering(ws, receiveEventQueue, bufferPtr, bufferLength);

        return resolvedPromise();
    }

    if (ws[wasmWsCloseReceived]) {
        const receiveStatusPtr = ws[wasmWsReceiveStatusPtr];
        dotnetApi.setHeapI32(receiveStatusPtr, 0); // count
        dotnetApi.setHeapI32(<any>receiveStatusPtr + 4, 2); // type:close
        dotnetApi.setHeapI32(<any>receiveStatusPtr + 8, 1); // end_of_message: true
        return resolvedPromise();
    }

    const pcs = dotnetLoaderExports.createPromiseCompletionSource<void>();
    const receivePromiseControl = pcs as ReceivePromiseControl;
    receivePromiseControl.bufferPtr = fixupPointer(bufferPtr, 0);
    receivePromiseControl.bufferLength = bufferLength;
    receivePromiseQueue.enqueue(receivePromiseControl);

    return pcs.promise;
}

export function wsClose(ws: WebSocketExtension, code: number, reason: string | null, waitForCloseReceived: boolean): Promise<void> | null {
    dotnetAssert.check(!!ws, "ERR19: expected ws instance");

    if (ws[wasmWsIsAborted] || ws[wasmWsCloseSent] || ws.readyState == WebSocket.CLOSED) {
        return resolvedPromise();
    }
    if (ws[wasmWsPendingError]) {
        return rejectedPromise(ws[wasmWsPendingError]);
    }
    ws[wasmWsCloseSent] = true;
    if (waitForCloseReceived) {
        const pcs = dotnetLoaderExports.createPromiseCompletionSource<void>();
        ws[wasmWsPendingClosePromises].push(pcs);

        if (typeof reason === "string") {
            ws.close(code, reason);
        } else {
            ws.close(code);
        }
        return pcs.promise;
    } else {
        if (typeof reason === "string") {
            ws.close(code, reason);
        } else {
            ws.close(code);
        }
        return resolvedPromise();
    }
}

export function wsAbort(ws: WebSocketExtension): void {
    dotnetAssert.check(!!ws, "ERR18: expected ws instance");

    if (ws[wasmWsIsAborted] || ws[wasmWsCloseSent]) {
        return;
    }

    ws[wasmWsIsAborted] = true;
    rejectPromises(ws, new Error("OperationCanceledException"));

    try {
        // this is different from Managed implementation
        ws.close(1000, "Connection was aborted.");
    } catch (error: any) {
        dotnetLogger.warn("WebSocket error in ws_wasm_abort: " + error.toString());
    }
}

function rejectPromises(ws: WebSocketExtension, error: Error) {
    const openPromiseControl = ws[wasmWsPendingOpenPromise];
    const openPromiseUsed = ws[wasmWsPendingOpenPromiseUsed];

    // when `open_promise_used` is false, we should not reject it,
    // because it would be unhandled rejection. Nobody is subscribed yet.
    // The subscription comes on the next call, which is `ws_wasm_open`, but cancelation/abort could happen in the meantime.
    if (openPromiseControl && openPromiseUsed) {
        openPromiseControl.reject(error);
    }
    for (const closePromiseControl of ws[wasmWsPendingClosePromises]) {
        closePromiseControl.reject(error);
    }
    for (const sendPromiseControl of ws[wasmWsPendingSendPromises]) {
        sendPromiseControl.reject(error);
    }

    ws[wasmWsPendingReceivePromiseQueue].drain((receivePromiseControl: ReceivePromiseControl) => {
        receivePromiseControl.reject(error);
    });
}

// send and return promise
function webSocketSendAndWait(ws: WebSocketExtension, bufferView: Uint8Array | string): Promise<void> | null {
    ws.send(bufferView);
    ws[wasmWsPendingSendBuffer] = null;

    // if the remaining send buffer is small, we don't block so that the throughput doesn't suffer.
    // Otherwise we block so that we apply some backpresure to the application sending large data.
    // this is different from Managed implementation
    if (ws.bufferedAmount < wsSendBufferBlockingThreshold) {
        return resolvedPromise();
    }

    // block the promise/task until the browser passed the buffer to OS
    const pcs = dotnetLoaderExports.createPromiseCompletionSource<void>();
    const pending = ws[wasmWsPendingSendPromises];
    pending.push(pcs);

    let nextDelay = 1;
    const pollingCheck = () => {
        try {
            if (ws.bufferedAmount === 0) {
                pcs.resolve();
            } else {
                const readyState = ws.readyState;
                if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
                    // only reject if the data were not sent
                    // bufferedAmount does not reset to zero once the connection closes
                    pcs.reject(new Error(`InvalidState: ${readyState} The WebSocket is not connected.`));
                } else if (!pcs.isDone) {
                    globalThis.setTimeout(pollingCheck, nextDelay);
                    // exponentially longer delays, up to 1000ms
                    nextDelay = Math.min(nextDelay * 1.5, 1000);
                    return;
                }
            }
            // remove from pending
            const index = pending.indexOf(pcs);
            if (index > -1) {
                pending.splice(index, 1);
            }
        } catch (error: any) {
            dotnetLogger.warn("WebSocket error in webSocketSendAndWait: " + error.toString());
            pcs.reject(error);
        }
    };

    globalThis.setTimeout(pollingCheck, 0);

    return pcs.promise;
}

function webSocketOnMessage(ws: WebSocketExtension, event: MessageEvent) {
    const eventQueue = ws[wasmWsPendingReceiveEventQueue];
    const promiseQueue = ws[wasmWsPendingReceivePromiseQueue];

    if (typeof event.data === "string") {
        eventQueue.enqueue({
            type: 0, // WebSocketMessageType.Text
            // according to the spec https://encoding.spec.whatwg.org/
            // - Unpaired surrogates will get replaced with 0xFFFD
            // - utf8 encode specifically is defined to never throw
            data: dotnetBrowserUtilsExports.stringToUTF8(event.data),
            offset: 0
        });
    } else {
        if (event.data.constructor.name !== "ArrayBuffer") {
            throw new Error("ERR22: WebSocket receive expected ArrayBuffer");
        }
        eventQueue.enqueue({
            type: 1, // WebSocketMessageType.Binary
            data: new Uint8Array(event.data),
            offset: 0
        });
    }
    if (promiseQueue.getLength() && eventQueue.getLength() > 1) {
        throw new Error("ERR21: Invalid WS state");// assert
    }
    while (promiseQueue.getLength() && eventQueue.getLength()) {
        const promiseControl = promiseQueue.dequeue()!;
        webSocketReceiveBuffering(ws, eventQueue, promiseControl.bufferPtr, promiseControl.bufferLength);
        promiseControl.resolve();
    }
    preventTimerThrottling();
}

function webSocketReceiveBuffering(ws: WebSocketExtension, eventQueue: Queue<any>, bufferPtr: VoidPtr, bufferLength: number) {
    const event = eventQueue.peek();

    const count = Math.min(bufferLength, event.data.length - event.offset);
    if (count > 0) {
        const sourceView = event.data.subarray(event.offset, event.offset + count);
        const bufferView = new Uint8Array(dotnetApi.localHeapViewU8().buffer, fixupPointer(bufferPtr, 0), bufferLength);
        bufferView.set(sourceView, 0);
        event.offset += count;
    }
    const endOfMessage = event.data.length === event.offset ? 1 : 0;
    if (endOfMessage) {
        eventQueue.dequeue();
    }
    const responsePtr = ws[wasmWsReceiveStatusPtr];
    dotnetApi.setHeapI32(responsePtr, count);
    dotnetApi.setHeapI32(<any>responsePtr + 4, event.type);
    dotnetApi.setHeapI32(<any>responsePtr + 8, endOfMessage);
}

function webSocketSendBuffering(ws: WebSocketExtension, bufferView: Uint8Array, messageType: number, endOfMessage: boolean): Uint8Array | string | null {
    let buffer = ws[wasmWsPendingSendBuffer];
    let offset = 0;
    const length = bufferView.byteLength;

    if (buffer) {
        offset = ws[wasmWsPendingSendBufferOffset];
        messageType = ws[wasmWsPendingSendBufferType];
        if (length !== 0) {
            if (offset + length > buffer.length) {
                const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
                newbuffer.set(buffer, 0);// copy previous buffer
                newbuffer.subarray(offset).set(bufferView);// append copy at the end
                ws[wasmWsPendingSendBuffer] = buffer = newbuffer;
            } else {
                buffer.subarray(offset).set(bufferView);// append copy at the end
            }
            offset += length;
            ws[wasmWsPendingSendBufferOffset] = offset;
        }
    } else if (!endOfMessage) {
        if (length !== 0) {
            buffer = <Uint8Array>bufferView.slice(); // copy
            offset = length;
            ws[wasmWsPendingSendBufferOffset] = offset;
            ws[wasmWsPendingSendBuffer] = buffer;
        }
        ws[wasmWsPendingSendBufferType] = messageType;
    } else {
        if (length !== 0) {
            // TODO-WASM: copy, because the provided ArrayBufferView value must not be shared in MT.
            buffer = bufferView;
            offset = length;
        }
    }
    if (endOfMessage) {
        if (offset == 0 || buffer == null) {
            return emptyBuffer;
        }
        if (messageType === 0) {
            // text, convert from UTF-8 bytes to string, because of bad browser API
            const bytes = buffer.subarray(0, offset >>> 0);
            // we do not validate outgoing data https://github.com/dotnet/runtime/issues/59214
            return utf8ToStringRelaxed(bytes);
        } else {
            // binary, view to used part of the buffer
            return buffer.subarray(0, offset);
        }
    }
    return null;
}

type WebSocketExtension = WebSocket & {
    [wasmWsPendingReceiveEventQueue]: Queue<Message>;
    [wasmWsPendingReceivePromiseQueue]: Queue<ReceivePromiseControl>;
    [wasmWsPendingOpenPromise]: PromiseCompletionSource<WebSocketExtension>;
    [wasmWsPendingOpenPromiseUsed]: boolean;
    [wasmWsPendingSendPromises]: PromiseCompletionSource<void>[];
    [wasmWsPendingClosePromises]: PromiseCompletionSource<void>[];
    [wasmWsPendingError]: string | undefined;
    [wasmWsIsAborted]: boolean;
    [wasmWsCloseReceived]: boolean;
    [wasmWsCloseSent]: boolean;
    [wasmWsReceiveStatusPtr]: VoidPtr;
    [wasmWsPendingSendBufferOffset]: number;
    [wasmWsPendingSendBufferType]: number;
    [wasmWsPendingSendBuffer]: Uint8Array | null;
    closeStatus: number | undefined;
    closeStatusDescription: string | undefined;
    dispose(): void;
};

type ReceivePromiseControl = PromiseCompletionSource<void> & {
    bufferPtr: VoidPtr;
    bufferLength: number;
}

type Message = {
    type: number, // WebSocketMessageType
    data: Uint8Array,
    offset: number
}

function resolvedPromise(): Promise<void> | null {
    // signal that we are finished synchronously
    // this is optimization, which doesn't allocate and doesn't require to marshal resolve() call to C# side.
    return null;
}

function rejectedPromise(message: string): Promise<any> | null {
    const resolved = Promise.reject(new Error(message));
    return wrapAsCancelable<void>(resolved);
}

function verifyEnvironment() {
    if (ENVIRONMENT_IS_SHELL) {
        throw new Error("WebSockets are not supported in shell JS engine.");
    }
    if (typeof globalThis.WebSocket !== "function") {
        const message = ENVIRONMENT_IS_NODE
            ? "Please install `ws` npm package to enable networking support. See also https://aka.ms/dotnet-wasm-features"
            : "This browser doesn't support WebSocket API. Please use a modern browser. See also https://aka.ms/dotnet-wasm-features";
        throw new Error(message);
    }
}
