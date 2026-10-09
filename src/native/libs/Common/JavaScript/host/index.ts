// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { InternalExchange, BrowserHostExports, RuntimeAPI, BrowserHostExportsTable, LoaderConfigInternal } from "./types";
import { InternalExchangeIndex } from "./types";
import { _ems_ } from "../ems-ambient";
import { ENVIRONMENT_IS_NODE } from "../per-module";

import GitHash from "consts:gitHash";

import { runMain, runMainAndExit, initializeCoreCLR } from "./host";
import { registerPdbBytes, instantiateWebcilModule, registerDllBytes, installVfsFile, loadIcuData, instantiateWasm, } from "./assets";

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
        instantiateWasm,
        instantiateWebcilModule,
    });
    _ems_.dotnetUpdateInternals(internals, _ems_.dotnetUpdateInternalsSubscriber);

    setupEmscripten();

    function browserHostExportsToTable(map: BrowserHostExports): BrowserHostExportsTable {
        // keep in sync with browserHostExportsFromTable()
        return [
            map.registerDllBytes,
            map.installVfsFile,
            map.loadIcuData,
            map.initializeCoreCLR,
            map.registerPdbBytes,
            map.instantiateWasm,
            map.instantiateWebcilModule,
        ];
    }
}

function setupEmscripten() {
    const loaderConfig = _ems_.dotnetApi.getConfig() as LoaderConfigInternal;
    if (!loaderConfig.resources ||
        !loaderConfig.resources.assembly ||
        !loaderConfig.resources.coreAssembly ||
        loaderConfig.resources.coreAssembly.length === 0 ||
        !loaderConfig.mainAssemblyName ||
        !loaderConfig.virtualWorkingDirectory ||
        !loaderConfig.environmentVariables) {
        throw new Error("Invalid runtime config, cannot initialize the runtime.");
    }

    const globalThisAny = globalThis as any;
    // On Node.js, process.env is the lowest-precedence source of environment variables: it must not
    // override values from the generated manifest (dotnet.js) or from withEnvironmentVariable().
    if (ENVIRONMENT_IS_NODE && globalThisAny.process && globalThisAny.process.env) {
        const processEnv = globalThisAny.process.env;
        for (const key in processEnv) {
            if (loaderConfig.environmentVariables[key] === undefined) {
                loaderConfig.environmentVariables[key] = processEnv[key];
            }
        }
    }

    for (const key in loaderConfig.environmentVariables) {
        _ems_.ENV[key] = loaderConfig.environmentVariables[key];
    }
}

export { BrowserHost_ExternalAssemblyProbe } from "./assets";
