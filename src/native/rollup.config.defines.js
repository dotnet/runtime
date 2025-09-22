//! Licensed to the .NET Foundation under one or more agreements.
//! The .NET Foundation licenses this file to you under the MIT license.

import gitCommitInfo from "git-commit-info";

export const configuration = process.argv[2] || "Debug";
export const productVersion = process.argv[3] || "10.0.0-dev";
export const isContinuousIntegrationBuild = process.argv[4] === "true" ? true : false;

console.log(`Rollup configuration: Configuration=${configuration}, ProductVersion=${productVersion}, ContinuousIntegrationBuild=${isContinuousIntegrationBuild}`);

export const banner = "//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n//! This is generated file, see src/native/libs/Browser/rollup.config.defines.js\n\n";
export const banner_dts = banner + "//! This is not considered public API with backward compatibility guarantees. \n";
export const keep_classnames = /(ManagedObject|ManagedError|Span|ArraySegment)/;
export const reserved = ["Assert", "JSEngine", "Logger", "Module", "dotnetInternals", "interopExports", "loaderExports", "nativeExports", "runtimeApi", "runtimeExports"];
export const externalDependencies = ["module", "process", "perf_hooks", "node:crypto"];
export const artifactsObjDir = "../../artifacts/obj";
export const isDebug = configuration !== "Release";

let gitHash;
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
