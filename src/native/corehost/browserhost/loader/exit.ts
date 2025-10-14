// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { dotnetLogger } from "./cross-module";
import { ENVIRONMENT_IS_NODE } from "./per-module";

// WASM-TODO: redirect to host.ts
export function exit(exit_code: number, reason: any): void {
    const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
    if (exit_code !== 0) {
        dotnetLogger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
    }
    if (ENVIRONMENT_IS_NODE) {
        (globalThis as any).process.exit(exit_code);
    }
}
