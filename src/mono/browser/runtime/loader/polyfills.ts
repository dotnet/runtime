// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import type { DotnetModuleInternal } from "../types/internal";
import { INTERNAL, ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, loaderHelpers, ENVIRONMENT_IS_WEB, mono_assert } from "./globals";

let node_fs: any | undefined = undefined;
let node_url: any | undefined = undefined;
const URLPolyfill = class URL {
    private url;
    constructor(url: string) {
        this.url = url;
    }
    toString() {
        return this.url;
    }
};

export function verifyEnvironment() {
    mono_assert(ENVIRONMENT_IS_SHELL || typeof globalThis.URL === "function", "This browser/engine doesn't support URL API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    mono_assert(typeof globalThis.BigInt64Array === "function", "This browser/engine doesn't support BigInt64Array API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    if (WasmEnableThreads) {
        mono_assert(!ENVIRONMENT_IS_SHELL && !ENVIRONMENT_IS_NODE, "This build of dotnet is multi-threaded, it doesn't support shell environments like V8 or NodeJS. See also https://aka.ms/dotnet-wasm-features");
        mono_assert(globalThis.SharedArrayBuffer !== undefined, "SharedArrayBuffer is not enabled on this page. Please use a modern browser and set Cross-Origin-Opener-Policy and Cross-Origin-Embedder-Policy http headers. See also https://aka.ms/dotnet-wasm-features");
        mono_assert(typeof globalThis.EventTarget === "function", "This browser/engine doesn't support EventTarget API. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    }
}

export async function detect_features_and_polyfill(module: DotnetModuleInternal): Promise<void> {
    if (ENVIRONMENT_IS_NODE) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        const process = await import(/*! webpackIgnore: true */"process");
        const minNodeVersion = 14;
        if (process.versions.node.split(".")[0] < minNodeVersion) {
            throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}', please use at least ${minNodeVersion}. See also https://aka.ms/dotnet-wasm-features`);
        }
    }

    const scriptUrlQuery =/*! webpackIgnore: true */import.meta.url;
    const queryIndex = scriptUrlQuery.indexOf("?");
    if (queryIndex > 0) {
        loaderHelpers.modulesUniqueQuery = scriptUrlQuery.substring(queryIndex);
    }
    loaderHelpers.scriptUrl = normalizeFileUrl(scriptUrlQuery);
    loaderHelpers.scriptDirectory = normalizeDirectoryUrl(loaderHelpers.scriptUrl);
    loaderHelpers.locateFile = (path) => {
        if ("URL" in globalThis && globalThis.URL !== (URLPolyfill as any)) {
            return new URL(path, loaderHelpers.scriptDirectory).toString();
        }

        if (isPathAbsolute(path)) return path;
        return loaderHelpers.scriptDirectory + path;
    };
    loaderHelpers.fetch_like = fetch_like;
    // eslint-disable-next-line no-console
    loaderHelpers.out = console.log;
    // eslint-disable-next-line no-console
    loaderHelpers.err = console.error;
    loaderHelpers.onDownloadResourceProgress = module.onDownloadResourceProgress;

    if (ENVIRONMENT_IS_WEB && globalThis.navigator) {
        const navigator: any = globalThis.navigator;
        const brands = navigator.userAgentData && navigator.userAgentData.brands;
        if (brands && brands.length > 0) {
            loaderHelpers.isChromium = brands.some((b: any) => b.brand === "Google Chrome" || b.brand === "Microsoft Edge" || b.brand === "Chromium");
        }
        else if (navigator.userAgent) {
            loaderHelpers.isChromium = navigator.userAgent.includes("Chrome");
            loaderHelpers.isFirefox = navigator.userAgent.includes("Firefox");
        }
    }

    if (ENVIRONMENT_IS_NODE) {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        INTERNAL.require = await import(/*! webpackIgnore: true */"module").then(mod => mod.createRequire(/*! webpackIgnore: true */import.meta.url));
    } else {
        INTERNAL.require = Promise.resolve(() => { throw new Error("require not supported"); });
    }

    if (typeof globalThis.URL === "undefined") {
        globalThis.URL = URLPolyfill as any;
    }
}

export async function fetch_like(url: string, init?: RequestInit): Promise<Response> {
    try {
        // this need to be detected only after we import node modules in onConfigLoaded
        const hasFetch = typeof (globalThis.fetch) === "function";
        if (ENVIRONMENT_IS_NODE) {
            const isFileUrl = url.startsWith("file://");
            if (!isFileUrl && hasFetch) {
                return globalThis.fetch(url, init || { credentials: "same-origin" });
            }
            if (!node_fs) {
                node_url = INTERNAL.require("url");
                node_fs = INTERNAL.require("fs");
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
                text: () => { throw new Error("NotImplementedException"); }
            };
        }
        else if (hasFetch) {
            return globalThis.fetch(url, init || { credentials: "same-origin" });
        }
        else if (typeof (read) === "function") {
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
    }
    catch (e: any) {
        return <Response><any>{
            ok: false,
            url,
            status: 500,
            headers: {
                length: 0,
                get: () => null
            },
            statusText: "ERR28: " + e,
            arrayBuffer: () => { throw e; },
            json: () => { throw e; },
            text: () => { throw e; }
        };
    }
    throw new Error("No fetch implementation available");
}

// context: the loadBootResource extension point can return URL/string which is unqualified. 
// For example `xxx/a.js` and we have to make it absolute
// For compatibility reasons, it's based of document.baseURI even for JS modules like `./xxx/a.js`, which normally use script directory of a caller of `import`
// Script directory in general doesn't match document.baseURI
export function makeURLAbsoluteWithApplicationBase(url: string) {
    mono_assert(typeof url === "string", "url must be a string");
    if (!isPathAbsolute(url) && url.indexOf("./") !== 0 && url.indexOf("../") !== 0 && globalThis.URL && globalThis.document && globalThis.document.baseURI) {
        url = (new URL(url, globalThis.document.baseURI)).toString();
    }
    return url;
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path: string): boolean {
    if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }

    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}
