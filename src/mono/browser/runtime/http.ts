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
    const controller: Partial<HttpController> = {
        __abort_controller: new AbortController()
    };
    return controller as HttpController;
}

export function http_wasm_abort_request(controller: HttpController): void {
    if (controller.__writer) {
        controller.__writer.abort();
    }
    http_wasm_abort_response(controller);
}

export function http_wasm_abort_response(controller: HttpController): void {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    controller.__abort_controller.abort();
    if (controller.__reader) {
        controller.__reader.cancel().catch((err) => {
            if (err && err.name !== "AbortError") {
                Module.err("Error in http_wasm_abort_response: " + err);
            }
            // otherwise, it's expected
        });
    }
}

export function http_wasm_transform_stream_write(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    mono_assert(bufferLength > 0, "expected bufferLength > 0");
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return wrap_as_cancelable_promise(async () => {
        mono_assert(controller.__fetch_promise, "expected fetch promise");
        // race with fetch because fetch does not cancel the ReadableStream see https://bugs.chromium.org/p/chromium/issues/detail?id=1480250
        await Promise.race([controller.__writer.ready, controller.__fetch_promise]);
        await Promise.race([controller.__writer.write(copy), controller.__fetch_promise]);
    });
}

export function http_wasm_transform_stream_close(controller: HttpController): ControllablePromise<void> {
    mono_assert(controller, "expected controller");
    return wrap_as_cancelable_promise(async () => {
        mono_assert(controller.__fetch_promise, "expected fetch promise");
        // race with fetch because fetch does not cancel the ReadableStream see https://bugs.chromium.org/p/chromium/issues/detail?id=1480250
        await Promise.race([controller.__writer.ready, controller.__fetch_promise]);
        await Promise.race([controller.__writer.close(), controller.__fetch_promise]);
    });
}

export function http_wasm_fetch_stream(controller: HttpController, url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[]): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    controller.__transform_stream = new TransformStream<Uint8Array, Uint8Array>();
    controller.__writer = controller.__transform_stream.writable.getWriter();
    const fetch_promise = http_wasm_fetch(controller, url, header_names, header_values, option_names, option_values, controller.__transform_stream.readable);
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
        signal: controller.__abort_controller.signal
    };
    if (typeof ReadableStream !== "undefined" && body instanceof ReadableStream) {
        options.duplex = "half";
    }
    for (let i = 0; i < option_names.length; i++) {
        options[option_names[i]] = option_values[i];
    }
    controller.__fetch_promise = wrap_as_cancelable_promise(() => {
        return loaderHelpers.fetch_like(url, options).then((res) => {
            controller.__response = res;
            controller.__headerNames = [];
            controller.__headerValues = [];
            if (res.headers && (<any>res.headers).entries) {
                const entries: Iterable<string[]> = (<any>res.headers).entries();

                for (const pair of entries) {
                    controller.__headerNames.push(pair[0]);
                    controller.__headerValues.push(pair[1]);
                }
            }
            return res;
        });
    });
    return controller.__fetch_promise as ControllablePromise<any>;
}

export function http_wasm_get_response_header_names(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.__headerNames;
}

export function http_wasm_get_response_type(controller: HttpController): string | undefined {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.__response?.type;
}

export function http_wasm_get_response_status(controller: HttpController): number {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.__response?.status ?? 0;
}


export function http_wasm_get_response_header_values(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.__headerValues;
}

export function http_wasm_get_response_length(controller: HttpController): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return wrap_as_cancelable_promise(async () => {
        const buffer = await controller.__response!.arrayBuffer();
        controller.__buffer = buffer;
        controller.__source_offset = 0;
        return buffer.byteLength;
    });
}

export function http_wasm_get_response_bytes(controller: HttpController, view: Span): number {
    mono_assert(controller, "expected controller");
    mono_assert(controller.__buffer, "expected resoved arrayBuffer");
    if (controller.__source_offset == controller.__buffer!.byteLength) {
        return 0;
    }
    const source_view = new Uint8Array(controller.__buffer!, controller.__source_offset);
    view.set(source_view, 0);
    const bytes_read = Math.min(view.byteLength, source_view.byteLength);
    controller.__source_offset += bytes_read;
    return bytes_read;
}

export function http_wasm_get_streamed_response_bytes(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    return wrap_as_cancelable_promise(async () => {
        if (!controller.__reader) {
            controller.__reader = controller.__response!.body!.getReader();
        }
        if (!controller.__chunk) {
            controller.__chunk = await controller.__reader.read();
            controller.__source_offset = 0;
        }
        if (controller.__chunk!.done) {
            return 0;
        }

        const remaining_source = controller.__chunk.value.byteLength - controller.__source_offset;
        mono_assert(remaining_source > 0, "expected remaining_source to be greater than 0");

        const bytes_copied = Math.min(remaining_source, view.byteLength);
        const source_view = controller.__chunk.value.subarray(controller.__source_offset, controller.__source_offset + bytes_copied);
        view.set(source_view, 0);
        controller.__source_offset += bytes_copied;
        if (remaining_source == bytes_copied) {
            controller.__chunk = undefined;
        }

        return bytes_copied;
    });
}

interface HttpController {
    __abort_controller: AbortController
    __buffer?: ArrayBuffer
    __reader?: ReadableStreamDefaultReader<Uint8Array>
    __chunk?: ReadableStreamReadResult<Uint8Array>
    __source_offset: number
    __headerNames: string[];//response headers
    __headerValues: string[];
    __writer: WritableStreamDefaultWriter<Uint8Array>
    __fetch_promise?: ControllablePromise<Response>
    __response?: Response
    __transform_stream?: TransformStream
}
