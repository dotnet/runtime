// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoggerType, AssertType, RuntimeAPI, LoaderExports } from "./types";

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import GitHash from "consts:gitHash";

import { config, getConfig } from "./config";
import { exit } from "./exit";
import { invokeLibraryInitializers } from "./lib-initializers";
import { check, error, info, warn } from "./logging";

import { Assert, dotnetInternals, loaderExports, loaderExportsToTable, Logger, setInternals, updateInternals, updateInternalsImpl } from "./cross-module";
import { browserHostRejectMain, browserHostResolveMain, getRunMainPromise } from "./run";

export function initialize(): RuntimeAPI {
    const runtimeApi: Partial<RuntimeAPI> = {
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

    const updates: (() => void)[] = [];
    setInternals({
        config: config,
        runtimeApi: runtimeApi as RuntimeAPI,
        updates,
    });
    const runtimeApiFunctions: Partial<RuntimeAPI> = {
        getConfig,
        exit,
        invokeLibraryInitializers,
    };
    const loaderFunctions: LoaderExports = {
        getRunMainPromise,
        browserHostRejectMain,
        browserHostResolveMain,
    };
    const logger: LoggerType = {
        info,
        warn,
        error,
    };
    const assert: AssertType = {
        check,
    };
    Object.assign(runtimeApi, runtimeApiFunctions);
    Object.assign(Logger, logger);
    Object.assign(Assert, assert);
    Object.assign(loaderExports, loaderFunctions);
    dotnetInternals.loaderExportsTable = [...loaderExportsToTable(Logger, Assert, loaderExports)];
    updates.push(updateInternalsImpl);
    updateInternals();

    return runtimeApi as RuntimeAPI;
}
