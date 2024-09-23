// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers, runtimeHelpers } from "./globals";
import { AssetEntry } from "./types";

export async function loadLazyAssembly(assemblyNameToLoad: string): Promise<boolean> {
    const resources = loaderHelpers.config.resources!;
    const lazyAssemblies = resources.lazyAssembly;
    if (!lazyAssemblies) {
        throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
    }

    if (!lazyAssemblies[assemblyNameToLoad]) {
        throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
    }

    const dllAsset: AssetEntry = {
        name: assemblyNameToLoad,
        hash: lazyAssemblies[assemblyNameToLoad],
        behavior: "assembly",
    };

    if (loaderHelpers.loadedAssemblies.includes(assemblyNameToLoad)) {
        return false;
    }

    const pdbNameToLoad = changeExtension(dllAsset.name, ".pdb");
    const shouldLoadPdb = loaderHelpers.config.debugLevel != 0 && loaderHelpers.isDebuggingSupported() && Object.prototype.hasOwnProperty.call(lazyAssemblies, pdbNameToLoad);

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

    runtimeHelpers.javaScriptExports.load_lazy_assembly(dll, pdb);
    return true;
}

function changeExtension(filename: string, newExtensionWithLeadingDot: string) {
    const lastDotIndex = filename.lastIndexOf(".");
    if (lastDotIndex < 0) {
        throw new Error(`No extension to replace in '${filename}'`);
    }

    return filename.substring(0, lastDotIndex) + newExtensionWithLeadingDot;
}