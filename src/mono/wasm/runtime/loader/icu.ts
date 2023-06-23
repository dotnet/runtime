// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { ENVIRONMENT_IS_WEB, loaderHelpers } from "./globals";
import { mono_log_info, mono_log_debug } from "./logging";

export function init_globalization() {
    loaderHelpers.invariantMode = loaderHelpers.config.globalizationMode === "invariant";
    loaderHelpers.preferredIcuAsset = get_preferred_icu_asset();

    if (!loaderHelpers.invariantMode) {
        if (loaderHelpers.preferredIcuAsset) {
            mono_log_debug("ICU data archive(s) available, disabling invariant mode");
        } else if (loaderHelpers.config.globalizationMode !== "icu") {
            mono_log_debug("ICU data archive(s) not available, using invariant globalization mode");
            loaderHelpers.invariantMode = true;
            loaderHelpers.preferredIcuAsset = null;
        } else {
            const msg = "invariant globalization mode is inactive and no ICU data archives are available";
            loaderHelpers.err(`ERROR: ${msg}`);
            throw new Error(msg);
        }
    }

    const invariantEnv = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT";
    const hybridEnv = "DOTNET_SYSTEM_GLOBALIZATION_HYBRID";
    const env_variables = loaderHelpers.config.environmentVariables!;
    if (env_variables[hybridEnv] === undefined && loaderHelpers.config.globalizationMode === "hybrid") {
        env_variables[hybridEnv] = "1";
    }
    else if (env_variables[invariantEnv] === undefined && loaderHelpers.invariantMode) {
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
            mono_log_info("failed to detect timezone, will fallback to UTC");
        }
    }
}

export function get_preferred_icu_asset(): string | null {
    if (!loaderHelpers.config.assets || loaderHelpers.invariantMode)
        return null;

    // By setting <WasmIcuDataFileName> user can define what ICU source file they want to load.
    // There is no need to check application's culture when <WasmIcuDataFileName> is set.
    // If it was not set, then we have 3 "icu" assets in config and we should choose
    // only one for loading, the one that matches the application's locale.
    const icuAssets = loaderHelpers.config.assets.filter(a => a["behavior"] == "icu");
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
