// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type {
    LoggerType, AssertType, RuntimeAPI, LoaderExports,
    NativeBrowserExportsTable, LoaderExportsTable, RuntimeExportsTable, InternalExchange, BrowserHostExportsTable, InteropJavaScriptExportsTable, BrowserUtilsExportsTable
} from "./types";
import { InternalExchangeIndex } from "../types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { netLoaderConfig, getLoaderConfig } from "./config";
import { exit } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn } from "./logging";

import { dotnetAssert, dotnetInternals, dotnetLoaderExports, dotnetLogger, dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
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
        getConfig: getLoaderConfig,
        exit,
        invokeLibraryInitializers,
    };

    const internals:InternalExchange = [
        dotnetApi as RuntimeAPI, //0
        [], //1
        netLoaderConfig, //2
        null as any as LoaderExportsTable, //3
        null as any as RuntimeExportsTable, //4
        null as any as BrowserHostExportsTable, //5
        null as any as InteropJavaScriptExportsTable, //6
        null as any as NativeBrowserExportsTable, //7
        null as any as BrowserUtilsExportsTable, //8
    ];
    Object.assign(dotnetInternals, internals);
    const loaderFunctions: LoaderExports = {
        getRunMainPromise,
        rejectRunMainPromise,
        resolveRunMainPromise,
    };
    Object.assign(dotnetLoaderExports, loaderFunctions);
    const logger: LoggerType = {
        info,
        warn,
        error,
    };
    Object.assign(dotnetLogger, logger);
    const assert: AssertType = {
        check,
    };
    Object.assign(dotnetAssert, assert);

    dotnetInternals[InternalExchangeIndex.LoaderExportsTable] = loaderExportsToTable(dotnetLogger, dotnetAssert, dotnetLoaderExports);
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);
    return dotnetApi as RuntimeAPI;

    function loaderExportsToTable(logger:LoggerType, assert:AssertType, dotnetLoaderExports:LoaderExports):LoaderExportsTable {
        // keep in sync with loaderExportsFromTable()
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
