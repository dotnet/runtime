// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, dotnetAssert } from "./cross-module";
import { exit } from "./exit";
import { createPromiseController } from "./promise-controller";
import { getLoaderConfig } from "./config";

let CoreCLRInitialized = false;
const runMainPromiseController = createPromiseController<number>();

export function BrowserHost_InitializeCoreCLR():void {
    dotnetAssert.check(!CoreCLRInitialized, "CoreCLR should be initialized just once");
    CoreCLRInitialized = true;

    // int BrowserHost_InitializeCoreCLR(const char* tpaArg, const char* appPathArg, const char* searchPathsArg)
    // Get TPA, app_path, and search_paths from config.environmentVariables where they were prepared
    // during initialization in libBrowserHost.footer.js, and pass them as UTF-8 encoded arguments.
    const config = getLoaderConfig();
    const tpa = config.environmentVariables["TRUSTED_PLATFORM_ASSEMBLIES"] || "";
    const appPath = config.environmentVariables["APP_PATHS"] || "";
    const searchPaths = config.environmentVariables["NATIVE_DLL_SEARCH_DIRECTORIES"] || "";

    // Emscripten's ccall with "string" type handles UTF-8 encoding and memory management automatically
    const res = Module.ccall("BrowserHost_InitializeCoreCLR", "number", ["string", "string", "string"], [tpa, appPath, searchPaths]) as number;
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
}

export function resolveRunMainPromise(exitCode:number):void {
    runMainPromiseController.resolve(exitCode);
}

export function rejectRunMainPromise(reason:any):void {
    runMainPromiseController.reject(reason);
}

export function getRunMainPromise():Promise<number> {
    return runMainPromiseController.promise;
}
