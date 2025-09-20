// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { DotnetHostBuilder, LoaderConfig, RuntimeAPI, LoadBootResourceCallback, DotnetModuleConfig } from "./types";

import { Module, runtimeApi } from "./cross-module";
import { downloadConfig, getConfig, mergeConfig } from "./config";
import { createRuntime } from "./bootstrap";
import { exit } from "./exit";

let configUrl: string | undefined = undefined;
let applicationArguments: string[] | undefined = [];
let loadBootResourceCallback: LoadBootResourceCallback | undefined = undefined;

/* eslint-disable @typescript-eslint/no-unused-vars */
export class HostBuilder implements DotnetHostBuilder {
    private runtimeApi: RuntimeAPI | undefined;
    withConfig(config: LoaderConfig): DotnetHostBuilder {
        mergeConfig(config);
        return this;
    }
    withConfigSrc(configSrc: string): DotnetHostBuilder {
        configUrl = configSrc;
        return this;
    }
    withApplicationArguments(...args: string[]): DotnetHostBuilder {
        applicationArguments = args;
        return this;
    }
    withEnvironmentVariable(name: string, value: string): DotnetHostBuilder {
        mergeConfig({
            environmentVariables: {
                [name]: value
            }
        });
        return this;
    }
    withEnvironmentVariables(variables: { [i: string]: string; }): DotnetHostBuilder {
        mergeConfig({
            environmentVariables: variables
        });
        return this;
    }
    withVirtualWorkingDirectory(vfsPath: string): DotnetHostBuilder {
        mergeConfig({
            virtualWorkingDirectory: vfsPath
        });
        return this;
    }
    withDiagnosticTracing(enabled: boolean): DotnetHostBuilder {
        mergeConfig({
            diagnosticTracing: enabled
        });
        return this;
    }
    withDebugging(level: number): DotnetHostBuilder {
        mergeConfig({
            debugLevel: level
        });
        return this;
    }
    withMainAssembly(mainAssemblyName: string): DotnetHostBuilder {
        mergeConfig({
            mainAssemblyName: mainAssemblyName
        });
        return this;
    }
    withApplicationArgumentsFromQuery(): DotnetHostBuilder {
        if (!globalThis.window) {
            throw new Error("Missing window to the query parameters from");
        }

        if (typeof globalThis.URLSearchParams == "undefined") {
            throw new Error("URLSearchParams is supported");
        }

        const params = new URLSearchParams(globalThis.window.location.search);
        const values = params.getAll("arg");
        return this.withApplicationArguments(...values);
    }
    withApplicationEnvironment(applicationEnvironment?: string): DotnetHostBuilder {
        mergeConfig({
            applicationEnvironment: applicationEnvironment
        });
        return this;
    }
    withApplicationCulture(applicationCulture?: string): DotnetHostBuilder {
        mergeConfig({
            applicationCulture: applicationCulture
        });
        return this;
    }
    withResourceLoader(loadBootResource?: LoadBootResourceCallback): DotnetHostBuilder {
        loadBootResourceCallback = loadBootResource;
        return this;
    }

    // internal
    withModuleConfig(moduleConfig: DotnetModuleConfig): DotnetHostBuilder {
        Object.assign(Module, moduleConfig);
        return this;
    }

    // internal
    withConsoleForwarding(): DotnetHostBuilder {
        // TODO
        return this;
    }

    // internal
    withExitOnUnhandledError(): DotnetHostBuilder {
        // TODO
        return this;
    }

    // internal
    withAsyncFlushOnExit(): DotnetHostBuilder {
        // TODO
        return this;
    }

    // internal
    withExitCodeLogging(): DotnetHostBuilder {
        // TODO
        return this;
    }

    // internal
    withElementOnExit(): DotnetHostBuilder {
        // TODO
        return this;
    }

    // internal
    withInteropCleanupOnExit(): DotnetHostBuilder {
        // TODO
        return this;
    }

    async download(): Promise<void> {
        try {
            await downloadConfig(configUrl, loadBootResourceCallback);
            return createRuntime(true, loadBootResourceCallback);
        } catch (err) {
            exit(1, err);
            throw err;
        }
    }

    async create(): Promise<RuntimeAPI> {
        try {
            await downloadConfig(configUrl, loadBootResourceCallback);
            await createRuntime(false, loadBootResourceCallback);
            this.runtimeApi = runtimeApi;
            return this.runtimeApi;
        } catch (err) {
            exit(1, err);
            throw err;
        }
    }

    async run(): Promise<number> {
        try {
            if (!this.runtimeApi) {
                await this.create();
            }
            const config = getConfig();
            return this.runtimeApi!.runMainAndExit(config.mainAssemblyName, applicationArguments);
        } catch (err) {
            exit(1, err);
            throw err;
        }
    }
}
