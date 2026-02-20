// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { exceptions, simd } from "wasm-feature-detect";

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, globalThisAny } from "./per-module";
import { dotnetAssert } from "./cross-module";

const scriptUrlQuery = /*! webpackIgnore: true */import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);

export async function validateWasmFeatures(): Promise<void> {
    dotnetAssert.check(await exceptions(), "This browser/engine doesn't support WASM exception handling. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    dotnetAssert.check(await simd(), "This browser/engine doesn't support WASM SIMD. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    if (ENVIRONMENT_IS_NODE) {
        const nodeMajorVersion = parseInt(globalThisAny.process.versions.node.split(".")[0], 10);
        dotnetAssert.check(nodeMajorVersion >= 18, `Node.js version ${globalThisAny.process.versions.node} is not supported. Please use Node.js 18 or later. See also https://aka.ms/dotnet-wasm-features`);
    } else if (ENVIRONMENT_IS_SHELL) {
        if (typeof globalThisAny.version === "function" && globalThisAny.d8) {
            const v8v = globalThisAny.version();
            const v8MajorVersion = parseInt(v8v.split(".")[0], 10);
            dotnetAssert.check(v8MajorVersion >= 14, "This V8 shell is too old. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
        }
    }
}

export function locateFile(path: string, isModule = false): string {
    let res;
    if (isPathAbsolute(path)) {
        res = path;
    } else if (globalThis.URL) {
        res = new globalThis.URL(path, scriptDirectory).href;
    } else {
        res = scriptDirectory + path;
    }

    if (isModule) {
        res += modulesUniqueQuery;
    }

    return res;
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

export function makeURLAbsoluteWithApplicationBase(url: string) {
    dotnetAssert.check(typeof url === "string", "url must be a string");
    if (!isPathAbsolute(url) && url.indexOf("./") !== 0 && url.indexOf("../") !== 0 && globalThis.URL && globalThis.document && globalThis.document.baseURI) {
        const absoluteUrl = new URL(url, globalThis.document.baseURI);
        return absoluteUrl.href;
    }
    return url;
}


