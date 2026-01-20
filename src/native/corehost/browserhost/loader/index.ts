// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type {
    LoggerType, AssertType, RuntimeAPI, LoaderExports,
    NativeBrowserExportsTable, LoaderExportsTable, RuntimeExportsTable, InternalExchange, BrowserHostExportsTable, InteropJavaScriptExportsTable, BrowserUtilsExportsTable,
    EmscriptenModuleInternal,
    DiagnosticsExportsTable
} from "./types";
import { InternalExchangeIndex } from "../types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { loaderConfig, getLoaderConfig } from "./config";
import { exit, isExited, isRuntimeRunning, addOnExitListener, registerExit, quitNow } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn, debug, fastCheck } from "./logging";

import { dotnetAssert, dotnetLoaderExports, dotnetLogger, dotnetUpdateInternals, dotnetUpdateInternalsSubscriber } from "./cross-module";
import { rejectRunMainPromise, resolveRunMainPromise, getRunMainPromise, abortStartup } from "./run";
import { createPromiseCompletionSource, getPromiseCompletionSource, isControllablePromise } from "./promise-completion-source";
import { instantiateWasm } from "./assets";

export function dotnetInitializeModule(): RuntimeAPI {

    const dotnetApi: Partial<RuntimeAPI> = {
        INTERNAL: {},
        Module: {} as any,
        runtimeId: undefined,
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

    const internals: InternalExchange = [
        dotnetApi as RuntimeAPI, //0
        [], //1
        loaderConfig, //2
        undefined as any as LoaderExportsTable, //3
        undefined as any as RuntimeExportsTable, //4
        undefined as any as BrowserHostExportsTable, //5
        undefined as any as InteropJavaScriptExportsTable, //6
        undefined as any as NativeBrowserExportsTable, //7
        undefined as any as BrowserUtilsExportsTable, //8
        undefined as any as DiagnosticsExportsTable, //9
    ];
    const loaderFunctions: LoaderExports = {
        getRunMainPromise,
        rejectRunMainPromise,
        resolveRunMainPromise,
        createPromiseCompletionSource,
        isControllablePromise,
        getPromiseCompletionSource,
        isExited,
        isRuntimeRunning,
        addOnExitListener,
        abortStartup,
        quitNow,
    };
    Object.assign(dotnetLoaderExports, loaderFunctions);
    const logger: LoggerType = {
        debug,
        info,
        warn,
        error,
    };
    Object.assign(dotnetLogger, logger);
    const assert: AssertType = {
        check,
        fastCheck,
    };
    Object.assign(dotnetAssert, assert);

    // emscripten extension point
    const localModule: Partial<EmscriptenModuleInternal> = {
        instantiateWasm,
    };
    Object.assign(dotnetApi.Module!, localModule);

    internals[InternalExchangeIndex.LoaderExportsTable] = loaderExportsToTable(dotnetLogger, dotnetAssert, dotnetLoaderExports);
    dotnetUpdateInternals(internals, dotnetUpdateInternalsSubscriber);

    registerExit();

    function loaderExportsToTable(logger: LoggerType, assert: AssertType, dotnetLoaderExports: LoaderExports): LoaderExportsTable {
        // keep in sync with loaderExportsFromTable()
        return [
            logger.debug,
            logger.info,
            logger.warn,
            logger.error,
            assert.check,
            assert.fastCheck,
            dotnetLoaderExports.resolveRunMainPromise,
            dotnetLoaderExports.rejectRunMainPromise,
            dotnetLoaderExports.getRunMainPromise,
            dotnetLoaderExports.createPromiseCompletionSource,
            dotnetLoaderExports.isControllablePromise,
            dotnetLoaderExports.getPromiseCompletionSource,
            dotnetLoaderExports.isExited,
            dotnetLoaderExports.isRuntimeRunning,
            dotnetLoaderExports.addOnExitListener,
            dotnetLoaderExports.abortStartup,
            dotnetLoaderExports.quitNow,
        ];
    }

    return dotnetApi as RuntimeAPI;

}
