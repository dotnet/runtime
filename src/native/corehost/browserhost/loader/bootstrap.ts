// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { type LoaderConfig, type DotnetHostBuilder, GlobalizationMode } from "./types";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL } from "./per-module";
import { nodeFs } from "./polyfills";

const scriptUrlQuery = /*! webpackIgnore: true */import.meta.url;
const queryIndex = scriptUrlQuery.indexOf("?");
const modulesUniqueQuery = queryIndex > 0 ? scriptUrlQuery.substring(queryIndex) : "";
const scriptUrl = normalizeFileUrl(scriptUrlQuery);
const scriptDirectory = normalizeDirectoryUrl(scriptUrl);

export function locateFile(path: string) {
    if ("URL" in globalThis) {
        return new URL(path, scriptDirectory).toString();
    }

    if (isPathAbsolute(path)) return path;
    return scriptDirectory + path + modulesUniqueQuery;
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

export function isShellHosted(): boolean {
    return ENVIRONMENT_IS_SHELL && typeof (globalThis as any).arguments !== "undefined";
}

export function isNodeHosted(): boolean {
    if (!ENVIRONMENT_IS_NODE || globalThis.process.argv.length < 3) {
        return false;
    }
    const argv1 = globalThis.process.argv[1].toLowerCase();
    const argScript = normalizeFileUrl("file:///" + locateFile(argv1));
    const importScript = normalizeFileUrl(locateFile(scriptUrl.toLowerCase()));

    return argScript === importScript;
}

export async function findResources(dotnet: DotnetHostBuilder): Promise<void> {
    if (!ENVIRONMENT_IS_NODE) {
        return;
    }
    const fs = await nodeFs();
    const mountedDir = "/managed";
    const files: string[] = await fs.promises.readdir(".");
    const assemblies = files
        // TODO-WASM: webCIL
        .filter(file => file.endsWith(".dll"))
        .map(filename => {
            // filename without path
            const name = filename.substring(filename.lastIndexOf("/") + 1);
            return { virtualPath: mountedDir + "/" + filename, name };
        });
    const mainAssemblyName = globalThis.process.argv[2];
    const runtimeConfigName = mainAssemblyName.replace(/\.dll$/, ".runtimeconfig.json");
    let runtimeConfig = {};
    if (fs.existsSync(runtimeConfigName)) {
        const json = await fs.promises.readFile(runtimeConfigName, { encoding: "utf8" });
        runtimeConfig = JSON.parse(json);
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

    const config: LoaderConfig = {
        mainAssemblyName,
        runtimeConfig,
        globalizationMode,
        virtualWorkingDirectory: mountedDir,
        environmentVariables,
        resources: {
            jsModuleNative: [{ name: "dotnet.native.js" }],
            jsModuleRuntime: [{ name: "dotnet.runtime.js" }],
            wasmNative: [{ name: "dotnet.native.wasm", }],
            coreAssembly: [{ virtualPath: mountedDir + "/System.Private.CoreLib.dll", name: "System.Private.CoreLib.dll" },],
            assembly: assemblies,
            icu: icus,
        }
    };
    dotnet.withConfig(config);
    dotnet.withApplicationArguments(...globalThis.process.argv.slice(3));
}
