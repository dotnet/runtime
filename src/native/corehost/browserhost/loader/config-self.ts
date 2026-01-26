// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig, DotnetHostBuilder } from "./types";
import { GlobalizationMode } from "./types";
import { browserAppBase, ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, globalThisAny } from "./per-module";
import { fetchLike, nodeFs } from "./polyfills";
import { quitNow, exit } from "./exit";
import { isValidLoaderConfig } from "./config";
import { isCurrentScript } from "./bootstrap";

// Auto-start when in NodeJS environment as a entry script
export async function selfConfigureAndRun(dotnet: DotnetHostBuilder): Promise<void> {
    try {
        if (isNodeHosted()) {
            await nodeFindResources(dotnet);
            await dotnet.runMainAndExit();
        } else if (isShellHosted()) {
            await shellFindResources(dotnet);
            await dotnet.runMainAndExit();
        }
    } catch (err: any) {
        exit(1, err);
    }
}

function isShellHosted(): boolean {
    if (!ENVIRONMENT_IS_SHELL || isValidLoaderConfig()) {
        return false;
    }
    const argumentsAny = globalThisAny.arguments as string[];
    if (typeof argumentsAny === "undefined" || argumentsAny.length < 3) {
        printUsageAndQuit();
        return false;
    }
    return true;
}

function isNodeHosted(): boolean {
    if (!ENVIRONMENT_IS_NODE || isValidLoaderConfig()) {
        return false;
    }
    if (globalThis.process.argv.length < 3) {
        printUsageAndQuit();
        return false;
    }
    return isCurrentScript(globalThis.process.argv[1]);
}

async function shellFindResources(dotnet: DotnetHostBuilder): Promise<void> {
    if (!ENVIRONMENT_IS_SHELL) {
        return;
    }
    const argumentsAny = globalThisAny.arguments as string[];

    const filesRes = await fetchLike("dotnet.assets.txt", {}, "text/plain");
    if (!filesRes.ok) {
        // eslint-disable-next-line no-console
        console.log("Shell/V8 can't list files in the current directory. \n"
            + "Please generate an 'dotnet.assets.txt' file with the list of files to load. \n"
            + "Depending on your shell, you can use one of the following commands: \n"
            + "  Get-ChildItem -Name > dotnet.assets.txt \n"
            + "  dir /b > dotnet.assets.txt \n"
            + "  ls > dotnet.assets.txt \n"
        );
        quitNow(1);
    }
    const fileList = await filesRes.text();
    const files: string[] = fileList.split(/\r?\n/).filter(line => line.length > 0);
    const mainAssemblyName = argumentsAny[0];
    dotnet.withApplicationArguments(...argumentsAny.slice(1));
    return findResources(dotnet, files, mainAssemblyName);
}

async function nodeFindResources(dotnet: DotnetHostBuilder): Promise<void> {
    if (!ENVIRONMENT_IS_NODE) {
        return;
    }
    const fs = await nodeFs();
    const files: string[] = await fs.promises.readdir(".");
    const mainAssemblyName = globalThis.process.argv[2];
    dotnet.withApplicationArguments(...globalThis.process.argv.slice(3));
    return findResources(dotnet, files, mainAssemblyName);
}

// Finds resources when running in NodeJS environment without explicit configuration
async function findResources(dotnet: DotnetHostBuilder, files: string[], mainAssemblyName: string): Promise<void> {
    const assemblies = files
        // TODO-WASM: webCIL https://github.com/dotnet/runtime/issues/120248
        .filter(file => file.endsWith(".dll"))
        .map(filepath => {
            // ignore path and just use file name
            const name = filepath.substring(filepath.lastIndexOf("/") + 1);
            return { virtualPath: filepath, name };
        });
    const coreAssembly = files
        // TODO-WASM: webCIL https://github.com/dotnet/runtime/issues/120248
        .filter(file => file.endsWith("System.Private.CoreLib.dll"))
        .map(filepath => {
            // ignore path and just use file name
            const name = filepath.substring(filepath.lastIndexOf("/") + 1);
            return { virtualPath: filepath, name };
        });

    const runtimeConfigName = mainAssemblyName.replace(/\.dll$/, ".runtimeconfig.json");
    let runtimeConfig = {};
    if (files.indexOf(runtimeConfigName) >= 0) {
        const res = await fetchLike(runtimeConfigName, {}, "application/json");
        runtimeConfig = await res.json();
    }
    const icus = files
        .filter(file => file.startsWith("icudt") && file.endsWith(".dat"))
        .map(filename => {
            // ignore path and just use file name
            const name = filename.substring(filename.lastIndexOf("/") + 1);
            return { virtualPath: name, name };
        });

    const environmentVariables: { [key: string]: string } = {};
    let globalizationMode = GlobalizationMode.All;
    if (!icus.length) {
        globalizationMode = GlobalizationMode.Invariant;
        environmentVariables["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
    }

    const loaderConfig: LoaderConfig = {
        mainAssemblyName,
        runtimeConfig,
        globalizationMode,
        virtualWorkingDirectory: browserAppBase,
        environmentVariables,
        resources: {
            jsModuleNative: [{ name: "dotnet.native.js" }],
            jsModuleRuntime: [{ name: "dotnet.runtime.js" }],
            wasmNative: [{ name: "dotnet.native.wasm", }],
            coreAssembly,
            assembly: assemblies,
            icu: icus,
        }
    };
    dotnet.withConfig(loaderConfig);
}

function printUsageAndQuit() {
    // eslint-disable-next-line no-console
    console.log("usage: v8 --module dotnet.js -- hello.dll arg1 arg2");
    // eslint-disable-next-line no-console
    console.log("usage: node dotnet.js HelloWorld.dll arg1 arg2");
    quitNow(1);
}
