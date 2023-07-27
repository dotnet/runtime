// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers, runtimeHelpers } from "./globals";

export async function loadLazyAssembly(assemblyNameToLoad: string): Promise<boolean> {
    const resources = loaderHelpers.config.resources!;
    const lazyAssemblies = resources.lazyAssembly;
    if (!lazyAssemblies) {
        throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
    }

    const assemblyAsset = loaderHelpers.getAssetByNameWithResolvedUrl(lazyAssemblies, "assembly", assemblyNameToLoad);
    // const assemblyMarkedAsLazy = Object.prototype.hasOwnProperty.call(lazyAssemblies, assemblyNameToLoad);
    if (!assemblyAsset) {
        throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
    }

    if (loaderHelpers.loadedAssemblies.some(f => f.includes(assemblyNameToLoad))) {
        return false;
    }

    const dllNameToLoad = assemblyNameToLoad;
    const pdbNameToLoad = changeExtension(assemblyNameToLoad, ".pdb");
    const shouldLoadPdb = loaderHelpers.hasDebuggingEnabled(loaderHelpers.config) && resources.pdb && Object.prototype.hasOwnProperty.call(lazyAssemblies, pdbNameToLoad);

    const dllBytesPromise = loaderHelpers.loadResource(dllNameToLoad, assemblyAsset.resolvedUrl!, assemblyAsset.hash ?? "", assemblyAsset.behavior).response.then(response => response.arrayBuffer());

    let dll = null;
    let pdb = null;
    if (shouldLoadPdb) {
        const pdbAsset = loaderHelpers.getAssetByNameWithResolvedUrl(lazyAssemblies, "pdb", assemblyNameToLoad);
        const pdbBytesPromise = pdbAsset
            ? await loaderHelpers.loadResource(pdbNameToLoad, pdbAsset.resolvedUrl!, pdbAsset.hash ?? "", pdbAsset.behavior).response.then(response => response.arrayBuffer())
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