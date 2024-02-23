// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-console */
import { INTERNAL, runtimeHelpers } from "./globals";
import { utf8ToString } from "./strings";
import { CharPtr, VoidPtr } from "./types/emscripten";

let prefix = "MONO_WASM: ";

export function set_thread_prefix(threadPrefix: string) {
    prefix = `[${threadPrefix}] MONO_WASM: `;
}

export function mono_log_debug(msg: string, ...data: any) {
    if (runtimeHelpers.diagnosticTracing) {
        console.debug(prefix + msg, ...data);
    }
}

export function mono_log_info(msg: string, ...data: any) {
    console.info(prefix + msg, ...data);
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
        console.debug(`failed to symbolicate: ${error}`);
        return message;
    }
}

export function mono_wasm_stringify_as_error_with_stack(reason: any): string {
    let stack: string;
    if (typeof reason === "string") {
        stack = reason;
    }
    else if (reason === undefined || reason === null || reason.stack === undefined) {
        stack = new Error().stack + "";
    } else {
        stack = reason.stack + "";
    }

    // Error
    return mono_wasm_symbolicate_string(stack);
}

export function mono_wasm_trace_logger(log_domain_ptr: CharPtr, log_level_ptr: CharPtr, message_ptr: CharPtr, fatal: number, user_data: VoidPtr): void {
    const origMessage = utf8ToString(message_ptr);
    const isFatal = !!fatal;
    const domain = utf8ToString(log_domain_ptr);
    const dataPtr = user_data;
    const log_level = utf8ToString(log_level_ptr);

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


export function parseSymbolMapFile(text: string) {
    text.split(/[\r\n]/).forEach((line: string) => {
        const parts: string[] = line.split(/:/);
        if (parts.length < 2)
            return;

        parts[1] = parts.splice(1).join(":");
        wasm_func_map.set(Number(parts[0]), parts[1]);
    });

    mono_log_debug(`Loaded ${wasm_func_map.size} symbols`);
}

export function mono_wasm_get_func_id_to_name_mappings() {
    return [...wasm_func_map.values()];
}

export function mono_wasm_console_clear() {
    console.clear();
}