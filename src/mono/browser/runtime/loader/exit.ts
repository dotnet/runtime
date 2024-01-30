// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB, ENVIRONMENT_IS_WORKER, INTERNAL, emscriptenModule, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { mono_log_debug, mono_log_error, mono_log_info_no_prefix, mono_log_warn, teardown_proxy_console } from "./logging";

export function is_exited() {
    return loaderHelpers.exitCode !== undefined;
}

export function is_runtime_running() {
    return runtimeHelpers.runtimeReady && !is_exited();
}

export function assert_runtime_running() {
    mono_assert(runtimeHelpers.runtimeReady, ".NET runtime didn't start yet. Please call dotnet.create() first.");
    mono_assert(!loaderHelpers.assertAfterExit || !is_exited(), () => `.NET runtime already exited with ${loaderHelpers.exitCode} ${loaderHelpers.exitReason}. You can use runtime.runMain() which doesn't exit the runtime.`);
}

export function register_exit_handlers() {
    if (!emscriptenModule.onAbort) {
        emscriptenModule.onAbort = onAbort;
    }
    if (!emscriptenModule.onExit) {
        emscriptenModule.onExit = onExit;
    }
}

export function unregister_exit_handlers() {
    if (emscriptenModule.onAbort == onAbort) {
        emscriptenModule.onAbort = undefined;
    }
    if (emscriptenModule.onExit == onExit) {
        emscriptenModule.onExit = undefined;
    }
}

function onExit(code: number) {
    mono_exit(code, loaderHelpers.exitReason);
}

function onAbort(reason: any) {
    mono_exit(1, loaderHelpers.exitReason || reason);
}

// this will also call mono_wasm_exit if available, which will call exitJS -> _proc_exit -> terminateAllThreads
export function mono_exit(exit_code: number, reason?: any): void {
    unregister_exit_handlers();

    // unify shape of the reason object
    const is_object = reason && typeof reason === "object";
    exit_code = (is_object && typeof reason.status === "number") ? reason.status : exit_code;
    const message = (is_object && typeof reason.message === "string")
        ? reason.message
        : "" + reason;
    reason = is_object
        ? reason
        : (runtimeHelpers.ExitStatus
            ? new runtimeHelpers.ExitStatus(exit_code)
            : new Error("Exit with code " + exit_code + " " + message));
    reason.status = exit_code;
    if (!reason.message) {
        reason.message = message;
    }

    // force stack property to be generated before we shut down managed code, or create current stack if it doesn't exist
    if (!reason.stack) {
        reason.stack = new Error().stack || "";
    }

    // don't report this error twice
    reason.silent = true;

    if (!is_exited()) {
        try {
            if (!runtimeHelpers.runtimeReady) {
                mono_log_debug("abort_startup, reason: " + reason);
                abort_promises(reason);
            } else {
                if (runtimeHelpers.jiterpreter_dump_stats) {
                    runtimeHelpers.jiterpreter_dump_stats(false);
                }
                if (exit_code === 0 && loaderHelpers.config?.interopCleanupOnExit) {
                    runtimeHelpers.forceDisposeProxies(true, true);
                }
            }
        }
        catch (err) {
            mono_log_warn("mono_exit failed", err);
            // don't propagate any failures
        }

        try {
            logOnExit(exit_code, reason);
            appendElementOnExit(exit_code);
        }
        catch (err) {
            mono_log_warn("mono_exit failed", err);
            // don't propagate any failures
        }

        loaderHelpers.exitCode = exit_code;
        loaderHelpers.exitReason = reason.message;

        if (!ENVIRONMENT_IS_WORKER && runtimeHelpers.runtimeReady) {
            emscriptenModule.runtimeKeepalivePop();
        }
    }

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
        throw reason;
    } else {
        set_exit_code_and_quit_now(exit_code, reason);
    }
}

