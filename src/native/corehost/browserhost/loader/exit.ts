// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { netJSEngine, Logger } from "./cross-module";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export function exit(exit_code: number, reason: any): void {
    const reasonStr = reason ? (reason.stack ? reason.stack || reason.message : reason.toString()) : "";
    if (exit_code !== 0) {
        Logger.error(`Exit with code ${exit_code} ${reason ? "and reason: " + reasonStr : ""}`);
    }
    if (netJSEngine.IS_NODE) {
        (globalThis as any).process.exit(exit_code);
    }
}
