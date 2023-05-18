// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import GitHash from "consts:gitHash";
import BuildConfiguration from "consts:configuration";
import WasmEnableLegacyJsInterop from "consts:WasmEnableLegacyJsInterop";
import type { RuntimeAPI } from "./types";

import { Module, disableLegacyJsInterop, exportedRuntimeAPI, passEmscriptenInternals, runtimeHelpers, setRuntimeGlobals, } from "./globals";
import { GlobalObjects, is_nullish } from "./types/internal";
import { configureEmscriptenStartup, configureWorkerStartup } from "./startup";

import { create_weak_ref } from "./weak-ref";
import { export_internal } from "./exports-internal";
import { export_api } from "./export-api";
import { initializeReplacements } from "./polyfills";

// legacy
import { mono_bind_static_method } from "./net6-legacy/method-calls";
import { export_binding_api, export_internal_api, export_mono_api } from "./net6-legacy/exports-legacy";
import { initializeLegacyExports } from "./net6-legacy/globals";
import { mono_log_warn, mono_wasm_stringify_as_error_with_stack } from "./logging";
import { instantiate_asset, instantiate_symbols_asset } from "./assets";
import { jiterpreter_dump_stats } from "./jiterpreter";

function initializeExports(globalObjects: GlobalObjects): RuntimeAPI {
    const module = Module;
    const globals = globalObjects;
    const globalThisAny = globalThis as any;

    if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
        initializeLegacyExports(globals);
    }

    // here we merge methods from the local objects into exported objects
    if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
        Object.assign(globals.mono, export_mono_api());
        Object.assign(globals.binding, export_binding_api());
        Object.assign(globals.internal, export_internal_api());
    }
    Object.assign(globals.internal, export_internal());
    Object.assign(runtimeHelpers, {
        stringify_as_error_with_stack: mono_wasm_stringify_as_error_with_stack,
        instantiate_symbols_asset,
        instantiate_asset,
        jiterpreter_dump_stats,
    });

    const API = export_api();
    Object.assign(exportedRuntimeAPI, {
        INTERNAL: globals.internal,
        Module: module,
        runtimeBuildInfo: {
            productVersion: ProductVersion,
            gitHash: GitHash,
            buildConfiguration: BuildConfiguration
        },
        ...API,
    });
    if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
        Object.assign(exportedRuntimeAPI, {
            MONO: globals.mono,
            BINDING: globals.binding,
        });
    }

    if (typeof module.disableDotnet6Compatibility === "undefined") {
        module.disableDotnet6Compatibility = true;
    }
    // here we expose objects global namespace for tests and backward compatibility
    if (!module.disableDotnet6Compatibility) {
        Object.assign(module, exportedRuntimeAPI);

        if (WasmEnableLegacyJsInterop && !disableLegacyJsInterop) {
            // backward compatibility
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            module.mono_bind_static_method = (fqn: string, signature: string/*ArgsMarshalString*/): Function => {
                mono_log_warn("Module.mono_bind_static_method is obsolete, please use [JSExportAttribute] interop instead");
                return mono_bind_static_method(fqn, signature);
            };
        }

        const warnWrap = (name: string, provider: () => any) => {
            if (typeof globalThisAny[name] !== "undefined") {
                // it already exists in the global namespace
                return;
            }
            let value: any = undefined;
            Object.defineProperty(globalThis, name, {
                get: () => {
                    if (is_nullish(value)) {
                        const stack = (new Error()).stack;
                        const nextLine = stack ? stack.substr(stack.indexOf("\n", 8) + 1) : "";
                        mono_log_warn(`global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                        value = provider();
                    }
                    return value;
                }
            });
        };
        globalThisAny.MONO = globals.mono;
        globalThisAny.BINDING = globals.binding;
        globalThisAny.INTERNAL = globals.internal;
        globalThisAny.Module = module;

        // Blazor back compat
        warnWrap("cwrap", () => module.cwrap);
        warnWrap("addRunDependency", () => module.addRunDependency);
        warnWrap("removeRunDependency", () => module.removeRunDependency);
    }

    // this code makes it possible to find dotnet runtime on a page via global namespace, even when there are multiple runtimes at the same time
    let list: RuntimeList;
    if (!globalThisAny.getDotnetRuntime) {
        globalThisAny.getDotnetRuntime = (runtimeId: string) => globalThisAny.getDotnetRuntime.__list.getRuntime(runtimeId);
        globalThisAny.getDotnetRuntime.__list = list = new RuntimeList();
    }
    else {
        list = globalThisAny.getDotnetRuntime.__list;
    }
    list.registerRuntime(exportedRuntimeAPI);

    return exportedRuntimeAPI;
}

class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<RuntimeAPI> } = {};

    public registerRuntime(api: RuntimeAPI): number {
        api.runtimeId = Object.keys(this.list).length;
        this.list[api.runtimeId] = create_weak_ref(api);
        return api.runtimeId;
    }

    public getRuntime(runtimeId: number): RuntimeAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}

// export external API
export {
    passEmscriptenInternals, initializeExports, initializeReplacements, configureEmscriptenStartup, configureWorkerStartup, setRuntimeGlobals
};