// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers, runtimeHelpers } from "./globals";
import { mono_wasm_wait_for_debugger } from "./debug";
import { mono_wasm_set_main_args } from "./startup";
import cwraps from "./cwraps";
import { assembly_load } from "./class-loader";

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<number> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        loaderHelpers.mono_exit(result);
        return result;
    } catch (error) {
        if (error instanceof runtimeHelpers.ExitStatus) {
            return error.status;
        }
        loaderHelpers.mono_exit(1, error);
        return 1;
    }
}

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main(main_assembly_name: string, args: string[]): Promise<number> {
    mono_wasm_set_main_args(main_assembly_name, args);
    if (runtimeHelpers.waitForDebugger == -1) {
        console.log("MONO_WASM: waiting for debugger...");
        await mono_wasm_wait_for_debugger();
    }
    const method = find_entry_point(main_assembly_name);
    return runtimeHelpers.javaScriptExports.call_entry_point(method, args);
}

export function find_entry_point(assembly: string) {
    mono_assert(runtimeHelpers.mono_wasm_bindings_is_ready, "The runtime must be initialized.");
    const asm = assembly_load(assembly);
    if (!asm)
        throw new Error("Could not find assembly: " + assembly);

    let auto_set_breakpoint = 0;
    if (runtimeHelpers.waitForDebugger == 1)
        auto_set_breakpoint = 1;

    const method = cwraps.mono_wasm_assembly_get_entry_point(asm, auto_set_breakpoint);
    if (!method)
        throw new Error("Could not find entry point for assembly: " + assembly);
    return method;
}

