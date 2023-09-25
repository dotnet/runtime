// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_NODE, loaderHelpers, runtimeHelpers, disposeRuntimeGlobals, Module, wasmTable } from "./globals";
import { mono_wasm_wait_for_debugger } from "./debug";
import { mono_wasm_set_main_args } from "./startup";
import cwraps, { disposeCwraps } from "./cwraps";
import { assembly_load } from "./class-loader";
import { mono_log_info } from "./logging";
import { assert_bindings } from "./invoke-js";
import { create_weak_ref } from "./weak-ref";
import { RuntimeAPI } from "./types/export-types";
import { forceDisposeProxies } from "./gc-handles";

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit(main_assembly_name: string, args?: string[]): Promise<number> {
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
export async function mono_run_main(main_assembly_name: string, args?: string[]): Promise<number> {
    if (args === undefined || args === null) {
        args = runtimeHelpers.config.applicationArguments;
    }
    if (args === undefined || args === null) {
        if (ENVIRONMENT_IS_NODE) {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore:
            const process = await import(/* webpackIgnore: true */"process");
            args = process.argv.slice(2) as string[];
        } else {
            args = [];
        }
    }

    mono_wasm_set_main_args(main_assembly_name, args);
    if (runtimeHelpers.waitForDebugger == -1) {
        mono_log_info("waiting for debugger...");
        await mono_wasm_wait_for_debugger();
    }
    const method = find_entry_point(main_assembly_name);
    return runtimeHelpers.javaScriptExports.call_entry_point(method, args);
}

export function find_entry_point(assembly: string) {
    loaderHelpers.assert_runtime_running();
    assert_bindings();
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

export function mono_dispose_runtime(graceful?: boolean): void {
    try {
        forceDisposeProxies(true, false, !graceful);
        runtimeHelpers.abort("runtime is being disposed");
    } catch (e) {
        // ignore
    }
    try {
        const __list = (globalThis as any).getDotnetRuntime.__list;
        __list.list[runtimeId] = undefined as any;
        const cnt = Object.keys(__list.list).reduce((a: number, k: any) => __list.list[k] ? 1 : 0, 0);
        if (cnt === 0) {
            delete (globalThis as any).getDotnetRuntime;
        }
        for (let i = 0; i < wasmTable.length; i++) {
            wasmTable.set(i, null);
        }
        for (const key in loaderHelpers.config) {
            (loaderHelpers.config as any)[key] = undefined as any;
        }
        runtimeHelpers.disposeWasm();
        for (const key in Module) {
            (Module as any)[key] = undefined as any;
        }
        loaderHelpers.disposeRuntimeGlobals();
        disposeRuntimeGlobals();
        disposeCwraps();
    } catch (e) {
        if (graceful) throw e;
    }
}

let runtimeId = -1;

export class RuntimeList {
    private list: { [runtimeId: number]: WeakRef<RuntimeAPI> } = {};

    public registerRuntime(api: RuntimeAPI): number {
        runtimeId = api.runtimeId = Object.keys(this.list).length;
        this.list[api.runtimeId] = create_weak_ref(api);
        return api.runtimeId;
    }

    public getRuntime(runtimeId: number): RuntimeAPI | undefined {
        const wr = this.list[runtimeId];
        return wr ? wr.deref() : undefined;
    }
}
