// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
import ProductVersion from "consts:productVersion";
import WasmEnableThreads from "consts:wasmEnableThreads";

import { ENVIRONMENT_IS_WEB, Module, loaderHelpers, runtimeHelpers } from "./globals";
import { mono_log_info, mono_log_error, mono_log_warn } from "./logging";
import { localHeapViewU8 } from "./memory";
import cwraps from "./cwraps";
import { MonoConfigInternal } from "./types/internal";

export const tablePrefix = "https://dotnet.generated.invalid/interp_pgo";

export async function interp_pgo_save_data() {
    if (!loaderHelpers.is_runtime_running()) {
        mono_log_info("Skipped saving interp_pgo table (already exited)");
        return;
    }
    const cacheKey = await getCacheKey(tablePrefix);
    if (!cacheKey) {
        mono_log_error("Failed to save interp_pgo table (No cache key)");
        return;
    }

    try {
        const expectedSize = cwraps.mono_interp_pgo_save_table(<any>0, 0);
        // If save_table returned 0 despite not being passed a buffer, that means there is no
        //  table data to save, either because interp_pgo is disabled or no methods were tiered yet
        if (expectedSize <= 0) {
            mono_log_info("Failed to save interp_pgo table (No data to save)");
            return;
        }

        const pData = <any>Module._malloc(expectedSize);
        const saved = cwraps.mono_interp_pgo_save_table(pData, expectedSize) === 0;
        if (!saved) {
            mono_log_error("Failed to save interp_pgo table (Unknown error)");
            return;
        }

        const u8 = localHeapViewU8();
        const data = u8.slice(pData, pData + expectedSize);

        if (await storeCacheEntry(cacheKey, data, "application/octet-stream")) {
            mono_log_info("Saved interp_pgo table to cache");
        }

        cleanupCache(tablePrefix, cacheKey); // no await

        Module._free(pData);
    } catch (exc) {
        mono_log_error(`Failed to save interp_pgo table: ${exc}`);
    }
}

export async function interp_pgo_load_data() {
    const cacheKey = await getCacheKey(tablePrefix);
    if (!cacheKey) {
        mono_log_error("Failed to create cache key for interp_pgo table");
        return;
    }

    const data = await getCacheEntry(cacheKey);
    if (!data) {
        mono_log_info("Failed to load interp_pgo table (No table found in cache)");
        return;
    }

    const pData = <any>Module._malloc(data.byteLength);
    const u8 = localHeapViewU8();
    u8.set(new Uint8Array(data), pData);

    if (cwraps.mono_interp_pgo_load_table(pData, data.byteLength))
        mono_log_error("Failed to load interp_pgo table (Unknown error)");

    Module._free(pData);
}

async function openCache(): Promise<Cache | null> {
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

// calculate hash of things which affect config hash
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

    // some things are not relevant for config hash
    delete inputs.forwardConsoleLogsToWS;
    delete inputs.diagnosticTracing;
    delete inputs.appendElementOnExit;
    delete inputs.assertAfterExit;
    delete inputs.interopCleanupOnExit;
    delete inputs.dumpThreadsOnNonZeroExit;
    delete inputs.logExitCode;
    delete inputs.pthreadPoolSize;
    delete inputs.pthreadPoolReady;
    delete inputs.asyncFlushOnExit;
    delete inputs.remoteSources;
    delete inputs.ignorePdbLoadErrors;
    delete inputs.maxParallelDownloads;
    delete inputs.enableDownloadRetry;
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
