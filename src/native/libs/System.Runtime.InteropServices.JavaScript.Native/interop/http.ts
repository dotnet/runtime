// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import type { VoidPtr, ControllablePromise } from "./types";

import { wrapAsCancelablePromise } from "./cancelable-promise";
import { ENVIRONMENT_IS_NODE } from "./per-module";
import { assertJsInterop } from "./utils";
import { MemoryViewType, Span } from "./marshaled-types";
import { dotnetLogger, dotnetAssert } from "./cross-module";


function verifyEnvironment() {
    if (typeof globalThis.fetch !== "function" || typeof globalThis.AbortController !== "function") {
        const message = ENVIRONMENT_IS_NODE
            ? "Please install `node-fetch` and `node-abort-controller` npm packages to enable HTTP client support. See also https://aka.ms/dotnet-wasm-features"
            : "This browser doesn't support fetch API. Please use a modern browser. See also https://aka.ms/dotnet-wasm-features";
        throw new Error(message);
    }
}

function commonAsserts(controller: HttpController) {
    assertJsInterop();
    dotnetAssert.check(controller, "expected controller");
}

export function httpSupportsStreamingRequest(): boolean {
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

export function httpSupportsStreamingResponse(): boolean {
    return typeof Response !== "undefined" && "body" in Response.prototype && typeof ReadableStream === "function";
}

export function httpCreateController(): HttpController {
    verifyEnvironment();
    assertJsInterop();
    const controller: HttpController = {
        abortController: new AbortController()
    };
    return controller;
}

function muteUnhandledRejection(promise: Promise<any>) {
    promise.catch((err) => {
        if (err && err !== "AbortError" && err.name !== "AbortError") {
            dotnetLogger.debug("http muted: " + err);
        }
    });
}

export function httpAbort(controller: HttpController): void {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    try {
        if (!controller.isAborted) {
            if (controller.streamWriter) {
                muteUnhandledRejection(controller.streamWriter.abort());
                controller.isAborted = true;
            }
            if (controller.streamReader) {
                muteUnhandledRejection(controller.streamReader.cancel());
                controller.isAborted = true;
            }
        }
        if (!controller.isAborted && !controller.abortController.signal.aborted) {
            controller.abortController.abort("AbortError");
        }
    } catch (err) {
        // ignore
    }
}

export function httpTransformStreamWrite(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    dotnetAssert.check(bufferLength > 0, "expected bufferLength > 0");
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return wrapAsCancelablePromise(async () => {
        dotnetAssert.check(controller.streamWriter, "expected streamWriter");
        dotnetAssert.check(controller.responsePromise, "expected fetch promise");
        try {
            await controller.streamWriter.ready;
            await controller.streamWriter.write(copy);
        } catch (ex) {
            throw new Error("BrowserHttpWriteStream.Rejected");
        }
    });
}

export function httpTransformStreamClose(controller: HttpController): ControllablePromise<void> {
    dotnetAssert.check(controller, "expected controller");
    return wrapAsCancelablePromise(async () => {
        dotnetAssert.check(controller.streamWriter, "expected streamWriter");
        dotnetAssert.check(controller.responsePromise, "expected fetch promise");
        try {
            await controller.streamWriter.ready;
            await controller.streamWriter.close();
        } catch (ex) {
            throw new Error("BrowserHttpWriteStream.Rejected");
        }
    });
}

export function httpFetchStream(controller: HttpController, url: string, headerNames: string[], headerValues: string[], optionNames: string[], optionValues: any[]): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    const transformStream = new TransformStream<Uint8Array, Uint8Array>();
    controller.streamWriter = transformStream.writable.getWriter();
    muteUnhandledRejection(controller.streamWriter.closed);
    muteUnhandledRejection(controller.streamWriter.ready);
    const fetchPromise = httpFetch(controller, url, headerNames, headerValues, optionNames, optionValues, transformStream.readable);
    return fetchPromise;
}

export function httpFetchBytes(controller: HttpController, url: string, headerNames: string[], headerValues: string[], optionNames: string[], optionValues: any[], bodyPtr: VoidPtr, bodyLength: number): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    // the bodyPtr is pinned by the caller
    const view = new Span(bodyPtr, bodyLength, MemoryViewType.Byte);
    const copy = view.slice() as Uint8Array;
    return httpFetch(controller, url, headerNames, headerValues, optionNames, optionValues, copy);
}

