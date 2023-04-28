// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { ENVIRONMENT_IS_WEB, Module, runtimeHelpers } from "./globals";
import { VoidPtr } from "./types/emscripten";

// @offset must be the address of an ICU data archive in the native heap.
// returns true on success.
export function mono_wasm_load_icu_data(offset: VoidPtr): boolean {
    return (cwraps.mono_wasm_load_icu_data(offset)) === 1;
}

export function init_globalization() {
    runtimeHelpers.invariantMode = runtimeHelpers.config.globalizationMode === "invariant";
    runtimeHelpers.preferredIcuAsset = get_preferred_icu_asset();

    if (!runtimeHelpers.invariantMode) {
        if (runtimeHelpers.preferredIcuAsset) {
            if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: ICU data archive(s) available, disabling invariant mode");
        } else if (runtimeHelpers.config.globalizationMode !== "icu") {
            if (runtimeHelpers.diagnosticTracing) console.debug("MONO_WASM: ICU data archive(s) not available, using invariant globalization mode");
            runtimeHelpers.invariantMode = true;
            runtimeHelpers.preferredIcuAsset = null;
        } else {
            const msg = "invariant globalization mode is inactive and no ICU data archives are available";
            Module.err(`MONO_WASM: ERROR: ${msg}`);
            throw new Error(msg);
        }
    }

    const invariantEnv = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT";
    const hybridEnv = "DOTNET_SYSTEM_GLOBALIZATION_HYBRID";
    const env_variables = runtimeHelpers.config.environmentVariables!;
    if (env_variables[hybridEnv] === undefined && runtimeHelpers.config.globalizationMode === "hybrid") {
        env_variables[hybridEnv] = "1";
    }
    else if (env_variables[invariantEnv] === undefined && runtimeHelpers.invariantMode) {
        env_variables[invariantEnv] = "1";
    }
    if (env_variables["TZ"] === undefined) {
        try {
            // this call is relatively expensive, so we call it during download of other assets
            const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone || null;
            if (timezone) {
                env_variables!["TZ"] = timezone;
            }
        } catch {
            console.info("MONO_WASM: failed to detect timezone, will fallback to UTC");
        }
    }
}

export function get_preferred_icu_asset(): string | null {
    if (!runtimeHelpers.config.assets || runtimeHelpers.invariantMode)
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
