// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserHostExports, RuntimeAPI, BrowserHostExportsTable } from "./types";
import { InternalExchangeIndex } from "./types";
import { _ems_ } from "../../../libs/Common/JavaScript/ems-ambient";

import GitHash from "consts:gitHash";

import { runMain, runMainAndExit, registerDllBytes, installVfsFile, loadIcuData, initializeCoreCLR, registerPdbBytes } from "./host";

export function dotnetInitializeModule(internals: InternalExchange): void {
    if (!Array.isArray(internals)) throw new Error("Expected internals to be an array");
    const runtimeApiLocal: Partial<RuntimeAPI> = {
        runMain,
        runMainAndExit,
    };
    const runtimeApi = internals[InternalExchangeIndex.RuntimeAPI];
    if (typeof runtimeApi !== "object") throw new Error("Expected internals to have RuntimeAPI");
    if (runtimeApi.runtimeBuildInfo.gitHash && runtimeApi.runtimeBuildInfo.gitHash !== GitHash) {
        throw new Error(`Mismatched git hashes between loader and runtime. Loader: ${runtimeApi.runtimeBuildInfo.gitHash}, BrowserHost: ${GitHash}`);
    }
    Object.assign(runtimeApi, runtimeApiLocal);

    internals[InternalExchangeIndex.BrowserHostExportsTable] = browserHostExportsToTable({
        registerDllBytes,
        installVfsFile,
        loadIcuData,
        initializeCoreCLR,
        registerPdbBytes,
    });
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);
    function browserHostExportsToTable(map: BrowserHostExports): BrowserHostExportsTable {
        // keep in sync with browserHostExportsFromTable()
        return [
            map.registerDllBytes,
            map.installVfsFile,
            map.loadIcuData,
            map.initializeCoreCLR,
            map.registerPdbBytes,
        ];
    }
}

export { BrowserHost_ExternalAssemblyProbe } from "./host";
