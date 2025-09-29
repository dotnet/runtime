//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import gitCommitInfo from "git-commit-info";

if (process.env.ContinuousIntegrationBuild === undefined) {
    throw new Error("ContinuousIntegrationBuild environment variable is not defined");
}
if (process.env.Configuration === undefined) {
    throw new Error("Configuration environment variable is not defined");
}
if (process.env.ProductVersion === undefined) {
    throw new Error("ProductVersion environment variable is not defined");
}

export const configuration = process.env.Configuration;
export const productVersion = process.env.ProductVersion;
export const isContinuousIntegrationBuild = process.env.ContinuousIntegrationBuild === "true" ? true : false;

console.log(`Rollup configuration: Configuration=${configuration}, ProductVersion=${productVersion}, ContinuousIntegrationBuild=${isContinuousIntegrationBuild}`);

export const banner = "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js\n\n";
export const banner_dts = banner + "//! This is not considered public API with backward compatibility guarantees. \n";
export const keep_classnames = /(ManagedObject|ManagedError|Span|ArraySegment)/;
export const keep_fnames = /(dotnetUpdateInternals|dotnetUpdateInternalsSubscriber)/;
export const reserved = [
    "Module", "dotnetApi",
    "dotnetInternals", "dotnetLogger", "dotnetAssert", "dotnetJSEngine",
    "dotnetUpdateInternals", "dotnetUpdateInternalsSubscriber", "dotnetInitializeModule",
    "dotnetLoaderExports", "dotnetRuntimeExports", "dotnetBrowserHostExports", "dotnetInteropJSExports", "dotnetNativeBrowserExports", "dotnetBrowserUtilsExports",
];

export const externalDependencies = ["module", "process", "perf_hooks", "node:crypto"];
export const artifactsObjDir = "../../artifacts/obj";
export const isDebug = process.env.Configuration !== "Release";

export let gitHash;
try {
    const gitInfo = gitCommitInfo();
    gitHash = gitInfo.hash;
} catch (e) {
    gitHash = "unknown";
}

export const envConstants = {
    productVersion,
    configuration,
    gitHash,
    isContinuousIntegrationBuild,
};
