// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import WasmEnableThreads from "consts:wasmEnableThreads";

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WORKER, Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { mono_wasm_wait_for_debugger } from "./debug";
import { mono_wasm_set_main_args } from "./startup";
import cwraps from "./cwraps";
import { mono_log_error, mono_log_info, mono_wasm_stringify_as_error_with_stack } from "./logging";
import { postCancelThreads, terminateAllThreads } from "./pthreads";
import { stringToUTF8Ptr, utf16ToString } from "./strings";
import { VoidPtr } from "./types/emscripten";
import { PromiseAndController } from "./types/internal";

/**
 * Possible signatures are described here  https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit (main_assembly_name?: string, args?: string[]): Promise<number> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        loaderHelpers.mono_exit(result);
        return result;
    } catch (error: any) {
        try {
            loaderHelpers.mono_exit(1, error);
        } catch (e) {
            // ignore
        }
        if (error && typeof error.status === "number") {
            return error.status;
        }
        return 1;
    }
}

/**
 * Possible signatures are described here  https://learn.microsoft.com/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main (main_assembly_name?: string, args?: string[]): Promise<number> {
    if (main_assembly_name === undefined || main_assembly_name === null || main_assembly_name === "") {
        main_assembly_name = loaderHelpers.config.mainAssemblyName;
        mono_assert(main_assembly_name, "Null or empty config.mainAssemblyName");
    }
    if (args === undefined || args === null) {
        args = runtimeHelpers.config.applicationArguments;
    }
    if (args === undefined || args === null) {
        if (ENVIRONMENT_IS_NODE) {
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

    mainPromiseCont = loaderHelpers.createPromiseController<number>();
    try {
        const mainAssemblyNamePtr = stringToUTF8Ptr(main_assembly_name) as any;

        args ??= [];

        const sp = Module.stackSave();
        const argsvPtr: number = Module.stackAlloc((args.length + 1) * 4) as any;
        const ptrs: VoidPtr[] = [];
        try {

            Module.HEAPU32[(argsvPtr >>> 2)] = mainAssemblyNamePtr;
            for (let i = 0; i < args.length; i++) {
                const ptr = stringToUTF8Ptr(args[i]) as any;
                ptrs.push(ptr);
                Module.HEAPU32[(argsvPtr >>> 2) + i + 1] = ptr;
            }
            const EXITSTATUS = cwraps.BrowserHost_ExecuteAssembly(mainAssemblyNamePtr, args.length + 1, argsvPtr);
            for (const ptr of ptrs) {
                Module._free(ptr);
            }

            if (EXITSTATUS !== 0x0BADF00D) {
                const reason = new Error("Failed to execute assembly");
                loaderHelpers.mono_exit(-1, reason);
                throw reason;
            }
            return mainPromiseCont.promise;
        } finally {
            Module.stackRestore(sp);
        }
    } catch (error: any) {
        // do not propagate ExitStatus exception
        if (!error || typeof error.status !== "number") {
            loaderHelpers.mono_exit(1, error);
            throw error;
        }
        return error.status;
    }
}

let mainPromiseCont: PromiseAndController<number> = undefined as any;

export function SystemJS_ResolveMainPromise (exitCode: number): void {
    mainPromiseCont.promise_control.resolve(exitCode);
}

export function SystemJS_RejectMainPromise (messagePtr: number, messageLength: number, stackTracePtr: number, stackTraceLength: number): void {
    const message = utf16ToString(messagePtr, messagePtr + messageLength * 2);
    const stackTrace = utf16ToString(stackTracePtr, stackTracePtr + stackTraceLength * 2);
    const error = new Error(message);
    error.stack = stackTrace;
    mainPromiseCont.promise_control.reject(error);
}

export function nativeExit (code: number) {
    if (runtimeHelpers.runtimeReady) {
        runtimeHelpers.runtimeReady = false;
        if (WasmEnableThreads) {
            postCancelThreads();
        }
        cwraps.mono_wasm_exit(code);
    }
}

export function nativeAbort (reason: any) {
    loaderHelpers.exitReason = reason;
    if (runtimeHelpers.runtimeReady) {
        runtimeHelpers.runtimeReady = false;
        if (WasmEnableThreads) {
            if (!ENVIRONMENT_IS_WORKER) {
                terminateAllThreads();
            } else {
                // just in case if the UI thread is blocked, we need to force exit
                // if UI thread receives message from Module.abort below, this thread will be terminated earlier
                setTimeout(() => {
                    mono_log_error("forcing abort 3000ms after nativeAbort attempt", reason);
                    // _emscripten_force_exit is proxied to UI thread and should also arrive in spin wait loop
                    Module._emscripten_force_exit(1);
                }, 3000);
            }
        }

        const reasonString = mono_wasm_stringify_as_error_with_stack(reason);
        Module.abort(reasonString);
    }
    throw reason;
}
