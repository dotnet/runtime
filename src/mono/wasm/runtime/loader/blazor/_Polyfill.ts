// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData } from "../../types/blazor";
import { loaderHelpers } from "../globals";

let testAnchor: HTMLAnchorElement;
export function toAbsoluteUri(relativeUri: string): string {
    testAnchor = testAnchor || document.createElement("a");
    testAnchor.href = relativeUri;
    return testAnchor.href;
}

export function hasDebuggingEnabled(bootConfig: BootJsonData): boolean {
    // Copied from blazor MonoDebugger.ts/attachDebuggerHotkey

    const hasReferencedPdbs = !!bootConfig.resources.pdb;
    const debugBuild = bootConfig.debugBuild;

    return (hasReferencedPdbs || debugBuild) && (loaderHelpers.isChromium || loaderHelpers.isFirefox);
}