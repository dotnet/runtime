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

