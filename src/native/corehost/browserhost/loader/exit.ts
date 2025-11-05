// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetLogger } from "./cross-module";
import { ENVIRONMENT_IS_NODE } from "./per-module";

// WASM-TODO: redirect to host.ts
export function exit(exit_code: number, reason: any): void {
    if (reason) {
        const reasonStr = (typeof reason === "object") ? `${reason.message || ""}\n${reason.stack || ""}` : reason.toString();
        dotnetLogger.error(reasonStr);
    }
    if (ENVIRONMENT_IS_NODE) {
        (globalThis as any).process.exit(exit_code);
    }
}
