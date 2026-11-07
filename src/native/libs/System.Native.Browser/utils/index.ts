// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserUtilsExports, BrowserUtilsExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";

import GitHash from "consts:gitHash";

import {
    installMemoryAPI, zeroRegion,
    arrayBufferNeedsCopy,
    viewOrCopy,
} from "./memory";
import { stringsInit, stringToUTF16, stringToUTF16Ptr, stringToUTF8, stringToUTF8Ptr, utf16ToString, utf8ToStringRelaxed } from "./strings";
import { abortPosix, getExitStatus } from "./host";
import { dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "../utils/cross-module";
import { initPolyfills } from "../utils/polyfills";
import { registerRuntime } from "./runtime-list";
import { registerCDAC } from "./cdac";
import { abortBackgroundTimers, runBackgroundTimers } from "./scheduling";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");

    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== GitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, BrowserUtils: ${GitHash}`);
    }

    initPolyfills();
    stringsInit();
    registerRuntime(runtimeApi);
    registerCDAC(runtimeApi);

    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    installMemoryAPI(runtimeApi);

    internals[InternalExchangeIndex.BrowserUtilsExportsTable] = browserUtilsExportsToTable({
        utf16ToString,
        stringToUTF16,
        stringToUTF16Ptr,
        stringToUTF8Ptr,
        stringToUTF8,
        utf8ToStringRelaxed,
        zeroRegion,
        arrayBufferNeedsCopy,
        viewOrCopy,
        abortBackgroundTimers,
        abortPosix,
        getExitStatus,
        runBackgroundTimers,
    });
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
    function browserUtilsExportsToTable(map: BrowserUtilsExports): BrowserUtilsExportsTable {
        // keep in sync with browserUtilsExportsFromTable()
        return [
            map.utf16ToString,
            map.stringToUTF16,
            map.stringToUTF16Ptr,
            map.stringToUTF8Ptr,
            map.stringToUTF8,
            map.utf8ToStringRelaxed,
            map.zeroRegion,
            map.arrayBufferNeedsCopy,
            map.viewOrCopy,
            map.abortBackgroundTimers,
            map.abortPosix,
            map.getExitStatus,
            map.runBackgroundTimers,
        ];
    }
}

// see also `reserved` in `rollup.config.defines.js`
export * as _ems_ambient_ from "./cross-module";
