// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { dotnetLoggerType, dotnetAssertType, RuntimeAPI, LoaderExports, NativeBrowserExportsTable, LoaderExportsTable, RuntimeExportsTable, InternalExchange, BrowserHostExportsTable, InteropJavaScriptExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { netLoaderConfig, getLoaderConfig } from "./config";
import { exit } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn } from "./logging";

import { dotnetAssert, dotnetInternals, dotnetJSEngine, dotnetLoaderExports, dotnetTabLE, dotnetLogger, dotnetSetInternals, dotnetUpdateAllInternals, dotnetUpdateModuleInternals } from "./cross-module";
import { rejectRunMainPromise, resolveRunMainPromise, getRunMainPromise } from "./run";

export function dotnetInitializeModule(): RuntimeAPI {
    const ENVIRONMENT_IS_NODE = () => typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
    const ENVIRONMENT_IS_WEB_WORKER = () => typeof importScripts == "function";
    const ENVIRONMENT_IS_SIDECAR = () => ENVIRONMENT_IS_WEB_WORKER() && typeof dotnetSidecar !== "undefined"; // sidecar is emscripten main running in a web worker
    const ENVIRONMENT_IS_WORKER = () => ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_SIDECAR(); // we redefine what ENVIRONMENT_IS_WORKER, we replace it in emscripten internals, so that sidecar works
    const ENVIRONMENT_IS_WEB = () => typeof window == "object" || (ENVIRONMENT_IS_WEB_WORKER() && !ENVIRONMENT_IS_NODE());
    const ENVIRONMENT_IS_SHELL = () => !ENVIRONMENT_IS_WEB() && !ENVIRONMENT_IS_NODE();

    const dotnetApi: Partial<RuntimeAPI> = {
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

    const internals:InternalExchange = [
        dotnetApi as RuntimeAPI, //0
        [dotnetUpdateModuleInternals], //1
        netLoaderConfig, //2
        null as any as RuntimeExportsTable, //3
        null as any as LoaderExportsTable, //4
        null as any as BrowserHostExportsTable, //5
        null as any as InteropJavaScriptExportsTable, //6
        null as any as NativeBrowserExportsTable, //7
    ];
    dotnetSetInternals(internals);
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
    const logger: dotnetLoggerType = {
        info,
        warn,
        error,
    };
    const assert: dotnetAssertType = {
        check,
    };
    Object.assign(dotnetApi, runtimeApiFunctions);
    Object.assign(dotnetLogger, logger);
    Object.assign(dotnetAssert, assert);
    Object.assign(dotnetJSEngine, jsEngine);
    Object.assign(dotnetLoaderExports, loaderFunctions);
    dotnetInternals[InternalExchangeIndex.LoaderExportsTable] = dotnetTabLE(dotnetLogger, dotnetAssert, dotnetLoaderExports);
    dotnetUpdateAllInternals();
    return dotnetApi as RuntimeAPI;
}
