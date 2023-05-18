// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoConfig, DotnetHostBuilder, DotnetModuleConfig, RuntimeAPI, WebAssemblyStartOptions } from "../types";
import type { MonoConfigInternal, EmscriptenModuleInternal, RuntimeModuleExportsInternal, NativeModuleExportsInternal, } from "../types/internal";

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB, exportedRuntimeAPI, globalObjectsRoot } from "./globals";
import { deep_merge_config, deep_merge_module, mono_wasm_load_config } from "./config";
import { mono_exit } from "./exit";
import { setup_proxy_console } from "./logging";
import { resolve_asset_path, start_asset_download } from "./assets";
import { init_polyfills } from "./polyfills";
import { runtimeHelpers, loaderHelpers } from "./globals";
import { init_globalization } from "./icu";
import { setupPreloadChannelToMainThread } from "./worker";


const module = globalObjectsRoot.module;
const monoConfig = module.config as MonoConfigInternal;

export class HostBuilder implements DotnetHostBuilder {
    private instance?: RuntimeAPI;
    private applicationArguments?: string[];
    private virtualWorkingDirectory?: string;

    // internal
    withModuleConfig(moduleConfig: DotnetModuleConfig): DotnetHostBuilder {
        try {
            deep_merge_module(module, moduleConfig);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withConsoleForwarding(): DotnetHostBuilder {
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
    withExitOnUnhandledError(): DotnetHostBuilder {
        const handler = function fatal_handler(event: Event, error: any) {
            event.preventDefault();
            try {
                if (!error || !error.silent) mono_exit(1, error);
            } catch (err) {
                // no not re-throw from the fatal handler
            }
        };
        try {
            // it seems that emscripten already does the right thing for NodeJs and that there is no good solution for V8 shell.
            if (ENVIRONMENT_IS_WEB) {
                window.addEventListener("unhandledrejection", (event) => handler(event, event.reason));
                window.addEventListener("error", (event) => handler(event, event.error));
            }
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withAsyncFlushOnExit(): DotnetHostBuilder {
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
    withExitCodeLogging(): DotnetHostBuilder {
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
    withElementOnExit(): DotnetHostBuilder {
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
    //  todo fallback later by debugLevel
    withWaitingForDebugger(level: number): DotnetHostBuilder {
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

    withStartupMemoryCache(value: boolean): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, {
                startupMemoryCache: value
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfig(config: MonoConfig): DotnetHostBuilder {
        try {
            deep_merge_config(monoConfig, config);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfigSrc(configSrc: string): DotnetHostBuilder {
        try {
            mono_assert(configSrc && typeof configSrc === "string", "must be file path or URL");
            deep_merge_module(module, { configSrc });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder {
        try {
            mono_assert(vfsPath && typeof vfsPath === "string", "must be directory path");
            this.virtualWorkingDirectory = vfsPath;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder {
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

    withEnvironmentVariables(variables: { [i: string]: string; }): DotnetHostBuilder {
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

    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder {
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

    withDebugging(level: number): DotnetHostBuilder {
        try {
            mono_assert(level && typeof level === "number", "must be number");
            deep_merge_config(monoConfig, {
                debugLevel: level
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withApplicationArguments(...args: string[]): DotnetHostBuilder {
        try {
            mono_assert(args && Array.isArray(args), "must be array of strings");
            this.applicationArguments = args;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withRuntimeOptions(runtimeOptions: string[]): DotnetHostBuilder {
        try {
            mono_assert(runtimeOptions && Array.isArray(runtimeOptions), "must be array of strings");
            deep_merge_config(monoConfig, {
                runtimeOptions
            });
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder {
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

    withApplicationArgumentsFromQuery(): DotnetHostBuilder {
        try {
            if (!globalThis.window) {
                throw new Error("Missing window to the query parameters from");
            }

            if (typeof globalThis.URLSearchParams == "undefined") {
                throw new Error("URLSearchParams is supported");
            }

            const params = new URLSearchParams(window.location.search);
            const values = params.getAll("arg");
            return this.withApplicationArguments(...values);
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withStartupOptions(startupOptions: Partial<WebAssemblyStartOptions>): DotnetHostBuilder {
        deep_merge_config(monoConfig, {
            startupOptions
        });
        return this;
    }

    async create(): Promise<RuntimeAPI> {
        try {
            if (!this.instance) {
                if (ENVIRONMENT_IS_WEB && (module.config! as MonoConfigInternal).forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
                    setup_proxy_console("main", globalThis.console, globalThis.location.origin);
                }
                if (ENVIRONMENT_IS_NODE) {
                    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                    // @ts-ignore:
                    const process = await import(/* webpackIgnore: true */"process");
                    if (process.versions.node.split(".")[0] < 14) {
                        throw new Error(`NodeJS at '${process.execPath}' has too low version '${process.versions.node}'`);
                    }
                }
                mono_assert(module, "Null moduleConfig");
                mono_assert(module.config, "Null moduleConfig.config");
                await createEmscripten(module);
                this.instance = globalObjectsRoot.api;
            }
            if (this.virtualWorkingDirectory) {
                const FS = (this.instance!.Module as any).FS;
                const wds = FS.stat(this.virtualWorkingDirectory);
                mono_assert(wds && FS.isDir(wds.mode), () => `Could not find working directory ${this.virtualWorkingDirectory}`);
                FS.chdir(this.virtualWorkingDirectory);
            }
            return this.instance;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    async run(): Promise<number> {
        try {
            mono_assert(module.config, "Null moduleConfig.config");
            if (!this.instance) {
                await this.create();
            }
            mono_assert(module.config.mainAssemblyName, "Null moduleConfig.config.mainAssemblyName");
            if (!this.applicationArguments) {
                if (ENVIRONMENT_IS_NODE) {
                    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                    // @ts-ignore:
                    const process = await import(/* webpackIgnore: true */"process");
                    this.applicationArguments = process.argv.slice(2);
                } else {
                    this.applicationArguments = [];
                }
            }
            return this.instance!.runMainAndExit(module.config.mainAssemblyName, this.applicationArguments!);
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }
}

export async function createEmscripten(moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): Promise<RuntimeAPI | EmscriptenModuleInternal> {
    // extract ModuleConfig
    if (typeof moduleFactory === "function") {
        const extension = moduleFactory(globalObjectsRoot.api) as any;
        if (extension.ready) {
            throw new Error("MONO_WASM: Module.ready couldn't be redefined.");
        }
        Object.assign(module, extension);
        deep_merge_module(module, extension);
    }
    else if (typeof moduleFactory === "object") {
        deep_merge_module(module, moduleFactory);
    }
    else {
        throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.");
    }

    return module.ENVIRONMENT_IS_PTHREAD
        ? createEmscriptenWorker()
        : createEmscriptenMain();
}

function importModules() {
    runtimeHelpers.runtimeModuleUrl = resolve_asset_path("js-module-runtime").resolvedUrl!;
    runtimeHelpers.nativeModuleUrl = resolve_asset_path("js-module-native").resolvedUrl!;
    return [
        // keep js module names dynamic by using config, in the future we can use feature detection to load different flavors
        import(runtimeHelpers.runtimeModuleUrl),
        import(runtimeHelpers.nativeModuleUrl),
    ];
}

function initializeModules(es6Modules: [RuntimeModuleExportsInternal, NativeModuleExportsInternal]) {
    const { initializeExports, initializeReplacements, configureEmscriptenStartup, configureWorkerStartup, setRuntimeGlobals, passEmscriptenInternals } = es6Modules[0];
    const { default: emscriptenFactory } = es6Modules[1];
    setRuntimeGlobals(globalObjectsRoot);
    initializeExports(globalObjectsRoot);
    loaderHelpers.runtimeModuleLoaded.promise_control.resolve();

    emscriptenFactory((originalModule: EmscriptenModuleInternal) => {
        Object.assign(module, {
            ready: originalModule.ready,
            __dotnet_runtime: {
                initializeReplacements, configureEmscriptenStartup, configureWorkerStartup, passEmscriptenInternals
            }
        });

        return module;
    });
}

async function createEmscriptenMain(): Promise<RuntimeAPI> {
    await init_polyfills(module);

    if (!module.configSrc && (!module.config || Object.keys(module.config).length === 0 || !module.config.assets)) {
        // if config file location nor assets are provided
        if (loaderHelpers.scriptDirectory.indexOf("/_framework") == -1) {
            // we are not inside _framework (= wasm template)
            module.configSrc = "./_framework/blazor.boot.json";
        } else {
            // blazor app
            module.configSrc = "./blazor.boot.json";
        }
    }

    // download config
    await mono_wasm_load_config(module);

    const promises = importModules();

    const wasmModuleAsset = resolve_asset_path("dotnetwasm");
    start_asset_download(wasmModuleAsset).then(asset => {
        loaderHelpers.wasmDownloadPromise.promise_control.resolve(asset);
    });

    init_globalization();
    // TODO call mono_download_assets(); here in parallel ?
    const es6Modules = await Promise.all(promises);
    initializeModules(es6Modules as any);

    await runtimeHelpers.dotnetReady.promise;

    return exportedRuntimeAPI;
}

async function createEmscriptenWorker(): Promise<EmscriptenModuleInternal> {
    await init_polyfills(module);

    setupPreloadChannelToMainThread();

    await loaderHelpers.afterConfigLoaded.promise;

    const promises = importModules();
    const es6Modules = await Promise.all(promises);
    initializeModules(es6Modules as any);

    return module;
}
