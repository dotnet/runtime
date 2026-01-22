// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_NODE } from "./per-module";

export function initPolyfills(): void {
    if (typeof globalThis.fetch !== "function") {
        globalThis.fetch = fetchLike as any;
    }
}

export async function initPolyfillsAsync(): Promise<void> {
    if (ENVIRONMENT_IS_NODE) {
        if (!globalThis.crypto) {
            globalThis.crypto = <any>{};
        }
        if (!globalThis.crypto.getRandomValues) {
            let nodeCrypto: any = undefined;
            try {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                nodeCrypto = await import(/*! webpackIgnore: true */"node:crypto");
            } catch (err: any) {
                // Noop, error throwing polyfill provided bellow
            }

            if (!nodeCrypto) {
                globalThis.crypto.getRandomValues = () => {
                    throw new Error("Using node without crypto support. To enable current operation, either provide polyfill for 'globalThis.crypto.getRandomValues' or enable 'node:crypto' module.");
                };
            } else if (nodeCrypto.webcrypto) {
                globalThis.crypto = nodeCrypto.webcrypto;
            } else if (nodeCrypto.randomBytes) {
                const getRandomValues = (buffer: Uint8Array) => {
                    if (buffer) {
                        buffer.set(nodeCrypto.randomBytes(buffer.length));
                    }
                };
                globalThis.crypto.getRandomValues = getRandomValues as any;
            }
        }
        if (!globalThis.performance) {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore:
            globalThis.performance = (await import(/*! webpackIgnore: true */"perf_hooks")).performance;
        }
    }
    // WASM-TODO: performance polyfill for V8
}

let _nodeFs: any | undefined = undefined;
let _nodeUrl: any | undefined = undefined;

export async function nodeFs(): Promise<any> {
    if (ENVIRONMENT_IS_NODE && !_nodeFs) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        _nodeFs = await import(/*! webpackIgnore: true */"fs");
    }
    return _nodeFs;
}

export async function nodeUrl(): Promise<any> {
    if (ENVIRONMENT_IS_NODE && !_nodeUrl) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        _nodeUrl = await import(/*! webpackIgnore: true */"node:url");
    }
    return _nodeUrl;
}

export async function fetchLike(url: string, init?: RequestInit, expectedContentType?: string): Promise<Response> {
    try {
        await nodeFs();
        await nodeUrl();
        // this need to be detected only after we import node modules in onConfigLoaded
        const hasFetch = typeof (globalThis.fetch) === "function";
        if (ENVIRONMENT_IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (isFileUrl) {
                url = _nodeUrl.fileURLToPath(url);
            }

            const arrayBuffer = await _nodeFs.promises.readFile(url);
            return responseLike(url, arrayBuffer, {
                status: 200,
                statusText: "OK",
                headers: {
                    "Content-Length": arrayBuffer.byteLength.toString(),
                    "Content-Type": expectedContentType || "application/octet-stream"
                }
            });
        } else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
        } else if (typeof (read) === "function") {
            const arrayBuffer = read(url, "binary");
            return responseLike(url, arrayBuffer, {
                status: 200,
                statusText: "OK",
                headers: {
                    "Content-Length": arrayBuffer.byteLength.toString(),
                    "Content-Type": expectedContentType || "application/octet-stream"
                }
            });
        }
    } catch (e: any) {
        return responseLike(url, null, {
            status: 500,
            statusText: "ERR28: " + e,
            headers: {},
        });
    }
    throw new Error("No fetch implementation available");
}

export function responseLike(url: string, body: ArrayBuffer | null, options: ResponseInit): Response {
    if (typeof globalThis.Response === "function") {
        const response = new Response(body, options);

        // Best-effort alignment with the fallback object shape:
        // only define `url` if it does not already exist on the response.
        if (typeof (response as any).url === "undefined") {
            try {
                Object.defineProperty(response, "url", { value: url });
            } catch {
                // Ignore if the implementation does not allow redefining `url`
            }
        }

        return response;
    }
    return <Response><any>{
        ok: body !== null && options.status === 200,
        headers: {
            ...options.headers,
            get: (name: string) => (options.headers as any)[name] || null
        },
        url,
        arrayBuffer: () => Promise.resolve(body),
        json: () => {
            throw new Error("NotImplementedException");
        },
        text: () => {
            throw new Error("NotImplementedException");
        }
    };
}