function set_exit_code_and_quit_now(exit_code: number, reason?: any): void {
    if (runtimeHelpers.runtimeReady && runtimeHelpers.nativeExit) {
        runtimeHelpers.runtimeReady = false;
        try {
            runtimeHelpers.nativeExit(exit_code);
        }
        catch (err) {
            if (runtimeHelpers.ExitStatus && !(err instanceof runtimeHelpers.ExitStatus)) {
                mono_log_warn("mono_wasm_exit failed", err);
            }
        }
    }
    // just in case mono_wasm_exit didn't exit or throw
    if (exit_code !== 0 || !ENVIRONMENT_IS_WEB) {
        if (ENVIRONMENT_IS_NODE && INTERNAL.process) {
            INTERNAL.process.exit(exit_code);
        }
        else if (runtimeHelpers.quit) {
            runtimeHelpers.quit(exit_code, reason);
        }
        throw reason;
    }
}

async function flush_node_streams() {
    try {
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore:
        const process = await import(/*! webpackIgnore: true */"process");
        const flushStream = (stream: any) => {
            return new Promise<void>((resolve, reject) => {
                stream.on("error", reject);
                stream.end("", "utf8", resolve);
            });
        };
        const stderrFlushed = flushStream(process.stderr);
        const stdoutFlushed = flushStream(process.stdout);
        let timeoutId;
        const timeout = new Promise(resolve => {
            timeoutId = setTimeout(() => resolve("timeout"), 1000);
        });
        await Promise.race([Promise.all([stdoutFlushed, stderrFlushed]), timeout]);
        clearTimeout(timeoutId);
    } catch (err) {
        mono_log_error(`flushing std* streams failed: ${err}`);
    }
}

function abort_promises(reason: any) {
    loaderHelpers.exitReason = reason;
    loaderHelpers.allDownloadsQueued.promise_control.reject(reason);
    loaderHelpers.afterConfigLoaded.promise_control.reject(reason);
    loaderHelpers.wasmCompilePromise.promise_control.reject(reason);
    loaderHelpers.runtimeModuleLoaded.promise_control.reject(reason);
    loaderHelpers.memorySnapshotSkippedOrDone.promise_control.reject(reason);
    if (runtimeHelpers.dotnetReady) {
        runtimeHelpers.dotnetReady.promise_control.reject(reason);
        runtimeHelpers.afterInstantiateWasm.promise_control.reject(reason);
        runtimeHelpers.beforePreInit.promise_control.reject(reason);
        runtimeHelpers.afterPreInit.promise_control.reject(reason);
        runtimeHelpers.afterPreRun.promise_control.reject(reason);
        runtimeHelpers.beforeOnRuntimeInitialized.promise_control.reject(reason);
        runtimeHelpers.afterOnRuntimeInitialized.promise_control.reject(reason);
        runtimeHelpers.afterPostRun.promise_control.reject(reason);
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

function logOnExit(exit_code: number, reason: any) {
    if (exit_code !== 0 && reason) {
        // ExitStatus usually is not real JS error and so stack strace is not very useful.
        // We will use debug level for it, which will print only when diagnosticTracing is set.
        const mono_log = runtimeHelpers.ExitStatus && reason instanceof runtimeHelpers.ExitStatus
            ? mono_log_debug
            : mono_log_error;
        if (typeof reason == "string") {
            mono_log(reason);
        }
        else if (reason.stack && reason.message) {
            if (runtimeHelpers.stringify_as_error_with_stack) {
                mono_log(runtimeHelpers.stringify_as_error_with_stack(reason));
            } else {
                mono_log(reason.message + "\n" + reason.stack);
            }
        }
        else {
            mono_log(JSON.stringify(reason));
        }
    }
    if (loaderHelpers.config) {
        if (loaderHelpers.config.logExitCode) {
            if (loaderHelpers.config.forwardConsoleLogsToWS) {
                teardown_proxy_console("WASM EXIT " + exit_code);
            } else {
                mono_log_info_no_prefix("WASM EXIT " + exit_code);
            }
        }
        else if (loaderHelpers.config.forwardConsoleLogsToWS) {
            teardown_proxy_console();
        }
    }
}
