import type { DotnetModuleInternal, MonoConfigInternal } from "./types";
import { DotnetModuleConfig } from "./types-api";

export function deep_merge_config(target: MonoConfigInternal, source: MonoConfigInternal): MonoConfigInternal {
    const providedConfig: MonoConfigInternal = { ...source };
    if (providedConfig.assets) {
        providedConfig.assets = [...(target.assets || []), ...(providedConfig.assets || [])];
    }
    if (providedConfig.environmentVariables) {
        providedConfig.environmentVariables = { ...(target.environmentVariables || {}), ...(providedConfig.environmentVariables || {}) };
    }
    if (providedConfig.startupOptions) {
        providedConfig.startupOptions = { ...(target.startupOptions || {}), ...(providedConfig.startupOptions || {}) };
    }
    if (providedConfig.runtimeOptions) {
        providedConfig.runtimeOptions = [...(target.runtimeOptions || []), ...(providedConfig.runtimeOptions || [])];
    }
    return Object.assign(target, providedConfig);
}

export function deep_merge_module(target: DotnetModuleInternal, source: DotnetModuleConfig): DotnetModuleInternal {
    const providedConfig: DotnetModuleConfig = { ...source };
    if (providedConfig.config) {
        if (!target.config) target.config = {};
        providedConfig.config = deep_merge_config(target.config, providedConfig.config);
    }
    return Object.assign(target, providedConfig);
}