//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

/* eslint-disable no-undef */
function libCoreRunFactory() {
    let commonDeps = [
        "$DOTNET",
        "$ENV",
        "$FS",
        "corerun_shutdown",
        "__stack_pointer",
        "__async_continuation",
        "$UTF8ToString",
        "$stringToNewUTF8"
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
                CORERUN.bundleDirectories = new Map();
                CORERUN.bundledAssemblies = new Map();
                const browserVirtualAppBase = "/";// keep in sync other places that define browserVirtualAppBase
                FS.createPath("/", browserVirtualAppBase, true, true);

                // copy all node/shell env variables to emscripten env
                if (globalThis.process && globalThis.process.env) {
                    for (const [key, value] of Object.entries(process.env)) {
                        ENV[key] = value;
                    }
                }

                ENV["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "true";

                if (ENVIRONMENT_IS_NODE) {
                    // V8 formats every wasm frame as "<url>:wasm-function[<index>]:<offset>".
                    const isWasmOnStack = () => {
                        const savedStackTraceLimit = Error.stackTraceLimit;
                        try {
                            Error.stackTraceLimit = Infinity;
                            return /wasm-function\[/.test(new Error().stack || "");
                        } finally {
                            Error.stackTraceLimit = savedStackTraceLimit;
                        }
                    };

                    // An exit requested from inside wasm (for example Environment.Exit) must not unwind
                    // the ExitStatus exception through live CLR frames, so end the process immediately.
                    // Otherwise let Node.js exit on its own once the event loop drains: process.exit()
                    // joins V8's worker threads without disposing the isolate, which can deadlock when
                    // a background compile job is waiting for a main thread GC (nodejs/node#64274).
                    const original_proc_exit = _proc_exit;
                    _proc_exit = (code) => {
                        if (!keepRuntimeAlive() && isWasmOnStack()) {
                            process.exit(code);
                        }
                        return original_proc_exit(code);
                    };

                    // SystemJS_ResolveMainPromise queues an exit that can run after the runtime already
                    // exited, now that the process outlives that exit until the event loop drains.
                    const original_exit = _exit;
                    _exit = (status, implicit) => {
                        if (runtimeExited) {
                            throw new ExitStatus(EXITSTATUS);
                        }
                        return original_exit(status, implicit);
                    };
                }
            },
            readWebcilPayloadSizes: (bytes) => {
                function readULEB128(offset, limit = bytes.length) {
                    let value = 0;
                    let shift = 0;

                    for (; ;) {
                        if (offset >= limit) {
                            throw new RangeError("Unexpected end of input while reading ULEB128");
                        }

                        const b = bytes[offset++];
                        if (shift === 28 && (b & 0xf0) !== 0) {
                            throw new RangeError("ULEB128 is too large for a u32");
                        }
                        value |= (b & 0x7f) << shift;

                        if ((b & 0x80) === 0) {
                            return { value: value >>> 0, offset };
                        }

                        shift += 7;
                        if (shift >= 35) {
                            throw new RangeError("ULEB128 is too large for a u32");
                        }
                    }
                }

                if (bytes.length < 8) {
                    throw new Error("Not a valid wasm module");
                }

                const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
                if (view.getUint32(0, true) !== 0x6d736100) {
                    throw new Error("Invalid wasm magic");
                }

                const version = view.getUint32(4, true);
                if (version !== 1) {
                    throw new Error(`Unsupported wasm version: ${version}`);
                }

                let offset = 8;
                while (offset < bytes.length) {
                    const sectionCode = bytes[offset++];
                    const sizeInfo = readULEB128(offset);
                    const sectionStart = sizeInfo.offset;
                    const sectionEnd = sectionStart + sizeInfo.value;
                    if (sectionEnd > bytes.length) {
                        throw new RangeError("Section extends past end of module");
                    }

                    if (sectionCode === 11) {
                        const countInfo = readULEB128(sectionStart, sectionEnd);
                        if (countInfo.value < 1 || countInfo.offset >= sectionEnd || bytes[countInfo.offset] !== 1) {
                            throw new Error("Webcil data section has no passive size segment");
                        }

                        const sizeSegment = readULEB128(countInfo.offset + 1, sectionEnd);
                        const sizeSegmentEnd = sizeSegment.offset + sizeSegment.value;
                        if (sizeSegment.value < 4 || sizeSegmentEnd > sectionEnd) {
                            throw new Error("Invalid Webcil size segment");
                        }

                        const payloadSize = view.getUint32(sizeSegment.offset, true);
                        const tableSize = sizeSegment.value >= 8 ? view.getUint32(sizeSegment.offset + 4, true) : 0;
                        let payloadMagic;
                        let payloadOffset = sizeSegmentEnd;
                        if (countInfo.value >= 2 && payloadOffset + 2 <= sectionEnd &&
                            bytes[payloadOffset++] === 0 && bytes[payloadOffset++] === 0x23) {
                            const globalIndex = readULEB128(payloadOffset, sectionEnd);
                            payloadOffset = globalIndex.offset;
                            if (payloadOffset < sectionEnd && bytes[payloadOffset++] === 0x0b) {
                                const payload = readULEB128(payloadOffset, sectionEnd);
                                if (payload.value >= 4 && payload.value <= sectionEnd - payload.offset) {
                                    payloadMagic = view.getUint32(payload.offset, true);
                                }
                            }
                        }
                        return { payloadSize, tableSize, payloadMagic };
                    }

                    offset = sectionEnd;
                }

                throw new Error("Wasm data section not found");
            },
            getWebcilBundleAssemblyPaths: (directory) => {
                const directoryInfo = FS.analyzePath(directory);
                if (!directoryInfo.exists || !FS.isDir(directoryInfo.object.mode)) {
                    return [];
                }
                const resolvedDirectory = directoryInfo.path.replace(/\/?$/, "/");
                const cached = CORERUN.bundleDirectories.get(resolvedDirectory);
                if (cached) {
                    return cached;
                }

                const paths = [];
                for (const file of FS.readdir(resolvedDirectory).filter(name => name.endsWith(".components.wasm"))) {
                    const bundlePath = resolvedDirectory + file;
                    const wasmBytes = FS.readFile(bundlePath);
                    const wasmModule = new WebAssembly.Module(wasmBytes);
                    if (!WebAssembly.Module.exports(wasmModule).some(exported => exported.name === "webcilVersion" && exported.kind === "global")) {
                        continue;
                    }
                    const sizes = CORERUN.readWebcilPayloadSizes(wasmBytes);
                    if (sizes.payloadMagic === 0x4c496257) {
                        continue;
                    }
                    if (sizes.tableSize !== 0) {
                        throw new Error(`Webcil bundle '${bundlePath}' cannot require a function table.`);
                    }
                    const sp = stackSave();
                    let pathPtr = 0;
                    let payloadPtr;
                    let payloadSize;
                    try {
                        CORERUN.preloadedBundle = { path: bundlePath, bytes: wasmBytes, module: wasmModule };
                        pathPtr = stringToNewUTF8(bundlePath);
                        const resultPtr = stackAlloc(12);
                        if (!_BrowserHost_ExternalAssemblyProbe(pathPtr, resultPtr, resultPtr + 4)) {
                            throw new Error(`Failed to load Webcil bundle '${bundlePath}'.`);
                        }
                        payloadPtr = HEAPU32[resultPtr >>> 2];
                        payloadSize = HEAPU32[(resultPtr + 4) >>> 2];
                    } finally {
                        CORERUN.preloadedBundle = undefined;
                        if (pathPtr) _free(pathPtr);
                        stackRestore(sp);
                    }

                    const invalid = () => new Error(`Invalid Webcil bundle '${bundlePath}'.`);
                    if (payloadSize < 20) throw invalid();
                    const view = new DataView(HEAPU8.buffer, payloadPtr, payloadSize);
                    if (view.getUint32(0, true) !== 0x42496257 || view.getUint16(4, true) !== 1 || view.getUint16(6, true) !== 0) throw invalid();
                    const count = view.getUint32(8, true);
                    const stringsOffset = view.getUint32(12, true);
                    const stringsSize = view.getUint32(16, true);
                    if (count > Math.floor((payloadSize - 20) / 16) ||
                        stringsOffset < 20 + count * 16 || stringsOffset > payloadSize ||
                        stringsSize > payloadSize - stringsOffset) throw invalid();

                    const decoder = new TextDecoder("utf-8", { fatal: true });
                    const entries = [];
                    const names = new Set();
                    for (let i = 0; i < count; i++) {
                        const offset = 20 + i * 16;
                        const nameOffset = view.getUint32(offset, true);
                        const nameLength = view.getUint32(offset + 4, true);
                        const imageOffset = view.getUint32(offset + 8, true);
                        const imageSize = view.getUint32(offset + 12, true);
                        if (nameLength === 0 || nameOffset > stringsSize || nameLength > stringsSize - nameOffset ||
                            imageOffset % 16 !== 0 || imageOffset < stringsOffset + stringsSize ||
                            imageOffset > payloadSize || imageSize < 32 || imageSize > payloadSize - imageOffset ||
                            view.getUint32(imageOffset, true) !== 0x4c496257 ||
                            view.getUint16(imageOffset + 4, true) !== 1 || view.getUint16(imageOffset + 6, true) !== 0 ||
                            view.getUint32(imageOffset + 28, true) !== 0) throw invalid();
                        const name = decoder.decode(new Uint8Array(HEAPU8.buffer, payloadPtr + stringsOffset + nameOffset, nameLength));
                        if (!name.endsWith(".dll") || name.includes("\\") || name.split("").some(character => character.charCodeAt(0) < 0x20) ||
                            name.split("/").some(part => part === "" || part === "." || part === "..")) throw invalid();
                        const shortName = name.substring(name.lastIndexOf("/") + 1).toLowerCase();
                        if (names.has(shortName) || CORERUN.bundledAssemblies.has(resolvedDirectory + name)) throw invalid();
                        names.add(shortName);
                        entries.push({ name, offset: imageOffset, size: imageSize });
                    }
                    const byOffset = [...entries].sort((a, b) => a.offset - b.offset);
                    for (let i = 1; i < byOffset.length; i++) {
                        if (byOffset[i - 1].size > byOffset[i].offset - byOffset[i - 1].offset) throw invalid();
                    }
                    for (const entry of entries) {
                        const path = resolvedDirectory + entry.name;
                        CORERUN.bundledAssemblies.set(path, { ptr: payloadPtr + entry.offset, length: entry.size });
                        paths.push(path);
                    }
                }

                CORERUN.bundleDirectories.set(resolvedDirectory, paths);
                return paths;
            },
        },
        $CORERUN__postset: "CORERUN.selfInitialize()",
        $CORERUN__deps: commonDeps,
        BrowserHost_ShutdownDotnet: (exitCode) => _corerun_shutdown(exitCode),
        BrowserHost_GetWebcilBundleAssemblyPaths__deps: ["BrowserHost_ExternalAssemblyProbe"],
        BrowserHost_GetWebcilBundleAssemblyPaths: (directoryPtr) =>
            stringToNewUTF8(CORERUN.getWebcilBundleAssemblyPaths(UTF8ToString(directoryPtr)).join("\n")),
        BrowserHost_ExternalAssemblyProbe: (pathPtr, outDataStartPtr, outSize) => {
            const path = UTF8ToString(pathPtr);
            const bundled = CORERUN.bundledAssemblies.get(path);
            if (bundled) {
                HEAPU32[outDataStartPtr >>> 2] = bundled.ptr;
                HEAPU32[outSize >>> 2] = bundled.length;
                HEAPU32[(outSize + 4) >>> 2] = 0;
                return true;
            }
            let wasmPath;
            if (path.endsWith(".dll")) {
                wasmPath = path.slice(0, -4) + ".wasm";
            } else if (path.endsWith(".wasm")) {
                wasmPath = path;
            } else {
                return false;
            }
            let wasmBytes;
            let wasmModule;
            if (CORERUN.preloadedBundle?.path === wasmPath) {
                wasmBytes = CORERUN.preloadedBundle.bytes;
                wasmModule = CORERUN.preloadedBundle.module;
            } else {
                try {
                    wasmBytes = FS.readFile(wasmPath);
                } catch (e) {
                    return false;
                }
                try {
                    wasmModule = new WebAssembly.Module(wasmBytes);
                } catch (e) {
                    const errorMessage = e instanceof Error ? e.message : String(e);
                    console.error("Failed to construct WebAssembly module for Webcil image:", { wasmPath, errorMessage });
                    throw new Error(`Failed to construct WebAssembly module for Webcil image '${wasmPath}': ${errorMessage}`);
                }
            }

            const tableStartIndex = wasmTable.length;

            var payloadSize = 0;
            var tableSize = 0;
            try {
                const sizes = CORERUN.readWebcilPayloadSizes(wasmBytes);
                payloadSize = sizes.payloadSize;
                tableSize = sizes.tableSize;
            } catch (e) {
                console.error("Failed to read Webcil payload size from wasm data section:", e);
                return false;
            }
            if (payloadSize === 0) {
                console.error("Webcil payload size is 0; cannot load image");
                return false;
            }

            try {
                wasmTable.grow(tableSize);
            } catch (e) {
                const errorMessage = e instanceof Error ? e.message : String(e);
                console.error("Failed to grow WebAssembly table for Webcil image:", { wasmPath, errorMessage });
                return false;
            }

            var payloadPtr = 0;
            var wasmInstance;
            const sp = stackSave();
            try {
                const ptrPtr = stackAlloc(4);
                if (_posix_memalign(ptrPtr, 16, payloadSize)) {
                    throw new Error("posix_memalign failed for Webcil payload");
                }
                if (typeof (wasmExports.__stack_pointer) === "undefined") {
                    throw new Error("__stack_pointer was not preserved by the linker or optimizer");
                }
                if (typeof (wasmExports.__coreclr_wasm_rtlrestorecontext_tag) === "undefined") {
                    throw new Error("__coreclr_wasm_rtlrestorecontext_tag was not preserved by the linker or optimizer");
                }
                if (typeof (wasmExports.__async_continuation) === "undefined") {
                    throw new Error("__async_continuation was not preserved by the linker or optimizer");
                }
                payloadPtr = HEAPU32[ptrPtr >>> 2 >>> 0];
                wasmInstance = new WebAssembly.Instance(wasmModule, {
                    webcil: {
                        memory: wasmMemory,
                        __stack_pointer: wasmExports.__stack_pointer,
                        __coreclr_wasm_rtlrestorecontext_tag: wasmExports.__coreclr_wasm_rtlrestorecontext_tag,
                        __indirect_function_table: wasmTable,
                        __table_base: new WebAssembly.Global({ value: "i32", mutable: false }, tableStartIndex),
                        __memory_base: new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr),
                        // Runtime-async continuation return value, shared with the runtime module.
                        __async_continuation: wasmExports.__async_continuation
                    }
                });
            } catch (e) {
                const errorMessage = e instanceof Error ? e.message : String(e);
                console.error("Failed to construct WebAssembly instance for Webcil image:", { wasmPath, errorMessage });
                return false;
            } finally {
                stackRestore(sp);
            }

            // Only self-installing wrappers are supported (keep in sync with
            // WebcilConstants.WASM_WRAPPER_VERSION_SELF_INSTALLING): the engine has already installed the
            // payload, and any table slice, from the module's active segments.
            const webcilVersion = wasmInstance.exports.webcilVersion.value;
            if (webcilVersion !== 2) {
                throw new Error(`Webcil image '${wasmPath}' has unsupported Webcil wrapper version ${webcilVersion}; expected 2`);
            }

            if (tableSize > 0) {
                // The header's tableBase field lives in linear memory, so no segment can supply it.
                if (typeof (wasmInstance.exports.patchWebcilHeader) !== "function") {
                    throw new Error(`Webcil R2R image '${wasmPath}' does not export patchWebcilHeader`);
                }
                wasmInstance.exports.patchWebcilHeader(payloadPtr, payloadSize);
            }

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
