// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { ENVIRONMENT_IS_NODE, Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { mono_wasm_wait_for_debugger } from "./debug";
import { mono_wasm_set_main_args } from "./startup";
import cwraps from "./cwraps";
import { mono_log_info } from "./logging";
import { cancelThreads } from "./pthreads/browser";
import { call_entry_point } from "./managed-exports";

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit(main_assembly_name?: string, args?: string[]): Promise<number> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        loaderHelpers.mono_exit(result);
        return result;
    } catch (error: any) {
        try {
            loaderHelpers.mono_exit(1, error);
        }
        catch (e) {
            // ignore
        }
        if (error && typeof error.status === "number") {
            return error.status;
        }
        return 1;
    }
}

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main(main_assembly_name?: string, args?: string[]): Promise<number> {
    if (main_assembly_name === undefined || main_assembly_name === null || main_assembly_name === "") {
        main_assembly_name = loaderHelpers.config.mainAssemblyName;
        mono_assert(main_assembly_name, "Null or empty config.mainAssemblyName");
    }
    if (args === undefined || args === null) {
        args = runtimeHelpers.config.applicationArguments;
    }
    if (args === undefined || args === null) {
        if (ENVIRONMENT_IS_NODE) {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore:
            const process = await import(/*! webpackIgnore: true */"process");
            args = process.argv.slice(2) as string[];
        } else {
            args = [];
        }
    }

    mono_wasm_set_main_args(main_assembly_name, args);
    loaderHelpers.config.mainAssemblyName = main_assembly_name;

    if (runtimeHelpers.waitForDebugger == -1) {
        mono_log_info("waiting for debugger...");
        await mono_wasm_wait_for_debugger();
    }

    try {
        Module.runtimeKeepalivePush();

        // one more timer loop before we return, so that any remaining queued calls could run
        await new Promise(resolve => globalThis.setTimeout(resolve, 0));

        return await call_entry_point(main_assembly_name, args, runtimeHelpers.waitForDebugger == 1);
    } finally {
        Module.runtimeKeepalivePop();// after await promise !
    }
}



export function nativeExit(code: number) {
    if (WasmEnableThreads) {
        cancelThreads();
    }
    cwraps.mono_wasm_exit(code);
}

export function nativeAbort(reason: any) {
    loaderHelpers.exitReason = reason;
    if (!loaderHelpers.is_exited()) {
        cwraps.mono_wasm_abort();
    }
    throw reason;
}