// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotnetHostBuilder } from "../types";
import { findResources, isNodeHosted, isShellHosted } from "./bootstrap";
import { dotnetAssert, dotnetBrowserHostExports } from "./cross-module";
import { exit, runtimeState } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";

const runMainPromiseController = createPromiseCompletionSource<number>();

export function initializeCoreCLR(): void {
    dotnetAssert.check(!runtimeState.runtimeReady, "CoreCLR should be initialized just once");
    const res = dotnetBrowserHostExports.initializeCoreCLR();
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
    runtimeState.runtimeReady = true;
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
