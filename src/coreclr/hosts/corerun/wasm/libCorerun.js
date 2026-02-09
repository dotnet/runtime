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
        "corerun_shutdown",
        "__funcs_on_exit",
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

                Module.preInit = [() => {
                    const orig_funcs_on_exit = ___funcs_on_exit;
                    // it would be better to use addOnExit(), but it's called too late.
                    ___funcs_on_exit = () => {
                        // this will prevent more timers (like finalizer) to get scheduled during thread destructor
                        if (dotnetBrowserUtilsExports.abortBackgroundTimers) {
                            dotnetBrowserUtilsExports.abortBackgroundTimers();
                        }
                        EXITSTATUS = _corerun_shutdown(EXITSTATUS || 0);
                        orig_funcs_on_exit();
                    };

                }, ...(Module.preInit || [])];

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
