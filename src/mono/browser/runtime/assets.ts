// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import type { AssetEntryInternal } from "./types/internal";

import cwraps from "./cwraps";
import { mono_wasm_load_icu_data } from "./icu";
import { Module, loaderHelpers, mono_assert, runtimeHelpers } from "./globals";
import { mono_log_info, mono_log_debug, parseSymbolMapFile } from "./logging";
import { mono_wasm_load_bytes_into_heap } from "./memory";
import { endMeasure, MeasuredBlock, startMeasure } from "./profiler";
import { AssetEntry } from "./types";
import { VoidPtr } from "./types/emscripten";
import { setSegmentationRulesFromJson } from "./hybrid-globalization/grapheme-segmenter";

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
        case "segmentation-rules":
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

export async function instantiate_symbols_asset(pendingAsset: AssetEntryInternal): Promise<void> {
    try {
        const response = await pendingAsset.pendingDownloadInternal!.response;
        const text = await response.text();
        parseSymbolMapFile(text);
    } catch (error: any) {
        mono_log_info(`Error loading symbol file ${pendingAsset.name}: ${JSON.stringify(error)}`);
    }
}

export async function instantiate_segmentation_rules_asset(pendingAsset: AssetEntryInternal): Promise<void> {
    try {
        const response = await pendingAsset.pendingDownloadInternal!.response;
        const json = await response.json();
        setSegmentationRulesFromJson(json);
    } catch (error: any) {
        mono_log_info(`Error loading static json asset ${pendingAsset.name}: ${JSON.stringify(error)}`);
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