// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-console */
import { loaderHelpers } from "./globals";

const prefix = "MONO_WASM: ";

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
export let consoleWebSocket: WebSocket;

export function setup_proxy_console(id: string, console: Console, origin: string): void {
    // this need to be copy, in order to keep reference to original methods
    const originalConsole = {
        log: console.log,
        error: console.error
    };
    const anyConsole = console as any;

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
                        if (id !== "main") {
                            payload = `[${id}][${now}] ${payload}`;
                        } else {
                            payload = `[${now}] ${payload}`;
                        }
                    } else if (id !== "main") {
                        payload = `[${id}] ${payload}`;
                    }
                }

                if (asJson) {
                    func(JSON.stringify({
                        method: prefix,
                        payload: payload,
                        arguments: args
                    }));
                } else {
                    func([prefix + payload, ...args.slice(1)]);
                }
            } catch (err) {
                originalConsole.error(`proxyConsole failed: ${err}`);
            }
        };
    }

    const methods = ["debug", "trace", "warn", "info", "error"];
    for (const m of methods) {
        if (typeof (anyConsole[m]) !== "function") {
            anyConsole[m] = proxyConsoleMethod(`console.${m}: `, console.log, false);
        }
    }

    const consoleUrl = `${origin}/console`.replace("https://", "wss://").replace("http://", "ws://");

    consoleWebSocket = new WebSocket(consoleUrl);
    consoleWebSocket.addEventListener("open", () => {
        originalConsole.log(`browser: [${id}] Console websocket connected.`);
    });
    consoleWebSocket.addEventListener("error", (event) => {
        originalConsole.error(`[${id}] websocket error: ${event}`, event);
    });
    consoleWebSocket.addEventListener("close", (event) => {
        originalConsole.error(`[${id}] websocket closed: ${event}`, event);
    });

    const send = (msg: string) => {
        if (consoleWebSocket.readyState === WebSocket.OPEN) {
            consoleWebSocket.send(msg);
        }
        else {
            originalConsole.log(msg);
        }
    };

    for (const m of ["log", ...methods])
        anyConsole[m] = proxyConsoleMethod(`console.${m}`, send, true);
}
