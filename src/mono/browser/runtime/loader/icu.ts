// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { mono_log_error } from "./logging";
import { GlobalizationMode, MonoConfig } from "../types";
import { ENVIRONMENT_IS_WEB, loaderHelpers } from "./globals";
import { mono_log_info, mono_log_debug } from "./logging";

export function init_globalization () {
    loaderHelpers.preferredIcuAsset = getIcuResourceName(loaderHelpers.config);
    let invariantMode = loaderHelpers.config.globalizationMode == GlobalizationMode.Invariant;

    if (!invariantMode) {
        if (loaderHelpers.preferredIcuAsset) {
            mono_log_debug("ICU data archive(s) available, disabling invariant mode");
        } else if (loaderHelpers.config.globalizationMode !== GlobalizationMode.Custom && loaderHelpers.config.globalizationMode !== GlobalizationMode.All && loaderHelpers.config.globalizationMode !== GlobalizationMode.Sharded) {
            mono_log_debug("ICU data archive(s) not available, using invariant globalization mode");
            invariantMode = true;
            loaderHelpers.preferredIcuAsset = null;
        } else {
            const msg = "invariant globalization mode is inactive and no ICU data archives are available";
            mono_log_error(`ERROR: ${msg}`);
            throw new Error(msg);
        }
    }

    const invariantEnv = "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT";
    const env_variables = loaderHelpers.config.environmentVariables!;
    if (env_variables[invariantEnv] === undefined && invariantMode) {
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

export function getIcuResourceName (config: MonoConfig): string | null {
    if (config.resources?.icu && config.globalizationMode != GlobalizationMode.Invariant) {
        // TODO: when starting on sidecar, we should pass default culture from UI thread
        const culture = config.applicationCulture || (ENVIRONMENT_IS_WEB ? (globalThis.navigator && globalThis.navigator.languages && globalThis.navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);

        const icuFiles = config.resources.icu;

        let icuFile = null;
        if (config.globalizationMode === GlobalizationMode.Custom) {
            // custom ICU file is saved in the resources with fingerprinting and does not require mapping
            if (icuFiles.length >= 1) {
                return icuFiles[0].name;
            }
        } else if (!culture || config.globalizationMode === GlobalizationMode.All) {
            icuFile = "icudt.dat";
        } else if (config.globalizationMode === GlobalizationMode.Sharded) {
            icuFile = getShardedIcuResourceName(culture);
        }

        if (icuFile) {
            for (let i = 0; i < icuFiles.length; i++) {
                const asset = icuFiles[i];
                if (asset.virtualPath === icuFile) {
                    return asset.name;
                }
            }
        }
    }

    config.globalizationMode = GlobalizationMode.Invariant;
    return null;
}

function getShardedIcuResourceName (culture: string): string {
    const prefix = culture.split("-")[0];
    if (prefix === "en" || ["fr", "fr-FR", "it", "it-IT", "de", "de-DE", "es", "es-ES"].includes(culture)) {
        return "icudt_EFIGS.dat";
    }

    if (["zh", "ko", "ja"].includes(prefix)) {
        return "icudt_CJK.dat";
    }

    return "icudt_no_CJK.dat";
}
