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
    const config = getLoaderConfig();
    const assemblyPaths = config.resources.assembly.map(a => a.virtualPath);
    const coreAssemblyPaths = config.resources.coreAssembly.map(a => a.virtualPath);
    const tpa = [...coreAssemblyPaths, ...assemblyPaths].join(":");
    const appPath = config.virtualWorkingDirectory;
    const searchPaths = config.virtualWorkingDirectory;

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
