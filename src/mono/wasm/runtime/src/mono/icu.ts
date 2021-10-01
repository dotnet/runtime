// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from './cwraps'

let num_icu_assets_loaded_successfully = 0;

// @offset must be the address of an ICU data archive in the native heap.
// returns true on success.
export function mono_wasm_load_icu_data(offset: VoidPtr): boolean {
    var ok = (cwraps.mono_wasm_load_icu_data(offset)) === 1;
    if (ok)
        num_icu_assets_loaded_successfully++;
    return ok;
}

// Get icudt.dat exact filename that matches given culture, examples:
//   "ja" -> "icudt_CJK.dat"
//   "en_US" (or "en-US" or just "en") -> "icudt_EFIGS.dat"
// etc, see "mono_wasm_get_icudt_name" implementation in pal_icushim_static.c
export function mono_wasm_get_icudt_name(culture: string): string {
    return cwraps.mono_wasm_get_icudt_name(culture);
}


// Performs setup for globalization.
// @globalization_mode is one of "icu", "invariant", or "auto".
// "auto" will use "icu" if any ICU data archives have been loaded,
//  otherwise "invariant".
export function mono_wasm_globalization_init(globalization_mode: GlobalizationMode) {
    var invariantMode = false;

    if (globalization_mode === "invariant")
        invariantMode = true;

    if (!invariantMode) {
        if (num_icu_assets_loaded_successfully > 0) {
            console.debug("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");
        } else if (globalization_mode !== "icu") {
            console.debug("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
            invariantMode = true;
        } else {
            var msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
            console.error("MONO_WASM: ERROR: " + msg);
            throw new Error(msg);
        }
    }

    if (invariantMode)
        cwraps.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");

    // Set globalization mode to PredefinedCulturesOnly
    cwraps.mono_wasm_setenv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "1");
}

export const enum GlobalizationMode {
    ICU = "icu", // load ICU globalization data from any runtime assets with behavior "icu".
    INVARIANT = "invariant", //  operate in invariant globalization mode.
    AUTO = "auto" // (default): if "icu" behavior assets are present, use ICU, otherwise invariant.
}
