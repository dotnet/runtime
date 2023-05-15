// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import GitHash from "consts:gitHash";
import MonoWasmThreads from "consts:monoWasmThreads";
import { ENVIRONMENT_IS_WEB, loaderHelpers, runtimeHelpers } from "./globals";

const memoryPrefix = "https://dotnet.generated.invalid/wasm-memory";

// adapted from Blazor's WebAssemblyResourceLoader.ts
async function openCache(): Promise<Cache | null> {
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
        console.warn("MONO_WASM: Failed to open cache");
        return null;
    }
}

export async function getMemorySnapshotSize(): Promise<number | undefined> {
    try {
        const cacheKey = await getCacheKey();
        if (!cacheKey) {
            return undefined;
        }
        const cache = await openCache();
        if (!cache) {
            return undefined;
        }
        const res = await cache.match(cacheKey);
        const contentLength = res?.headers.get("content-length");
        return contentLength ? parseInt(contentLength) : undefined;
    } catch (ex) {
        console.warn("MONO_WASM: Failed find memory snapshot in the cache", ex);
        return undefined;
    }
}

export async function getMemorySnapshot(): Promise<ArrayBuffer | undefined> {
    try {
        const cacheKey = await getCacheKey();
        if (!cacheKey) {
            return undefined;
        }
        const cache = await openCache();
        if (!cache) {
            return undefined;
        }
        const res = await cache.match(cacheKey);
        if (!res) {
            return undefined;
        }
        return res.arrayBuffer();
    } catch (ex) {
        console.warn("MONO_WASM: Failed load memory snapshot from the cache", ex);
        return undefined;
    }
}

export async function storeMemorySnapshot(memory: ArrayBuffer) {
    try {
        const cacheKey = await getCacheKey();
        if (!cacheKey) {
            return;
        }
        const cache = await openCache();
        if (!cache) {
            return;
        }
        const copy = MonoWasmThreads
            // storing SHaredArrayBuffer in the cache is not working
            ? (new Int8Array(memory)).slice(0)
            : memory;

        const responseToCache = new Response(copy, {
            headers: {
                "content-type": "wasm-memory",
                "content-length": memory.byteLength.toString(),
            },
        });

        await cache.put(cacheKey, responseToCache);

        cleanupMemorySnapshots(cacheKey); // no await
    } catch (ex) {
        console.warn("MONO_WASM: Failed to store memory snapshot in the cache", ex);
        return;
    }
}

export async function cleanupMemorySnapshots(protectKey: string) {
    try {
        const cache = await openCache();
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
async function getCacheKey(): Promise<string | null> {
    if (runtimeHelpers.memorySnapshotCacheKey) {
        return runtimeHelpers.memorySnapshotCacheKey;
    }
    if (!runtimeHelpers.subtle) {
        return null;
    }
    const inputs = Object.assign({}, runtimeHelpers.config) as any;
    // above already has env variables, runtime options, etc

    if (!inputs.assetsHash) {
        // this is fallback for blazor which does not have assetsHash yet
        inputs.assetsHash = [];
        for (const asset of inputs.assets) {
            if (!asset.hash) {
                // if we don't have hash, we can't use the cache
                return null;
            }
            inputs.assetsHash.push(asset.hash);
        }
    }
    // otherwise config.assetsHash already has hashes for all the assets (DLLs, ICU, .wasms, etc). 

    // Now we remove assets collection from the hash.
    delete inputs.assets;
    // some things are calculated at runtime, so we need to add them to the hash
    inputs.preferredIcuAsset = loaderHelpers.preferredIcuAsset;
    // timezone is part of env variables, so it is already in the hash

    // some things are not relevant for memory snapshot
    delete inputs.forwardConsoleLogsToWS;
    delete inputs.diagnosticTracing;
    delete inputs.appendElementOnExit;
    delete inputs.logExitCode;
    delete inputs.pthreadPoolSize;
    delete inputs.asyncFlushOnExit;
    delete inputs.assemblyRootFolder;
    delete inputs.remoteSources;
    delete inputs.ignorePdbLoadErrors;
    delete inputs.maxParallelDownloads;
    delete inputs.enableDownloadRetry;
    delete inputs.exitAfterSnapshot;
    delete inputs.assetUniqueQuery;

    inputs.GitHash = GitHash;
    inputs.ProductVersion = ProductVersion;

    const inputsJson = JSON.stringify(inputs);
    const sha256Buffer = await runtimeHelpers.subtle.digest("SHA-256", new TextEncoder().encode(inputsJson));
    const uint8ViewOfHash = new Uint8Array(sha256Buffer);
    const hashAsString = Array.from(uint8ViewOfHash).map((b) => b.toString(16).padStart(2, "0")).join("");
    runtimeHelpers.memorySnapshotCacheKey = `${memoryPrefix}-${hashAsString}`;
    return runtimeHelpers.memorySnapshotCacheKey;
}
