// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers } from "./globals";
import { load_lazy_assembly } from "./managed-exports";
import { AssetEntry } from "./types";

export async function loadLazyAssembly (assemblyNameToLoad: string): Promise<boolean> {
    const resources = loaderHelpers.config.resources!;
    const originalAssemblyName = assemblyNameToLoad;
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

    if (loaderHelpers.config.resources!.fingerprinting) {
        const map = loaderHelpers.config.resources!.fingerprinting;
        for (const fingerprintedName in map) {
            const nonFingerprintedName = map[fingerprintedName];
            if (nonFingerprintedName == assemblyNameToLoadDll || nonFingerprintedName == assemblyNameToLoadWasm) {
                assemblyNameToLoad = fingerprintedName;
                break;
            }
        }
    }

    if (!lazyAssemblies[assemblyNameToLoad]) {
        if (lazyAssemblies[assemblyNameToLoadDll]) {
            assemblyNameToLoad = assemblyNameToLoadDll;
        } else if (lazyAssemblies[assemblyNameToLoadWasm]) {
            assemblyNameToLoad = assemblyNameToLoadWasm;
        } else {
            throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
        }
    }

    const dllAsset: AssetEntry = {
        name: assemblyNameToLoad,
        hash: lazyAssemblies[assemblyNameToLoad],
        behavior: "assembly",
    };

    if (loaderHelpers.loadedAssemblies.includes(assemblyNameToLoad)) {
        return false;
    }

    let pdbNameToLoad = assemblyNameWithoutExtension + ".pdb";
    let shouldLoadPdb = false;
    if (loaderHelpers.config.debugLevel != 0 && loaderHelpers.isDebuggingSupported()) {
        shouldLoadPdb = Object.prototype.hasOwnProperty.call(lazyAssemblies, pdbNameToLoad);
        if (loaderHelpers.config.resources!.fingerprinting) {
            const map = loaderHelpers.config.resources!.fingerprinting;
            for (const fingerprintedName in map) {
                const nonFingerprintedName = map[fingerprintedName];
                if (nonFingerprintedName == pdbNameToLoad) {
                    pdbNameToLoad = fingerprintedName;
                    shouldLoadPdb = true;
                    break;
                }
            }
        }
    }

    const dllBytesPromise = loaderHelpers.retrieve_asset_download(dllAsset);

    let dll = null;
    let pdb = null;
    if (shouldLoadPdb) {
        const pdbBytesPromise = lazyAssemblies[pdbNameToLoad]
            ? loaderHelpers.retrieve_asset_download({
                name: pdbNameToLoad,
                hash: lazyAssemblies[pdbNameToLoad],
                behavior: "pdb"
            })
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
