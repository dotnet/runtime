// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { INTERNAL, Module, runtimeHelpers } from "./globals";
import { CharPtr, VoidPtr } from "./types/emscripten";

export const wasm_func_map = new Map<number, string>();
const regexes: any[] = [];

// V8
//   at <anonymous>:wasm-function[1900]:0x83f63
//   at dlfree (<anonymous>:wasm-function[18739]:0x2328ef)
regexes.push(/at (?<replaceSection>[^:()]+:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)((?![^)a-fA-F\d])|$)/);

//# 5: WASM [009712b2], function #111 (''), pc=0x7c16595c973 (+0x53), pos=38740 (+11)
regexes.push(/(?:WASM \[[\da-zA-Z]+\], (?<replaceSection>function #(?<funcNum>[\d]+) \(''\)))/);

//# chrome
//# at http://127.0.0.1:63817/dotnet.wasm:wasm-function[8963]:0x1e23f4
regexes.push(/(?<replaceSection>[a-z]+:\/\/[^ )]*:wasm-function\[(?<funcNum>\d+)\]:0x[a-fA-F\d]+)/);

//# <?>.wasm-function[8962]
regexes.push(/(?<replaceSection><[^ >]+>[.:]wasm-function\[(?<funcNum>[0-9]+)\])/);

export function mono_wasm_symbolicate_string(message: string): string {
    try {
        if (wasm_func_map.size == 0)
            return message;

        const origMessage = message;

        for (let i = 0; i < regexes.length; i++) {
            const newRaw = message.replace(new RegExp(regexes[i], "g"), (substring, ...args) => {
                const groups = args.find(arg => {
                    return typeof (arg) == "object" && arg.replaceSection !== undefined;
                });

                if (groups === undefined)
                    return substring;

                const funcNum = groups.funcNum;
                const replaceSection = groups.replaceSection;
                const name = wasm_func_map.get(Number(funcNum));

                if (name === undefined)
                    return substring;

                return substring.replace(replaceSection, `${name} (${replaceSection})`);
            });

            if (newRaw !== origMessage)
                return newRaw;
        }

        return origMessage;
    } catch (error) {
        console.debug(`MONO_WASM: failed to symbolicate: ${error}`);
        return message;
    }
}

export function mono_wasm_stringify_as_error_with_stack(err: Error | string): string {
    let errObj: any = err;
    if (!errObj || !errObj.stack || !(errObj instanceof Error)) {
        errObj = new Error(errObj || "Unknown error");
    }

    // Error
    return mono_wasm_symbolicate_string(errObj.stack);
}

export function mono_wasm_trace_logger(log_domain_ptr: CharPtr, log_level_ptr: CharPtr, message_ptr: CharPtr, fatal: number, user_data: VoidPtr): void {
    const origMessage = Module.UTF8ToString(message_ptr);
    const isFatal = !!fatal;
    const domain = Module.UTF8ToString(log_domain_ptr);
    const dataPtr = user_data;
    const log_level = Module.UTF8ToString(log_level_ptr);

    const message = `[MONO] ${origMessage}`;

    if (INTERNAL["logging"] && typeof INTERNAL.logging["trace"] === "function") {
        INTERNAL.logging.trace(domain, log_level, message, isFatal, dataPtr);
        return;
    }

    switch (log_level) {
        case "critical":
        case "error":
            console.error(mono_wasm_stringify_as_error_with_stack(message));
            break;
        case "warning":
            console.warn(message);
            break;
        case "message":
            console.log(message);
            break;
        case "info":
            console.info(message);
            break;
        case "debug":
            console.debug(message);
            break;
        default:
            console.log(message);
            break;
    }
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

export function parseSymbolMapFile(text: string) {
    text.split(/[\r\n]/).forEach((line: string) => {
        const parts: string[] = line.split(/:/);
        if (parts.length < 2)
            return;

        parts[1] = parts.splice(1).join(":");
        wasm_func_map.set(Number(parts[0]), parts[1]);
    });

    if (runtimeHelpers.diagnosticTracing) console.debug(`MONO_WASM: Loaded ${wasm_func_map.size} symbols`);
}