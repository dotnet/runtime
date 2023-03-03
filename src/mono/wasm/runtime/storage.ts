// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import GitHash from "consts:gitHash";
import { runtimeHelpers } from "./imports";

// adapted from Blazor's WebAssemblyResourceLoader.ts
async function open(): Promise<Cache | null> {
    // caches will be undefined if we're running on an insecure origin (secure means https or localhost)
    if (typeof globalThis.caches === "undefined") {
        return null;
    }

    // cache integrity is compromised if the first request has been served over http (except localhost)
    // in this case, we want to disable caching and integrity validation
    if (ENVIRONMENT_IS_WEB && globalThis.window.isSecureContext === false) {
        return null;
    }

    // Define a separate cache for each base href, so we're isolated from any other
    // Blazor application running on the same origin. We need this so that we're free
    // to purge from the cache anything we're not using and don't let it keep growing,
    // since we don't want to be worst offenders for space usage.
    const relativeBaseHref = document.baseURI.substring(document.location.origin.length);
    const cacheName = `dotnet-resources${relativeBaseHref}`;

    try {
        // There's a Chromium bug we need to be aware of here: the CacheStorage APIs say that when
        // caches.open(name) returns a promise that succeeds, the value is meant to be a Cache instance.
        // However, if the browser was launched with a --user-data-dir param that's "too long" in some sense,
        // then even through the promise resolves as success, the value given is `undefined`.
        // See https://stackoverflow.com/a/46626574 and https://bugs.chromium.org/p/chromium/issues/detail?id=1054541
        // If we see this happening, return "null" to mean "proceed without caching".
        return (await globalThis.caches.open(cacheName)) || null;
    } catch {
        // There's no known scenario where we should get an exception here, but considering the
        // Chromium bug above, let's tolerate it and treat as "proceed without caching".
        return null;
    }
}
const memoryPrefix = "https://dotnet.generated.invalid/wasm-memory";
export async function getMemory(): Promise<ArrayBuffer | undefined> {
    try {
        const inputsHash = await getInputsHash();
        if (!inputsHash) {
            return undefined;
        }
        const cacheKey = `${memoryPrefix}-${ProductVersion}-${GitHash}-${inputsHash}`;
        const cache = await open();
        if (!cache) {
            return undefined;
        }
        const res = await cache.match(cacheKey);
        if (!res) {
            return undefined;
        }
        return res.arrayBuffer();
    } catch (ex) {
        return undefined;
    }
}

export async function storeMemory(memory: ArrayBuffer) {
    try {
        const inputsHash = await getInputsHash();
        if (!inputsHash) {
            return;
        }
        const cacheKey = `${memoryPrefix}-${ProductVersion}-${GitHash}-${inputsHash}`;
        const cache = await open();
        if (!cache) {
            return;
        }

        const responseToCache = new Response(memory, {
            headers: {
                "content-type": "wasm-memory",
                "content-length": memory.byteLength.toString(),
            },
        });

        await cache.put(cacheKey, responseToCache);

        cleanupMemory(cacheKey); // no await
    } catch (ex) {
        return;
    }
}

export async function cleanupMemory(protectKey: string) {
    try {
        const cache = await open();
        if (!cache) {
            return;
        }
        const items = await cache.keys();
        for (const item of items) {
            if (item.url && item.url !== protectKey && item.url.startsWith(memoryPrefix)) {
                await cache.delete(item);
            }
        }
    } catch (ex) {
        return;
    }
}

// calculate hash of things which affect the memory snapshot
export async function getInputsHash(): Promise<string | null> {
    if (!runtimeHelpers.subtle) {
        return null;
    }
    const inputs = Object.assign({}, runtimeHelpers.config) as any;
    // above already has env variables, runtime options, etc
    // above also already has config.assetsHash for this. It has all the asserts (DLLs, ICU, .wasms, etc). 
    // So we could remove assets collectionfrom the hash.
    inputs.assets = null;
    // some things are calculated at runtime, so we need to add them to the hash
    inputs.preferredIcuAsset = runtimeHelpers.preferredIcuAsset;
    inputs.timezone = runtimeHelpers.timezone;
    const inputsJson = JSON.stringify(inputs);
    const sha256Buffer = await runtimeHelpers.subtle.digest("SHA-256", new TextEncoder().encode(inputsJson));
    const uint8ViewOfHash = new Uint8Array(sha256Buffer);
    const hashAsString = Array.from(uint8ViewOfHash).map((b) => b.toString(16).padStart(2, "0")).join("");
    return hashAsString;
}