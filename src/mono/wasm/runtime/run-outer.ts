// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: code in this file is executed before any of the emscripten code, so there is very little initialized already

import type { MonoConfig, DotnetHostBuilder, DotnetModuleConfig, RuntimeAPI, WebAssemblyStartOptions } from "./types-api";
import type { MonoConfigInternal, GlobalObjects, EmscriptenModuleInternal } from "./types";

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB, setGlobalObjects } from "./globals";
import { mono_exit } from "./run";
import { mono_assert } from "./types";
import { setup_proxy_console } from "./logging";
import { deep_merge_config, deep_merge_module } from "./config";
import { initializeExports } from "./exports";

export const globalObjectsRoot: GlobalObjects = {
    mono: {},
    binding: {},
    internal: {},
    module: {},
    helpers: {},
    api: {}
} as any;

setGlobalObjects(globalObjectsRoot);
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
        return this.withConfigSrc("blazor.boot.json");
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

export function unifyModuleConfig(originalModule: EmscriptenModuleInternal, moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): DotnetModuleConfig {
    initializeExports();
    Object.assign(module, { ready: originalModule.ready });
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

    return module;
}
