// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { INTERNAL, loaderHelpers, runtimeHelpers } from "./globals";
import type { WebAssemblyResourceLoader } from "./loader/blazor/WebAssemblyResourceLoader";

export async function loadLazyAssembly(assemblyNameToLoad: string): Promise<boolean> {
    const resourceLoader: WebAssemblyResourceLoader = INTERNAL.resourceLoader;
    const resources = resourceLoader.bootConfig.resources;
    const lazyAssemblies = resources.lazyAssembly;
    if (!lazyAssemblies) {
        throw new Error("No assemblies have been marked as lazy-loadable. Use the 'BlazorWebAssemblyLazyLoad' item group in your project file to enable lazy loading an assembly.");
    }

    const assemblyMarkedAsLazy = Object.prototype.hasOwnProperty.call(lazyAssemblies, assemblyNameToLoad);
    if (!assemblyMarkedAsLazy) {
        throw new Error(`${assemblyNameToLoad} must be marked with 'BlazorWebAssemblyLazyLoad' item group in your project file to allow lazy-loading.`);
    }

    if (loaderHelpers.loadedAssemblies.some(f => f.includes(assemblyNameToLoad))) {
        return false;
    }

    const dllNameToLoad = assemblyNameToLoad;
    const pdbNameToLoad = changeExtension(assemblyNameToLoad, ".pdb");
    const shouldLoadPdb = loaderHelpers.hasDebuggingEnabled(resourceLoader.bootConfig) && resources.pdb && Object.prototype.hasOwnProperty.call(lazyAssemblies, pdbNameToLoad);

    const dllBytesPromise = resourceLoader.loadResource(dllNameToLoad, loaderHelpers.locateFile(dllNameToLoad), lazyAssemblies[dllNameToLoad], "assembly").response.then(response => response.arrayBuffer());

    let dll = null;
    let pdb = null;
    if (shouldLoadPdb) {
        const pdbBytesPromise = await resourceLoader.loadResource(pdbNameToLoad, loaderHelpers.locateFile(pdbNameToLoad), lazyAssemblies[pdbNameToLoad], "pdb").response.then(response => response.arrayBuffer());
        const [dllBytes, pdbBytes] = await Promise.all([dllBytesPromise, pdbBytesPromise]);

        dll = new Uint8Array(dllBytes);
        pdb = new Uint8Array(pdbBytes);
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