// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import BuildConfiguration from "consts:configuration";
import WasmEnableThreads from "consts:wasmEnableThreads";
import WasmEnableSIMD from "consts:wasmEnableSIMD";
import WasmEnableExceptionHandling from "consts:wasmEnableExceptionHandling";

import type { RuntimeAPI } from "./types";

import { Module, exportedRuntimeAPI, loaderHelpers, passEmscriptenInternals, runtimeHelpers, setRuntimeGlobals, } from "./globals";
import { GlobalObjects } from "./types/internal";
import { configureEmscriptenStartup, configureRuntimeStartup, configureWorkerStartup } from "./startup";

import { create_weak_ref } from "./weak-ref";
import { export_internal } from "./exports-internal";
import { export_api } from "./export-api";
import { initializeReplacements } from "./polyfills";

import { mono_wasm_stringify_as_error_with_stack } from "./logging";
import { instantiate_asset, instantiate_symbols_asset, instantiate_segmentation_rules_asset } from "./assets";
import { jiterpreter_dump_stats } from "./jiterpreter";
import { forceDisposeProxies } from "./gc-handles";
import { dumpThreads } from "./pthreads";

export let runtimeList: RuntimeList;

function initializeExports(globalObjects: GlobalObjects): RuntimeAPI {
    const module = Module;
    const globals = globalObjects;
    const globalThisAny = globalThis as any;

    Object.assign(globals.internal, export_internal());
    Object.assign(runtimeHelpers, {
        stringify_as_error_with_stack: mono_wasm_stringify_as_error_with_stack,
        instantiate_symbols_asset,
        instantiate_asset,
        jiterpreter_dump_stats,
        forceDisposeProxies,
        instantiate_segmentation_rules_asset,
    });
    if (WasmEnableThreads) {
        Object.assign(runtimeHelpers, {
            dumpThreads,
        });
    }

    const API = export_api();
    Object.assign(exportedRuntimeAPI, {
        INTERNAL: globals.internal,
        Module: module,
        runtimeBuildInfo: {
            productVersion: ProductVersion,
            gitHash: runtimeHelpers.gitHash,
            buildConfiguration: BuildConfiguration,
            wasmEnableThreads: WasmEnableThreads,
            wasmEnableSIMD: WasmEnableSIMD,
            wasmEnableExceptionHandling: WasmEnableExceptionHandling,
        },
        ...API,
    });

    // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
    if (!globalThisAny.getDotnetRuntime) {
        globalThisAny.getDotnetRuntime = (runtimeId: string) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
        globalThisAny.getDotnetRuntime.__list = runtimeList = new RuntimeList();
    }
    else {
        runtimeList = globalThisAny.getDotnetRuntime.__list;
    }

    return exportedRuntimeAPI;
}

class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<RuntimeAPI> } = {};

    public registerRuntime(api: RuntimeAPI): number {
        if (api.runtimeId === undefined) {
            api.runtimeId = Object.keys(this.list).length;
        }
        this.list[api.runtimeId] = create_weak_ref(api);
        loaderHelpers.config.runtimeId = api.runtimeId;
        return api.runtimeId;
    }

    public getRuntime(runtimeId: number): RuntimeAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}

// export external API
export {
    passEmscriptenInternals, initializeExports, initializeReplacements, configureRuntimeStartup, configureEmscriptenStartup, configureWorkerStartup, setRuntimeGlobals
};