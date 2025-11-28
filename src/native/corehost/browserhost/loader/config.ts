// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { Assets, LoaderConfig, LoaderConfigInternal } from "./types";

export const netLoaderConfig: LoaderConfigInternal = {};

export function getLoaderConfig(): LoaderConfig {
    return netLoaderConfig;
}

export function validateLoaderConfig(): void {
    if (!netLoaderConfig.mainAssemblyName) {
        throw new Error("Loader configuration error: 'mainAssemblyName' is required.");
    }
    if (!netLoaderConfig.resources || !netLoaderConfig.resources.coreAssembly || netLoaderConfig.resources.coreAssembly.length === 0) {
        throw new Error("Loader configuration error: 'resources.coreAssembly' is required and must contain at least one assembly.");
    }
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
    source.logExitCode = source.logExitCode !== undefined ? source.logExitCode : target.logExitCode;
    source.exitOnUnhandledError = source.exitOnUnhandledError !== undefined ? source.exitOnUnhandledError : target.exitOnUnhandledError;
    source.loadAllSatelliteResources = source.loadAllSatelliteResources !== undefined ? source.loadAllSatelliteResources : target.loadAllSatelliteResources;
    source.mainAssemblyName = source.mainAssemblyName !== undefined ? source.mainAssemblyName : target.mainAssemblyName;
    source.virtualWorkingDirectory = source.virtualWorkingDirectory !== undefined ? source.virtualWorkingDirectory : target.virtualWorkingDirectory;
    source.debugLevel = source.debugLevel !== undefined ? source.debugLevel : target.debugLevel;
    source.diagnosticTracing = source.diagnosticTracing !== undefined ? source.diagnosticTracing : target.diagnosticTracing;
    source.environmentVariables = { ...target.environmentVariables, ...source.environmentVariables };
    source.runtimeOptions = [...target.runtimeOptions!, ...source.runtimeOptions!];
    Object.assign(target, source);
    return target;
}

function mergeResources(target: Assets, source: Assets): Assets {
    // no need to merge the same object
    if (target === source || source === undefined || source === null) return target;

    source.coreAssembly = [...target.coreAssembly!, ...source.coreAssembly!];
    source.assembly = [...target.assembly!, ...source.assembly!];
    source.lazyAssembly = [...target.lazyAssembly!, ...source.lazyAssembly!];
    source.corePdb = [...target.corePdb!, ...source.corePdb!];
    source.pdb = [...target.pdb!, ...source.pdb!];
    source.jsModuleWorker = [...target.jsModuleWorker!, ...source.jsModuleWorker!];
    source.jsModuleNative = [...target.jsModuleNative!, ...source.jsModuleNative!];
    source.jsModuleDiagnostics = [...target.jsModuleDiagnostics!, ...source.jsModuleDiagnostics!];
    source.jsModuleRuntime = [...target.jsModuleRuntime!, ...source.jsModuleRuntime!];
    source.wasmSymbols = [...target.wasmSymbols!, ...source.wasmSymbols!];
    source.wasmNative = [...target.wasmNative!, ...source.wasmNative!];
    source.icu = [...target.icu!, ...source.icu!];
    source.vfs = [...target.vfs!, ...source.vfs!];
    source.modulesAfterConfigLoaded = [...target.modulesAfterConfigLoaded!, ...source.modulesAfterConfigLoaded!];
    source.modulesAfterRuntimeReady = [...target.modulesAfterRuntimeReady!, ...source.modulesAfterRuntimeReady!];
    source.extensions = { ...target.extensions!, ...source.extensions! };
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
