// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntryInternal, GlobalObjects, LoaderHelpers, RuntimeHelpers } from "../types/internal";
import type { MonoConfig, RuntimeAPI } from "../types";
import { abort_startup, mono_exit } from "./exit";
import { assertIsControllablePromise, createPromiseController, getPromiseController } from "./promise-controller";
import { mono_download_assets, resolve_asset_path } from "./assets";
import { setup_proxy_console } from "./logging";

export const ENVIRONMENT_IS_NODE = typeof process == "object" && typeof process.versions == "object" && typeof process.versions.node == "string";
export const ENVIRONMENT_IS_WEB = typeof window == "object";
export const ENVIRONMENT_IS_WORKER = typeof importScripts == "function";
export const ENVIRONMENT_IS_SHELL = !ENVIRONMENT_IS_WEB && !ENVIRONMENT_IS_NODE && !ENVIRONMENT_IS_WORKER;

export let runtimeHelpers: RuntimeHelpers = null as any;
export let loaderHelpers: LoaderHelpers = null as any;
export let exportedRuntimeAPI: RuntimeAPI = null as any;
export let INTERNAL: any;
export let _loaderModuleLoaded = false; // please keep it in place also as rollup guard

export const globalObjectsRoot: GlobalObjects = {
    mono: {},
    binding: {},
    internal: {},
    module: {},
    loaderHelpers: {},
    runtimeHelpers: {},
    api: {}
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
        INTERNAL
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
    });
    Object.assign(loaderHelpers, {
        config: globalObjects.module.config,
        diagnosticTracing: false,

        maxParallelDownloads: 16,
        enableDownloadRetry: true,

        _loaded_files: [],
        loadedFiles: [],
        actual_downloaded_assets_count: 0,
        actual_instantiated_assets_count: 0,
        expected_downloaded_assets_count: 0,
        expected_instantiated_assets_count: 0,

        afterConfigLoaded: createPromiseController<MonoConfig>(),
        allDownloadsQueued: createPromiseController<void>(),
        wasmDownloadPromise: createPromiseController<AssetEntryInternal>(),
        runtimeModuleLoaded: createPromiseController<void>(),

        abort_startup,
        mono_exit,
        createPromiseController,
        getPromiseController,
        assertIsControllablePromise,
        mono_download_assets,
        resolve_asset_path,
        setup_proxy_console,

    } as Partial<LoaderHelpers>);
}
