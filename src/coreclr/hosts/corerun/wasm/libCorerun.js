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
            function asUint8Array(bufferSource) {
                if (bufferSource instanceof ArrayBuffer) {
                    return new Uint8Array(bufferSource);
                }

                if (ArrayBuffer.isView(bufferSource)) {
                    return new Uint8Array(
                        bufferSource.buffer,
                        bufferSource.byteOffset,
                        bufferSource.byteLength);
                }

                throw new TypeError("Expected a BufferSource");
            }

            function readULEB128(bytes, offset, limit = bytes.length) {
                let value = 0;
                let shift = 0;

                for (;;) {
                    if (offset >= limit) {
                        throw new RangeError("Unexpected end of input while reading ULEB128");
                    }

                    const b = bytes[offset++];
                    value |= (b & 0x7f) << shift;

                    if ((b & 0x80) === 0) {
                        return { value, offset };
                    }

                    shift += 7;
                    if (shift >= 35) {
                        throw new RangeError("ULEB128 is too large for a u32");
                    }
                }
            }

            function readPassiveDataSegment(bytes, offset, limit) {
                if (offset >= limit) {
                    throw new RangeError("Unexpected end of input while reading data segment");
                }

                const mode = bytes[offset++];
                if (mode !== 1) {
                    throw new Error("Data segment is not passive");
                }

                const lenInfo = readULEB128(bytes, offset, limit);
                const dataLength = lenInfo.value;
                const dataStart = lenInfo.offset;
                const dataEnd = dataStart + dataLength;

                if (dataEnd > limit) {
                    throw new RangeError("Data segment payload extends past section bounds");
                }

                return {
                    dataStart,
                    dataLength,
                    offset: dataEnd
                };
            }

            function readPayloadSizeAndTableSize(bufferSource) {
                const bytes = asUint8Array(bufferSource);

                if (bytes.length < 8) {
                    throw new Error("Not a valid wasm module");
                }

                const headerView = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
                const wasmMagic = 0x6d736100;
                const magic = headerView.getUint32(0, true);
                const version = headerView.getUint32(4, true);

                if (magic !== wasmMagic) {
                    throw new Error("Invalid wasm magic");
                }

                if (version !== 1) {
                    throw new Error(`Unsupported wasm version: ${version}`);
                }

                let offset = 8;

                while (offset < bytes.length) {
                    const sectionCode = bytes[offset++];
                    const sizeInfo = readULEB128(bytes, offset);
                    const sectionSize = sizeInfo.value;
                    const sectionStart = sizeInfo.offset;
                    const sectionEnd = sectionStart + sectionSize;

                    if (sectionEnd > bytes.length) {
                        throw new RangeError("Section extends past end of module");
                    }

                    if (sectionCode === 11) {
                        const countInfo = readULEB128(bytes, sectionStart, sectionEnd);
                        const segmentCount = countInfo.value;

                        if (segmentCount < 1) {
                            throw new Error("Wasm data section has no segments");
                        }

                        const segment0 = readPassiveDataSegment(bytes, countInfo.offset, sectionEnd);

                        if (segment0.dataLength < 4) {
                            throw new Error("Data segment 0 is shorter than 4 bytes");
                        }

                        const valueView = new DataView(bytes.buffer, bytes.byteOffset + segment0.dataStart, segment0.dataLength);
                        const payloadSize = valueView.getUint32(0, true);
                        const tableSize = segment0.dataLength >= 8 ? valueView.getUint32(4, true) : 0;

                        return {
                            payloadSize,
                            tableSize
                        };
                    }

                    offset = sectionEnd;
                }

                throw new Error("Wasm data section not found");
            }

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
            let wasmModule;
            try {
                wasmModule = new WebAssembly.Module(wasmBytes);
            } catch (e) {
                const errorMessage = e instanceof Error ? e.message : String(e);
                console.error("Failed to construct WebAssembly module for Webcil image:", {wasmPath, errorMessage});
                return false;
            }

            const tableStartIndex = wasmTable.length;

            var payloadSize = 0;
            var tableSize = 0;
            try {
                const sizes = readPayloadSizeAndTableSize(wasmBytes);
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
                console.error("Failed to grow WebAssembly table for Webcil image:", {wasmPath, errorMessage});
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
                payloadPtr = HEAPU32[ptrPtr >>> 2 >>> 0];
                wasmInstance = new WebAssembly.Instance(wasmModule, {
                    webcil: {
                        memory: wasmMemory,
                        stackPointer: wasmExports.__stack_pointer,
                        table: wasmTable,
                        tableBase: new WebAssembly.Global({ value: "i32", mutable: false }, tableStartIndex),
                        imageBase: new WebAssembly.Global({ value: "i32", mutable: false }, payloadPtr)
                    }});
            } finally {
                stackRestore(sp);
            }

            const webcilVersion = wasmInstance.exports.webcilVersion.value;
            if ((webcilVersion > 1) || (webcilVersion < 0)) {
                throw new Error(`Unsupported Webcil version: ${webcilVersion}`);
            }

            wasmInstance.exports.getWebcilPayload(payloadPtr, payloadSize);
            if (tableSize > 0) {
                wasmInstance.exports.fillWebcilTable();
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
