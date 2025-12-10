// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotnetHostBuilder } from "../types";
import { findResources, isNodeHosted, isShellHosted } from "./bootstrap";
import { Module, dotnetAssert } from "./cross-module";
import { exit } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";

let CoreCLRInitialized = false;
const runMainPromiseController = createPromiseCompletionSource<number>();

export function BrowserHost_InitializeCoreCLR(): void {
    dotnetAssert.check(!CoreCLRInitialized, "CoreCLR should be initialized just once");
    CoreCLRInitialized = true;

    // int BrowserHost_InitializeCoreCLR(void)
    // WASM-TODO: add more formal ccall wrapper like cwraps in Mono
    const res = Module.ccall("BrowserHost_InitializeCoreCLR", "number") as number;
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
}

export function resolveRunMainPromise(exitCode: number): void {
    runMainPromiseController.resolve(exitCode);
}

export function rejectRunMainPromise(reason: any): void {
    runMainPromiseController.reject(reason);
}

export function getRunMainPromise(): Promise<number> {
    return runMainPromiseController.promise;
}

// Auto-start when in NodeJS environment as a entry script
export async function selfHostNodeJS(dotnet: DotnetHostBuilder): Promise<void> {
    try {
        if (isNodeHosted()) {
            await findResources(dotnet);
            await dotnet.run();
        } else if (isShellHosted()) {
            // because in V8 we can't probe directories to find assemblies
            throw new Error("Shell/V8 hosting is not supported");
        }
    } catch (err: any) {
        exit(1, err);
    }
}
