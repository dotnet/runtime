// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoggerType, AssertType, RuntimeAPI, LoaderExports } from "./types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { netLoaderConfig, getLoaderConfig } from "./config";
import { exit } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn } from "./logging";

import { Assert, netInternals, netJSEngine, netLoaderExports, netTabulateLE, Logger, netSetInternals, netUpdateAllInternals, netUpdateModuleInternals } from "./cross-module";
import { rejectRunMainPromise, resolveRunMainPromise, getRunMainPromise } from "./run";

export function netInitializeModule(): RuntimeAPI {
    const ENVIRONMENT_IS_NODE = () => typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
    const ENVIRONMENT_IS_WEB_WORKER = () => typeof importScripts == "function";
    const ENVIRONMENT_IS_SIDECAR = () => ENVIRONMENT_IS_WEB_WORKER() && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
    const ENVIRONMENT_IS_WORKER = () => ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_SIDECAR(); // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
    const ENVIRONMENT_IS_WEB = () => typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_NODE());
    const ENVIRONMENT_IS_SHELL = () => !ENVIRONMENT_IS_WEB() && !ENVIRONMENT_IS_NODE();

    const netPublicApi: Partial<RuntimeAPI> = {
        INTERNAL: {},
        Module: {} as any,
        runtimeId: -1,
        runtimeBuildInfo: {
            productVersion: ProductVersion,
            gitHash: GitHash,
            buildConfiguration: BuildConfiguration,
            wasmEnableThreads: false,
            wasmEnableSIMD: true,
            wasmEnableExceptionHandling: true,
        },
    };

    const netInternalUpdates: (() => void)[] = [];
    netSetInternals({
        netLoaderConfig: netLoaderConfig,
        netPublicApi: netPublicApi as RuntimeAPI,
        netInternalUpdates,
    });
    const runtimeApiFunctions: Partial<RuntimeAPI> = {
        getConfig: getLoaderConfig,
        exit,
        invokeLibraryInitializers,
    };
    const loaderFunctions: LoaderExports = {
        ENVIRONMENT_IS_NODE,
        ENVIRONMENT_IS_SHELL,
        ENVIRONMENT_IS_WEB,
        ENVIRONMENT_IS_WORKER,
        ENVIRONMENT_IS_SIDECAR,
        getRunMainPromise,
        rejectRunMainPromise,
        resolveRunMainPromise,
    };
    const jsEngine = {
        IS_NODE: ENVIRONMENT_IS_NODE(),
        IS_SHELL: ENVIRONMENT_IS_SHELL(),
        IS_WEB: ENVIRONMENT_IS_WEB(),
        IS_WORKER: ENVIRONMENT_IS_WORKER(),
        IS_SIDECAR: ENVIRONMENT_IS_SIDECAR(),
    };
    const logger: LoggerType = {
        info,
        warn,
        error,
    };
    const assert: AssertType = {
        check,
    };
    Object.assign(netPublicApi, runtimeApiFunctions);
    Object.assign(Logger, logger);
    Object.assign(Assert, assert);
    Object.assign(netJSEngine, jsEngine);
    Object.assign(netLoaderExports, loaderFunctions);
    netInternals.netLoaderExportsTable = [...netTabulateLE(Logger, Assert, netLoaderExports)];
    netInternalUpdates.push(netUpdateModuleInternals);
    netUpdateAllInternals();

    return netPublicApi as RuntimeAPI;
}
