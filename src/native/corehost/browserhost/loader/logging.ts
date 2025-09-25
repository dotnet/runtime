// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// WASM-TODO: inline the code

export function check(condition: unknown, messageFactory: string | (() => string)): asserts condition {
    if (!condition) {
        const message = typeof messageFactory === "string" ? messageFactory : messageFactory();
        throw new Error(`Assert failed: ${message}`);
    }
}

/* eslint-disable no-console */

const prefix = "CLR_WASM: ";

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
