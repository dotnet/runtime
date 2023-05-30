// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import type { DotnetModuleInternal, MonoConfigInternal } from "../types/internal";
import type { DotnetModuleConfig } from "../types";
import { exportedRuntimeAPI, loaderHelpers, runtimeHelpers } from "./globals";
import { initializeBootConfig, loadBootConfig } from "./blazor/_Integration";
import { BootConfigResult } from "./blazor/BootConfig";
import { BootJsonData } from "../types/blazor";
import { mono_log_error, mono_log_debug } from "./logging";

export function deep_merge_config(target: MonoConfigInternal, source: MonoConfigInternal): MonoConfigInternal {
    const providedConfig: MonoConfigInternal = { ...source };
    if (providedConfig.assets) {
        providedConfig.assets = [...(target.assets || []), ...(providedConfig.assets || [])];
    }
    if (providedConfig.environmentVariables) {
        providedConfig.environmentVariables = { ...(target.environmentVariables || {}), ...(providedConfig.environmentVariables || {}) };
    }
    if (providedConfig.startupOptions) {
        providedConfig.startupOptions = { ...(target.startupOptions || {}), ...(providedConfig.startupOptions || {}) };
    }
    if (providedConfig.runtimeOptions) {
        providedConfig.runtimeOptions = [...(target.runtimeOptions || []), ...(providedConfig.runtimeOptions || [])];
    }
    return Object.assign(target, providedConfig);
}

export function deep_merge_module(target: DotnetModuleInternal, source: DotnetModuleConfig): DotnetModuleInternal {
    const providedConfig: DotnetModuleConfig = { ...source };
    if (providedConfig.config) {
        if (!target.config) target.config = {};
        providedConfig.config = deep_merge_config(target.config, providedConfig.config);
    }
    return Object.assign(target, providedConfig);
}

// NOTE: this is called before setRuntimeGlobals
export function normalizeConfig() {
    // normalize
    const config = loaderHelpers.config;

    config.environmentVariables = config.environmentVariables || {};
    config.assets = config.assets || [];
    config.runtimeOptions = config.runtimeOptions || [];
    config.globalizationMode = config.globalizationMode || "auto";

    if (config.debugLevel === undefined && BuildConfiguration === "Debug") {
        config.debugLevel = -1;
    }
    if (config.diagnosticTracing === undefined && BuildConfiguration === "Debug") {
        config.diagnosticTracing = true;
    }
    runtimeHelpers.diagnosticTracing = loaderHelpers.diagnosticTracing = !!config.diagnosticTracing;
    loaderHelpers.assetUniqueQuery = config.assetUniqueQuery;
    runtimeHelpers.waitForDebugger = config.waitForDebugger;
    config.startupMemoryCache = !!config.startupMemoryCache;
    if (config.startupMemoryCache && runtimeHelpers.waitForDebugger) {
        mono_log_debug("Disabling startupMemoryCache because waitForDebugger is set");
        config.startupMemoryCache = false;
    }

    runtimeHelpers.enablePerfMeasure = !!config.browserProfilerOptions
        && globalThis.performance
        && typeof globalThis.performance.measure === "function";

}

let configLoaded = false;
export async function mono_wasm_load_config(module: DotnetModuleInternal): Promise<void> {
    const configFilePath = module.configSrc;
    if (configLoaded) {
        await loaderHelpers.afterConfigLoaded.promise;
        return;
    }
    configLoaded = true;
    if (!configFilePath) {
        normalizeConfig();
        loaderHelpers.afterConfigLoaded.promise_control.resolve(loaderHelpers.config);
        return;
    }
    mono_log_debug("mono_wasm_load_config");
    try {
        loaderHelpers.config.applicationEnvironment = loaderHelpers.config.applicationEnvironment ?? loaderHelpers.config.startupOptions?.environment ?? "Production";

        if (loaderHelpers.config.startupOptions && loaderHelpers.config.startupOptions.loadBootResource) {
            // If we have custom loadBootResource
            await loadBootConfig(loaderHelpers.config, module);
        } else {
            // Otherwise load using fetch_like
            const resolveSrc = loaderHelpers.locateFile(configFilePath);
            const configResponse = await loaderHelpers.fetch_like(resolveSrc);
            const loadedAnyConfig: any = (await configResponse.json()) || {};
            if (loadedAnyConfig.resources) {
                // If we found boot config schema
                await initializeBootConfig(BootConfigResult.fromFetchResponse(configResponse, loadedAnyConfig as BootJsonData, loaderHelpers.config.applicationEnvironment), module, loaderHelpers.config.startupOptions);
            } else {
                // Otherwise we found mono config schema
                const loadedConfig = loadedAnyConfig as MonoConfigInternal;
                if (loadedConfig.environmentVariables && typeof (loadedConfig.environmentVariables) !== "object")
                    throw new Error("Expected config.environmentVariables to be unset or a dictionary-style object");
                deep_merge_config(loaderHelpers.config, loadedConfig);
            }
        }

        normalizeConfig();

        if (module.onConfigLoaded) {
            try {
                await module.onConfigLoaded(loaderHelpers.config, exportedRuntimeAPI);
                normalizeConfig();
            }
            catch (err: any) {
                mono_log_error("onConfigLoaded() failed", err);
                throw err;
            }
        }
        loaderHelpers.afterConfigLoaded.promise_control.resolve(loaderHelpers.config);
    } catch (err) {
        const errMessage = `Failed to load config file ${configFilePath} ${err}`;
        loaderHelpers.config = module.config = <any>{ message: errMessage, error: err, isError: true };
        loaderHelpers.abort_startup(errMessage, true);
        throw err;
    }
}