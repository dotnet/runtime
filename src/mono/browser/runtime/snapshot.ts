// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import WasmEnableThreads from "consts:wasmEnableThreads";
import { ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_WORKER, loaderHelpers, runtimeHelpers } from "./globals";
import { mono_log_warn } from "./logging";
import { MonoConfigInternal } from "./types/internal";

export const memoryPrefix = "https://dotnet.generated.invalid/wasm-memory";

// adapted from Blazor's WebAssemblyResourceLoader.ts
export async function openCache(): Promise<Cache | null> {
    // cache integrity is compromised if the first request has been served over http (except localhost)
    // in this case, we want to disable caching and integrity validation
    if (ENVIRONMENT_IS_WEB && globalThis.window.isSecureContext === false) {
        mono_log_warn("Failed to open the cache, running on an insecure origin");
        return null;
    }

    // caches will be undefined if we're running on an insecure origin (secure means https or localhost)
    if (typeof globalThis.caches === "undefined") {
        mono_log_warn("Failed to open the cache, probably running on an insecure origin");
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
        mono_log_warn("Failed to open cache");
        return null;
    }
}

export async function checkMemorySnapshotSize(): Promise<void> {
    try {
        if (!ENVIRONMENT_IS_WEB || ENVIRONMENT_IS_WORKER) {
            return;
        }
        if (!runtimeHelpers.config.startupMemoryCache) {
            // we could start downloading DLLs because snapshot is disabled
            return;
        }

        const cacheKey = await getMemorySnapshotCacheKey();
        if (!cacheKey) {
            return;
        }
        const cache = await openCache();
        if (!cache) {
            return;
        }
        const res = await cache.match(cacheKey);
        const contentLength = res?.headers.get("content-length");
        const memorySize = contentLength ? parseInt(contentLength) : undefined;

        runtimeHelpers.loadedMemorySnapshotSize = memorySize;
        runtimeHelpers.storeMemorySnapshotPending = !memorySize;
    } catch (ex) {
        mono_log_warn("Failed find memory snapshot in the cache", ex);
    }
    finally {
        if (!runtimeHelpers.loadedMemorySnapshotSize) {
            // we could start downloading DLLs because there is no snapshot yet
            loaderHelpers.memorySnapshotSkippedOrDone.promise_control.resolve();
        }
    }
}

export async function getMemorySnapshot(): Promise<ArrayBuffer | undefined> {
    const cacheKey = await getMemorySnapshotCacheKey();
    if (!cacheKey)
        return undefined;
    return await getCacheEntry(cacheKey);
}

export async function storeMemorySnapshot(memory: ArrayBuffer) {
    const cacheKey = await getMemorySnapshotCacheKey();
    if (!cacheKey)
        return;

    await storeCacheEntry(cacheKey, memory, "wasm-memory");

    cleanupCache(memoryPrefix, cacheKey); // no await
}

export async function getCacheEntry(cacheKey: string): Promise<ArrayBuffer | undefined> {
    try {
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
        mono_log_warn("Failed to load entry from the cache: " + cacheKey, ex);
        return undefined;
    }
}

export async function storeCacheEntry(cacheKey: string, memory: ArrayBuffer, mimeType: string): Promise<boolean> {
    try {
        const cache = await openCache();
        if (!cache) {
            return false;
        }
        const copy = WasmEnableThreads
            // storing SHaredArrayBuffer in the cache is not working
            ? (new Uint8Array(memory)).slice(0)
            : memory;

        const responseToCache = new Response(copy, {
            headers: {
                "content-type": mimeType,
                "content-length": memory.byteLength.toString(),
            },
        });

        await cache.put(cacheKey, responseToCache);

        return true;
    } catch (ex) {
        mono_log_warn("Failed to store entry to the cache: " + cacheKey, ex);
        return false;
    }
}

export async function cleanupCache(prefix: string, protectKey: string) {
    try {
        const cache = await openCache();
        if (!cache) {
            return;
        }
        const items = await cache.keys();
        for (const item of items) {
            if (item.url && item.url !== protectKey && item.url.startsWith(prefix)) {
                await cache.delete(item);
            }
        }
    } catch (ex) {
        return;
    }
}

export async function getMemorySnapshotCacheKey(): Promise<string | null> {
    if (runtimeHelpers.memorySnapshotCacheKey)
        return runtimeHelpers.memorySnapshotCacheKey;

    const result = await getCacheKey(memoryPrefix);
    if (result)
        runtimeHelpers.memorySnapshotCacheKey = result;
    return result;
}

// calculate hash of things which affect the memory snapshot
export async function getCacheKey(prefix: string): Promise<string | null> {
    if (!runtimeHelpers.subtle) {
        return null;
    }
    const inputs = Object.assign({}, runtimeHelpers.config) as MonoConfigInternal;

    // Now we remove assets collection from the hash.
    inputs.resourcesHash = inputs.resources!.hash;
    delete inputs.assets;
    delete inputs.resources;
    // some things are calculated at runtime, so we need to add them to the hash
    inputs.preferredIcuAsset = loaderHelpers.preferredIcuAsset;
    // timezone is part of env variables, so it is already in the hash

    // some things are not relevant for memory snapshot
    delete inputs.forwardConsoleLogsToWS;
    delete inputs.diagnosticTracing;
    delete inputs.appendElementOnExit;
    delete inputs.assertAfterExit;
    delete inputs.interopCleanupOnExit;
    delete inputs.logExitCode;
    delete inputs.pthreadPoolSize;
    delete inputs.asyncFlushOnExit;
    delete inputs.remoteSources;
    delete inputs.ignorePdbLoadErrors;
    delete inputs.maxParallelDownloads;
    delete inputs.enableDownloadRetry;
    delete inputs.exitAfterSnapshot;
    delete inputs.extensions;
    delete inputs.runtimeId;

    inputs.GitHash = loaderHelpers.gitHash;
    inputs.ProductVersion = ProductVersion;

    const inputsJson = JSON.stringify(inputs);
    const sha256Buffer = await runtimeHelpers.subtle.digest("SHA-256", new TextEncoder().encode(inputsJson));
    const uint8ViewOfHash = new Uint8Array(sha256Buffer);
    const hashAsString = Array.from(uint8ViewOfHash).map((b) => b.toString(16).padStart(2, "0")).join("");
    return `${prefix}-${hashAsString}`;
}
