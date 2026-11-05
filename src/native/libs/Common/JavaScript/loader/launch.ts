// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, globalThisAny } from "./per-module";
import { mergeLoaderConfig } from "./config";
import { exit } from "./exit";

type RuntimeProperty = string | number | boolean;

interface LaunchArguments {
    applicationArguments?: string[];
    environmentVariables: Record<string, string>;
    runtimeProperties: Record<string, RuntimeProperty>;
    help: boolean;
}

let launchArguments: Promise<LaunchArguments> | undefined;

export function parseLaunchArguments(args: string[]): LaunchArguments {
    const result: LaunchArguments = {
        applicationArguments: args.length > 0 ? [] : undefined,
        environmentVariables: Object.create(null),
        runtimeProperties: Object.create(null),
        help: false,
    };
    for (let i = 0; i < args.length; i++) {
        const arg = args[i];
        if (arg === "--") {
            result.applicationArguments = args.slice(i + 1);
            break;
        }
        if (!arg.startsWith("--dotnet-")) {
            result.applicationArguments = args.slice(i);
            break;
        }
        if (arg === "--dotnet-help") {
            result.help = true;
            continue;
        }
        const environment = arg.startsWith("--dotnet-env=");
        const property = arg.startsWith("--dotnet-runtime-property=");
        if (!environment && !property) {
            throw invalidOption(arg);
        }
        const assignment = arg.substring(environment ? "--dotnet-env=".length : "--dotnet-runtime-property=".length);
        const separator = assignment.indexOf("=");
        if (separator <= 0 || assignment.includes("\0")) {
            throw invalidOption(arg);
        }
        const name = assignment.substring(0, separator);
        const text = assignment.substring(separator + 1);
        if (environment) {
            result.environmentVariables[name] = text;
        } else {
            let value: unknown;
            try {
                value = JSON.parse(text);
            } catch (error) {
                if (!(error instanceof SyntaxError)) throw error;
                throw invalidOption(arg);
            }
            if (!isRuntimeProperty(value) || isHostProperty(name)) {
                throw invalidOption(arg);
            }
            result.runtimeProperties[name] = value;
        }
    }
    return result;
}

export function getLaunchArguments(): Promise<LaunchArguments> {
    return launchArguments ??= readLaunchArguments();
}

export async function applyLaunchArguments(): Promise<void> {
    const options = await getLaunchArguments();
    if (options.help) {
        // eslint-disable-next-line no-console
        console.log("Usage: [--dotnet-env=NAME=VALUE] [--dotnet-runtime-property=NAME=JSON_SCALAR] [--dotnet-help] [--] [application arguments]");
        exit(0, { status: 0, silent: true, message: "Help requested" });
    }
    const config: LoaderConfig = {
        environmentVariables: options.environmentVariables,
        runtimeConfig: { runtimeOptions: { configProperties: options.runtimeProperties } },
    };
    mergeLoaderConfig(config);
}

async function readLaunchArguments(): Promise<LaunchArguments> {
    if (ENVIRONMENT_IS_NODE) {
        const result = parseLaunchArguments(process.argv.slice(2));
        const environment: Record<string, string> = Object.create(null);
        for (const [name, value] of Object.entries(process.env)) {
            if (value !== undefined) environment[name] = value;
        }
        result.environmentVariables = { ...environment, ...result.environmentVariables };
        return result;
    }
    if (ENVIRONMENT_IS_SHELL) {
        const args: unknown = globalThisAny.scriptArgs ?? globalThisAny.arguments;
        return parseLaunchArguments(isStringArray(args) ? args : []);
    }
    if (ENVIRONMENT_IS_WEB && typeof window === "object") {
        const params = new URLSearchParams(window.location.search);
        const launchPath = params.get("dotnet-launch");
        if (launchPath !== null) {
            const url = new URL(launchPath, window.location.href);
            if (url.origin !== window.location.origin) {
                throw new Error("The .NET launch configuration must use the application's origin.");
            }
            const response = await fetch(url, { cache: "no-store" });
            if (!response.ok) {
                throw new Error(`Failed to load .NET launch configuration: HTTP ${response.status}`);
            }
            const payload: unknown = await response.json();
            if (!isRecord(payload) || !isStringArray(payload.applicationArguments)
                || !isRecord(payload.environmentVariables) || !isRecord(payload.runtimeProperties)) {
                throw new Error("Invalid .NET launch configuration.");
            }
            const result = parseLaunchArguments(["--", ...(params.has("arg") ? params.getAll("arg") : payload.applicationArguments)]);
            for (const [name, value] of Object.entries(payload.environmentVariables)) {
                if (!name || name.includes("=") || name.includes("\0") || typeof value !== "string" || value.includes("\0")) {
                    throw new Error("Invalid environment variable in .NET launch configuration.");
                }
                result.environmentVariables[name] = value;
            }
            for (const [name, value] of Object.entries(payload.runtimeProperties)) {
                if (!name || name.includes("\0") || !isRuntimeProperty(value) || isHostProperty(name)) {
                    throw new Error("Invalid runtime property in .NET launch configuration.");
                }
                result.runtimeProperties[name] = value;
            }
            return result;
        }
        const args = params.getAll("arg");
        return parseLaunchArguments(args.length > 0 ? ["--", ...args] : []);
    }
    return parseLaunchArguments([]);
}

function isStringArray(value: unknown): value is string[] {
    return Array.isArray(value) && value.every(item => typeof item === "string");
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isRuntimeProperty(value: unknown): value is RuntimeProperty {
    return typeof value === "boolean"
        || (typeof value === "number" && Number.isFinite(value))
        || (typeof value === "string" && !value.includes("\0"));
}

function isHostProperty(name: string): boolean {
    return [
        "HOST_RUNTIME_CONTRACT", "TRUSTED_PLATFORM_ASSEMBLIES", "ENTRY_ASSEMBLY_NAME",
        "NATIVE_DLL_SEARCH_DIRECTORIES", "APP_PATHS", "APP_CONTEXT_BASE_DIRECTORY", "RUNTIME_IDENTIFIER",
    ].includes(name);
}

function invalidOption(option: string): Error {
    return new Error(`Invalid .NET launch option '${option}'. Use --dotnet-help for usage.`);
}
