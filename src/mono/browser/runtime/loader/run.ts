// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import BuildConfiguration from "consts:configuration";

import { type MonoConfig, type DotnetHostBuilder, type DotnetModuleConfig, type RuntimeAPI, type LoadBootResourceCallback, GlobalizationMode } from "../types";
import type { EmscriptenModuleInternal, RuntimeModuleExportsInternal, NativeModuleExportsInternal, HybridGlobalizationModuleExportsInternal, } from "../types/internal";

import { ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_WORKER, emscriptenModule, exportedRuntimeAPI, globalObjectsRoot, monoConfig, mono_assert } from "./globals";
import { deep_merge_config, deep_merge_module, mono_wasm_load_config } from "./config";
import { installUnhandledErrorHandler, mono_exit, registerEmscriptenExitHandlers } from "./exit";
import { setup_proxy_console, mono_log_info, mono_log_debug, mono_log_error } from "./logging";
import { mono_download_assets, preloadWorkers, prepareAssets, prepareAssetsWorker, resolve_single_asset_path, streamingCompileWasm } from "./assets";
import { detect_features_and_polyfill } from "./polyfills";
import { runtimeHelpers, loaderHelpers, globalizationHelpers } from "./globals";
import { init_globalization } from "./icu";
import { setupPreloadChannelToMainThread } from "./worker";
import { importLibraryInitializers, invokeLibraryInitializers } from "./libraryInitializers";
import { initCacheToUseIfEnabled } from "./assetsCache";


export class HostBuilder implements DotnetHostBuilder {
    private instance?: RuntimeAPI;

