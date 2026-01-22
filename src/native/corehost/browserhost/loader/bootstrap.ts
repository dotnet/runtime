// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfig, DotnetHostBuilder } from "./types";

import { exceptions, simd } from "wasm-feature-detect";

import { GlobalizationMode } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL } from "./per-module";
import { fetchLike, nodeFs } from "./polyfills";
import { dotnetAssert } from "./cross-module";
import { quitNow } from "./exit";
import { loaderConfig } from "./config";

const scriptUrlQuery = /*! webpackIgnore: true */import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);

export async function validateWasmFeatures(): Promise<void> {
    dotnetAssert.check(await exceptions, "This browser/engine doesn't support WASM exception handling. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
    dotnetAssert.check(await simd, "This browser/engine doesn't support WASM SIMD. Please use a modern version. See also https://aka.ms/dotnet-wasm-features");
}

export function locateFile(path: string, isModule = false): string {
    let res;
    if (isPathAbsolute(path)) {
        res = path;
    } else if (globalThis.URL) {
        res = new globalThis.URL(path, scriptDirectory).href;
    } else {
        res = scriptDirectory + path;
    }

    if (isModule) {
        res += modulesUniqueQuery;
    }

    return res;
}

function normalizeFileUrl(filename: string) {
    // unix vs windows
    // remove query string
    return filename.replace(/\\/g, "/").replace(/[?#].*/, "");
}

function normalizeDirectoryUrl(dir: string) {
    return dir.slice(0, dir.lastIndexOf("/")) + "/";
}

const protocolRx = /^[a-zA-Z][a-zA-Z\d+\-.]*?:\/\//;
const windowsAbsoluteRx = /[a-zA-Z]:[\\/]/;
function isPathAbsolute(path: string): boolean {
    if (ENVIRONMENT_IS_NODE || ENVIRONMENT_IS_SHELL) {
        // unix /x.json
        // windows \x.json
        // windows C:\x.json
        // windows C:/x.json
        return path.startsWith("/") || path.startsWith("\\") || path.indexOf("///") !== -1 || windowsAbsoluteRx.test(path);
    }

    // anything with protocol is always absolute
    // windows file:///C:/x.json
    // windows http://C:/x.json
    return protocolRx.test(path);
}

export function makeURLAbsoluteWithApplicationBase(url: string) {
    dotnetAssert.check(typeof url === "string", "url must be a string");
    if (!isPathAbsolute(url) && url.indexOf("./") !== 0 && url.indexOf("../") !== 0 && globalThis.URL && globalThis.document && globalThis.document.baseURI) {
        const absoluteUrl = new URL(url, globalThis.document.baseURI);
        return absoluteUrl.href;
    }
    return url;
}

function printUsageAndQuit() {
    // eslint-disable-next-line no-console
    console.log("usage: v8 --module dotnet.js -- hello.dll arg1 arg2");
    // eslint-disable-next-line no-console
    console.log("usage: node dotnet.js HelloWorld.dll arg1 arg2");
    quitNow(1);
}

export function isShellHosted(): boolean {
    if (!ENVIRONMENT_IS_SHELL || loaderConfig.resources) {
        return false;
    }
    const argumentsAny = (globalThis as any).arguments as string[];
    if (typeof argumentsAny === "undefined" || argumentsAny.length < 3) {
        printUsageAndQuit();
        return false;
    }
    return true;
}

export function isNodeHosted(): boolean {
    if (!ENVIRONMENT_IS_NODE || loaderConfig.resources) {
        return false;
    }
    if (globalThis.process.argv.length < 3) {
        printUsageAndQuit();
        return false;
    }
    const argv1 = globalThis.process.argv[1].toLowerCase();
    const argScript = normalizeFileUrl("file:///" + locateFile(argv1));
    const importScript = normalizeFileUrl(locateFile(scriptUrl.toLowerCase()));

    return argScript === importScript;
}

export async function shellFindResources(dotnet: DotnetHostBuilder): Promise<void> {
    if (!ENVIRONMENT_IS_SHELL) {
        return;
    }
    const argumentsAny = (globalThis as any).arguments as string[];

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

export async function nodeFindResources(dotnet: DotnetHostBuilder): Promise<void> {
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
    const prefix = "/managed/";
    const assemblies = files
        // TODO-WASM: webCIL
        .filter(file => file.endsWith(".dll"))
        .map(filename => {
            // Get file name.
            const name = filename.substring(filename.lastIndexOf("/") + 1);
            return { virtualPath: prefix + filename, name };
        });
    const coreAssembly = files
        // TODO-WASM: webCIL
        .filter(file => file.endsWith("System.Private.CoreLib.dll"))
        .map(filename => {
            // filename without path
            const name = filename.substring(filename.lastIndexOf("/") + 1);
            return { virtualPath: prefix + filename, name };
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
            // filename without path
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
        virtualWorkingDirectory: "/managed",
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
