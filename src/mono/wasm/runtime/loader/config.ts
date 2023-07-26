// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";
import type { DotnetModuleInternal, MonoConfigInternal } from "../types/internal";
import type { DotnetModuleConfig, MonoConfig } from "../types";
import { ENVIRONMENT_IS_WEB, exportedRuntimeAPI, loaderHelpers, runtimeHelpers } from "./globals";
import { hookDownloadResource, mapResourcesToAssets } from "./blazor/_Integration";
import { mono_log_error, mono_log_debug } from "./logging";
import { invokeLibraryInitializers } from "./libraryInitializers";
import { mono_exit } from "./exit";

export function deep_merge_config(target: MonoConfigInternal, source: MonoConfigInternal): MonoConfigInternal {
    const providedConfig: MonoConfigInternal = { ...source };
    if (providedConfig.assets) {
        providedConfig.assets = [...(target.assets || []), ...(providedConfig.assets || [])];
    }
    if (providedConfig.resources) {
        providedConfig.resources = { ...(target.resources || {}), ...(providedConfig.resources || {}) };
    }
    if (providedConfig.environmentVariables) {
        providedConfig.environmentVariables = { ...(target.environmentVariables || {}), ...(providedConfig.environmentVariables || {}) };
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
    loaderHelpers.assertAfterExit = config.assertAfterExit = config.assertAfterExit || !ENVIRONMENT_IS_WEB;

    if (config.debugLevel === undefined && BuildConfiguration === "Debug") {
        config.debugLevel = -1;
    }

    // Default values (when WasmDebugLevel is not set)
    // - Build   (debug)    => debugBuild=true  & debugLevel=-1 => -1
    // - Build   (release)  => debugBuild=true  & debugLevel=0  => 0
    // - Publish (debug)    => debugBuild=false & debugLevel=-1 => 0
    // - Publish (release)  => debugBuild=false & debugLevel=0  => 0
    config.debugLevel = hasDebuggingEnabled(config) ? config.debugLevel : 0;

    if (config.diagnosticTracing === undefined && BuildConfiguration === "Debug") {
        config.diagnosticTracing = true;
    }
    if (config.applicationCulture) {
        // If a culture is specified via start options use that to initialize the Emscripten \  .NET culture.
        config.environmentVariables!["LANG"] = `${config.applicationCulture}.UTF-8`;
    }

    runtimeHelpers.diagnosticTracing = loaderHelpers.diagnosticTracing = !!config.diagnosticTracing;
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
        await loadBootConfig(module);

        normalizeConfig();

        await invokeLibraryInitializers("onRuntimeConfigLoaded", [loaderHelpers.config], "onRuntimeConfigLoaded");

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
        const errMessage = `Failed to load config file ${configFilePath} ${err} ${(err as Error)?.stack}`;
        loaderHelpers.config = module.config = Object.assign(loaderHelpers.config, { message: errMessage, error: err, isError: true });
        mono_exit(1, new Error(errMessage));
        throw err;
    }
}

export function hasDebuggingEnabled(config: MonoConfigInternal): boolean {
    // Copied from blazor MonoDebugger.ts/attachDebuggerHotkey
    if (!globalThis.navigator) {
        return false;
    }

    const hasReferencedPdbs = !!config.resources!.pdb;
    return (hasReferencedPdbs || config.debugLevel != 0) && (loaderHelpers.isChromium || loaderHelpers.isFirefox);
}

async function loadBootConfig(module: DotnetModuleInternal) {
    const defaultConfigSrc = loaderHelpers.locateFile(module.configSrc!);

    const loaderResponse = loaderHelpers.loadBootResource !== undefined ?
        loaderHelpers.loadBootResource("manifest", "blazor.boot.json", defaultConfigSrc, "") :
        defaultLoadBootConfig(defaultConfigSrc);

    let loadConfigResponse: Response;

    if (!loaderResponse) {
        loadConfigResponse = await defaultLoadBootConfig(defaultConfigSrc);
    } else if (typeof loaderResponse === "string") {
        loadConfigResponse = await defaultLoadBootConfig(loaderResponse);
    } else {
        loadConfigResponse = await loaderResponse;
    }

    const loadedConfig: MonoConfig = await loadConfigResponse.json();

    readBootConfigResponseHeaders(loadConfigResponse);
    mapResourcesToAssets(loadedConfig);
    hookDownloadResource(module);

    function defaultLoadBootConfig(url: string): Promise<Response> {
        return loaderHelpers.fetch_like(url, {
            method: "GET",
            credentials: "include",
            cache: "no-cache",
        });
    }
}

function readBootConfigResponseHeaders(loadConfigResponse: Response) {
    const config = loaderHelpers.config;

    if (!config.applicationEnvironment) {
        config.applicationEnvironment = loadConfigResponse.headers.get("Blazor-Environment") || loadConfigResponse.headers.get("DotNet-Environment") || "Production";
    }

    if (!config.environmentVariables)
        config.environmentVariables = {};

    const modifiableAssemblies = loadConfigResponse.headers.get("DOTNET-MODIFIABLE-ASSEMBLIES");
    if (modifiableAssemblies) {
        // Configure the app to enable hot reload in Development.
        config.environmentVariables["DOTNET_MODIFIABLE_ASSEMBLIES"] = modifiableAssemblies;
    }

    const aspnetCoreBrowserTools = loadConfigResponse.headers.get("ASPNETCORE-BROWSER-TOOLS");
    if (aspnetCoreBrowserTools) {
        // See https://github.com/dotnet/aspnetcore/issues/37357#issuecomment-941237000
        config.environmentVariables["__ASPNETCORE_BROWSER_TOOLS"] = aspnetCoreBrowserTools;
    }
}