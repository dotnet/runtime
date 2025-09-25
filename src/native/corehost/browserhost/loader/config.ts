// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { Assets, LoadBootResourceCallback, LoaderConfig, LoaderConfigInternal } from "./types";

export const netLoaderConfig: LoaderConfigInternal = {};
let isConfigDownloaded = false;

export async function downloadConfig(url: string|undefined, loadBootResource?: LoadBootResourceCallback): Promise<void> {
    if (loadBootResource) throw new Error("TODO: loadBootResource is not implemented yet");
    if (isConfigDownloaded) return; // only download config once
    if (!url) {
        url = "./dotnet.boot.js";
    }

    // url ends with .json
    if (url.endsWith(".json")) {
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error(`Failed to download config from ${url}: ${response.status} ${response.statusText}`);
        }
        const newConfig = await response.json() as Partial<LoaderConfigInternal>;
        mergeLoaderConfig(newConfig);
    } else if (url.endsWith(".js") || url.endsWith(".mjs")) {
        const module = await import(/* webpackIgnore: true */ url);
        mergeLoaderConfig(module.config);
    }
    isConfigDownloaded = true;
}

export function getLoaderConfig(): LoaderConfig {
    return netLoaderConfig;
}

export function mergeLoaderConfig(source: Partial<LoaderConfigInternal>): void {
    normalizeConfig(netLoaderConfig);
    normalizeConfig(source);
    mergeConfigs(netLoaderConfig, source);
}

function mergeConfigs(target: LoaderConfigInternal, source: Partial<LoaderConfigInternal>): LoaderConfigInternal {
    // no need to merge the same object
    if (target === source || source === undefined || source === null) return target;

    mergeResources(target.resources!, source.resources!);
    source.appendElementOnExit = source.appendElementOnExit !== undefined ? source.appendElementOnExit : target.appendElementOnExit;
    source.applicationArguments = source.applicationArguments !== undefined ? [...target.applicationArguments! || [], ...source.applicationArguments] : target.applicationArguments;
    source.applicationCulture = source.applicationCulture !== undefined ? source.applicationCulture : target.applicationCulture;
    source.applicationEnvironment = source.applicationEnvironment !== undefined ? source.applicationEnvironment : target.applicationEnvironment;
    source.appsettings = source.appsettings !== undefined ? [...target.appsettings! || [], ...source.appsettings] : target.appsettings;
    source.debugLevel = source.debugLevel !== undefined ? source.debugLevel : target.debugLevel;
    source.diagnosticTracing = source.diagnosticTracing !== undefined ? source.diagnosticTracing : target.diagnosticTracing;
    source.disableIntegrityCheck = source.disableIntegrityCheck !== undefined ? source.disableIntegrityCheck : target.disableIntegrityCheck;
    source.disableNoCacheFetch = source.disableNoCacheFetch !== undefined ? source.disableNoCacheFetch : target.disableNoCacheFetch;
    source.enableDownloadRetry = source.enableDownloadRetry !== undefined ? source.enableDownloadRetry : target.enableDownloadRetry;
    source.environmentVariables = { ...target.environmentVariables, ...source.environmentVariables };
    source.exitOnUnhandledError = source.exitOnUnhandledError !== undefined ? source.exitOnUnhandledError : target.exitOnUnhandledError;
    source.extensions = { ...target.extensions, ...source.extensions };
    source.globalizationMode = source.globalizationMode !== undefined ? source.globalizationMode : target.globalizationMode;
    source.ignorePdbLoadErrors = source.ignorePdbLoadErrors !== undefined ? source.ignorePdbLoadErrors : target.ignorePdbLoadErrors;
    source.interpreterPgo = source.interpreterPgo !== undefined ? source.interpreterPgo : target.interpreterPgo;
    source.interpreterPgoSaveDelay = source.interpreterPgoSaveDelay !== undefined ? source.interpreterPgoSaveDelay : target.interpreterPgoSaveDelay;
    source.loadAllSatelliteResources = source.loadAllSatelliteResources !== undefined ? source.loadAllSatelliteResources : target.loadAllSatelliteResources;
    source.logExitCode = source.logExitCode !== undefined ? source.logExitCode : target.logExitCode;
    source.mainAssemblyName = source.mainAssemblyName !== undefined ? source.mainAssemblyName : target.mainAssemblyName;
    source.maxParallelDownloads = source.maxParallelDownloads !== undefined ? source.maxParallelDownloads : target.maxParallelDownloads;
    source.pthreadPoolInitialSize = source.pthreadPoolInitialSize !== undefined ? source.pthreadPoolInitialSize : target.pthreadPoolInitialSize;
    source.pthreadPoolUnusedSize = source.pthreadPoolUnusedSize !== undefined ? source.pthreadPoolUnusedSize : target.pthreadPoolUnusedSize;
    source.remoteSources = source.remoteSources !== undefined ? [...target.remoteSources! || [], ...source.remoteSources] : target.remoteSources;
    source.runtimeConfig = source.runtimeConfig !== undefined ? { ...target.runtimeConfig, ...source.runtimeConfig } : target.runtimeConfig;
    source.runtimeOptions = [...target.runtimeOptions!, ...source.runtimeOptions!];
    source.virtualWorkingDirectory = source.virtualWorkingDirectory !== undefined ? source.virtualWorkingDirectory : target.virtualWorkingDirectory;
    Object.assign(target, source);
    if (target.resources!.coreAssembly!.length) {
        isConfigDownloaded = true;
    }
    return target;
}