    // internal
    withModuleConfig (moduleConfig: DotnetModuleConfig): DotnetHostBuilder {
        try {
            deep_merge_module(emscriptenModule, moduleConfig);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withOnConfigLoaded (onConfigLoaded: (config: MonoConfig) => void | Promise<void>): DotnetHostBuilder {
        try {
            deep_merge_module(emscriptenModule, {
                onConfigLoaded
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withConsoleForwarding (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                forwardConsoleLogsToWS: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withExitOnUnhandledError (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                exitOnUnhandledError: true
            });
            installUnhandledErrorHandler();
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withAsyncFlushOnExit (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                asyncFlushOnExit: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withExitCodeLogging (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                logExitCode: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withElementOnExit (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                appendElementOnExit: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withInteropCleanupOnExit (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                interopCleanupOnExit: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withDumpThreadsOnNonZeroExit (): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                dumpThreadsOnNonZeroExit: true
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    //  todo fallback later by debugLevel
    withWaitingForDebugger (level: number): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                waitForDebugger: level
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withInterpreterPgo (value: boolean, autoSaveDelay?: number): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                interpreterPgo: value,
                interpreterPgoSaveDelay: autoSaveDelay
            });
            if (monoConfig.runtimeOptions)
                monoConfig.runtimeOptions.push("--interp-pgo-recording");
            else
                monoConfig.runtimeOptions = ["--interp-pgo-recording"];
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfig (config: MonoConfig): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, config);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfigSrc (configSrc: string): DotnetHostBuilder {
        try {
            mono_assert(configSrc && typeof configSrc === "string", "must be file path or URL");
            deep_merge_module(emscriptenModule, { configSrc });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withVirtualWorkingDirectory (vfsPath: string): DotnetHostBuilder {
        try {
            mono_assert(vfsPath && typeof vfsPath === "string", "must be directory path");
            deep_merge_config(monoConfig, {
                virtualWorkingDirectory: vfsPath
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withEnvironmentVariable (name: string, value: string): DotnetHostBuilder {
        try {
            const environmentVariables: { [key: string]: string } = {};
            environmentVariables[name] = value;
            deep_merge_config(monoConfig, {
                environmentVariables
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withEnvironmentVariables (variables: { [i: string]: string; }): DotnetHostBuilder {
        try {
            mono_assert(variables && typeof variables === "object", "must be dictionary object");
            deep_merge_config(monoConfig, {
                environmentVariables: variables
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withDiagnosticTracing (enabled: boolean): DotnetHostBuilder {
        try {
            mono_assert(typeof enabled === "boolean", "must be boolean");
            deep_merge_config(monoConfig, {
                diagnosticTracing: enabled
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withDebugging (level: number): DotnetHostBuilder {
        try {
            mono_assert(level !== undefined && level !== null && typeof level === "number", "must be number");
            deep_merge_config(monoConfig, {
                debugLevel: level
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withApplicationArguments (...args: string[]): DotnetHostBuilder {
        try {
            mono_assert(args && Array.isArray(args), "must be array of strings");
            deep_merge_config(monoConfig, {
                applicationArguments: args
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withRuntimeOptions (runtimeOptions: string[]): DotnetHostBuilder {
        try {
            mono_assert(runtimeOptions && Array.isArray(runtimeOptions), "must be array of strings");
            if (monoConfig.runtimeOptions)
                monoConfig.runtimeOptions.push(...runtimeOptions);
            else
                monoConfig.runtimeOptions = runtimeOptions;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withMainAssembly (mainAssemblyName: string): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                mainAssemblyName
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withApplicationArgumentsFromQuery (): DotnetHostBuilder {
        try {
            if (!globalThis.window) {
                throw new Error("Missing window to the query parameters from");
            }

            if (typeof globalThis.URLSearchParams == "undefined") {
                throw new Error("URLSearchParams is supported");
            }

            const params = new URLSearchParams(globalThis.window.location.search);
            const values = params.getAll("arg");
            return this.withApplicationArguments(...values);
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withApplicationEnvironment (applicationEnvironment?: string): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                applicationEnvironment,
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withApplicationCulture (applicationCulture?: string): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                applicationCulture,
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withResourceLoader (loadBootResource?: LoadBootResourceCallback): DotnetHostBuilder {
        try {
            loaderHelpers.loadBootResource = loadBootResource;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    async download (): Promise<void> {
        try {
            await downloadOnly();
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    async create (): Promise<RuntimeAPI> {
        try {
            if (!this.instance) {
                this.instance = await createApi();
            }
            return this.instance;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    async run (): Promise<number> {
        try {
            mono_assert(emscriptenModule.config, "Null moduleConfig.config");
            if (!this.instance) {
                await this.create();
            }
            return this.instance!.runMainAndExit();
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }
}

export async function createApi (): Promise<RuntimeAPI> {
    await createEmscripten(emscriptenModule);
    return globalObjectsRoot.api;
}

let emscriptenPrepared = false;
async function prepareEmscripten (moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)) {
    if (emscriptenPrepared) {
        return;
    }
    emscriptenPrepared = true;
    if (ENVIRONMENT_IS_WEB && loaderHelpers.config.forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
        setup_proxy_console("main", globalThis.console, globalThis.location.origin);
    }
    mono_assert(emscriptenModule, "Null moduleConfig");
    mono_assert(loaderHelpers.config, "Null moduleConfig.config");

    // extract ModuleConfig
    if (typeof moduleFactory === "function") {
        const extension = moduleFactory(globalObjectsRoot.api) as any;
        if (extension.ready) {
            throw new Error("Module.ready couldn't be redefined.");
        }
        Object.assign(emscriptenModule, extension);
        deep_merge_module(emscriptenModule, extension);
    } else if (typeof moduleFactory === "object") {
        deep_merge_module(emscriptenModule, moduleFactory);
    } else {
        throw new Error("Can't use moduleFactory callback of createDotnetRuntime function.");
    }

    await detect_features_and_polyfill(emscriptenModule);
}

export async function createEmscripten (moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): Promise<RuntimeAPI | EmscriptenModuleInternal> {
    await prepareEmscripten(moduleFactory);

    if (BuildConfiguration === "Debug" && !ENVIRONMENT_IS_WORKER) {
        mono_log_info(`starting script ${loaderHelpers.scriptUrl}`);
        mono_log_info(`starting in ${loaderHelpers.scriptDirectory}`);
    }

    registerEmscriptenExitHandlers();

    return emscriptenModule.ENVIRONMENT_IS_PTHREAD
        ? createEmscriptenWorker()
        : createEmscriptenMain();
}

let jsModuleRuntimePromise: Promise<RuntimeModuleExportsInternal>;
let jsModuleNativePromise: Promise<NativeModuleExportsInternal>;

// in the future we can use feature detection to load different flavors
function importModules () {
    const jsModuleRuntimeAsset = resolve_single_asset_path("js-module-runtime");
    const jsModuleNativeAsset = resolve_single_asset_path("js-module-native");
    if (jsModuleRuntimePromise && jsModuleNativePromise) {
        return [jsModuleRuntimePromise, jsModuleNativePromise];
    }

    if (typeof jsModuleRuntimeAsset.moduleExports === "object") {
        jsModuleRuntimePromise = jsModuleRuntimeAsset.moduleExports;
    } else {
        mono_log_debug(() => `Attempting to import '${jsModuleRuntimeAsset.resolvedUrl}' for ${jsModuleRuntimeAsset.name}`);
        jsModuleRuntimePromise = import(/*! webpackIgnore: true */jsModuleRuntimeAsset.resolvedUrl!);
    }

    if (typeof jsModuleNativeAsset.moduleExports === "object") {
        jsModuleNativePromise = jsModuleNativeAsset.moduleExports;
    } else {
        mono_log_debug(() => `Attempting to import '${jsModuleNativeAsset.resolvedUrl}' for ${jsModuleNativeAsset.name}`);
        jsModuleNativePromise = import(/*! webpackIgnore: true */jsModuleNativeAsset.resolvedUrl!);
    }
    return [jsModuleRuntimePromise, jsModuleNativePromise];
}

async function getHybridModuleExports () : Promise<HybridGlobalizationModuleExportsInternal> {
    let jsModuleHybridGlobalizationPromise: Promise<NativeModuleExportsInternal> | undefined = undefined;
    // todo: move it for after runtime startup
    const jsModuleHybridGlobalization = resolve_single_asset_path("js-module-globalization");
    if (typeof jsModuleHybridGlobalization.moduleExports === "object") {
        jsModuleHybridGlobalizationPromise = jsModuleHybridGlobalization.moduleExports;
    } else {
        mono_log_debug(`Attempting to import '${jsModuleHybridGlobalization.resolvedUrl}' for ${jsModuleHybridGlobalization.name}`);
        jsModuleHybridGlobalizationPromise = import(/*! webpackIgnore: true */jsModuleHybridGlobalization.resolvedUrl!);
    }
    const hybridModule = await jsModuleHybridGlobalizationPromise;
    return hybridModule as any;
}

async function initializeModules (es6Modules: [RuntimeModuleExportsInternal, NativeModuleExportsInternal]) {
    const { initializeExports, initializeReplacements, configureRuntimeStartup, configureEmscriptenStartup, configureWorkerStartup, setRuntimeGlobals, passEmscriptenInternals } = es6Modules[0];
    const { default: emscriptenFactory } = es6Modules[1];
    setRuntimeGlobals(globalObjectsRoot);
    initializeExports(globalObjectsRoot);
    if (loaderHelpers.config.globalizationMode === GlobalizationMode.Hybrid) {
        const hybridModule = await getHybridModuleExports();
        const { initHybrid } = hybridModule;
        initHybrid(globalizationHelpers, runtimeHelpers);
    }
    await configureRuntimeStartup(emscriptenModule);
    loaderHelpers.runtimeModuleLoaded.promise_control.resolve();

    const result = emscriptenFactory((originalModule: EmscriptenModuleInternal) => {
        Object.assign(emscriptenModule, {
            ready: originalModule.ready,
            __dotnet_runtime: {
                initializeReplacements, configureEmscriptenStartup, configureWorkerStartup, passEmscriptenInternals
            }
        });

        return emscriptenModule;
    });
    result.catch((error) => {
        if (error.message && error.message.toLowerCase().includes("out of memory")) {
            mono_log_error("Memory allocation exceeded. Please increase the memory by adjusting EmccMaximumHeapSize.", error);
        }
        mono_exit(1, error);
        throw error;
    });
}

async function downloadOnly ():Promise<void> {
    prepareEmscripten(emscriptenModule);

    // download config
    await mono_wasm_load_config(emscriptenModule);

    prepareAssets();

    await initCacheToUseIfEnabled();

    init_globalization();

    mono_download_assets(); // intentionally not awaited

    await loaderHelpers.allDownloadsFinished.promise;
}

async function createEmscriptenMain (): Promise<RuntimeAPI> {
    // download config
    await mono_wasm_load_config(emscriptenModule);

    prepareAssets();

    const promises = importModules();

    await initCacheToUseIfEnabled();

    streamingCompileWasm(); // intentionally not awaited

    setTimeout(async () => {
        try {
            init_globalization();
            preloadWorkers();
            await mono_download_assets();
        } catch (err) {
            mono_exit(1, err);
        }
    }, 0);

    const es6Modules = await Promise.all(promises);

    await initializeModules(es6Modules as any);

    await runtimeHelpers.dotnetReady.promise;

    await importLibraryInitializers(loaderHelpers.config.resources?.modulesAfterRuntimeReady);
    await invokeLibraryInitializers("onRuntimeReady", [globalObjectsRoot.api]);

    return exportedRuntimeAPI;
}

async function createEmscriptenWorker (): Promise<EmscriptenModuleInternal> {
    setupPreloadChannelToMainThread();

    await loaderHelpers.afterConfigLoaded.promise;

    prepareAssetsWorker();

    setTimeout(async () => {
        try {
            // load subset which is on JS heap rather than in WASM linear memory
            await mono_download_assets();
        } catch (err) {
            mono_exit(1, err);
        }
    }, 0);

    const promises = importModules();
    const es6Modules = await Promise.all(promises);
    await initializeModules(es6Modules as any);

    return emscriptenModule;
}
