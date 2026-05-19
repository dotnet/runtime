// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * BuildXL spec for src/tests/baseservices/TieredCompilation
 *
 * Ported from BUILD.bazel. All framework refs, xunit deps, and TestLibrary
 * are baked into coreclr_test automatically — just like Bazel's live_test.bzl.
 *
 * Source files use label strings — no f`` file literals allowed.
 */

import * as CoreClr from "CoreClrTest";

@@public
export const basicTest_DefaultMode = CoreClr.coreclr_test({
    name: "BasicTest_DefaultMode",
    srcs: ["BasicTest.cs"],
    optimize: true
});

@@public
export const basicTest_QuickJitForLoopsOff = CoreClr.coreclr_test({
    name: "BasicTest_QuickJitForLoopsOff",
    srcs: ["BasicTest.cs"],
    optimize: true,
    env: [{ name: "DOTNET_TC_QuickJitForLoops", value: "0" }]
});

@@public
export const basicTest_QuickJitForLoopsOn = CoreClr.coreclr_test({
    name: "BasicTest_QuickJitForLoopsOn",
    srcs: ["BasicTest.cs"],
    optimize: true,
    env: [{ name: "DOTNET_TC_QuickJitForLoops", value: "1" }]
});

@@public
export const basicTest_QuickJitOff = CoreClr.coreclr_test({
    name: "BasicTest_QuickJitOff",
    srcs: ["BasicTest.cs"],
    optimize: true,
    env: [{ name: "DOTNET_TC_QuickJit", value: "0" }]
});

@@public
export const basicTest_QuickJitOn = CoreClr.coreclr_test({
    name: "BasicTest_QuickJitOn",
    srcs: ["BasicTest.cs"],
    optimize: true,
    env: [{ name: "DOTNET_TC_QuickJit", value: "1" }]
});

@@public
export const mcjRecorderTimeoutBeforeStop = CoreClr.coreclr_test({
    name: "McjRecorderTimeoutBeforeStop",
    srcs: ["McjRecorderTimeoutBeforeStop.cs"],
    optimize: true,
    env: [{ name: "DOTNET_MultiCoreJitProfileWriteDelay", value: "1" }]
});

@@public
export const basicTestWithMcj = CoreClr.coreclr_test({
    name: "BasicTestWithMcj",
    srcs: ["BasicTestWithMcj.cs"],
    optimize: true,
    referenceXunitWrapperGenerator: false
});

@@public
export const tieredVtableMethodTests = CoreClr.coreclr_test({
    name: "TieredVtableMethodTests",
    srcs: ["TieredVtableMethodTests.cs"]
});
