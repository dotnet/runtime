// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { MonoConfig } from "../../types";
import { loaderHelpers } from "../globals";

let testAnchor: HTMLAnchorElement;
export function toAbsoluteUri(relativeUri: string): string {
    testAnchor = testAnchor || document.createElement("a");
    testAnchor.href = relativeUri;
    return testAnchor.href;
}

export function hasDebuggingEnabled(config: MonoConfig): boolean {
    // Copied from blazor MonoDebugger.ts/attachDebuggerHotkey
    if (!globalThis.navigator) {
        return false;
    }

    const hasReferencedPdbs = !!config.resources!.pdb;
    return (hasReferencedPdbs || config.debugLevel != 0) && (loaderHelpers.isChromium || loaderHelpers.isFirefox);
}