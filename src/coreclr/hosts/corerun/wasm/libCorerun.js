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
            auxiliaryModuleLoads: [],
            waitForAuxiliaryModules: async () => {
                await Promise.resolve();
                await Promise.all(CORERUN.auxiliaryModuleLoads);
            },
            installAuxiliaryModuleGates: () => {
                if (ENV["DOTNET_WasmAuxiliaryManualActivation"] !== "1") return;
                const releases = {};
                CORERUN.auxiliaryModuleGates = {};
                CORERUN.auxiliaryModuleCompletions = { ENTRYPOINTS: [], MODULES: [] };
                const requested = new Set();
                let preceding = Promise.resolve();
                for (const stage of ["ENTRYPOINTS", "MODULES"]) {
                    CORERUN.auxiliaryModuleGates[stage] = new Promise(resolve => releases[stage] = resolve);
                }
                const observeLine = message => {
                    for (const stage of ["ENTRYPOINTS", "MODULES"]) {
                        if (message.trim() === "AUXILIARY_R2R_ACTIVATE_" + stage && !requested.has(stage)) {
                            requested.add(stage);
                            const previous = preceding;
                            preceding = Promise.all(CORERUN.auxiliaryModuleCompletions[stage]);
                            // Console.WriteLine may still have native frames on the stack.
                            setTimeout(() => releases[stage](previous), 0);
                        }
                    }
                };
                const originalOut = out;
                out = message => {
                    originalOut(message);
                    observeLine(message);
                };
                const installWriteObserver = () => {
                    const write = FS.write;
                    const decoder = new TextDecoder();
                    let pending = "";
                    FS.write = (stream, buffer, offset, length, ...args) => {
                        const written = write.call(FS, stream, buffer, offset, length, ...args);
                        if (stream.fd === 1) {
                            const lines = (pending + decoder.decode(buffer.subarray(offset, offset + written), { stream: true })).split("\n");
                            pending = lines.pop();
                            lines.forEach(observeLine);
                        }
                        return written;
                    };
                };
                // NODERAWFS bypasses out, and replaces FS.write after library postsets run.
                const preRun = Module["preRun"];
                Module["preRun"] = typeof preRun === "function"
                    ? [preRun, installWriteObserver]
                    : [...preRun || [], installWriteObserver];
            },
            queueAuxiliaryModules: (instance, imports, payloadPtr, payloadSize, tableSize, wasmPath) => {
                if (instance.exports.getAuxiliaryModuleMetadata === undefined) return;
                if (typeof instance.exports.getAuxiliaryModuleMetadata !== "function") throw new Error("Invalid getAuxiliaryModuleMetadata export");
                const address = instance.exports.getAuxiliaryModuleMetadata() >>> 0;
                if (address === 0) return;
                const end = payloadPtr + payloadSize;
                const heap = new Uint8Array(wasmMemory.buffer);
                if (end > heap.length || address < payloadPtr || address % 4 !== 0 || address + 16 > end) {
                    throw new Error("Auxiliary R2R module metadata is outside the primary payload");
                }
                const view = new DataView(wasmMemory.buffer);
                const count = view.getUint32(address + 8, true);
                if (count > (end - address - 16) / 16) throw new Error("Truncated auxiliary R2R module metadata");
                const modules = [];
                const names = new Set();
                const addModule = (filenameRva, statusAddress, entrypoints) => {
                    const start = payloadPtr + filenameRva;
                    if (view.getUint32(statusAddress, true) > 1 || start >= end) throw new Error("Invalid auxiliary R2R module metadata");
                    let filenameEnd = start;
                    while (filenameEnd < end && heap[filenameEnd] !== 0) filenameEnd++;
                    if (filenameEnd === end) throw new Error("Unterminated auxiliary R2R module filename");
                    const name = new TextDecoder("utf-8", { fatal: true }).decode(heap.subarray(start, filenameEnd));
                    if (!/^[^./\\][^/\\?#:]*\.wasm$/.test(name) || names.has(name)) throw new Error(`Invalid auxiliary R2R module filename '${name}'`);
                    names.add(name);
                    modules.push({ name, statusAddress, entrypoints });
                };
                addModule(view.getUint32(address + 12, true), address, true);
                let nextTableIndex = view.getUint32(address + 4, true);
                if (nextTableIndex > tableSize) throw new Error("Invalid primary R2R module table range");
                for (let i = 0; i < count; i++) {
                    const entry = address + 16 + i * 16;
                    const tableBase = view.getUint32(entry + 4, true);
                    const methodCount = view.getUint32(entry + 8, true);
                    if (tableBase !== nextTableIndex || tableBase + methodCount > tableSize) {
                        throw new Error("Invalid auxiliary R2R module table range");
                    }
                    nextTableIndex += methodCount;
                    addModule(view.getUint32(entry, true), entry + 12, false);
                }
                // Native registration finishes after the synchronous probe returns. Never activate in the probe.
                const directory = wasmPath.substring(0, wasmPath.lastIndexOf("/") + 1);
                const operations = modules.map(module => Promise.resolve().then(async () => {
                    try {
                        if (new DataView(wasmMemory.buffer).getUint32(module.statusAddress, true) === 1) return;
                        if (CORERUN.auxiliaryModuleGates) {
                            await CORERUN.auxiliaryModuleGates[module.entrypoints ? "ENTRYPOINTS" : "MODULES"];
                        }
                        const delayName = module.entrypoints
                            ? "DOTNET_WasmAuxiliaryEntryPointsDelayMilliseconds"
                            : "DOTNET_WasmAuxiliaryModuleDelayMilliseconds";
                        const delay = Number(ENV[delayName] || 0);
                        if (!Number.isInteger(delay) || delay < 0 || delay > 2147483647) {
                            throw new Error(`Invalid ${delayName}: ${ENV[delayName]}`);
                        }
                        if (delay !== 0) await new Promise(resolve => setTimeout(resolve, delay));
                        const bytes = FS.readFile(directory + module.name);
                        const { instance } = await WebAssembly.instantiate(bytes, imports);
                        if (typeof instance.exports.ActivateModule !== "function") throw new Error("Missing ActivateModule export");
                        if (new DataView(wasmMemory.buffer).getUint32(module.statusAddress, true) === 0) {
                            instance.exports.ActivateModule();
                            if (new DataView(wasmMemory.buffer).getUint32(module.statusAddress, true) !== 1) {
                                throw new Error("ActivateModule did not publish its activation status");
                            }
                            if (module.entrypoints) {
                                if (typeof wasmExports.dotnet_wasm_notify_auxiliary_entrypoints_activated !== "function") {
                                    throw new Error("Runtime does not support auxiliary R2R modules");
                                }
                                wasmExports.dotnet_wasm_notify_auxiliary_entrypoints_activated();
                            }
                        }
                    } catch (reason) {
                        const error = new Error(`Failed to load auxiliary R2R module '${module.name}': ${reason instanceof Error ? reason.message : String(reason)}`);
                        err(error.message);
                        return error;
                    }
                }));
                if (CORERUN.auxiliaryModuleCompletions) {
                    for (let i = 0; i < modules.length; i++) {
                        CORERUN.auxiliaryModuleCompletions[modules[i].entrypoints ? "ENTRYPOINTS" : "MODULES"].push(operations[i]);
                    }
                }
                const completion = Promise.all(operations).then(errors => {
                    const error = errors.find(error => error !== undefined);
                    if (error) throw error;
                });
                CORERUN.auxiliaryModuleLoads.push(completion);
                void completion.catch(() => {});
            },
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
                Module["waitForAuxiliaryModules"] = CORERUN.waitForAuxiliaryModules;
                CORERUN.installAuxiliaryModuleGates();

                if (ENVIRONMENT_IS_NODE) {
                    const original_proc_exit = _proc_exit;
                    _proc_exit = (code) => {
                        if (!keepRuntimeAlive()) {
                            process.exit(code);
                        }
                        return original_proc_exit(code);
                    };
                }
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

                for (; ;) {
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
                console.error("Failed to construct WebAssembly module for Webcil image:", { wasmPath, errorMessage });
                throw new Error(`Failed to construct WebAssembly module for Webcil image '${wasmPath}': ${errorMessage}`);
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
                console.error("Failed to grow WebAssembly table for Webcil image:", { wasmPath, errorMessage });
                return false;
            }

            var payloadPtr = 0;
            var wasmInstance;
            var imports;
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
                imports = {
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
                };
                wasmInstance = new WebAssembly.Instance(wasmModule, imports);
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
            CORERUN.queueAuxiliaryModules(wasmInstance, imports, payloadPtr, payloadSize, tableSize, wasmPath);
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
