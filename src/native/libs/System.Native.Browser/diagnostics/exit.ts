// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { LoaderConfigInternal } from "./types";
import { dotnetLogger, dotnetLoaderExports, dotnetApi, dotnetBrowserUtilsExports, dotnetRuntimeExports } from "./cross-module";
import { ENVIRONMENT_IS_NODE, ENVIRONMENT_IS_WEB } from "./per-module";
import { teardownProxyConsole } from "./console-proxy";
import { symbolicateStackTrace } from "./symbolicate";

let loaderConfig: LoaderConfigInternal = null as any;
export function registerExit() {
    if (!dotnetApi || !dotnetApi.getConfig || !dotnetLoaderExports) {
        return;
    }
    loaderConfig = dotnetApi.getConfig() as LoaderConfigInternal;
    if (!loaderConfig) {
        return;
    }
    installUnhandledErrorHandler();

    dotnetLoaderExports.addOnExitListener(onExit);
}

function onExit(exitCode: number, reason: any, silent: boolean): boolean {
    if (!loaderConfig) {
        return true;
    }
    if (exitCode === 0 && loaderConfig.interopCleanupOnExit) {
        dotnetRuntimeExports.forceDisposeProxies(true, true);
    }
    uninstallUnhandledErrorHandler();
    if (loaderConfig.logExitCode) {
        if (!silent) {
            logExitReason(exitCode, reason);
        }
        logExitCode(exitCode);
    }
    if (ENVIRONMENT_IS_WEB && loaderConfig.appendElementOnExit) {
        appendElementOnExit(exitCode);
    }

    if (ENVIRONMENT_IS_NODE && loaderConfig.asyncFlushOnExit && exitCode === 0) {
        // this would NOT call Node's exit() immediately, it's a hanging promise
        (async function flush() {
            try {
                await flushNodeStreams();
            } finally {
                dotnetLoaderExports.quitNow(exitCode, reason);
            }
        })();
        return false;
    }
    return true;
}

function logExitReason(exit_code: number, reason: any) {
    if (exit_code !== 0 && reason) {
        const exitStatus = isExitStatus(reason);
        if (typeof reason === "string") {
            dotnetLogger.error(reason);
        } else {
            if (reason.stack === undefined && !exitStatus) {
                reason.stack = new Error().stack + "";
            }
            const message = reason.message
                ? symbolicateStackTrace(reason.message + "\n" + reason.stack)
                : reason.toString();

            if (exitStatus) {
                dotnetLogger.debug(message);
            } else {
                dotnetLogger.error(message);
            }
        }
    }
}

function isExitStatus(reason: any): boolean {
    const ExitStatus = dotnetBrowserUtilsExports.getExitStatus();
    return ExitStatus && reason instanceof ExitStatus;
}

function logExitCode(exitCode: number): void {
    const message = loaderConfig.logExitCode
        ? "WASM EXIT " + exitCode
        : undefined;
    if (loaderConfig.forwardConsole) {
        teardownProxyConsole(message);
    } else if (message) {
        // eslint-disable-next-line no-console
        console.log(message);
    }
}

// https://github.com/dotnet/xharness/blob/799df8d4c86ff50c83b7a57df9e3691eeab813ec/src/Microsoft.DotNet.XHarness.CLI/Commands/WASM/Browser/WasmBrowserTestRunner.cs#L122-L141
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
    if (ENVIRONMENT_IS_WEB && loaderConfig.exitOnUnhandledError) {
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
