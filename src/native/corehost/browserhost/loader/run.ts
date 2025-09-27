// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { Module } from "./cross-module";
import { exit } from "./exit";
import { createPromiseController } from "./promise-controller";

let CoreCLRInitialized = false;
const runMainPromiseController = createPromiseController<number>();

export function BrowserHost_InitializeCoreCLR():void {
    if (CoreCLRInitialized) {
        return;
    }
    // int BrowserHost_InitializeCoreCLR(void)
    // WASM-TODO: add more formal ccall wrapper like cwraps in Mono
    const res = Module.ccall("BrowserHost_InitializeCoreCLR", "number") as number;
    if (res != 0) {
        const reason = new Error("Failed to initialize CoreCLR");
        runMainPromiseController.reject(reason);
        exit(res, reason);
    }
    CoreCLRInitialized = true;
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
