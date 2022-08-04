import { INTERNAL, Module, runtimeHelpers } from "./imports";
import { mono_wasm_wait_for_debugger } from "./debug";
import { abort_startup, mono_wasm_set_main_args } from "./startup";
import cwraps from "./cwraps";
import { assembly_load } from "./class-loader";
import { mono_assert } from "./types";

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<void> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        set_exit_code(result);
    } catch (error) {
        if (error instanceof runtimeHelpers.ExitStatus) {
            return;// FIXME: should this be re-throw ?
        }
        set_exit_code(1, error);
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

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_on_abort(error: any): void {
    abort_startup(error, false);
    set_exit_code(1, error);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function set_exit_code(exit_code: number, reason?: any): void {
    if (reason && !(reason instanceof runtimeHelpers.ExitStatus)) {
        if (reason instanceof Error)
            Module.printErr(INTERNAL.mono_wasm_stringify_as_error_with_stack(reason));
        else if (typeof reason == "string")
            Module.printErr(reason);
        else
            Module.printErr(JSON.stringify(reason));
    }
    else {
        reason = new runtimeHelpers.ExitStatus(exit_code);
    }
    runtimeHelpers.quit(exit_code, reason);
}
