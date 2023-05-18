// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, INTERNAL, loaderHelpers, runtimeHelpers } from "./globals";
import { mono_log_debug, consoleWebSocket, mono_log_error, mono_log_info_no_prefix } from "./logging";

export function abort_startup(reason: any, should_exit: boolean): void {
    mono_log_debug("abort_startup");
    loaderHelpers.allDownloadsQueued.promise_control.reject(reason);
    loaderHelpers.afterConfigLoaded.promise_control.reject(reason);
    loaderHelpers.wasmDownloadPromise.promise_control.reject(reason);
    loaderHelpers.runtimeModuleLoaded.promise_control.reject(reason);
    if (runtimeHelpers.dotnetReady) {
        runtimeHelpers.dotnetReady.promise_control.reject(reason);
        runtimeHelpers.memorySnapshotSkippedOrDone.promise_control.reject(reason);
        runtimeHelpers.afterInstantiateWasm.promise_control.reject(reason);
        runtimeHelpers.beforePreInit.promise_control.reject(reason);
        runtimeHelpers.afterPreInit.promise_control.reject(reason);
        runtimeHelpers.afterPreRun.promise_control.reject(reason);
        runtimeHelpers.beforeOnRuntimeInitialized.promise_control.reject(reason);
        runtimeHelpers.afterOnRuntimeInitialized.promise_control.reject(reason);
        runtimeHelpers.afterPostRun.promise_control.reject(reason);
    }
    if (typeof reason !== "object" || reason.silent !== true) {
        if (should_exit || ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
            mono_exit(1, reason);
        }
        throw reason;
    }
}

export function mono_exit(exit_code: number, reason?: any): void {
    if (loaderHelpers.config && loaderHelpers.config.asyncFlushOnExit && exit_code === 0) {
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
        mono_log_error(`flushing std* streams failed: ${err}`);
    }
}

function set_exit_code_and_quit_now(exit_code: number, reason?: any): void {
    if (runtimeHelpers.ExitStatus) {
        if (reason && !(reason instanceof runtimeHelpers.ExitStatus)) {
            if (!loaderHelpers.config.logExitCode) {
                if (reason instanceof Error && runtimeHelpers.stringify_as_error_with_stack)
                    loaderHelpers.err(runtimeHelpers.stringify_as_error_with_stack(reason));
                else if (typeof reason == "string")
                    loaderHelpers.err(reason);
                else
                    loaderHelpers.err(JSON.stringify(reason));
            }
        }
        else if (!reason) {
            reason = new runtimeHelpers.ExitStatus(exit_code);
        } else if (typeof reason.status === "number") {
            exit_code = reason.status;
        }
    }
    logErrorOnExit(exit_code, reason);
    try {
        if (runtimeHelpers.jiterpreter_dump_stats) runtimeHelpers.jiterpreter_dump_stats(false);
    } catch {
        // eslint-disable-next-line @typescript-eslint/no-extra-semi
        ;
    }

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
    if (ENVIRONMENT_IS_WEB && loaderHelpers.config && loaderHelpers.config.appendElementOnExit) {
        //Tell xharness WasmBrowserTestRunner what was the exit code
        const tests_done_elem = document.createElement("label");
        tests_done_elem.id = "tests_done";
        if (exit_code) tests_done_elem.style.background = "red";
        tests_done_elem.innerHTML = exit_code.toString();
        document.body.appendChild(tests_done_elem);
    }
}

function logErrorOnExit(exit_code: number, reason?: any) {
    if (loaderHelpers.config && loaderHelpers.config.logExitCode) {
        if (exit_code != 0 && reason) {
            if (reason instanceof Error && runtimeHelpers.stringify_as_error_with_stack)
                mono_log_error(runtimeHelpers.stringify_as_error_with_stack(reason));
            else if (typeof reason == "string")
                mono_log_error(reason);
            else
                mono_log_error(JSON.stringify(reason));
        }
        if (consoleWebSocket) {
            const stop_when_ws_buffer_empty = () => {
                if (consoleWebSocket.bufferedAmount == 0) {
                    // tell xharness WasmTestMessagesProcessor we are done.
                    // note this sends last few bytes into the same WS
                    mono_log_info_no_prefix("WASM EXIT " + exit_code);
                }
                else {
                    setTimeout(stop_when_ws_buffer_empty, 100);
                }
            };
            stop_when_ws_buffer_empty();
        } else {
            mono_log_info_no_prefix("WASM EXIT " + exit_code);
        }
    }
}