export function httpFetch(controller: HttpController, url: string, headerNames: string[], headerValues: string[], optionNames: string[], optionValues: any[], body: Uint8Array | ReadableStream | null): ControllablePromise<void> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    verifyEnvironment();
    assertJsInterop();
    dotnetAssert.check(url && typeof url === "string", "expected url string");
    dotnetAssert.check(headerNames && headerValues && Array.isArray(headerNames) && Array.isArray(headerValues) && headerNames.length === headerValues.length, "expected headerNames and headerValues arrays");
    dotnetAssert.check(optionNames && optionValues && Array.isArray(optionNames) && Array.isArray(optionValues) && optionNames.length === optionValues.length, "expected headerNames and headerValues arrays");

    const headers = new Headers();
    for (let i = 0; i < headerNames.length; i++) {
        headers.append(headerNames[i], headerValues[i]);
    }
    const options: any = {
        body,
        headers,
        signal: controller.abortController.signal
    };
    if (typeof ReadableStream !== "undefined" && body instanceof ReadableStream) {
        options.duplex = "half";
    }
    for (let i = 0; i < optionNames.length; i++) {
        options[optionNames[i]] = optionValues[i];
    }
    controller.responsePromise = wrapAsCancelablePromise(() => {
        return globalThis.fetch(url, options).then((res: Response) => {
            controller.response = res;
            return null;// drop the response from the promise chain
        });
    });
    // avoid processing headers if the fetch is canceled
    controller.responsePromise.then(() => {
        dotnetAssert.check(controller.response, "expected response");
        controller.responseHeaderNames = [];
        controller.responseHeaderValues = [];
        if (controller.response.headers && (<any>controller.response.headers).entries) {
            const entries: Iterable<string[]> = (<any>controller.response.headers).entries();
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

export function httpGetResponseType(controller: HttpController): string | undefined {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.response?.type;
}

export function httpGetResponseStatus(controller: HttpController): number {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return controller.response?.status ?? 0;
}


export function httpGetResponseHeaderNames(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    dotnetAssert.check(controller.responseHeaderNames, "expected responseHeaderNames");
    return controller.responseHeaderNames;
}

export function httpGetResponseHeaderValues(controller: HttpController): string[] {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    dotnetAssert.check(controller.responseHeaderValues, "expected responseHeaderValues");
    return controller.responseHeaderValues;
}

export function httpGetResponseLength(controller: HttpController): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    return wrapAsCancelablePromise(async () => {
        const buffer = await controller.response!.arrayBuffer();
        controller.responseBuffer = buffer;
        controller.currentBufferOffset = 0;
        return buffer.byteLength;
    });
}

export function httpGetResponseBytes(controller: HttpController, view: Span): number {
    dotnetAssert.check(controller, "expected controller");
    dotnetAssert.check(controller.responseBuffer, "expected resoved arrayBuffer");
    dotnetAssert.check(controller.currentBufferOffset != undefined, "expected currentBufferOffset");
    if (controller.currentBufferOffset == controller.responseBuffer!.byteLength) {
        return 0;
    }
    const sourceView = new Uint8Array(controller.responseBuffer!, controller.currentBufferOffset);
    view.set(sourceView, 0);
    const bytesRead = Math.min(view.byteLength, sourceView.byteLength);
    controller.currentBufferOffset += bytesRead;
    return bytesRead;
}

export function httpGetStreamedResponseBytes(controller: HttpController, bufferPtr: VoidPtr, bufferLength: number): ControllablePromise<number> {
    if (BuildConfiguration === "Debug") commonAsserts(controller);
    // the bufferPtr is pinned by the caller
    const view = new Span(bufferPtr, bufferLength, MemoryViewType.Byte);
    return wrapAsCancelablePromise(async () => {
        await controller.responsePromise;
        dotnetAssert.check(controller.response, "expected response");
        if (!controller.response.body) {
            // in FF when the verb is HEAD, the body is null
            return 0;
        }
        if (!controller.streamReader) {
            controller.streamReader = controller.response.body.getReader();
            muteUnhandledRejection(controller.streamReader.closed);
        }
        if (!controller.currentStreamReaderChunk || controller.currentBufferOffset === undefined) {
            controller.currentStreamReaderChunk = await controller.streamReader.read();
            controller.currentBufferOffset = 0;
        }
        if (controller.currentStreamReaderChunk.done) {
            if (controller.isAborted) {
                throw new Error("OperationCanceledException");
            }
            return 0;
        }

        const remainingSource = controller.currentStreamReaderChunk.value.byteLength - controller.currentBufferOffset;
        dotnetAssert.check(remainingSource > 0, "expected remainingSource to be greater than 0");

        const bytesCopied = Math.min(remainingSource, view.byteLength);
        const sourceView = controller.currentStreamReaderChunk.value.subarray(controller.currentBufferOffset, controller.currentBufferOffset + bytesCopied);
        view.set(sourceView, 0);
        controller.currentBufferOffset += bytesCopied;
        if (remainingSource == bytesCopied) {
            controller.currentStreamReaderChunk = undefined;
        }

        return bytesCopied;
    });
}

interface HttpController {
    abortController: AbortController
    isAborted?: boolean

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
