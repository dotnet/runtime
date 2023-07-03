import { MonoConfig, RuntimeAPI } from "../types";
import { BootJsonData } from "../types/blazor";

export async function fetchInitializers(moduleConfig: MonoConfig, bootConfig: BootJsonData): Promise<void> {
    const libraryInitializers = bootConfig.resources.libraryInitializers;
    if (!libraryInitializers) {
        return;
    }

    if (!moduleConfig.libraryInitializers) {
        moduleConfig.libraryInitializers = [];
    }

    const initializerFiles = Object.keys(libraryInitializers);
    await Promise.all(initializerFiles.map(f => importInitializer(f)));

    function adjustPath(path: string): string {
        // This is the same we do in JS interop with the import callback
        const base = document.baseURI;
        path = base.endsWith("/") ? `${base}${path}` : `${base}/${path}`;
        return path;
    }

    async function importInitializer(path: string): Promise<void> {
        const adjustedPath = adjustPath(path);
        const initializer = await import(/* webpackIgnore: true */ adjustedPath);

        moduleConfig.libraryInitializers!.push(initializer);
    }
}

export async function invokeOnRuntimeReady(api: RuntimeAPI) {
    const moduleConfig = api.getConfig();
    const initializerPromises = [];
    if (moduleConfig.libraryInitializers) {
        for (let i = 0; i < moduleConfig.libraryInitializers.length; i++) {
            const initializer = moduleConfig.libraryInitializers[i];
            initializer as { onRuntimeReady: (api: RuntimeAPI) => Promise<void> };
            if (initializer?.onRuntimeReady) {
                initializerPromises.push(initializer?.onRuntimeReady(api));
            }
        }

        await Promise.all(initializerPromises);
    }
}