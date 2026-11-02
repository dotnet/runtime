// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AuxiliaryWorkerRequest } from "./auxiliary-module-worker-client";

globalThis.onmessage = async (event: MessageEvent<AuxiliaryWorkerRequest>) => {
    const { id, url, options, body, contentType } = event.data;
    try {
        const response = body ? new Response(body, { headers: { "Content-Type": contentType! } }) : await fetch(url, options);
        if (!response.ok) {
            globalThis.postMessage({ id, status: response.status, statusText: response.statusText });
            return;
        }
        const module = response.headers.get("Content-Type") === "application/wasm" && typeof WebAssembly.compileStreaming === "function"
            ? await WebAssembly.compileStreaming(response)
            : await WebAssembly.compile(await response.arrayBuffer());
        globalThis.postMessage({ id, status: response.status, statusText: response.statusText, module });
    } catch (error) {
        globalThis.postMessage({ id, error: error instanceof Error ? error.message : String(error),
            errorName: error instanceof Error ? error.name : "Error" });
    }
};
