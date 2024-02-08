// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { wrap_as_cancelable_promise } from "./cancelable-promise";
import { ENVIRONMENT_IS_NODE, Module, loaderHelpers, mono_assert } from "./globals";
import { assert_js_interop } from "./invoke-js";
import { MemoryViewType, Span } from "./marshal";
import type { VoidPtr } from "./types/emscripten";
import { ControllablePromise } from "./types/internal";


function verifyEnvironment() {
    if (typeof globalThis.fetch !== "function" || typeof globalThis.AbortController !== "function") {
        const message = ENVIRONMENT_IS_NODE
            ? "Please install `node-fetch` and `node-abort-controller` npm packages to enable HTTP client support. See also https://aka.ms/dotnet-wasm-features"
            : "This browser doesn't support fetch API. Please use a modern browser. See also https://aka.ms/dotnet-wasm-features";
        throw new Error(message);
    }
}

function commonAsserts(controller: HttpController) {
    assert_js_interop();
    mono_assert(controller, "expected controller");
}

export function http_wasm_supports_streaming_request(): boolean {
    // Detecting streaming request support works like this:
    // If the browser doesn't support a particular body type, it calls toString() on the object and uses the result as the body.
    // So, if the browser doesn't support request streams, the request body becomes the string "[object ReadableStream]".
    // When a string is used as a body, it conveniently sets the Content-Type header to text/plain;charset=UTF-8.
    // So, if that header is set, then we know the browser doesn't support streams in request objects, and we can exit early.
    // Safari does support streams in request objects, but doesn't allow them to be used with fetch, so the duplex option is tested, which Safari doesn't currently support.
    // See https://developer.chrome.com/articles/fetch-streaming-requests/
    if (typeof Request !== "undefined" && "body" in Request.prototype && typeof ReadableStream === "function" && typeof TransformStream === "function") {
        let duplexAccessed = false;
        const hasContentType = new Request("", {
            body: new ReadableStream(),
            method: "POST",
            get duplex() {
                duplexAccessed = true;
                return "half";
            },
        } as RequestInit /* https://github.com/microsoft/TypeScript-DOM-lib-generator/issues/1483 */).headers.has("Content-Type");
        return duplexAccessed && !hasContentType;
    }
    return false;
}

export function http_wasm_supports_streaming_response(): boolean {
    return typeof Response !== "undefined" && "body" in Response.prototype && typeof ReadableStream === "function";
}

export function http_wasm_create_controller(): HttpController {
    verifyEnvironment();
    assert_js_interop();
    const controller: HttpController = {
        abortController: new AbortController()
    };
    return controller;
}

export function http_wasm_abort_request(controller: HttpController): void {
    try {
        if (controller.streamWriter) {
            controller.streamWriter.abort();
        }
    }
    catch (err) {
        // ignore
    }
    http_wasm_abort_response(controller);
}

export function http_wasm_abort_response(controller: HttpController): void {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    try {
        if (controller.streamReader) {
            controller.streamReader.cancel().catch((err) => {
                if (err && err.name !== "AbortError") {
                    Module.err("Error in http_wasm_abort_response: " + err);
                }
                // otherwise, it's expected
            });
        }
        controller.abortController.abort();
    }
    catch (err) {
        // ignore
    }
}

export function http_wasm_transform_stream_write(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    mono_assert(bufferLength > 0, "expected bufferLength > 0");
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return wrap_as_cancelable_promise(async () => {
        mono_assert(controller.streamWriter, "expected streamWriter");
        mono_assert(controller.responsePromise, "expected fetch promise");
        // race with fetch because fetch does not cancel the ReadableStream see https://bugs.chromium.org/p/chromium/issues/detail?id=1480250
        await Promise.race([controller.streamWriter.ready, controller.responsePromise]);
        await Promise.race([controller.streamWriter.write(copy), controller.responsePromise]);
    });
}

export function http_wasm_transform_stream_close(controller: HttpController): ControllablePromise<void> {
    mono_assert(controller, "expected controller");
    return wrap_as_cancelable_promise(async () => {
        mono_assert(controller.streamWriter, "expected streamWriter");
        mono_assert(controller.responsePromise, "expected fetch promise");
        // race with fetch because fetch does not cancel the ReadableStream see https://bugs.chromium.org/p/chromium/issues/detail?id=1480250
        await Promise.race([controller.streamWriter.ready, controller.responsePromise]);
        await Promise.race([controller.streamWriter.close(), controller.responsePromise]);
    });
}

export function http_wasm_fetch_stream(controller: HttpController, url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[]): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    const transformStream = new TransformStream<Uint8Array, Uint8Array>();
    controller.streamWriter = transformStream.writable.getWriter();
    const fetch_promise = http_wasm_fetch(controller, url, header_names, header_values, option_names, option_values, transformStream.readable);
    return fetch_promise;
}

export function http_wasm_fetch_bytes(controller: HttpController, url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[], bodyPtr: VoidPtr, bodyLength: number): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    // the bodyPtr is pinned by the caller
    const view = new Span(bodyPtr, bodyLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return http_wasm_fetch(controller, url, header_names, header_values, option_names, option_values, copy);
}

