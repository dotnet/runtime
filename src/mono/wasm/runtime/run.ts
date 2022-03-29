import { Module, quit } from "./imports";
import { mono_call_assembly_entry_point } from "./method-calls";
import { mono_wasm_set_main_args, runtime_is_initialized_reject } from "./startup";


export async function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<void> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        set_exit_code(result);
    } catch (error) {
        set_exit_code(1, error);
    }
}

export async function mono_run_main(main_assembly_name: string, args: string[]): Promise<number> {
    mono_wasm_set_main_args(main_assembly_name, args);
    return mono_call_assembly_entry_point(main_assembly_name, [args], "m");
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_on_abort(error: any): void {
    runtime_is_initialized_reject(error);
    set_exit_code(1, error);
}

function set_exit_code(exit_code: number, reason?: any) {
    if (reason) {
        Module.printErr(reason.toString());
        if (reason.stack) {
            Module.printErr(reason.stack);
        }
    }
    quit(exit_code, reason);
}
