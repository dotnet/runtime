import type { LoaderConfig } from "./types";
import { GlobalizationMode } from "./types";
import { ENVIRONMENT_IS_WEB } from "./per-module";

export function getIcuResourceName(config: LoaderConfig): string | null {
    if (config.resources?.icu && config.globalizationMode != GlobalizationMode.Invariant) {
        const culture = config.applicationCulture || (ENVIRONMENT_IS_WEB ? (globalThis.navigator && globalThis.navigator.languages && globalThis.navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);
        if (!config.applicationCulture) {
            config.applicationCulture = culture;
        }

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
    config.environmentVariables!["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
    return null;
}

function getShardedIcuResourceName(culture: string): string {
    const prefix = culture.split("-")[0];
    if (prefix === "en" || ["fr", "fr-FR", "it", "it-IT", "de", "de-DE", "es", "es-ES"].includes(culture)) {
        return "icudt_EFIGS.dat";
    }

    if (["zh", "ko", "ja"].includes(prefix)) {
        return "icudt_CJK.dat";
    }

    return "icudt_no_CJK.dat";
}