export function http_wasm_fetch(controller: HttpController, url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[], body: Uint8Array | ReadableStream | null): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    verifyEnvironment();
    assert_js_interop();
    mono_assert(url && typeof url === "string", "expected url string");
    mono_assert(header_names && header_values && Array.isArray(header_names) && Array.isArray(header_values) && header_names.length === header_values.length, "expected headerNames and headerValues arrays");
    mono_assert(option_names && option_values && Array.isArray(option_names) && Array.isArray(option_values) && option_names.length === option_values.length, "expected headerNames and headerValues arrays");

    const headers = new Headers();
    for (let i = 0; i < header_names.length; i++) {
        headers.append(header_names[i], header_values[i]);
    }
    const options: any = {
        body,
        headers,
        signal: controller.abortController.signal
    };
    if (typeof ReadableStream !== "undefined" && body instanceof ReadableStream) {
        options.duplex = "half";
    }
    for (let i = 0; i < option_names.length; i++) {
        options[option_names[i]] = option_values[i];
    }
    // make the fetch cancellable
    controller.responsePromise = wrap_as_cancelable_promise(() => {
        return loaderHelpers.fetch_like(url, options);
    });
    // avoid processing headers if the fetch is canceled
    controller.responsePromise.then((res: Response) => {
        controller.response = res;
        controller.responseHeaderNames = [];
        controller.responseHeaderValues = [];
        if (res.headers && (<any>res.headers).entries) {
            const entries: Iterable<string[]> = (<any>res.headers).entries();

            for (const pair of entries) {
                controller.responseHeaderNames.push(pair[0]);
                controller.responseHeaderValues.push(pair[1]);
            }
        }
    }).catch(() => {
        // ignore
    });
    return controller.responsePromise;
}

export function http_wasm_get_response_type(controller: HttpController): string | undefined {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.response?.type;
}

export function http_wasm_get_response_status(controller: HttpController): number {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.response?.status ?? 0;
}


export function http_wasm_get_response_header_names(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    mono_assert(controller.responseHeaderNames, "expected responseHeaderNames");
    return controller.responseHeaderNames;
}

export function http_wasm_get_response_header_values(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    mono_assert(controller.responseHeaderValues, "expected responseHeaderValues");
    return controller.responseHeaderValues;
}

export function http_wasm_get_response_length(controller: HttpController): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return wrap_as_cancelable_promise(async () => {
        const buffer = await controller.response!.arrayBuffer();
        controller.responseBuffer = buffer;
        controller.currentBufferOffset = 0;
        return buffer.byteLength;
    });
}

export function http_wasm_get_response_bytes(controller: HttpController, view: Span): number {
    mono_assert(controller, "expected controller");
    mono_assert(controller.responseBuffer, "expected resoved arrayBuffer");
    mono_assert(controller.currentBufferOffset != undefined, "expected currentBufferOffset");
    if (controller.currentBufferOffset == controller.responseBuffer!.byteLength) {
        return 0;
    }
    const source_view = new Uint8Array(controller.responseBuffer!, controller.currentBufferOffset);
    view.set(source_view, 0);
    const bytes_read = Math.min(view.byteLength, source_view.byteLength);
    controller.currentBufferOffset += bytes_read;
    return bytes_read;
}

export function http_wasm_get_streamed_response_bytes(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    return wrap_as_cancelable_promise(async () => {
        mono_assert(controller.response, "expected response");
        if (!controller.streamReader) {
            controller.streamReader = controller.response.body!.getReader();
        }
        if (!controller.currentStreamReaderChunk || controller.currentBufferOffset === undefined) {
            controller.currentStreamReaderChunk = await controller.streamReader.read();
            controller.currentBufferOffset = 0;
        }
        if (controller.currentStreamReaderChunk.done) {
            return 0;
        }

        const remaining_source = controller.currentStreamReaderChunk.value.byteLength - controller.currentBufferOffset;
        mono_assert(remaining_source > 0, "expected remaining_source to be greater than 0");

        const bytes_copied = Math.min(remaining_source, view.byteLength);
        const source_view = controller.currentStreamReaderChunk.value.subarray(controller.currentBufferOffset, controller.currentBufferOffset + bytes_copied);
        view.set(source_view, 0);
        controller.currentBufferOffset += bytes_copied;
        if (remaining_source == bytes_copied) {
            controller.currentStreamReaderChunk = undefined;
        }

        return bytes_copied;
    });
}

interface HttpController {
    abortController: AbortController

    // streaming request
    streamReader?: ReadableStreamDefaultReader<Uint8Array>

    // response
    responsePromise?: ControllablePromise<any>
    response?: Response
    responseHeaderNames?: string[];
    responseHeaderValues?: string[];
    currentBufferOffset?: number

    // non-streaming response
    responseBuffer?: ArrayBuffer

    // streaming response
    streamWriter?: WritableStreamDefaultWriter<Uint8Array>
    currentStreamReaderChunk?: ReadableStreamReadResult<Uint8Array>
}
