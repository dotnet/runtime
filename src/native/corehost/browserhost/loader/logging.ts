// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderConfig } from "./config";
import { dotnetDiagnosticsExports } from "./cross-module";

export function check(condition: unknown, message: string): asserts condition {
    if (!condition) {
        throw new Error(`Assert failed: ${message}`);
    }
}

// calls to fastCheck will be inlined by rollup
// so that the string formatting or allocation of a closure would only happen in failure cases
// this is important for performance sensitive code paths
export function fastCheck(condition: unknown, messageFactory: (() => string)): asserts condition {
    if (!condition) {
        const message = messageFactory();
        throw new Error(`Assert failed: ${message}`);
    }
}

/* eslint-disable no-console */

const prefix = "DOTNET: ";

export function debug(msg: string | (() => string), ...data: any) {
    if (!loaderConfig.diagnosticTracing) {
        return;
    }
    if (typeof msg === "function") {
        msg = msg();
    }
    console.debug(prefix + msg, ...data);
}

export function info(msg: string, ...data: any) {
    console.info(prefix + msg, ...data);
}

export function warn(msg: string, ...data: any) {
    console.warn(prefix + msg, ...data);
}

export function error(msg: string, reason: any) {
    if (reason && typeof reason === "object" && reason.silent) {
        return;
    }
    console.error(prefix + msg, normalizeException(reason));
}

export function normalizeException(reason: any) {
    let res = "unknown exception";
    let stack: string | undefined;
    if (reason) {
        if (typeof reason === "object" && reason.status === undefined) {
            if (reason.stack === undefined) {
                stack = reason.stack + "";
            } else {
                stack = new Error().stack + "";
            }
        }
        if (reason.message) {
            res = reason.message;
        } else if (typeof reason.toString === "function") {
            res = reason.toString();
        } else {
            res = reason + "";
        }
        if (stack) {
            // Some JS runtimes insert the error message at the top of the stack, some don't,
            //  so normalize it by using the stack as the result if it already contains the error
            if (stack.startsWith(res))
                res = symbolicateStackTrace(stack);
            else
                res += "\n" + symbolicateStackTrace(stack);
        } else {
            res = symbolicateStackTrace(res);
        }
    }
    return res;
}

function symbolicateStackTrace(message: string): string {
    if (dotnetDiagnosticsExports.symbolicateStackTrace) {
        return dotnetDiagnosticsExports.symbolicateStackTrace(message);
    }
    return message;
}
