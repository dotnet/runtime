// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoConfig } from "../types";
import type { AssetEntryInternal } from "../types/internal";
import { ENVIRONMENT_IS_WEB, loaderHelpers } from "./globals";

const usedCacheKeys: { [key: string]: boolean } = {};
const networkLoads: { [name: string]: LoadLogEntry } = {};
const cacheLoads: { [name: string]: LoadLogEntry } = {};
let cacheIfUsed: Cache | null;

export function logDownloadStatsToConsole(): void {
    const cacheLoadsEntries = Object.values(cacheLoads);
    const networkLoadsEntries = Object.values(networkLoads);
    const cacheResponseBytes = countTotalBytes(cacheLoadsEntries);
    const networkResponseBytes = countTotalBytes(networkLoadsEntries);
    const totalResponseBytes = cacheResponseBytes + networkResponseBytes;
    if (totalResponseBytes === 0) {
        // We have no perf stats to display, likely because caching is not in use.
        return;
    }
    const useStyle = ENVIRONMENT_IS_WEB ? "%c" : "";
    const style = ENVIRONMENT_IS_WEB ? ["background: purple; color: white; padding: 1px 3px; border-radius: 3px;",
        "font-weight: bold;",
        "font-weight: normal;",
    ] : [];
    const linkerDisabledWarning = !loaderHelpers.config.linkerEnabled ? "\nThis application was built with linking (tree shaking) disabled. \nPublished applications will be significantly smaller if you install wasm-tools workload. \nSee also https://aka.ms/dotnet-wasm-features" : "";
    // eslint-disable-next-line no-console
    console.groupCollapsed(`${useStyle}dotnet${useStyle} Loaded ${toDataSizeString(totalResponseBytes)} resources${useStyle}${linkerDisabledWarning}`, ...style);

    if (cacheLoadsEntries.length) {
        // eslint-disable-next-line no-console
        console.groupCollapsed(`Loaded ${toDataSizeString(cacheResponseBytes)} resources from cache`);
        // eslint-disable-next-line no-console
        console.table(cacheLoads);
        // eslint-disable-next-line no-console
        console.groupEnd();
    }

    if (networkLoadsEntries.length) {
        // eslint-disable-next-line no-console
        console.groupCollapsed(`Loaded ${toDataSizeString(networkResponseBytes)} resources from network`);
        // eslint-disable-next-line no-console
        console.table(networkLoads);
        // eslint-disable-next-line no-console
        console.groupEnd();
    }

    // eslint-disable-next-line no-console
    console.groupEnd();
}

export async function purgeUnusedCacheEntriesAsync(): Promise<void> {
    // We want to keep the cache small because, even though the browser will evict entries if it
    // gets too big, we don't want to be considered problematic by the end user viewing storage stats
    const cache = cacheIfUsed;
    if (cache) {
        const cachedRequests = await cache.keys();
        const deletionPromises = cachedRequests.map(async cachedRequest => {
            if (!(cachedRequest.url in usedCacheKeys)) {
                await cache.delete(cachedRequest);
            }
        });

        await Promise.all(deletionPromises);
    }
}

export async function findCachedResponse(asset: AssetEntryInternal): Promise<Response | undefined> {
    const cache = cacheIfUsed;
    if (!cache || asset.noCache || !asset.hash || asset.hash.length === 0) {
        return undefined;
    }

    const cacheKey = getCacheKey(asset);
    usedCacheKeys[cacheKey] = true;

    let cachedResponse: Response | undefined;
    try {
        cachedResponse = await cache.match(cacheKey);
    } catch {
        // Be tolerant to errors reading from the cache. This is a guard for https://bugs.chromium.org/p/chromium/issues/detail?id=968444 where
        // chromium browsers may sometimes throw when working with the cache.
    }

    if (!cachedResponse) {
        return undefined;
    }

    // It's in the cache.
    const responseBytes = parseInt(cachedResponse.headers.get("content-length") || "0");
    cacheLoads[asset.name] = { responseBytes };
    return cachedResponse;
}

