// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { Asset, Assets, JsAsset, LoaderConfig } from "./types";

type WorkerAssets = Assets & { libraryInitializers?: JsAsset[]; jsModuleWorker?: JsAsset[] };

export function createWorkerBootConfig(config: LoaderConfig): { config: LoaderConfig; assets: Map<number, Asset> } {
    if (!config.resources) throw new Error("Worker boot configuration has no resources");
    const sources = new Map<number, Asset>();
    let nextId = 1;
    const map = <T extends Asset>(assets: T[] | undefined): T[] | undefined => assets?.map(asset => {
        const workerAssetId = nextId++;
        sources.set(workerAssetId, asset);
        return { ...asset, buffer: undefined, pendingDownload: undefined, moduleExports: undefined, workerAssetId };
    });
    const original: WorkerAssets = config.resources;
    const resources: WorkerAssets = {
        ...original,
        coreAssembly: map(original.coreAssembly)!,
        assembly: map(original.assembly)!,
        lazyAssembly: map(original.lazyAssembly),
        corePdb: map(original.corePdb),
        pdb: map(original.pdb),
        jsModuleNative: map(original.jsModuleNative)!,
        jsModuleRuntime: map(original.jsModuleRuntime)!,
        jsModuleDiagnostics: map(original.jsModuleDiagnostics),
        jsModuleWorker: map(original.jsModuleWorker),
        wasmNative: map(original.wasmNative)!,
        wasmSymbols: map(original.wasmSymbols),
        icu: map(original.icu),
        vfs: map(original.vfs),
        appsettings: map(original.appsettings),
        modulesAfterConfigLoaded: [],
        modulesAfterRuntimeReady: [],
        libraryInitializers: [],
        satelliteResources: original.satelliteResources && Object.fromEntries(
            Object.entries(original.satelliteResources).map(([culture, assets]) => [culture, map(assets)!])),
    };
    return { config: { ...config, resources }, assets: sources };
}

export function restoreWorkerModules(config: LoaderConfig, local: LoaderConfig): void {
    if (!config.resources || !local.resources) return;
    for (const kind of ["jsModuleNative", "jsModuleRuntime", "jsModuleDiagnostics"] as const) {
        for (const asset of config.resources[kind] ?? []) {
            const own = local.resources[kind]?.find(candidate => candidate.name === asset.name);
            if (own?.moduleExports && (!asset.resolvedUrl || asset.resolvedUrl === own.resolvedUrl)) {
                asset.moduleExports = own.moduleExports;
            }
        }
    }
}
