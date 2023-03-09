// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { Module, runtimeHelpers } from "./imports";
import { VoidPtr } from "./types/emscripten";

let num_icu_assets_loaded_successfully = 0;

// @offset must be the address of an ICU data archive in the native heap.
// returns true on success.
export function mono_wasm_load_icu_data(offset: VoidPtr): boolean {
    const ok = (cwraps.mono_wasm_load_icu_data(offset)) === 1;
    if (ok)
        num_icu_assets_loaded_successfully++;
    return ok;
}

// Performs setup for globalization.
// @globalizationMode is one of "icu", "invariant", or "auto".
// "auto" will use "icu" if any ICU data archives have been loaded,
//  otherwise "invariant".
export function mono_wasm_globalization_init(): void {
    const config = runtimeHelpers.config;
    let invariantMode = false;
    if (!config.globalizationMode)
        config.globalizationMode = "auto";
    if (config.globalizationMode === "invariant")
        invariantMode = true;

    if (!invariantMode) {
        if (num_icu_assets_loaded_successfully > 0) {
            if (runtimeHelpers.diagnosticTracing) {
                console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");
            }
        } else if (config.globalizationMode !== "icu") {
            if (runtimeHelpers.diagnosticTracing) {
                console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
            }
            invariantMode = true;
        } else {
            const msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
            Module.printErr(`MONO_WASM: ERROR: ${msg}`);
            throw new Error(msg);
        }
    }

    if (invariantMode) {
        cwraps.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
    }
}

export function get_preferred_icu_asset(): string | null {
    if (!runtimeHelpers.config.assets)
        return null;

    // By setting <WasmIcuDataFileName> user can define what ICU source file they want to load.
    // There is no need to check application's culture when <WasmIcuDataFileName> is set.
    // If it was not set, then we have 3 "icu" assets in config and we should choose
    // only one for loading, the one that matches the application's locale.
    const icuAssets = runtimeHelpers.config.assets.filter(a => a["behavior"] == "icu");
    if (icuAssets.length === 1)
        return icuAssets[0].name;

    // reads the browsers locale / the OS's locale
    const preferredCulture = ENVIRONMENT_IS_WEB ? navigator.language : Intl.DateTimeFormat().resolvedOptions().locale;
    const prefix = preferredCulture.split("-")[0];
    const CJK = "icudt_CJK.dat";
    const EFIGS = "icudt_EFIGS.dat";
    const OTHERS = "icudt_no_CJK.dat";

    // not all "fr-*", "it-*", "de-*", "es-*" are in EFIGS, only the one that is mostly used
    if (prefix == "en" || ["fr", "fr-FR", "it", "it-IT", "de", "de-DE", "es", "es-ES"].includes(preferredCulture))
        return EFIGS;
    if (["zh", "ko", "ja"].includes(prefix))
        return CJK;
    return OTHERS;
}
