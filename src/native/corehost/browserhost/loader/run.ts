// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module, dotnetAssert } from "./cross-module";
import { exit } from "./exit";
import { createPromiseCompletionSource } from "./promise-completion-source";

let CoreCLRInitialized = false;
const runMainPromiseCompletionSource = createPromiseCompletionSource<number>();

export function BrowserHost_InitializeCoreCLR():void {
    dotnetAssert.check(!CoreCLRInitialized, "CoreCLR should be initialized just once");
    CoreCLRInitialized = true;

    // int BrowserHost_InitializeCoreCLR(void)
    // WASM-TODO: add more formal ccall wrapper like cwraps in Mono
    const res = Module.ccall("BrowserHost_InitializeCoreCLR", "number") as number;
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseCompletionSource.reject(reason);
        exit(res, reason);
    }
}

export function resolveRunMainPromise(exitCode:number):void {
    runMainPromiseCompletionSource.resolve(exitCode);
}

export function rejectRunMainPromise(reason:any):void {
    runMainPromiseCompletionSource.reject(reason);
}

export function getRunMainPromise():Promise<number> {
    return runMainPromiseCompletionSource.promise;
}
