// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-console */
import { ENVIRONMENT_IS_WORKER, loaderHelpers } from "./globals";

const methods = ["debug", "log", "trace", "warn", "info", "error"];
const prefix = "MONO_WASM: ";
let consoleWebSocket: WebSocket;
let theConsoleApi: any;
let originalConsoleMethods: any;
let threadNamePrefix: string;

export function mono_set_thread_name(threadName: string) {
    threadNamePrefix = threadName;
}

export function mono_log_debug(msg: string, ...data: any) {
    if (loaderHelpers.diagnosticTracing) {
        console.debug(prefix + msg, ...data);
    }
}

export function mono_log_info(msg: string, ...data: any) {
    console.info(prefix + msg, ...data);
}

export function mono_log_info_no_prefix(msg: string, ...data: any) {
    console.info(msg, ...data);
}

export function mono_log_warn(msg: string, ...data: any) {
    console.warn(prefix + msg, ...data);
}

export function mono_log_error(msg: string, ...data: any) {
    if (data && data.length > 0 && data[0] && typeof data[0] === "object" && data[0].silent) {
        // don't log silent errors
        return;
    }
    console.error(prefix + msg, ...data);
}

function proxyConsoleMethod(prefix: string, func: any, asJson: boolean) {
    return function (...args: any[]) {
        try {
            let payload = args[0];
            if (payload === undefined) payload = "undefined";
            else if (payload === null) payload = "null";
            else if (typeof payload === "function") payload = payload.toString();
            else if (typeof payload !== "string") {
                try {
                    payload = JSON.stringify(payload);
                } catch (e) {
                    payload = payload.toString();
                }
            }

            if (typeof payload === "string") {
                if (payload[0] == "[") {
                    const now = new Date().toISOString();
                    if (ENVIRONMENT_IS_WORKER) {
                        payload = `[${threadNamePrefix}][${now}] ${payload}`;
                    } else {
                        payload = `[${now}] ${payload}`;
                    }
                } else if (ENVIRONMENT_IS_WORKER) {
                    payload = `[${threadNamePrefix}] ${payload}`;
                }
            }

            if (asJson) {
                func(JSON.stringify({
                    method: prefix,
                    payload: payload,
                    arguments: args.slice(1)
                }));
            } else {
                func([prefix + payload, ...args.slice(1)]);
            }
        } catch (err) {
            originalConsoleMethods.error(`proxyConsole failed: ${err}`);
        }
    };
}

export function setup_proxy_console(id: string, console: Console, origin: string): void {
    theConsoleApi = console as any;
    threadNamePrefix = id;
    originalConsoleMethods = {
        ...console
    };

    setupWS();

    const consoleUrl = `${origin}/console`.replace("https://", "wss://").replace("http://", "ws://");

    consoleWebSocket = new WebSocket(consoleUrl);
    consoleWebSocket.addEventListener("error", logWSError);
    consoleWebSocket.addEventListener("close", logWSClose);
}

export function teardown_proxy_console(message?: string) {
    const stop_when_ws_buffer_empty = () => {
        if (!consoleWebSocket) {
            if (message && originalConsoleMethods) {
                originalConsoleMethods.log(message);
            }
        }
        else if (consoleWebSocket.bufferedAmount == 0) {
            if (message) {
                // tell xharness WasmTestMessagesProcessor we are done.
                // note this sends last few bytes into the same WS
                mono_log_info_no_prefix(message);
            }
            setupOriginal();

            consoleWebSocket.removeEventListener("error", logWSError);
            consoleWebSocket.removeEventListener("close", logWSClose);
            consoleWebSocket.close(1000, message);
            (consoleWebSocket as any) = undefined;
        }
        else {
            globalThis.setTimeout(stop_when_ws_buffer_empty, 100);
        }
    };
    stop_when_ws_buffer_empty();
}

function send(msg: string) {
    if (consoleWebSocket.readyState === WebSocket.OPEN) {
        consoleWebSocket.send(msg);
    }
    else {
        originalConsoleMethods.log(msg);
    }
}

function logWSError(event: Event) {
    originalConsoleMethods.error(`[${threadNamePrefix}] websocket error: ${event}`, event);
}

function logWSClose(event: Event) {
    originalConsoleMethods.error(`[${threadNamePrefix}] websocket closed: ${event}`, event);
}

function setupWS() {
    for (const m of methods) {
        theConsoleApi[m] = proxyConsoleMethod(`console.${m}`, send, true);
    }
}

function setupOriginal() {
    for (const m of methods) {
        theConsoleApi[m] = proxyConsoleMethod(`console.${m}`, originalConsoleMethods.log, false);
    }
}
