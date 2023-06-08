/* eslint-disable no-prototype-builtins */

import { INTERNAL } from "./globals";
import { WebAssemblyResourceLoader } from "./loader/blazor/WebAssemblyResourceLoader";
import { LoadingResource } from "./types";

export async function loadSatelliteAssemblies(culturesToLoad: string[]): Promise<void> {
    const resourceLoader: WebAssemblyResourceLoader = INTERNAL.resourceLoader;
    const satelliteResources = resourceLoader.bootConfig.resources.satelliteResources;
    if (!satelliteResources) {
        return;
    }

    await Promise.all(culturesToLoad!
        .filter(culture => satelliteResources.hasOwnProperty(culture))
        .map(culture => resourceLoader.loadResources(satelliteResources[culture], fileName => `_framework/${fileName}`, "assembly"))
        .reduce((previous, next) => previous.concat(next), new Array<LoadingResource>())
        .map(async resource => {
            const response = await resource.response;
            const bytes = await response.arrayBuffer();
            const wrapper = { dll: new Uint8Array(bytes) };

            // TODO MF: call interop
            loader(wrapper);
        }));
}