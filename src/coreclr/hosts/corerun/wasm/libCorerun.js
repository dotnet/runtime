//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
function libCoreRunFactory() {
    let commonDeps = [
        "$DOTNET",
        "$ENV",
        "$FS",
        "$NODEFS",
        "$NODERAWFS",
        "corerun_shutdown"
    ];
    const mergeCoreRun = {
        $CORERUN: {
            selfInitialize: () => {
                const browserVirtualAppBase = "/";// keep in sync other places that define browserVirtualAppBase
                FS.createPath("/", browserVirtualAppBase, true, true);

                // copy all node/shell env variables to emscripten env
                if (globalThis.process && globalThis.process.env) {
                    for (const [key, value] of Object.entries(process.env)) {
                        ENV[key] = value;
                    }
                }

                ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";
                const originalExitJS = exitJS;
                exitJS = (status, implicit) => {
                    if (!implicit) {
                        EXITSTATUS = status;
                        ABORT = true;
                        if (dotnetBrowserUtilsExports.abortBackgroundTimers) {
                            dotnetBrowserUtilsExports.abortBackgroundTimers();
                        }
                    }
                    if (!keepRuntimeAlive()) {
                        ABORT = true;
                        var latched = _corerun_shutdown(EXITSTATUS || 0);
                        if (EXITSTATUS === undefined) {
                            EXITSTATUS = latched;
                        }
                    }
                    return originalExitJS(EXITSTATUS, implicit);
                };
            },
        },
        $CORERUN__postset: "CORERUN.selfInitialize()",
        $CORERUN__deps: commonDeps,
    };
    const patchNODERAWFS = {
        cwd: () => {
            // drop windows drive letter for NODEFS cwd to pretend we are in unix
            const path = process.cwd();
            return NODEFS.isWindows
                ? path.replace(/^[a-zA-Z]:/, "").replace(/\\/g, "/")
                : path;
        }
    }

    autoAddDeps(mergeCoreRun, "$CORERUN");
    addToLibrary(mergeCoreRun);
    if (LibraryManager.library.$NODERAWFS) {
        Object.assign(LibraryManager.library.$NODERAWFS, patchNODERAWFS);
    }
}

libCoreRunFactory();
