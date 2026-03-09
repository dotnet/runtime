//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
function libCoreRunBrowserFactory() {
    let commonDeps = [
        "$DOTNET",
        "$ENV",
        "$FS",
        "corerun_shutdown",
        "BrowserHost_ShutdownDotnet",
    ];
    const mergeCoreRun = {
        $CORERUN: {
            selfInitialize: () => {
                const browserVirtualAppBase = "/";// keep in sync other places that define browserVirtualAppBase
                FS.createPath("/", browserVirtualAppBase, true, true);

                ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
            },
        },
        $CORERUN__postset: "CORERUN.selfInitialize()",
        $CORERUN__deps: commonDeps,
        BrowserHost_ShutdownDotnet: (exitCode) => _corerun_shutdown(exitCode),
    };

    autoAddDeps(mergeCoreRun, "$CORERUN");
    addToLibrary(mergeCoreRun);
}

libCoreRunBrowserFactory();
