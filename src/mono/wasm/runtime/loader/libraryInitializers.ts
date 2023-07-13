// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_log_warn } from "./logging";
import { MonoConfig } from "../types";
import { appendUniqueQuery } from "./assets";
import { loaderHelpers } from "./globals";
import { abort_startup } from "./exit";

export type LibraryInitializerTypes =
    "onRuntimeConfigLoaded"
    | "onRuntimeReady";

async function fetchLibraryInitializers(config: MonoConfig, type: LibraryInitializerTypes): Promise<void> {
    if (!loaderHelpers.libraryInitializers) {
        loaderHelpers.libraryInitializers = [];
    }

    const libraryInitializers = type == "onRuntimeConfigLoaded"
        ? config.resources?.libraryStartupModules?.onRuntimeConfigLoaded
        : config.resources?.libraryStartupModules?.onRuntimeReady;

    if (!libraryInitializers) {
        return;
    }

    const initializerFiles = Object.keys(libraryInitializers);
    await Promise.all(initializerFiles.map(f => importInitializer(f)));

    async function importInitializer(path: string): Promise<void> {
        try {
            const adjustedPath = appendUniqueQuery(loaderHelpers.locateFile(path), "js-module-library-initializer");
            const initializer = await import(/* webpackIgnore: true */ adjustedPath);

            loaderHelpers.libraryInitializers!.push({ scriptName: path, exports: initializer });
        } catch (error) {
            mono_log_warn(`Failed to import library initializer '${path}': ${error}`);
        }
    }
}

export async function invokeLibraryInitializers(functionName: string, args: any[], type?: LibraryInitializerTypes) {
    if (type) {
        await fetchLibraryInitializers(loaderHelpers.config, type);
    }

    if (!loaderHelpers.libraryInitializers) {
        return;
    }

    const promises = [];
    for (let i = 0; i < loaderHelpers.libraryInitializers.length; i++) {
        const initializer = loaderHelpers.libraryInitializers[i];
        if (initializer.exports[functionName]) {
            promises.push(abortStartupOnError(initializer.scriptName, functionName, () => initializer.exports[functionName](...args)));
        }
    }

    await Promise.all(promises);
}

async function abortStartupOnError(scriptName: string, methodName: string, callback: () => Promise<void> | undefined): Promise<void> {
    try {
        await callback();
    } catch (error) {
        mono_log_warn(`Failed to invoke '${methodName}' on library initializer '${scriptName}': ${error}`);
        abort_startup(error, true);
    }
}