// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements PAL for the VM/runtime.
 * It also implements part of public JS API related to memory and runtime hosting.
 */

// Exports that can be trimmed away by emscripten linker if not used.
import * as trimmableExports from "./trimmable/index";

// Symbols that would be protected from emscripten linker and reused by System.Runtime.InteropServices.JavaScript.ts
import * as commonInfra from "./cross-module";
import * as exchange from "./native-exchange";

import { dotnetInternals } from "./cross-module";
import { initialize } from "./native-exchange";

declare const DOTNET: any;
declare const ENV: any;

function dotnetLibFactory() {
    // Symbols that would be protected from emscripten linker
    const commonInfraTransformed:any = {
        Assert: commonInfra.Assert,
        JSEngine: commonInfra.JSEngine,
        Logger: commonInfra.Logger,
        Module: commonInfra.Module,
        runtimeApi: commonInfra.runtimeApi,
        runtimeExports: commonInfra.runtimeExports,
        nativeExports: commonInfra.nativeExports,
        interopExports: commonInfra.interopExports,
        loaderExports : commonInfra.loaderExports,
        loadedAssemblies: commonInfra.loadedAssemblies,
        loaderExportsFromTable: commonInfra.loaderExportsFromTable,
        runtimeExportsFromTable: commonInfra.runtimeExportsFromTable,
        nativeExportsFromTable: commonInfra.nativeExportsFromTable,
        interopExportsFromTable: commonInfra.interopExportsFromTable,
        nativeExportsToTable: commonInfra.nativeExportsToTable,
        interopExportsToTable: commonInfra.interopExportsToTable,
        setInternals: commonInfra.setInternals,
        getInternals: commonInfra.getInternals,
        updateInternals: commonInfra.updateInternals,
        updateInternalsImpl: commonInfra.updateInternalsImpl,
    };
    // Symbols that would be protected from emscripten linker
    const exchangeTransformed:any = {
        setHeapB32: exchange.setHeapB32,
        setHeapB8: exchange.setHeapB8,
        setHeapU8: exchange.setHeapU8,
        setHeapU16: exchange.setHeapU16,
        setHeapU32: exchange.setHeapU32,
        setHeapI8: exchange.setHeapI8,
        setHeapI16: exchange.setHeapI16,
        setHeapI32: exchange.setHeapI32,
        setHeapI52: exchange.setHeapI52,
        setHeapU52: exchange.setHeapU52,
        setHeapI64Big: exchange.setHeapI64Big,
        setHeapF32: exchange.setHeapF32,
        setHeapF64: exchange.setHeapF64,
        getHeapB32: exchange.getHeapB32,
        getHeapB8: exchange.getHeapB8,
        getHeapU8: exchange.getHeapU8,
        getHeapU16: exchange.getHeapU16,
        getHeapU32: exchange.getHeapU32,
        getHeapI8: exchange.getHeapI8,
        getHeapI16: exchange.getHeapI16,
        getHeapI32: exchange.getHeapI32,
        getHeapI52: exchange.getHeapI52,
        getHeapU52: exchange.getHeapU52,
        getHeapI64Big: exchange.getHeapI64Big,
        getHeapF32: exchange.getHeapF32,
        getHeapF64: exchange.getHeapF64,
        localHeapViewI8: exchange.localHeapViewI8,
        localHeapViewI16: exchange.localHeapViewI16,
        localHeapViewI32: exchange.localHeapViewI32,
        localHeapViewI64Big: exchange.localHeapViewI64Big,
        localHeapViewU8: exchange.localHeapViewU8,
        localHeapViewU16: exchange.localHeapViewU16,
        localHeapViewU32: exchange.localHeapViewU32,
        localHeapViewF32: exchange.localHeapViewF32,
        localHeapViewF64: exchange.localHeapViewF64,
        isSharedArrayBuffer: exchange.isSharedArrayBuffer,
        exit: exchange.exit,
        runMain: exchange.runMain,
        runMainAndExit: exchange.runMainAndExit,
        setEnvironmentVariable: exchange.setEnvironmentVariable,
        registerDllBytes: exchange.registerDllBytes,
        browserHostExternalAssemblyProbe: exchange.browserHostExternalAssemblyProbe,
        browserHostRejectMain: exchange.browserHostRejectMain,
        browserHostResolveMain: exchange.browserHostResolveMain,
        _zero_region: exchange._zero_region,
        assert_int_in_range: exchange.assert_int_in_range,
        max_int64_big: exchange.max_int64_big,
        min_int64_big: exchange.min_int64_big,
        sharedArrayBufferDefined: exchange.sharedArrayBufferDefined,
    };
    const moduleDeps = ["$ENV"];
    for (const exportName of Reflect.ownKeys(commonInfraTransformed)) {
        const emName = "$" + exportName.toString();
        commonInfraTransformed[emName] = (commonInfra as any)[exportName];
        moduleDeps.push(emName);
    }
    for (const exportName of Reflect.ownKeys(exchangeTransformed)) {
        const emName = "$" + exportName.toString();
        exchangeTransformed[emName] = (exchange as any)[exportName];
        moduleDeps.push(emName);
    }
    const trimmableDeps: Record<string, string[]> = {};
    for (const exportName of Reflect.ownKeys(trimmableExports)) {
        const emName = exportName.toString() + "__deps";
        const deps = (trimmableExports as any)[exportName]["__deps"] as string[] | undefined;
        if (deps) {
            trimmableDeps[emName] = deps;
        }
    }
    const lib = {
        $DOTNET: {
            selfInitialize: () => {
                if (typeof dotnetInternals !== "undefined") {
                    DOTNET.dotnetInternals = dotnetInternals;
                    DOTNET.initialize(dotnetInternals);
                    if (dotnetInternals.config && dotnetInternals.config.environmentVariables) {
                        Object.assign(ENV, dotnetInternals.config.environmentVariables);
                    }
                }
            },
            initialize: initialize,
        },
        "$DOTNET__postset": "DOTNET.selfInitialize();",
        "$DOTNET__deps": moduleDeps,
        ...commonInfraTransformed,
        ...exchangeTransformed,
        ...trimmableExports,
        ...trimmableDeps
    };

    autoAddDeps(lib, "$DOTNET");
    addToLibrary(lib);
}

dotnetLibFactory();

// make sure we don't mangle those names
export * as commonInfra from "./cross-module";
export * as exchange from "./native-exchange";
export * as trimmableExports from "./trimmable";
