// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { BootJsonData } from "../../types/blazor";

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

    const navigatorUA = navigator as MonoNavigatorUserAgent;
    const brands = navigatorUA.userAgentData && navigatorUA.userAgentData.brands;
    const currentBrowserIsChromeOrEdge = brands
        ? brands.some(b => b.brand === "Google Chrome" || b.brand === "Microsoft Edge" || b.brand === "Chromium")
        : (window as any).chrome;

    return (hasReferencedPdbs || debugBuild) && (currentBrowserIsChromeOrEdge || navigator.userAgent.includes("Firefox"));
}

// can be removed once userAgentData is part of lib.dom.d.ts
declare interface MonoNavigatorUserAgent extends Navigator {
    readonly userAgentData: MonoUserAgentData;
}

declare interface MonoUserAgentData {
    readonly brands: ReadonlyArray<MonoUserAgentDataBrandVersion>;
    readonly platform: string;
}

declare interface MonoUserAgentDataBrandVersion {
    brand?: string;
    version?: string;
}