// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfigInternal } from "./types";
import { dotnetLogger, dotnetLoaderExports, dotnetBrowserHostExports, dotnetApi } from "./cross-module";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB } from "./per-module";
import { setupProxyConsole, teardownProxyConsole } from "./console-proxy";
import { symbolicateStackTrace } from "./symbolicate";

let config: LoaderConfigInternal = null as any;
export function registerExit() {
    if (!dotnetApi || !dotnetApi.getConfig || !dotnetLoaderExports) {
        return;
    }
    config = dotnetApi.getConfig() as LoaderConfigInternal;
    if (!config) {
        return;
    }
    installUnhandledErrorHandler();

    dotnetLoaderExports.addOnExitListener(onExit);
    if (ENVIRONMENT_IS_WEB && config.forwardConsoleLogsToWS && typeof globalThis.WebSocket != "undefined") {
        setupProxyConsole(globalThis.console, globalThis.location.origin);
    }

}

function onExit(exitCode: number, reason: any, silent: boolean) {
    if (!config) {
        return;
    }
    uninstallUnhandledErrorHandler();
    if (config.logExitCode) {
        if (!silent) {
            logExitReason(exitCode, reason);
        }
        logExitCode(exitCode);
    }
    if (ENVIRONMENT_IS_WEB && config.appendElementOnExit) {
        appendElementOnExit(exitCode);
    }

    if (ENVIRONMENT_IS_NODE && config.asyncFlushOnExit && exitCode === 0) {
        // this would NOT call Node's exit() immediately, it's a hanging promise
        (async function flush() {
            try {
                await flushNodeStreams();
            } finally {
                dotnetLoaderExports.quitNow(exitCode, reason);
            }
        })();
        // we need to throw, rather than let the caller continue the normal execution
        // in the middle of some code, which expects this to stop the process
        throw reason;
    } else {
        dotnetLoaderExports.quitNow(exitCode, reason);
    }
}

function logExitReason(exit_code: number, reason: any) {
    if (exit_code !== 0 && reason) {
        if (typeof reason == "string") {
            dotnetLogger.error(reason);
        } else {
            const ExitStatus = dotnetBrowserHostExports.getExitStatus();
            if (reason.stack === undefined && !ExitStatus && !(reason instanceof ExitStatus)) {
                reason.stack = new Error().stack + "";
            }
            if (reason.message) {
                dotnetLogger.error(symbolicateStackTrace(reason.message + "\n" + reason.stack));
            } else {
                dotnetLogger.error(JSON.stringify(reason));
            }
        }
    }
}

function logExitCode(exitCode: number): void {
    if (config.logExitCode) {
        if (config.forwardConsoleLogsToWS) {
            teardownProxyConsole("WASM EXIT " + exitCode);
        } else {
            dotnetLogger.info("WASM EXIT " + exitCode);
        }
    } else if (config.forwardConsoleLogsToWS) {
        teardownProxyConsole();
    }
}

function appendElementOnExit(exitCode: number): void {
    //Tell xharness WasmBrowserTestRunner what was the exit code
    const tests_done_elem = document.createElement("label");
    tests_done_elem.id = "tests_done";
    if (exitCode !== 0) tests_done_elem.style.background = "red";
    tests_done_elem.innerHTML = "" + exitCode;
    document.body.appendChild(tests_done_elem);
}

function installUnhandledErrorHandler() {
    // it seems that emscripten already does the right thing for NodeJs and that there is no good solution for V8 shell.
    if (ENVIRONMENT_IS_WEB && config.exitOnUnhandledError) {
        globalThis.addEventListener("unhandledrejection", unhandledRejectionHandler);
        globalThis.addEventListener("error", errorHandler);
    }
}

function uninstallUnhandledErrorHandler() {
    if (ENVIRONMENT_IS_WEB) {
        globalThis.removeEventListener("unhandledrejection", unhandledRejectionHandler);
        globalThis.removeEventListener("error", errorHandler);
    }
}

function unhandledRejectionHandler(event: PromiseRejectionEvent) {
    fatalHandler(event, event.reason, "rejection");
}

function errorHandler(event: ErrorEvent) {
    fatalHandler(event, event.error, "error");
}

function fatalHandler(event: any, reason: any, type: string) {
    event.preventDefault();
    try {
        if (!reason) {
            reason = new Error("Unhandled " + type);
        }
        if (reason.stack === undefined) {
            reason.stack = new Error().stack;
        }
        reason.stack = reason.stack + "";// string conversion (it could be getter)
        if (!reason.silent) {
            dotnetLogger.error("Unhandled error:", reason);
            dotnetApi.exit(1, reason);
        }
    } catch (err) {
        // no not re-throw from the fatal handler
    }
}

async function flushNodeStreams() {
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
        dotnetLogger.error(`flushing std* streams failed: ${err}`);
    }
}
