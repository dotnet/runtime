// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: code in this file is executed before any of the emscripten code, so there is very little initialized already
import { WebAssemblyStartOptions } from "./blazor/WebAssemblyStartOptions";
import { emscriptenEntrypoint, runtimeHelpers } from "./imports";
import { setup_proxy_console } from "./logging";
import { mono_exit } from "./run";
import { DotnetModuleConfig, MonoConfig, MonoConfigInternal, mono_assert, RuntimeAPI } from "./types";

export interface DotnetHostBuilder {
    withConfig(config: MonoConfig): DotnetHostBuilder
    withConfigSrc(configSrc: string): DotnetHostBuilder
    withApplicationArguments(...args: string[]): DotnetHostBuilder
    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder
    withEnvironmentVariables(variables: { [i: string]: string; }): DotnetHostBuilder
    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder
    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder
    withDebugging(level: number): DotnetHostBuilder
    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder
    withApplicationArgumentsFromQuery(): DotnetHostBuilder
    withStartupMemoryCache(value: boolean): DotnetHostBuilder
    create(): Promise<RuntimeAPI>
    run(): Promise<number>
}

class HostBuilder implements DotnetHostBuilder {
    private instance?: RuntimeAPI;
    private applicationArguments?: string[];
    private virtualWorkingDirectory?: string;
    private moduleConfig: DotnetModuleConfig = {
        disableDotnet6Compatibility: true,
        configSrc: "./mono-config.json",
        config: runtimeHelpers.config,
    };

    // internal
    withModuleConfig(moduleConfig: DotnetModuleConfig): DotnetHostBuilder {
        try {
            Object.assign(this.moduleConfig!, moduleConfig);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withConsoleForwarding(): DotnetHostBuilder {
        try {
            const configInternal: MonoConfigInternal = {
                forwardConsoleLogsToWS: true
            };
            Object.assign(this.moduleConfig.config!, configInternal);
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
                mono_exit(1, error);
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
            const configInternal: MonoConfigInternal = {
                asyncFlushOnExit: true
            };
            Object.assign(this.moduleConfig.config!, configInternal);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withExitCodeLogging(): DotnetHostBuilder {
        try {
            const configInternal: MonoConfigInternal = {
                logExitCode: true
            };
            Object.assign(this.moduleConfig.config!, configInternal);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    // internal
    withElementOnExit(): DotnetHostBuilder {
        try {
            const configInternal: MonoConfigInternal = {
                appendElementOnExit: true
            };
            Object.assign(this.moduleConfig.config!, configInternal);
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
            const configInternal: MonoConfigInternal = {
                waitForDebugger: level
            };
            Object.assign(this.moduleConfig.config!, configInternal);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withStartupMemoryCache(value: boolean): DotnetHostBuilder {
        try {
            const configInternal: MonoConfigInternal = {
                startupMemoryCache: value
            };
            Object.assign(this.moduleConfig.config!, configInternal);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfig(config: MonoConfig): DotnetHostBuilder {
        try {
            const providedConfig = { ...config };
            providedConfig.assets = [...(this.moduleConfig.config!.assets || []), ...(providedConfig.assets || [])];
            providedConfig.environmentVariables = { ...(this.moduleConfig.config!.environmentVariables || {}), ...(providedConfig.environmentVariables || {}) };
            Object.assign(this.moduleConfig.config!, providedConfig);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withConfigSrc(configSrc: string): DotnetHostBuilder {
        try {
            mono_assert(configSrc && typeof configSrc === "string", "must be file path or URL");
            Object.assign(this.moduleConfig, { configSrc });
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
            this.moduleConfig.config!.environmentVariables![name] = value;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withEnvironmentVariables(variables: { [i: string]: string; }): DotnetHostBuilder {
        try {
            mono_assert(variables && typeof variables === "object", "must be dictionary object");
            Object.assign(this.moduleConfig.config!.environmentVariables!, variables);
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder {
        try {
            mono_assert(typeof enabled === "boolean", "must be boolean");
            this.moduleConfig.config!.diagnosticTracing = enabled;
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withDebugging(level: number): DotnetHostBuilder {
        try {
            mono_assert(level && typeof level === "number", "must be number");
            this.moduleConfig.config!.debugLevel = level;
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
            const configInternal = this.moduleConfig.config as MonoConfigInternal;
            configInternal.runtimeOptions = [...(configInternal.runtimeOptions || []), ...(runtimeOptions || [])];
            return this;
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }

    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder {
        try {
            this.moduleConfig.config!.mainAssemblyName = mainAssemblyName;
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
        const configInternal = this.moduleConfig.config as MonoConfigInternal;
        configInternal.startupOptions = startupOptions;
        return this.withConfigSrc("blazor.boot.json");
    }

    async create(): Promise<RuntimeAPI> {
        try {
            if (!this.instance) {
                if (ENVIRONMENT_IS_WEB && (this.moduleConfig.config! as MonoConfigInternal).forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
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
                mono_assert(this.moduleConfig, "Null moduleConfig");
                mono_assert(this.moduleConfig.config, "Null moduleConfig.config");
                this.instance = await emscriptenEntrypoint(this.moduleConfig);
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
            mono_assert(this.moduleConfig.config, "Null moduleConfig.config");
            if (!this.instance) {
                await this.create();
            }
            mono_assert(this.moduleConfig.config.mainAssemblyName, "Null moduleConfig.config.mainAssemblyName");
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
            return this.instance!.runMainAndExit(this.moduleConfig.config.mainAssemblyName, this.applicationArguments!);
        } catch (err) {
            mono_exit(1, err);
            throw err;
        }
    }
}

export const dotnet: DotnetHostBuilder = new HostBuilder();
