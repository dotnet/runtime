// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetJSEngine } from "./cross-module";

export function initPolyfills(): void {
    if (typeof globalThis.WeakRef !== "function") {
        class WeakRefPolyfill<T> {
            private _value: T | undefined;

            constructor(value: T) {
                this._value = value;
            }

            deref(): T | undefined {
                return this._value;
            }
        }
        globalThis.WeakRef = WeakRefPolyfill as any;
    }
    if (typeof globalThis.fetch !== "function") {
        globalThis.fetch = fetchLike as any;
    }
}

export async function initPolyfillsAsync(): Promise<void> {
    if (dotnetJSEngine.IS_NODE) {
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

export async function fetchLike(url: string, init?: RequestInit): Promise<Response> {
    let node_fs: any | undefined = undefined;
    let node_url: any | undefined = undefined;
    try {
        // this need to be detected only after we import node modules in onConfigLoaded
        const hasFetch = typeof (globalThis.fetch) === "function";
        if (dotnetJSEngine.IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (!node_fs) {
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                node_url = await import(/*! webpackIgnore: true */"url");
                // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                // @ts-ignore:
                node_fs = await import(/*! webpackIgnore: true */"fs");
            }
            if (isFileUrl) {
                url = node_url.fileURLToPath(url);
            }

            const arrayBuffer = await node_fs.promises.readFile(url);
            return <Response><any>{
                ok: true,
                headers: {
                    length: 0,
                    get: () => null
                },
                url,
                arrayBuffer: () => arrayBuffer,
                json: () => JSON.parse(arrayBuffer),
                text: () => {
                    throw new Error("NotImplementedException");
                }
            };
        } else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
        } else if (typeof (read) === "function") {
            // note that it can't open files with unicode names, like Stra<unicode char - Latin Small Letter Sharp S>e.xml
            // https://bugs.chromium.org/p/v8/issues/detail?id=12541
            return <Response><any>{
                ok: true,
                url,
                headers: {
                    length: 0,
                    get: () => null
                },
                arrayBuffer: () => {
                    return new Uint8Array(read(url, "binary"));
                },
                json: () => {
                    return JSON.parse(read(url, "utf8"));
                },
                text: () => read(url, "utf8")
            };
        }
    } catch (e: any) {
        return <Response><any>{
            ok: false,
            url,
            status: 500,
            headers: {
                length: 0,
                get: () => null
            },
            statusText: "ERR28: " + e,
            arrayBuffer: () => {
                throw e;
            },
            json: () => {
                throw e;
            },
            text: () => {
                throw e;
            }
        };
    }
    throw new Error("No fetch implementation available");
}
