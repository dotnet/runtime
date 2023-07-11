// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_log_warn } from "./logging";
import { MonoConfig, RuntimeAPI } from "../types";
import { appendUniqueQuery, toAbsoluteBaseUri } from "./assets";
import { normalizeConfig } from "./config";
import { loaderHelpers } from "./globals";

export type LibraryInitializerTypes =
    "onRuntimeConfigLoaded"
    | "onRuntimeReady";

async function fetchLibraryInitializers(config: MonoConfig, type: LibraryInitializerTypes): Promise<void> {
    if (!loaderHelpers.libraryInitializers) {
        loaderHelpers.libraryInitializers = [];
    }

    const libraryInitializers = type == "onRuntimeConfigLoaded"
        ? config.resources?.libraryInitializers?.onRuntimeConfigLoaded
        : config.resources?.libraryInitializers?.onRuntimeReady;

    if (!libraryInitializers) {
        return;
    }

    const initializerFiles = Object.keys(libraryInitializers);
    await Promise.all(initializerFiles.map(f => importInitializer(f)));

    async function importInitializer(path: string): Promise<void> {
        try {
            const adjustedPath = appendUniqueQuery(toAbsoluteBaseUri(path), "js-module-library-initializer");
            const initializer = await import(/* webpackIgnore: true */ adjustedPath);

            loaderHelpers.libraryInitializers!.push(initializer);
        } catch (error) {
            mono_log_warn(`Failed to import library initializer '${path}': ${error}`);
        }
    }
}

export async function invokeOnRuntimeConfigLoaded(config: MonoConfig) {
    await fetchLibraryInitializers(config, "onRuntimeConfigLoaded");

    if (!loaderHelpers.libraryInitializers) {
        return;
    }

    const promises = [];
    for (let i = 0; i < loaderHelpers.libraryInitializers.length; i++) {
        const initializer = loaderHelpers.libraryInitializers[i] as { onRuntimeConfigLoaded: (config: MonoConfig) => Promise<void> };
        if (initializer.onRuntimeConfigLoaded) {
            promises.push(logAndSwallowError("onRuntimeConfigLoaded", () => initializer.onRuntimeConfigLoaded(config)));
        }
    }

    await Promise.all(promises);
    if (promises.length > 0)
        normalizeConfig();
}

export async function invokeOnRuntimeReady(api: RuntimeAPI) {
    const config = api.getConfig();
    await fetchLibraryInitializers(config, "onRuntimeReady");

    if (!loaderHelpers.libraryInitializers) {
        return;
    }

    const promises = [];
    for (let i = 0; i < loaderHelpers.libraryInitializers.length; i++) {
        const initializer = loaderHelpers.libraryInitializers[i] as { onRuntimeReady: (api: RuntimeAPI) => Promise<void> };
        if (initializer.onRuntimeReady) {
            promises.push(logAndSwallowError("onRuntimeReady", () => initializer.onRuntimeReady(api)));
        }
    }

    await Promise.all(promises);
}

async function logAndSwallowError(methodName: string, callback: () => Promise<void> | undefined): Promise<void> {
    try {
        await callback();
    } catch (error) {
        mono_log_warn(`Failed to invoke '${methodName}' on library initializer: ${error}`);
    }
}

export function getLibraryInitializerExports(): any[] {
    return loaderHelpers.libraryInitializers ?? [];
}