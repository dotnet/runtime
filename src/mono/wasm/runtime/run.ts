import { ExitStatus, INTERNAL, Module, quit, runtimeHelpers } from "./imports";
import { mono_call_assembly_entry_point } from "./method-calls";
import { mono_wasm_wait_for_debugger } from "./debug";
import { mono_wasm_set_main_args, runtime_is_initialized_reject } from "./startup";

export async function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<void> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        set_exit_code(result);
    } catch (error) {
        if (error instanceof ExitStatus) {
            return;
        }
        set_exit_code(1, error);
    }
}

 
export async function mono_run_main(main_assembly_name: string, args: string[]): Promise<number> {
    mono_wasm_set_main_args(main_assembly_name, args);
    if (runtimeHelpers.wait_for_debugger == -1) {
        console.log("waiting for debugger...");
        return await mono_wasm_wait_for_debugger().then(() => mono_call_assembly_entry_point(main_assembly_name, [args], "m"));
    }
    return mono_call_assembly_entry_point(main_assembly_name, [args], "m");
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_on_abort(error: any): void {
    runtime_is_initialized_reject(error);
    set_exit_code(1, error);
}

function set_exit_code(exit_code: number, reason?: any) {
    if (reason && !(reason instanceof ExitStatus)) {
        if (reason instanceof Error)
            Module.printErr(INTERNAL.mono_wasm_stringify_as_error_with_stack(reason));
        else if (typeof reason == "string")
            Module.printErr(reason);
        else
            Module.printErr(JSON.stringify(reason));
    }
    else {
        reason = new ExitStatus(exit_code);
    }
    quit(exit_code, reason);
}
