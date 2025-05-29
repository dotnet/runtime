// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { loaderHelpers } from "./globals";

/* eslint-disable no-console */

const prefix = "MONO_WASM: ";

export function mono_log_debug (messageFactory: string | (() => string)) {
    if (loaderHelpers.diagnosticTracing) {
        const message = (typeof messageFactory === "function"
            ? messageFactory()
            : messageFactory);
        console.debug(prefix + message);
    }
}

export function mono_log_info (msg: string, ...data: any) {
    console.info(prefix + msg, ...data);
}

export function mono_log_warn (msg: string, ...data: any) {
    console.warn(prefix + msg, ...data);
}

export function mono_log_error (msg: string, ...data: any) {
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
