import { loaderConfig } from "./config";
import { GlobalizationMode } from "./types";
import { ENVIRONMENT_IS_WEB } from "./per-module";

export function getIcuResourceName(): string | null {
    if (loaderConfig.resources?.icu && loaderConfig.globalizationMode !== GlobalizationMode.Invariant) {
        const culture = loaderConfig.applicationCulture || (ENVIRONMENT_IS_WEB ? (globalThis.navigator && globalThis.navigator.languages && globalThis.navigator.languages[0]) : Intl.DateTimeFormat().resolvedOptions().locale);
        if (!loaderConfig.applicationCulture) {
            loaderConfig.applicationCulture = culture;
        }

        const icuFiles = loaderConfig.resources.icu;
        let icuFile = null;
        if (loaderConfig.globalizationMode === GlobalizationMode.Custom) {
            // custom ICU file is saved in the resources with fingerprinting and does not require mapping
            if (icuFiles.length >= 1) {
                return icuFiles[0].name;
            }
        } else if (!culture || loaderConfig.globalizationMode === GlobalizationMode.All) {
            icuFile = "icudt.dat";
        } else if (loaderConfig.globalizationMode === GlobalizationMode.Sharded) {
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

    loaderConfig.globalizationMode = GlobalizationMode.Invariant;
    loaderConfig.environmentVariables!["DOTNET_SYSTEM_GLOBALIZATION_INVARIANT"] = "1";
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
