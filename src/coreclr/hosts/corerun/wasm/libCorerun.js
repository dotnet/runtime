//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
function libCoreRunFactory() {
    let commonDeps = [
        "$DOTNET",
        "$ENV",
        "$FS",
        "corerun_shutdown",
        "$UTF8ToString"
    ];
    if (LibraryManager.library.$NODEFS) {
        commonDeps.push("$NODEFS");
    }
    if (LibraryManager.library.$NODERAWFS) {
        commonDeps.push("$NODERAWFS");
    }
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
            },
        },
        $CORERUN__postset: "CORERUN.selfInitialize()",
        $CORERUN__deps: commonDeps,
        BrowserHost_ShutdownDotnet: (exitCode) => _corerun_shutdown(exitCode),
        BrowserHost_ExternalAssemblyProbe: (pathPtr, outDataStartPtr, outSize) => {
            const path = UTF8ToString(pathPtr);
            let wasmPath;
            if (path.endsWith('.dll')) {
                wasmPath = path.slice(0, -4) + '.wasm';
            } else if (path.endsWith('.wasm')) {
                wasmPath = path;
            } else {
                return false;
            }

            let wasmBytes;
            try {
                wasmBytes = FS.readFile(wasmPath);
            } catch (e) {
                return false;
            }

            // Synchronously instantiate the webcil WebAssembly module
            const wasmModule = new WebAssembly.Module(wasmBytes);
            const wasmInstance = new WebAssembly.Instance(wasmModule, {
                webcil: { memory: wasmMemory }
            });

            const webcilVersion = wasmInstance.exports.webcilVersion.value;
            if (webcilVersion !== 0) {
                throw new Error(`Unsupported Webcil version: ${webcilVersion}`);
            }

            const sp = stackSave();
            try {
                const sizePtr = stackAlloc(4);
                wasmInstance.exports.getWebcilSize(sizePtr);
                const payloadSize = HEAPU32[sizePtr >>> 2];
                if (payloadSize === 0) {
                    throw new Error("Webcil payload size is 0");
                }

                const ptrPtr = stackAlloc(4);
                if (_posix_memalign(ptrPtr, 16, payloadSize)) {
                    throw new Error("posix_memalign failed for Webcil payload");
                }
                const payloadPtr = HEAPU32[ptrPtr >>> 2];

                wasmInstance.exports.getWebcilPayload(payloadPtr, payloadSize);

                // Write out parameters: void** data_start, int64_t* size
                HEAPU32[outDataStartPtr >>> 2] = payloadPtr;
                HEAPU32[outSize >>> 2] = payloadSize;
                HEAPU32[(outSize + 4) >>> 2] = 0;

                return true;
            } finally {
                stackRestore(sp);
            }
        }
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
