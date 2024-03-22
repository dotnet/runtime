// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { exceptions, simd } from "wasm-feature-detect";

import gitHash from "consts:gitHash";

import type { AssetEntryInternal, GlobalObjects, LoaderHelpers, RuntimeHelpers } from "../types/internal";
import type { MonoConfig, RuntimeAPI } from "../types";
import { assert_runtime_running, is_exited, is_runtime_running, mono_exit } from "./exit";
import { assertIsControllablePromise, createPromiseController, getPromiseController } from "./promise-controller";
import { mono_download_assets, resolve_single_asset_path, retrieve_asset_download } from "./assets";
import { setup_proxy_console } from "./logging";
import { invokeLibraryInitializers } from "./libraryInitializers";
import { isDebuggingSupported } from "./config";
import { logDownloadStatsToConsole, purgeUnusedCacheEntriesAsync } from "./assetsCache";

export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_WORKER = typeof importScripts == "function";
export const ENVIRONMENT_IS_WEB = typeof window == "object" || (ENVIRONMENT_IS_WORKER && !ENVIRONMENT_IS_NODE);
export const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

export let runtimeHelpers: RuntimeHelpers = {} as any;
export let loaderHelpers: LoaderHelpers = {} as any;
export let exportedRuntimeAPI: RuntimeAPI = {} as any;
export let INTERNAL: any = {};
export let _loaderModuleLoaded = false; // please keep it in place also as rollup guard

export const globalObjectsRoot: GlobalObjects = {
    mono: {},
    binding: {},
    internal: INTERNAL,
    module: {},
    loaderHelpers,
    runtimeHelpers,
    api: exportedRuntimeAPI,
} as any;

setLoaderGlobals(globalObjectsRoot);

export function setLoaderGlobals(
    globalObjects: GlobalObjects,
) {
    if (_loaderModuleLoaded) {
        throw new Error("Loader module already loaded");
    }
    _loaderModuleLoaded = true;
    runtimeHelpers = globalObjects.runtimeHelpers;
    loaderHelpers = globalObjects.loaderHelpers;
    exportedRuntimeAPI = globalObjects.api;
    INTERNAL = globalObjects.internal;
    Object.assign(exportedRuntimeAPI, {
        INTERNAL,
        invokeLibraryInitializers
    });

    Object.assign(globalObjects.module, {
        disableDotnet6Compatibility: true,
        config: { environmentVariables: {} }
    });
    Object.assign(runtimeHelpers, {
        mono_wasm_bindings_is_ready: false,
        javaScriptExports: {} as any,
        config: globalObjects.module.config,
        diagnosticTracing: false,
        abort: (reason: any) => { throw reason; },
    });
    Object.assign(loaderHelpers, {
        gitHash,
        config: globalObjects.module.config,
        diagnosticTracing: false,

        maxParallelDownloads: 16,
        enableDownloadRetry: true,
        assertAfterExit: !ENVIRONMENT_IS_WEB,

        _loaded_files: [],
        loadedFiles: [],
        loadedAssemblies: [],
        libraryInitializers: [],
        actual_downloaded_assets_count: 0,
        actual_instantiated_assets_count: 0,
        expected_downloaded_assets_count: 0,
        expected_instantiated_assets_count: 0,

        afterConfigLoaded: createPromiseController<MonoConfig>(),
        allDownloadsQueued: createPromiseController<void>(),
        wasmDownloadPromise: createPromiseController<AssetEntryInternal>(),
        runtimeModuleLoaded: createPromiseController<void>(),
        memorySnapshotSkippedOrDone: createPromiseController<void>(),

        is_exited,
        is_runtime_running,
        assert_runtime_running,
        mono_exit,
        createPromiseController,
        getPromiseController,
        assertIsControllablePromise,
        mono_download_assets,
        resolve_single_asset_path,
        setup_proxy_console,
        logDownloadStatsToConsole,
        purgeUnusedCacheEntriesAsync,

        retrieve_asset_download,
        invokeLibraryInitializers,
        isDebuggingSupported,

        // from wasm-feature-detect npm package
        exceptions,
        simd,

    } as Partial<LoaderHelpers>);
}

// this will abort the program if the condition is false
// see src\mono\wasm\runtime\rollup.config.js
// we inline the condition, because the lambda could allocate closure on hot path otherwise
export function mono_assert(condition: unknown, messageFactory: string | (() => string)): asserts condition {
    if (condition) return;
    const message = "Assert failed: " + (typeof messageFactory === "function"
        ? messageFactory()
        : messageFactory);
    const error = new Error(message);
    runtimeHelpers.abort(error);
}