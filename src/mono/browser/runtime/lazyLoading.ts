// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers } from "./globals";
import { load_lazy_assembly } from "./managed-exports";
import { type AssemblyAsset, type PdbAsset } from "./types";
import { type AssetEntryInternal } from "./types/internal";

export async function loadLazyAssembly (assemblyNameToLoad: string): Promise<boolean> {
    const resources = loaderHelpers.config.resources!;
    const lazyAssemblies = resources.lazyAssembly;
    if (!lazyAssemblies) {
        throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
    }

    let assemblyNameWithoutExtension = assemblyNameToLoad;
    if (assemblyNameToLoad.endsWith(".dll"))
        assemblyNameWithoutExtension = assemblyNameToLoad.substring(0, assemblyNameToLoad.length - 4);
    else if (assemblyNameToLoad.endsWith(".wasm"))
        assemblyNameWithoutExtension = assemblyNameToLoad.substring(0, assemblyNameToLoad.length - 5);

    const assemblyNameToLoadDll = assemblyNameWithoutExtension + ".dll";
    const assemblyNameToLoadWasm = assemblyNameWithoutExtension + ".wasm";

    let dllAsset: (AssemblyAsset & AssetEntryInternal) | null = null;
    for (let i = 0; i < lazyAssemblies.length; i++) {
        const asset = lazyAssemblies[i];
        if (asset.virtualPath === assemblyNameToLoadDll || asset.virtualPath === assemblyNameToLoadWasm) {
            dllAsset = asset as AssemblyAsset & AssetEntryInternal;
            dllAsset.behavior = "assembly";
            break;
        }
    }

    if (dllAsset == null) {
        throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
    }

    if (loaderHelpers.loadedAssemblies.includes(dllAsset.name)) {
        return false;
    }

    const pdbNameToLoad = assemblyNameWithoutExtension + ".pdb";
    let shouldLoadPdb = false;
    let pdbAsset: (PdbAsset & AssetEntryInternal) | null = null;
    if (loaderHelpers.config.debugLevel != 0 && loaderHelpers.isDebuggingSupported()) {
        for (let i = 0; i < lazyAssemblies.length; i++) {
            if (lazyAssemblies[i].virtualPath === pdbNameToLoad) {
                shouldLoadPdb = true;
                pdbAsset = lazyAssemblies[i] as PdbAsset & AssetEntryInternal;
                pdbAsset.behavior = "pdb";
                break;
            }
        }
    }

    const dllBytesPromise = loaderHelpers.retrieve_asset_download(dllAsset);

    let dll = null;
    let pdb = null;
    if (shouldLoadPdb) {
        const pdbBytesPromise = pdbAsset != null
            ? loaderHelpers.retrieve_asset_download(pdbAsset)
            : Promise.resolve(null);

        const [dllBytes, pdbBytes] = await Promise.all([dllBytesPromise, pdbBytesPromise]);

        dll = new Uint8Array(dllBytes);
        pdb = pdbBytes ? new Uint8Array(pdbBytes) : null;
    } else {
        const dllBytes = await dllBytesPromise;
        dll = new Uint8Array(dllBytes);
        pdb = null;
    }

    load_lazy_assembly(dll, pdb);
    return true;
}
