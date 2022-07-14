// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import ProductVersion from "consts:productVersion";
import Configuration from "consts:configuration";

import { ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_WORKER, ExitStatusError, runtimeHelpers, setImportsAndExports } from "./imports";
import { DotnetModuleConfigImports, DotnetModule, is_nullish, DotnetPublicAPI, PThreadReplacements } from "./types";
import {
    configure_emscripten_startup
} from "./startup";
import {
    mono_bind_static_method
} from "./legacy/method-calls";
import {
    afterUpdateGlobalBufferAndViews
} from "./memory";
import { create_weak_ref } from "./weak-ref";
import { fetch_like, readAsync_like } from "./polyfills";
import { afterThreadInitTLS } from "./pthreads/worker";
import { afterLoadWasmModuleToWorker } from "./pthreads/browser";
import { export_binding_api, export_mono_api } from "./legacy/exports-legacy";
import { export_internal } from "./exports-internal";
import { export_linker } from "./exports-linker";

export const __initializeImportsAndExports: any = initializeImportsAndExports; // don't want to export the type
export let __linker_exports: any = null;
let exportedAPI: DotnetPublicAPI;


// this is executed early during load of emscripten runtime
// it exports methods to global objects MONO, BINDING and Module in backward compatible way
// At runtime this will be referred to as 'createDotnetRuntime'
// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function initializeImportsAndExports(
    imports: { isESM: boolean, isGlobal: boolean, isNode: boolean, isWorker: boolean, isShell: boolean, isWeb: boolean, isPThread: boolean, locateFile: Function, quit_: Function, ExitStatus: ExitStatusError, requirePromise: Promise<Function> },
    exports: { mono: any, binding: any, internal: any, module: any, marshaled_exports: any, marshaled_imports: any },
    replacements: { fetch: any, readAsync: any, require: any, requireOut: any, noExitRuntime: boolean, updateGlobalBufferAndViews: Function, pthreadReplacements: PThreadReplacements | undefined | null },
): DotnetPublicAPI {
    const module = exports.module as DotnetModule;
    const globalThisAny = globalThis as any;

    // we want to have same instance of MONO, BINDING and Module in dotnet iffe
    setImportsAndExports(imports, exports);

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
    module.imports = module.imports || <DotnetModuleConfigImports>{};
    if (!module.imports.require) {
        module.imports.require = (name) => {
            const resolved = (<any>module.imports)[name];
            if (resolved) {
                return resolved;
            }
            if (replacements.require) {
                return replacements.require(name);
            }
            throw new Error(`Please provide Module.imports.${name} or Module.imports.require`);
        };
    }

    if (module.imports.fetch) {
        runtimeHelpers.fetch = module.imports.fetch;
    }
    else {
        runtimeHelpers.fetch = fetch_like;
    }
    replacements.fetch = runtimeHelpers.fetch;
    replacements.readAsync = readAsync_like;
    replacements.requireOut = module.imports.require;
    const originalUpdateGlobalBufferAndViews = replacements.updateGlobalBufferAndViews;
    replacements.updateGlobalBufferAndViews = (buffer: ArrayBufferLike) => {
        originalUpdateGlobalBufferAndViews(buffer);
        afterUpdateGlobalBufferAndViews(buffer);
    };

    replacements.noExitRuntime = ENVIRONMENT_IS_WEB;

    if (replacements.pthreadReplacements) {
        const originalLoadWasmModuleToWorker = replacements.pthreadReplacements.loadWasmModuleToWorker;
        replacements.pthreadReplacements.loadWasmModuleToWorker = (worker: Worker, onFinishedLoading: Function): void => {
            originalLoadWasmModuleToWorker(worker, onFinishedLoading);
            afterLoadWasmModuleToWorker(worker);
        };
        const originalThreadInitTLS = replacements.pthreadReplacements.threadInitTLS;
        replacements.pthreadReplacements.threadInitTLS = (): void => {
            originalThreadInitTLS();
            afterThreadInitTLS();
        };
    }

    if (typeof module.disableDotnet6Compatibility === "undefined") {
        module.disableDotnet6Compatibility = imports.isESM;
    }
    // here we expose objects global namespace for tests and backward compatibility
    if (imports.isGlobal || !module.disableDotnet6Compatibility) {
        Object.assign(module, exportedAPI);

        // backward compatibility
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        module.mono_bind_static_method = (fqn: string, signature: string/*ArgsMarshalString*/): Function => {
            console.warn("Module.mono_bind_static_method is obsolete, please use BINDING.bind_static_method instead");
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
                        console.warn(`global ${name} is obsolete, please use Module.${name} instead ${nextLine}`);
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

    configure_emscripten_startup(module, exportedAPI);

    if (ENVIRONMENT_IS_WORKER) {
        // HACK: Emscripten's dotnet.worker.js expects the exports of dotnet.js module to be Module object
        // until we have our own fix for dotnet.worker.js file
        return <any>exportedAPI.Module;
    }

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
