// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import GitHash from "consts:gitHash";
import MonoWasmThreads from "consts:monoWasmThreads";
import BuildConfiguration from "consts:configuration";
import WasmEnableLegacyJsInterop from "consts:WasmEnableLegacyJsInterop";
import type { RuntimeAPI } from "./loader/types";

import { ENVIRONMENT_IS_PTHREAD, set_imports_exports } from "./imports";
import { is_nullish, EarlyImports, EarlyExports, DotnetModuleInternal } from "./types";
import { configure_emscripten_startup, mono_wasm_pthread_worker_init } from "./startup";

import { create_weak_ref } from "./weak-ref";
import { export_internal } from "./exports-internal";
import { mono_exit } from "./run";
import { export_api } from "./export-api";

// legacy
import { mono_bind_static_method } from "./net6-legacy/method-calls";
import { export_binding_api, export_internal_api, export_mono_api } from "./net6-legacy/exports-legacy";
import { set_legacy_exports } from "./net6-legacy/imports";

// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
export function initializeImportsAndExports(
    imports: EarlyImports,
    exports: EarlyExports,
): RuntimeAPI {
    const exportedRuntimeAPI = exports.api;
    const module = exports.module as DotnetModuleInternal;
    const globalThisAny = globalThis as any;
    exports.mono_exit = mono_exit;

    // we want to have same instance of MONO, BINDING and Module in dotnet iife
    set_imports_exports(imports, exports);
    if (WasmEnableLegacyJsInterop) {
        set_legacy_exports(exports);
    }

    // here we merge methods from the local objects into exported objects
    if (WasmEnableLegacyJsInterop) {
        Object.assign(exports.mono, export_mono_api());
        Object.assign(exports.binding, export_binding_api());
        Object.assign(exports.internal, export_internal_api());
    }
    Object.assign(exports.internal, export_internal());
    const API = export_api();
    Object.assign(exportedRuntimeAPI, {
        INTERNAL: exports.internal,
        Module: module,
        runtimeBuildInfo: {
            productVersion: ProductVersion,
            gitHash: GitHash,
            buildConfiguration: BuildConfiguration
        },
        ...API,
    });
    if (WasmEnableLegacyJsInterop) {
        Object.assign(exportedRuntimeAPI, {
            MONO: exports.mono,
            BINDING: exports.binding,
        });
    }

    if (exports.module.__undefinedConfig) {
        module.disableDotnet6Compatibility = true;
        module.configSrc = "./mono-config.json";
    }

    if (!module.out) {
        module.out = console.log.bind(console);
    }
    if (!module.err) {
        module.err = console.error.bind(console);
    }

    if (typeof module.disableDotnet6Compatibility === "undefined") {
        module.disableDotnet6Compatibility = true;
    }
    // here we expose objects global namespace for tests and backward compatibility
    if (!module.disableDotnet6Compatibility) {
        Object.assign(module, exportedRuntimeAPI);

        if (WasmEnableLegacyJsInterop) {
            // backward compatibility
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            module.mono_bind_static_method = (fqn: string, signature: string/*ArgsMarshalString*/): Function => {
                console.warn("MONO_WASM: Module.mono_bind_static_method is obsolete, please use [JSExportAttribute] interop instead");
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
                        console.warn(`MONO_WASM: global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
                        value = provider();
                    }
                    return value;
                }
            });
        };
        globalThisAny.MONO = exports.mono;
        globalThisAny.BINDING = exports.binding;
        globalThisAny.INTERNAL = exports.internal;
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

    if (MonoWasmThreads && ENVIRONMENT_IS_PTHREAD) {
        return <any>mono_wasm_pthread_worker_init(module, exportedRuntimeAPI);
    }

    configure_emscripten_startup(module, exportedRuntimeAPI);

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

