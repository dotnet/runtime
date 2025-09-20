// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * This is root of **Emscripten library** that would become part of `dotnet.native.js`
 * It implements interop between JS and .NET
 */

import { dotnetInternals } from "./cross-module";
import { initialize } from "./native-exchange";

// Exports that can be trimmed away by emscripten linker if not used.
import * as trimmableExports from "./trimmable";

declare const DOTNET_INTEROP: any;

function DotnetInteropLibFactory() {
    // Symbols that would be protected from emscripten linker
    const moduleDeps = ["$DOTNET"];
    const trimmableDeps: Record<string, string[]> = {};
    for (const exportName of Reflect.ownKeys(trimmableExports)) {
        const emName = exportName.toString() + "__deps";
        const deps = (trimmableExports as any)[exportName]["__deps"] as string[] | undefined;
        if (deps) {
            trimmableDeps[emName] = deps;
        }
    }
    const lib = {
        $DOTNET_INTEROP: {
            selfInitialize: () => {
                if (typeof dotnetInternals !== "undefined") {
                    DOTNET_INTEROP.dotnetInternals = dotnetInternals;
                    DOTNET_INTEROP.initialize(dotnetInternals);
                }
            },
            initialize: initialize,
        },
        "$DOTNET_INTEROP__deps": moduleDeps,
        "$DOTNET_INTEROP__postset": "DOTNET_INTEROP.selfInitialize();",
        ...trimmableExports,
        ...trimmableDeps,
    };

    autoAddDeps(lib, "$DOTNET_INTEROP");
    addToLibrary(lib);
}

DotnetInteropLibFactory();

// make sure we don't mangle names
export * as commonInfra from "./cross-module";
export * as trimmableExports from "./trimmable";
export * as exchange from "./native-exchange";
