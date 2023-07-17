// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { mono_wasm_load_icu_data } from "./icu";
import { ENVIRONMENT_IS_SHELL, ENVIRONMENT_IS_WEB, Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { mono_log_info, mono_log_debug, mono_log_warn, parseSymbolMapFile } from "./logging";
import { mono_wasm_load_bytes_into_heap } from "./memory";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { AssetEntryInternal } from "./types/internal";
import { AssetEntry } from "./types";
import { InstantiateWasmSuccessCallback, VoidPtr } from "./types/emscripten";

// this need to be run only after onRuntimeInitialized event, when the memory is ready
export function instantiate_asset(asset: AssetEntry, url: string, bytes: Uint8Array): void {
    mono_log_debug(`Loaded:${asset.name} as ${asset.behavior} size ${bytes.length} from ${url}`);
    const mark = startMeasure();

    const virtualName: string = typeof (asset.virtualPath) === "string"
        ? asset.virtualPath
        : asset.name;
    let offset: VoidPtr | null = null;

    switch (asset.behavior) {
        case "dotnetwasm":
        case "js-module-threads":
        case "symbols":
            // do nothing
            break;
        case "resource":
        case "assembly":
        case "pdb":
            loaderHelpers._loaded_files.push({ url: url, file: virtualName });
        // falls through
        case "heap":
        case "icu":
            offset = mono_wasm_load_bytes_into_heap(bytes);
            break;

        case "vfs": {
            // FIXME
            const lastSlash = virtualName.lastIndexOf("/");
            let parentDirectory = (lastSlash > 0)
                ? virtualName.substr(0, lastSlash)
                : null;
            let fileName = (lastSlash > 0)
                ? virtualName.substr(lastSlash + 1)
                : virtualName;
            if (fileName.startsWith("/"))
                fileName = fileName.substr(1);
            if (parentDirectory) {
                mono_log_debug(`Creating directory '${parentDirectory}'`);

                Module.FS_createPath(
                    "/", parentDirectory, true, true // fixme: should canWrite be false?
                );
            } else {
                parentDirectory = "/";
            }

            mono_log_debug(`Creating file '${fileName}' in directory '${parentDirectory}'`);

            Module.FS_createDataFile(
                parentDirectory, fileName,
                bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
            );
            break;
        }
        default:
            throw new Error(`Unrecognized asset behavior:${asset.behavior}, for asset ${asset.name}`);
    }

    if (asset.behavior === "assembly") {
        // this is reading flag inside the DLL about the existence of PDB
        // it doesn't relate to whether the .pdb file is downloaded at all
        const hasPdb = cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);

        if (!hasPdb) {
            const index = loaderHelpers._loaded_files.findIndex(element => element.file == virtualName);
            loaderHelpers._loaded_files.splice(index, 1);
        }
    }
    else if (asset.behavior === "pdb") {
        cwraps.mono_wasm_add_assembly(virtualName, offset!, bytes.length);
    }
    else if (asset.behavior === "icu") {
        if (!mono_wasm_load_icu_data(offset!))
            Module.err(`Error loading ICU asset ${asset.name}`);
    }
    else if (asset.behavior === "resource") {
        cwraps.mono_wasm_add_satellite_assembly(virtualName, asset.culture || "", offset!, bytes.length);
    }
    endMeasure(mark, MeasuredBlock.instantiateAsset, asset.name);
    ++loaderHelpers.actual_instantiated_assets_count;
}

export async function instantiate_wasm_asset(
    pendingAsset: AssetEntryInternal,
    wasmModuleImports: WebAssembly.Imports,
    successCallback: InstantiateWasmSuccessCallback,
): Promise<void> {
    mono_assert(pendingAsset && pendingAsset.pendingDownloadInternal && pendingAsset.pendingDownloadInternal.response, "Can't load dotnet.native.wasm");
    const response = await pendingAsset.pendingDownloadInternal.response;
    const contentType = response.headers && response.headers.get ? response.headers.get("Content-Type") : undefined;
    let compiledInstance: WebAssembly.Instance;
    let compiledModule: WebAssembly.Module;
    if (typeof WebAssembly.instantiateStreaming === "function" && contentType === "application/wasm") {
        mono_log_debug("instantiate_wasm_module streaming");
        const streamingResult = await WebAssembly.instantiateStreaming(response, wasmModuleImports!);
        compiledInstance = streamingResult.instance;
        compiledModule = streamingResult.module;
    } else {
        if (ENVIRONMENT_IS_WEB && contentType !== "application/wasm") {
            mono_log_warn("WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.");
        }
        const arrayBuffer = await response.arrayBuffer();
        mono_log_debug("instantiate_wasm_module buffered");
        if (ENVIRONMENT_IS_SHELL) {
            // workaround for old versions of V8 with https://bugs.chromium.org/p/v8/issues/detail?id=13823
            compiledModule = new WebAssembly.Module(arrayBuffer);
            compiledInstance = new WebAssembly.Instance(compiledModule, wasmModuleImports);
        } else {
            const arrayBufferResult = await WebAssembly.instantiate(arrayBuffer, wasmModuleImports!);
            compiledInstance = arrayBufferResult.instance;
            compiledModule = arrayBufferResult.module;
        }
    }
    successCallback(compiledInstance, compiledModule);
}

export async function instantiate_symbols_asset(pendingAsset: AssetEntryInternal): Promise<void> {
    try {
        const response = await pendingAsset.pendingDownloadInternal!.response;
        const text = await response.text();
        parseSymbolMapFile(text);
    } catch (error: any) {
        mono_log_info(`Error loading symbol file ${pendingAsset.name}: ${JSON.stringify(error)}`);
    }
}

export async function wait_for_all_assets() {
    // wait for all assets in memory
    await runtimeHelpers.allAssetsInMemory.promise;
    if (runtimeHelpers.config.assets) {
        mono_assert(loaderHelpers.actual_downloaded_assets_count == loaderHelpers.expected_downloaded_assets_count, () => `Expected ${loaderHelpers.expected_downloaded_assets_count} assets to be downloaded, but only finished ${loaderHelpers.actual_downloaded_assets_count}`);
        mono_assert(loaderHelpers.actual_instantiated_assets_count == loaderHelpers.expected_instantiated_assets_count, () => `Expected ${loaderHelpers.expected_instantiated_assets_count} assets to be in memory, but only instantiated ${loaderHelpers.actual_instantiated_assets_count}`);
        loaderHelpers._loaded_files.forEach(value => loaderHelpers.loadedFiles.push(value.url));
        mono_log_debug("all assets are loaded in wasm memory");
    }
}

// Used by the debugger to enumerate loaded dlls and pdbs
export function mono_wasm_get_loaded_files(): string[] {
    return loaderHelpers.loadedFiles;
}