export function addCachedReponse(asset: AssetEntryInternal, networkResponse: Response): void {
    const cache = cacheIfUsed;
    if (!cache || asset.noCache || !asset.hash || asset.hash.length === 0) {
        return;
    }
    const clonedResponse = networkResponse.clone();

    // postpone adding to cache until after we load the assembly, so that we could do the dotnet loading of the asset first
    setTimeout(() => {
        const cacheKey = getCacheKey(asset);
        addToCacheAsync(cache, asset.name, cacheKey, clonedResponse); // Don't await - add to cache in background
    }, 0);
}

function getCacheKey(asset: AssetEntryInternal) {
    return `${asset.resolvedUrl}.${asset.hash}`;
}

async function addToCacheAsync(cache: Cache, name: string, cacheKey: string, clonedResponse: Response) {
    // We have to clone in order to put this in the cache *and* not prevent other code from
    // reading the original response stream.
    const responseData = await clonedResponse.arrayBuffer();

    // Now is an ideal moment to capture the performance stats for the request, since it
    // only just completed and is most likely to still be in the buffer. However this is
    // only done on a 'best effort' basis. Even if we do receive an entry, some of its
    // properties may be blanked out if it was a CORS request.
    const performanceEntry = getPerformanceEntry(clonedResponse.url);
    const responseBytes = (performanceEntry && performanceEntry.encodedBodySize) || undefined;
    networkLoads[name] = { responseBytes };

    // Add to cache as a custom response object so we can track extra data such as responseBytes
    // We can't rely on the server sending content-length (ASP.NET Core doesn't by default)
    const responseToCache = new Response(responseData, {
        headers: {
            "content-type": clonedResponse.headers.get("content-type") || "",
            "content-length": (responseBytes || clonedResponse.headers.get("content-length") || "").toString(),
        },
    });

    try {
        await cache.put(cacheKey, responseToCache);
    } catch {
        // Be tolerant to errors writing to the cache. This is a guard for https://bugs.chromium.org/p/chromium/issues/detail?id=968444 where
        // chromium browsers may sometimes throw when performing cache operations.
    }
}

export async function initCacheToUseIfEnabled(): Promise<void> {
    cacheIfUsed = await getCacheToUseIfEnabled(loaderHelpers.config);
}

async function getCacheToUseIfEnabled(config: MonoConfig): Promise<Cache | null> {
    // caches will be undefined if we're running on an insecure origin (secure means https or localhost)
    if (!config.cacheBootResources || typeof globalThis.caches === "undefined" || typeof globalThis.document === "undefined") {
        return null;
    }

    // cache integrity is compromised if the first request has been served over http (except localhost)
    // in this case, we want to disable caching and integrity validation
    if (globalThis.isSecureContext === false) {
        return null;
    }

    // Define a separate cache for each base href, so we're isolated from any other
    // Blazor application running on the same origin. We need this so that we're free
    // to purge from the cache anything we're not using and don't let it keep growing,
    // since we don't want to be worst offenders for space usage.
    const relativeBaseHref = globalThis.document.baseURI.substring(globalThis.document.location.origin.length);
    const cacheName = `dotnet-resources-${relativeBaseHref}`;

    try {
        // There's a Chromium bug we need to be aware of here: the CacheStorage APIs say that when
        // caches.open(name) returns a promise that succeeds, the value is meant to be a Cache instance.
        // However, if the browser was launched with a --user-data-dir param that's "too long" in some sense,
        // then even through the promise resolves as success, the value given is `undefined`.
        // See https://stackoverflow.com/a/46626574 and https://bugs.chromium.org/p/chromium/issues/detail?id=1054541
        // If we see this happening, return "null" to mean "proceed without caching".
        return (await caches.open(cacheName)) || null;
    } catch {
        // There's no known scenario where we should get an exception here, but considering the
        // Chromium bug above, let's tolerate it and treat as "proceed without caching".
        return null;
    }
}

function countTotalBytes(loads: LoadLogEntry[]) {
    return loads.reduce((prev, item) => prev + (item.responseBytes || 0), 0);
}

function toDataSizeString(byteCount: number) {
    return `${(byteCount / (1024 * 1024)).toFixed(2)} MB`;
}

function getPerformanceEntry(url: string): PerformanceResourceTiming | undefined {
    if (typeof performance !== "undefined") {
        return performance.getEntriesByName(url)[0] as PerformanceResourceTiming;
    }
}

interface LoadLogEntry {
    responseBytes: number | undefined;
}

export interface LoadingResource {
    name: string;
    url: string;
    response: Promise<Response>;
}
