// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderConfig } from "./config";

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

export function error(msg: string, ...data: any) {
    if (data && data.length > 0 && data[0] && typeof data[0] === "object") {
        // don't log silent errors
        if (data[0].silent) {
            return;
        }
        if (data[0].toString) {
            console.error(prefix + msg, data[0].toString());
            return;
        }
    }
    console.error(prefix + msg, ...data);
}
