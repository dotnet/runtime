// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./globals";
import { getCacheKey, cleanupCache, getCacheEntry, storeCacheEntry } from "./snapshot";
import { mono_log_info, mono_log_error } from "./logging";
import { localHeapViewU8 } from "./memory";
import cwraps from "./cwraps";

export const tablePrefix = "https://dotnet.generated.invalid/interp_pgo";

export async function getInterpPgoTable(): Promise<ArrayBuffer | undefined> {
    const cacheKey = await getCacheKey(tablePrefix);
    if (!cacheKey)
        return undefined;
    return await getCacheEntry(cacheKey);
}

async function storeInterpPgoTable(memory: ArrayBuffer) {
    const cacheKey = await getCacheKey(tablePrefix);
    if (!cacheKey)
        return;

    await storeCacheEntry(cacheKey, memory, "application/octet-stream");

    cleanupCache(tablePrefix, cacheKey); // no await
}

export async function interp_pgo_save_data () {
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

        await storeInterpPgoTable(data);

        mono_log_info("Saved interp_pgo table to cache");
        Module._free(pData);
    } catch (exc) {
        mono_log_error(`Failed to save interp_pgo table: ${exc}`);
    }
}

export async function interp_pgo_load_data () {
    const data = await getInterpPgoTable();
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
