// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoggerType, AssertType, RuntimeAPI, LoaderExports, NativeBrowserExportsTable, LoaderExportsTable, RuntimeExportsTable, InternalExchange, BrowserHostExportsTable, InteropJavaScriptExportsTable } from "./types";
import { InternalExchangeIndex } from "../types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { netLoaderConfig, getLoaderConfig } from "./config";
import { exit } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn } from "./logging";

import { dotnetAssert, dotnetInternals, dotnetLoaderExports, dotnetLogger, dotnetSetInternals, dotnetUpdateAllInternals, dotnetUpdateModuleInternals } from "./cross-module";
import { rejectRunMainPromise, resolveRunMainPromise, getRunMainPromise } from "./run";

export function dotnetInitializeModule(): RuntimeAPI {

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
        getRunMainPromise,
        rejectRunMainPromise,
        resolveRunMainPromise,
    };
    const logger: LoggerType = {
        info,
        warn,
        error,
    };
    const assert: AssertType = {
        check,
    };
    Object.assign(dotnetApi, runtimeApiFunctions);
    Object.assign(dotnetLogger, logger);
    Object.assign(dotnetAssert, assert);
    Object.assign(dotnetLoaderExports, loaderFunctions);
    dotnetInternals[InternalExchangeIndex.LoaderExportsTable] = tabulateLoaderExports(dotnetLogger, dotnetAssert, dotnetLoaderExports);
    dotnetUpdateAllInternals();
    return dotnetApi as RuntimeAPI;

    function tabulateLoaderExports(logger:LoggerType, assert:AssertType, dotnetLoaderExports:LoaderExports):LoaderExportsTable {
        // keep in sync with dotnetUpdateModuleInternals()
        return [
            logger.info,
            logger.warn,
            logger.error,
            assert.check,
            dotnetLoaderExports.resolveRunMainPromise,
            dotnetLoaderExports.rejectRunMainPromise,
            dotnetLoaderExports.getRunMainPromise,
        ];
    }
}
