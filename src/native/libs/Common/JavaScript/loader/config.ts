// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { Assets, LoaderConfig, LoaderConfigInternal } from "./types";
import { browserVirtualAppBase } from "./per-module";

export const loaderConfig: LoaderConfigInternal = {};

export function getLoaderConfig(): LoaderConfig {
    return loaderConfig;
}

export function validateLoaderConfig(): void {
    if (!loaderConfig.mainAssemblyName) {
        throw new Error("Loader configuration error: 'mainAssemblyName' is required.");
    }
    if (!loaderConfig.resources || !loaderConfig.resources.coreAssembly || loaderConfig.resources.coreAssembly.length === 0) {
        throw new Error("Loader configuration error: 'resources.coreAssembly' is required and must contain at least one assembly.");
    }
}

export function mergeLoaderConfig(source: Partial<LoaderConfigInternal>): void {
    defaultConfig(loaderConfig);
    normalizeConfig(source);
    mergeConfigs(loaderConfig, source);
}

function mergeConfigs(target: LoaderConfigInternal, source: Partial<LoaderConfigInternal>): LoaderConfigInternal {
    // no need to merge the same object
    if (target === source || source === undefined || source === null) return target;

    // Merge collections: target values first, then source values appended/spread on top.
    mergeResources(target.resources!, source.resources!);
    // For scalar fields with defaults, prefer source when defined, otherwise keep target default.
    // We patch source so that Object.assign below copies the correct resolved value.
    source.appendElementOnExit = source.appendElementOnExit !== undefined ? source.appendElementOnExit : target.appendElementOnExit;
    source.logExitCode = source.logExitCode !== undefined ? source.logExitCode : target.logExitCode;
    source.exitOnUnhandledError = source.exitOnUnhandledError !== undefined ? source.exitOnUnhandledError : target.exitOnUnhandledError;
    source.loadAllSatelliteResources = source.loadAllSatelliteResources !== undefined ? source.loadAllSatelliteResources : target.loadAllSatelliteResources;
    source.mainAssemblyName = source.mainAssemblyName !== undefined ? source.mainAssemblyName : target.mainAssemblyName;
    source.virtualWorkingDirectory = source.virtualWorkingDirectory !== undefined ? source.virtualWorkingDirectory : target.virtualWorkingDirectory;
    source.debugLevel = source.debugLevel !== undefined ? source.debugLevel : target.debugLevel;
    source.diagnosticTracing = source.diagnosticTracing !== undefined ? source.diagnosticTracing : target.diagnosticTracing;
    // Merge maps and arrays: target values first, source values merged on top.
    source.environmentVariables = { ...target.environmentVariables, ...source.environmentVariables };
    source.runtimeOptions = [...target.runtimeOptions!, ...source.runtimeOptions!];
    source.runtimeConfig!.runtimeOptions!.configProperties = { ...target.runtimeConfig!.runtimeOptions!.configProperties!, ...source.runtimeConfig!.runtimeOptions!.configProperties! };
    // Copy all properties from source into target, including any simple properties
    // (e.g. maxParallelDownloads, applicationCulture, disableIntegrityCheck, etc.)
    // that don't need special merge logic.
    const mergedResources = target.resources;
    Object.assign(target, source);
    // Restore merged resources that Object.assign would have overwritten.
    target.resources = mergedResources;
    return target;
}

function mergeResources(target: Assets, source: Assets): Assets {
    // no need to merge the same object
    if (target === source || source === undefined || source === null) return target;

    target.hash = source.hash ?? target.hash;
    target.coreAssembly = [...target.coreAssembly!, ...source.coreAssembly || []];
    target.assembly = [...target.assembly!, ...source.assembly || []];
    target.lazyAssembly = [...target.lazyAssembly!, ...source.lazyAssembly || []];
    target.corePdb = [...target.corePdb!, ...source.corePdb || []];
    target.pdb = [...target.pdb!, ...source.pdb || []];
    target.jsModuleWorker = [...target.jsModuleWorker!, ...source.jsModuleWorker || []];
    target.jsModuleNative = [...target.jsModuleNative!, ...source.jsModuleNative || []];
    target.jsModuleDiagnostics = [...target.jsModuleDiagnostics!, ...source.jsModuleDiagnostics || []];
    target.jsModuleRuntime = [...target.jsModuleRuntime!, ...source.jsModuleRuntime || []];
    target.wasmSymbols = [...target.wasmSymbols!, ...source.wasmSymbols || []];
    target.wasmNative = [...target.wasmNative!, ...source.wasmNative || []];
    target.icu = [...target.icu!, ...source.icu || []];
    target.vfs = [...target.vfs!, ...source.vfs || []];
    target.coreVfs = [...target.coreVfs!, ...source.coreVfs || []];
    target.modulesAfterConfigLoaded = [...target.modulesAfterConfigLoaded!, ...source.modulesAfterConfigLoaded || []];
    target.modulesAfterRuntimeReady = [...target.modulesAfterRuntimeReady!, ...source.modulesAfterRuntimeReady || []];
    target.extensions = { ...target.extensions!, ...source.extensions || {} };
    for (const key in source.satelliteResources || {}) {
        target.satelliteResources![key] = [...target.satelliteResources![key] || [], ...source.satelliteResources![key] || []];
    }
    return target;
}

function defaultConfig(target: LoaderConfigInternal) {
    if (target.appendElementOnExit === undefined) target.appendElementOnExit = false;
    if (target.logExitCode === undefined) target.logExitCode = false;
    if (target.exitOnUnhandledError === undefined) target.exitOnUnhandledError = false;
    if (target.loadAllSatelliteResources === undefined) target.loadAllSatelliteResources = false;
    if (target.debugLevel === undefined) target.debugLevel = 0;
    if (target.diagnosticTracing === undefined) target.diagnosticTracing = false;
    if (target.virtualWorkingDirectory === undefined) target.virtualWorkingDirectory = browserVirtualAppBase;
    if (target.maxParallelDownloads === undefined) target.maxParallelDownloads = 16;
    normalizeConfig(target);
}

function normalizeConfig(target: LoaderConfigInternal) {
    if (!target.resources) target.resources = {} as any;
    normalizeResources(target.resources!);
    if (!target.environmentVariables) target.environmentVariables = {};
    if (!target.runtimeOptions) target.runtimeOptions = [];
    if (!target.runtimeConfig) {
        target.runtimeConfig = { runtimeOptions: { configProperties: {} }, };
    } else if (!target.runtimeConfig.runtimeOptions) {
        target.runtimeConfig.runtimeOptions = { configProperties: {} };
    } else if (!target.runtimeConfig.runtimeOptions.configProperties) {
        target.runtimeConfig.runtimeOptions.configProperties = {};
    }
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
    if (!target.coreVfs) target.coreVfs = [];
}
