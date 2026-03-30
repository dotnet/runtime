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
            if (path.endsWith(".dll")) {
                wasmPath = path.slice(0, -4) + ".wasm";
            } else if (path.endsWith(".wasm")) {
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
            const wasmModule = new WebAssembly.Module(wasmBytes);
            const tableStartIndex = wasmTable.length;

            var payloadSize = 0;
            const sections = WebAssembly.Module.customSections(wasmModule, "webcilMetadata");

            if (sections !== null && sections.length === 1) {
                const sectionBuffer = sections[0];

                if (sectionBuffer.byteLength >= 8) {
                    const view = new DataView(sectionBuffer);
                    const version = view.getUint32(0, true);
                    const size = view.getUint32(4, true);
                    if (version >= 1) {
                        payloadSize = size;
                    }
                }
            }

            var payloadPtr = 0;
            var wasmInstance;
            if (payloadSize > 0) {
                // webcil version 1. This format allows the payload to be allocated before loading the webcilModule, as well as allowing the module to access the stack/table/memory from code
                const sp = stackSave();
                try {
                    const ptrPtr = stackAlloc(4);
                    if (_posix_memalign(ptrPtr, 16, payloadSize)) {
                        throw new Error("posix_memalign failed for Webcil payload");
                    }
                    payloadPtr = HEAPU32[ptrPtr >>> 2 >>> 0];
                    wasmInstance = new WebAssembly.Instance(wasmModule, {
                        webcil: {
                            memory: wasmMemory,
                            stackPointer: ___stack_pointer,
                            table: wasmTable,
                            tableBase: new WebAssembly.Global({ value: "i32", mutable: false }, tableStartIndex),
                            imageBase: new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr)
                        }});
                } finally {
                    stackRestore(sp);
                }

                const webcilVersion = wasmInstance.exports.webcilVersion.value;
                if (webcilVersion !== 1) {
                    throw new Error(`Unsupported Webcil version: ${webcilVersion}`);
                }
            } else {
                // webcil version 0. This format requires the webcilModule to be loaded before the payload can be allocated. Suitable only as a container format for interpreter execution
                wasmInstance = new WebAssembly.Instance(wasmModule, {
                webcil: {
                    memory: wasmMemory
                }});
                const webcilVersion = wasmInstance.exports.webcilVersion.value;
                if (webcilVersion !== 0) {
                    throw new Error(`Unsupported Webcil version: ${webcilVersion}`);
                }
                const sp = stackSave();
                try {
                    const sizePtr = stackAlloc(4);
                    wasmInstance.exports.getWebcilSize(sizePtr);
                    const payloadSize = HEAPU32[sizePtr >>> 2 >>> 0];
                    if (payloadSize === 0) {
                        throw new Error("Webcil payload size is 0");
                    }
                    const ptrPtr = stackAlloc(4);
                    if (_posix_memalign(ptrPtr, 16, payloadSize)) {
                        throw new Error("posix_memalign failed for Webcil payload");
                    }
                    payloadPtr = HEAPU32[ptrPtr >>> 2 >>> 0];
                } finally {
                    stackRestore(sp);
                }
            }

            wasmInstance.exports.getWebcilPayload(payloadPtr, payloadSize);
            HEAPU32[outDataStartPtr >>> 2 >>> 0] = payloadPtr;
            HEAPU32[outSize >>> 2 >>> 0] = payloadSize;
            HEAPU32[(outSize + 4) >>> 2 >>> 0] = 0;
            return true;
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