function mergeResources(target: Assets, source: Assets): Assets {
    // no need to merge the same object
    if (target === source || source === undefined || source === null) return target;

    source.assembly = [...target.assembly!, ...source.assembly!];
    source.coreAssembly = [...target.coreAssembly!, ...source.coreAssembly!];
    source.corePdb = [...target.corePdb!, ...source.corePdb!];
    source.extensions = { ...target.extensions!, ...source.extensions! };
    source.icu = [...target.icu!, ...source.icu!];
    source.jsModuleDiagnostics = [...target.jsModuleDiagnostics!, ...source.jsModuleDiagnostics!];
    source.jsModuleNative = [...target.jsModuleNative!, ...source.jsModuleNative!];
    source.jsModuleRuntime = [...target.jsModuleRuntime!, ...source.jsModuleRuntime!];
    source.jsModuleWorker = [...target.jsModuleWorker!, ...source.jsModuleWorker!];
    source.lazyAssembly = [...target.lazyAssembly!, ...source.lazyAssembly!];
    source.modulesAfterConfigLoaded = [...target.modulesAfterConfigLoaded!, ...source.modulesAfterConfigLoaded!];
    source.modulesAfterRuntimeReady = [...target.modulesAfterRuntimeReady!, ...source.modulesAfterRuntimeReady!];
    source.pdb = [...target.pdb!, ...source.pdb!];
    source.vfs = [...target.vfs!, ...source.vfs!];
    source.wasmNative = [...target.wasmNative!, ...source.wasmNative!];
    source.wasmSymbols = [...target.wasmSymbols!, ...source.wasmSymbols!];
    for (const key in source.satelliteResources) {
        source.satelliteResources![key] = [...target.satelliteResources![key] || [], ...source.satelliteResources![key] || []];
    }
    return Object.assign(target, source);
}


function normalizeConfig(target: LoaderConfigInternal) {
    if (!target.resources) target.resources = {} as any;
    normalizeResources(target.resources!);
    if (!target.environmentVariables) target.environmentVariables = {};
    if (!target.runtimeOptions) target.runtimeOptions = [];
    if (target.appendElementOnExit === undefined) target.appendElementOnExit = false;
    if (target.logExitCode === undefined) target.logExitCode = false;
    if (target.exitOnUnhandledError === undefined) target.exitOnUnhandledError = false;
    if (target.loadAllSatelliteResources === undefined) target.loadAllSatelliteResources = false;
    if (target.debugLevel === undefined) target.debugLevel = 0;
    if (target.diagnosticTracing === undefined) target.diagnosticTracing = false;
    if (target.virtualWorkingDirectory === undefined) target.virtualWorkingDirectory = "/";
    if (target.mainAssemblyName === undefined) target.mainAssemblyName = "HelloWorld.dll";
}

function normalizeResources(target: Assets) {
    if (!target.coreAssembly) target.coreAssembly = [];
    if (!target.assembly) target.assembly = [];
    if (!target.lazyAssembly) target.lazyAssembly = [];
    if (!target.corePdb) target.corePdb = [];
    if (!target.pdb) target.pdb = [];
    if (!target.jsModuleWorker) target.jsModuleWorker = [];
    if (!target.jsModuleNative) target.jsModuleNative = [];
    if (!target.jsModuleDiagnostics) target.jsModuleDiagnostics = [];
    if (!target.jsModuleRuntime) target.jsModuleRuntime = [];
    if (!target.wasmSymbols) target.wasmSymbols = [];
    if (!target.wasmNative) target.wasmNative = [];
    if (!target.icu) target.icu = [];
    if (!target.modulesAfterConfigLoaded) target.modulesAfterConfigLoaded = [];
    if (!target.modulesAfterRuntimeReady) target.modulesAfterRuntimeReady = [];
    if (!target.satelliteResources) target.satelliteResources = {};
    if (!target.extensions) target.extensions = {};
    if (!target.vfs) target.vfs = [];
}
