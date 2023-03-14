// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WARNING: code in this file is executed before any of the emscripten code, so there is very little initialized already

import type { DotnetHostBuilder, RuntimeAPI, RuntimeModuleAPI } from "./types";
import type { DotnetModuleConfig, MonoConfig, MonoConfigInternal, RuntimeHelpers, AssetEntry, ResourceRequest, EarlyExports, EarlyImports, EarlyReplacements } from "../types";
import type { IMemoryView } from "../marshal";
import type { EmscriptenModule } from "../types/emscripten";

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB, runtimeHelpers, setImports } from "./imports";
import { init_polyfills_async, init_replacements } from "./polyfills";

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
            Object.assign(this.moduleConfig, { runtimeOptions });
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
                this.instance = await createDotnetRuntime(this.moduleConfig);
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

let mono_exit = (code: number, reason: any) => {
    console.log(`dotnet failed early ${code} ${reason}`);
};

let consoleWebSocket: WebSocket;

function setup_proxy_console(id: string, console: Console, origin: string): void {
    // this need to be copy, in order to keep reference to original methods
    const originalConsole = {
        log: console.log,
        error: console.error
    };
    const anyConsole = console as any;

    function proxyConsoleMethod(prefix: string, func: any, asJson: boolean) {
        return function (...args: any[]) {
            try {
                let payload = args[0];
                if (payload === undefined) payload = "undefined";
                else if (payload === null) payload = "null";
                else if (typeof payload === "function") payload = payload.toString();
                else if (typeof payload !== "string") {
                    try {
                        payload = JSON.stringify(payload);
                    } catch (e) {
                        payload = payload.toString();
                    }
                }

                if (typeof payload === "string" && id !== "main")
                    payload = `[${id}] ${payload}`;

                if (asJson) {
                    func(JSON.stringify({
                        method: prefix,
                        payload: payload,
                        arguments: args
                    }));
                } else {
                    func([prefix + payload, ...args.slice(1)]);
                }
            } catch (err) {
                originalConsole.error(`proxyConsole failed: ${err}`);
            }
        };
    }

    const methods = ["debug", "trace", "warn", "info", "error"];
    for (const m of methods) {
        if (typeof (anyConsole[m]) !== "function") {
            anyConsole[m] = proxyConsoleMethod(`console.${m}: `, console.log, false);
        }
    }

    const consoleUrl = `${origin}/console`.replace("https://", "wss://").replace("http://", "ws://");

    consoleWebSocket = new WebSocket(consoleUrl);
    consoleWebSocket.addEventListener("open", () => {
        originalConsole.log(`browser: [${id}] Console websocket connected.`);
    });
    consoleWebSocket.addEventListener("error", (event) => {
        originalConsole.error(`[${id}] websocket error: ${event}`, event);
    });
    consoleWebSocket.addEventListener("close", (event) => {
        originalConsole.error(`[${id}] websocket closed: ${event}`, event);
    });

    const send = (msg: string) => {
        if (consoleWebSocket.readyState === WebSocket.OPEN) {
            consoleWebSocket.send(msg);
        }
        else {
            originalConsole.log(msg);
        }
    };

    for (const m of ["log", ...methods])
        anyConsole[m] = proxyConsoleMethod(`console.${m}`, send, true);
}

function mono_assert(condition: unknown, messageFactory: string | (() => string)): asserts condition {
    if (!condition) {
        const message = typeof messageFactory === "string"
            ? messageFactory
            : messageFactory();
        throw new Error(`Assert failed: ${message}`);
    }
}


async function createDotnetRuntime(moduleFactory: DotnetModuleConfig | ((api: RuntimeAPI) => DotnetModuleConfig)): Promise<RuntimeAPI> {
    const moduleNames = ["./dotnet-runtime.js", "./dotnet-core.js"];
    const modulePromises = moduleNames.map(name => import(name));

    const module = {} as any;
    const helpers: RuntimeHelpers = {} as any;
    const api: RuntimeAPI = {} as any;
    const exports: EarlyExports = {
        mono: {},
        binding: {},
        internal: {},
        module,
        helpers,
        api,
        mono_exit
    };
    setImports(module, helpers);
    await init_polyfills_async();
    if (typeof moduleFactory === "function") {
        const extension = moduleFactory({ Module: module, ...module });
        Object.assign(module, extension);
    }
    else if (typeof moduleFactory === "object") {
        Object.assign(module, moduleFactory);
    }
    else {
        throw new Error("MONO_WASM: Can't use moduleFactory callback of createDotnetRuntime function.");
    }

    const runtimeModule: RuntimeModuleAPI = await modulePromises[0];
    const { initializeImportsAndExports } = runtimeModule;

    const { default: emscriptenModule } = await modulePromises[1];
    module["initializeImportsAndExports"] = (imports: EarlyImports, replacements: EarlyReplacements) => {
        init_replacements(replacements);
        initializeImportsAndExports(imports, exports);
        exit = mono_exit = exports.mono_exit;
    };
    await emscriptenModule(module);
    return api;
}

const dotnet: DotnetHostBuilder = new HostBuilder();
let exit = mono_exit;
type CreateDotnetRuntimeType = typeof createDotnetRuntime;


export {
    dotnet, exit,
    EmscriptenModule,
    RuntimeAPI, DotnetModuleConfig, CreateDotnetRuntimeType, MonoConfig, IMemoryView, AssetEntry, ResourceRequest,
};
export default createDotnetRuntime;
declare global {
    function getDotnetRuntime(runtimeId: number): RuntimeAPI | undefined;
}