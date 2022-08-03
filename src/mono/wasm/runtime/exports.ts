// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import Configuration from "consts:configuration";
import MonoWasmThreads from "consts:monoWasmThreads";

import { ENVIRONMENT_IS_PTHREAD, set_imports_exports } from "./imports";
import { DotnetModule, is_nullish, DotnetPublicAPI, EarlyImports, EarlyExports, EarlyReplacements } from "./types";
import { configure_emscripten_startup, mono_wasm_pthread_worker_init } from "./startup";
import { mono_bind_static_method } from "./net6-legacy/method-calls";

import { create_weak_ref } from "./weak-ref";
import { export_binding_api, export_mono_api } from "./net6-legacy/exports-legacy";
import { export_internal } from "./exports-internal";
import { export_linker } from "./exports-linker";
import { init_polyfills } from "./polyfills";

export const __initializeImportsAndExports: any = initializeImportsAndExports; // don't want to export the type
export let __linker_exports: any = null;
let exportedAPI: DotnetPublicAPI;


// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
// At runtime this will be referred to as 'createDotnetRuntime'
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function initializeImportsAndExports(
    imports: EarlyImports,
    exports: EarlyExports,
    replacements: EarlyReplacements,
): DotnetPublicAPI {
    const module = exports.module as DotnetModule;
    const globalThisAny = globalThis as any;

    // we want to have same instance of MONO, BINDING and Module in dotnet iffe
    set_imports_exports(imports, exports);
    init_polyfills(replacements);

    // here we merge methods from the local objects into exported objects
    Object.assign(exports.mono, export_mono_api());
    Object.assign(exports.binding, export_binding_api());
    Object.assign(exports.internal, export_internal());
    __linker_exports = export_linker();

    exportedAPI = <any>{
        MONO: exports.mono,
        BINDING: exports.binding,
        INTERNAL: exports.internal,
        EXPORTS: exports.marshaled_exports,
        IMPORTS: exports.marshaled_imports,
        Module: module,
        RuntimeBuildInfo: {
            ProductVersion,
            Configuration
        }
    };
    if (exports.module.__undefinedConfig) {
        module.disableDotnet6Compatibility = true;
        module.configSrc = "./mono-config.json";
    }

    if (!module.print) {
        module.print = console.log.bind(console);
    }
    if (!module.printErr) {
        module.printErr = console.error.bind(console);
    }

    if (typeof module.disableDotnet6Compatibility === "undefined") {
        module.disableDotnet6Compatibility = true;
    }
    // here we expose objects global namespace for tests and backward compatibility
    if (imports.isGlobal || !module.disableDotnet6Compatibility) {
        Object.assign(module, exportedAPI);

        // backward compatibility
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        module.mono_bind_static_method = (fqn: string, signature: string/*ArgsMarshalString*/): Function => {
            console.warn("MONO_WASM: Module.mono_bind_static_method is obsolete, please use BINDING.bind_static_method instead");
            return mono_bind_static_method(fqn, signature);
        };

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
        if (!imports.isGlobal) {
            globalThisAny.Module = module;
        }

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
    list.registerRuntime(exportedAPI);

    if (MonoWasmThreads && ENVIRONMENT_IS_PTHREAD) {
        // eslint-disable-next-line no-inner-declarations
        async function workerInit(): Promise<DotnetModule> {
            await mono_wasm_pthread_worker_init();

            // HACK: Emscripten's dotnet.worker.js expects the exports of dotnet.js module to be Module object
            // until we have our own fix for dotnet.worker.js file
            // we also skip all emscripten startup event and configuration of worker's JS state
            // note that emscripten events are not firing either

            return exportedAPI.Module;
        }
        // Emscripten pthread worker.js is ok with a Promise here.
        return <any>workerInit();
    }

    configure_emscripten_startup(module, exportedAPI);

    return exportedAPI;
}


class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<DotnetPublicAPI> } = {};

    public registerRuntime(api: DotnetPublicAPI): number {
        api.RuntimeId = Object.keys(this.list).length;
        this.list[api.RuntimeId] = create_weak_ref(api);
        return api.RuntimeId;
    }

    public getRuntime(runtimeId: number): DotnetPublicAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}

export function get_dotnet_instance(): DotnetPublicAPI {
    return exportedAPI;
}
