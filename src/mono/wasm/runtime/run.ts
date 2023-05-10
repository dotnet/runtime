// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB, INTERNAL, Module, runtimeHelpers } from "./globals";
import { mono_wasm_wait_for_debugger } from "./debug";
import { abort_startup, mono_wasm_set_main_args } from "./startup";
import cwraps from "./cwraps";
import { assembly_load } from "./class-loader";
import { mono_assert } from "./types";
import { consoleWebSocket, mono_wasm_stringify_as_error_with_stack } from "./logging";
import { jiterpreter_dump_stats } from "./jiterpreter";

/**
 * Possible signatures are described here  https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
 */
export async function mono_run_main_and_exit(main_assembly_name: string, args: string[]): Promise<number> {
    try {
        const result = await mono_run_main(main_assembly_name, args);
        mono_exit(result);
        return result;
    } catch (error) {
        if (error instanceof runtimeHelpers.ExitStatus) {
            return error.status;
        }
        mono_exit(1, error);
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

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_on_abort(error: any): void {
    abort_startup(error, false);
    mono_exit(1, error);
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
export function mono_exit(exit_code: number, reason?: any): void {
    if (runtimeHelpers.config && runtimeHelpers.config.asyncFlushOnExit && exit_code === 0) {
        // this would NOT call Node's exit() immediately, it's a hanging promise
        (async () => {
            try {
                await flush_node_streams();
            }
            finally {
                set_exit_code_and_quit_now(exit_code, reason);
            }
        })();
        // we need to throw, rather than let the caller continue the normal execution
        // in the middle of some code, which expects this to stop the process
        throw runtimeHelpers.ExitStatus
            ? new runtimeHelpers.ExitStatus(exit_code)
            : reason
                ? reason
                : new Error("Stop with exit code " + exit_code);
    } else {
        set_exit_code_and_quit_now(exit_code, reason);
    }
}

async function flush_node_streams() {
    try {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        const process = await import(/* webpackIgnore: true */"process");
        const flushStream = (stream: any) => {
            return new Promise<void>((resolve, reject) => {
                stream.on("error", (error: any) => reject(error));
                stream.write("", function () { resolve(); });
            });
        };
        const stderrFlushed = flushStream(process.stderr);
        const stdoutFlushed = flushStream(process.stdout);
        await Promise.all([stdoutFlushed, stderrFlushed]);
    } catch (err) {
        console.error(`flushing std* streams failed: ${err}`);
    }
}

// eslint-disable-next-line @typescript-eslint/explicit-module-boundary-types
function set_exit_code_and_quit_now(exit_code: number, reason?: any): void {
    if (runtimeHelpers.ExitStatus) {
        if (reason && !(reason instanceof runtimeHelpers.ExitStatus)) {
            if (!runtimeHelpers.config.logExitCode) {
                if (reason instanceof Error)
                    Module.err(mono_wasm_stringify_as_error_with_stack(reason));
                else if (typeof reason == "string")
                    Module.err(reason);
                else
                    Module.err(JSON.stringify(reason));
            }
        }
        else if (!reason) {
            reason = new runtimeHelpers.ExitStatus(exit_code);
        } else if (typeof reason.status === "number") {
            exit_code = reason.status;
        }
    }
    logErrorOnExit(exit_code, reason);
    appendElementOnExit(exit_code);
    if (exit_code !== 0 || !ENVIRONMENT_IS_WEB) {
        if (ENVIRONMENT_IS_NODE && INTERNAL.process) {
            INTERNAL.process.exit(exit_code);
            throw reason;
        }
        else if (runtimeHelpers.quit) {
            runtimeHelpers.quit(exit_code, reason);
        } else {
            throw reason;
        }
    }
}

function appendElementOnExit(exit_code: number) {
    if (ENVIRONMENT_IS_WEB && runtimeHelpers.config && runtimeHelpers.config.appendElementOnExit) {
        //Tell xharness WasmBrowserTestRunner what was the exit code
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        if (exit_code) tests_done_elem.style.background = "red";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);
    }
}

function logErrorOnExit(exit_code: number, reason?: any) {
    if (runtimeHelpers.config && runtimeHelpers.config.logExitCode) {
        if (exit_code != 0 && reason) {
            if (reason instanceof Error)
                console.error(mono_wasm_stringify_as_error_with_stack(reason));
            else if (typeof reason == "string")
                console.error(reason);
            else
                console.error(JSON.stringify(reason));
        }
        if (consoleWebSocket) {
            const stop_when_ws_buffer_empty = () => {
                if (consoleWebSocket.bufferedAmount == 0) {
                    // tell xharness WasmTestMessagesProcessor we are done.
                    // note this sends last few bytes into the same WS
                    console.log("WASM EXIT " + exit_code);
                }
                else {
                    setTimeout(stop_when_ws_buffer_empty, 100);
                }
            };
            stop_when_ws_buffer_empty();
        } else {
            console.log("WASM EXIT " + exit_code);
        }
    }

    try {
        jiterpreter_dump_stats(false);
    } catch {
        // eslint-disable-next-line @typescript-eslint/no-extra-semi
        ;
    }
}
