// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { wrap_as_cancelable_promise } from "./cancelable-promise";
import { Module, loaderHelpers } from "./globals";
import { MemoryViewType, Span } from "./marshal";
import type { VoidPtr } from "./types/emscripten";

export function http_wasm_supports_streaming_response(): boolean {
    return typeof Response !== "undefined" && "body" in Response.prototype && typeof ReadableStream === "function";
}

export function http_wasm_create_abort_controler(): AbortController {
    return new AbortController();
}

export function http_wasm_abort_request(abort_controller: AbortController): void {
    abort_controller.abort();
}

export function http_wasm_abort_response(res: ResponseExtension): void {
    res.__abort_controller.abort();
    if (res.__reader) {
        res.__reader.cancel().catch((err) => {
            if (err && err.name !== "AbortError") {
                Module.err("Error in http_wasm_abort_response: " + err);
            }
            // otherwise, it's expected
        });
    }
}

export function http_wasm_fetch_bytes(url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[], abort_controller: AbortController, bodyPtr: VoidPtr, bodyLength: number): Promise<ResponseExtension> {
    // the bufferPtr is pinned by the caller
    const view = new Span(bodyPtr, bodyLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return http_wasm_fetch(url, header_names, header_values, option_names, option_values, abort_controller, copy);
}

export function http_wasm_fetch(url: string, header_names: string[], header_values: string[], option_names: string[], option_values: any[], abort_controller: AbortController, body: string | Uint8Array | null): Promise<ResponseExtension> {
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
        signal: abort_controller.signal
    };
    for (let i = 0; i < option_names.length; i++) {
        options[option_names[i]] = option_values[i];
    }

    return wrap_as_cancelable_promise(async () => {
        const res = await loaderHelpers.fetch_like(url, options) as ResponseExtension;
        res.__abort_controller = abort_controller;
        return res;
    });
}

function get_response_headers(res: ResponseExtension): void {
    if (!res.__headerNames) {
        res.__headerNames = [];
        res.__headerValues = [];
        if (res.headers && (<any>res.headers).entries) {
            const entries: Iterable<string[]> = (<any>res.headers).entries();

            for (const pair of entries) {
                res.__headerNames.push(pair[0]);
                res.__headerValues.push(pair[1]);
            }
        }
    }
}

export function http_wasm_get_response_header_names(res: ResponseExtension): string[] {
    get_response_headers(res);
    return res.__headerNames;
}

export function http_wasm_get_response_header_values(res: ResponseExtension): string[] {
    get_response_headers(res);
    return res.__headerValues;
}

export function http_wasm_get_response_length(res: ResponseExtension): Promise<number> {
    return wrap_as_cancelable_promise(async () => {
        const buffer = await res.arrayBuffer();
        res.__buffer = buffer;
        res.__source_offset = 0;
        return buffer.byteLength;
    });
}

export function http_wasm_get_response_bytes(res: ResponseExtension, view: Span): number {
    mono_assert(res.__buffer, "expected resoved arrayBuffer");
    if (res.__source_offset == res.__buffer!.byteLength) {
        return 0;
    }
    const source_view = new Uint8Array(res.__buffer!, res.__source_offset);
    view.set(source_view, 0);
    const bytes_read = Math.min(view.byteLength, source_view.byteLength);
    res.__source_offset += bytes_read;
    return bytes_read;
}

export function http_wasm_get_streamed_response_bytes(res: ResponseExtension, bufferPtr: VoidPtr, bufferLength: number): Promise<number> {
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    return wrap_as_cancelable_promise(async () => {
        if (!res.__reader) {
            res.__reader = res.body!.getReader();
        }
        if (!res.__chunk) {
            res.__chunk = await res.__reader.read();
            res.__source_offset = 0;
        }
        if (res.__chunk.done) {
            return 0;
        }

        const remaining_source = res.__chunk.value.byteLength - res.__source_offset;
        mono_assert(remaining_source > 0, "expected remaining_source to be greater than 0");

        const bytes_copied = Math.min(remaining_source, view.byteLength);
        const source_view = res.__chunk.value.subarray(res.__source_offset, res.__source_offset + bytes_copied);
        view.set(source_view, 0);
        res.__source_offset += bytes_copied;
        if (remaining_source == bytes_copied) {
            res.__chunk = undefined;
        }

        return bytes_copied;
    });
}

interface ResponseExtension extends Response {
    __buffer?: ArrayBuffer
    __reader?: ReadableStreamDefaultReader<Uint8Array>
    __chunk?: ReadableStreamReadResult<Uint8Array>
    __source_offset: number
    __abort_controller: AbortController
    __headerNames: string[];
    __headerValues: string[];
}
