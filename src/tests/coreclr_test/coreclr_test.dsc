// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * coreclr_test — repo-specific build macro for CoreCLR xunit tests.
 *
 * This is intentionally build-only for now: it compiles the test assembly
 * and bakes in the common CoreCLR test references, but does not stage or
 * execute the test binary through a runtime harness.
 */

import * as Rules from "Sdk.Rules";
import * as CSharp from "Sdk.Rules.CSharp";
import * as Defs from "Defs";

const dotNetRoot = Environment.getPathValue("DOTNET_ROOT");
const dotNetSdkVersion = Environment.getStringValue("DOTNET_SDK_VERSION");

const csharpToolchain = CSharp.csharpToolchain({
    name: "dotnet-sdk",
    hostExe: f`${dotNetRoot}/dotnet`,
    compiler: f`${dotNetRoot}/sdk/${dotNetSdkVersion}/Roslyn/bincore/csc.dll`,
});

// ============================================================================
//  coreclr_test arguments and result
// ============================================================================

@@public
export interface CoreClrTestArguments {
    name: string;
    srcs: Rules.Label[];
    deps?: Rules.Label[];
    optimize?: boolean;
    allowUnsafe?: boolean;
    defines?: string[];
    nowarn?: string[];
}

@@public
export interface CoreClrTestResult extends Rules.Provider {
    binary: File;
    csInfo: CSharp.CSharpInfo;
    defaultInfo: Rules.DefaultInfo;
}

// ============================================================================
//  coreclr_test implementation
// ============================================================================

@@public
export function coreclr_test(args: CoreClrTestArguments): CoreClrTestResult {
    const testNowarn = [
        "CS0078", "CS0162", "CS0164", "CS0168", "CS0169", "CS0219",
        "CS0251", "CS0252", "CS0414", "CS0429", "CS0618", "CS0642",
        "CS0649", "CS0652", "CS0659", "CS0675", "CS1691", "CS1717",
        "CS1718", "CS3001", "CS3002", "CS3003", "CS3005", "CS3008",
        "CS3016", "CS8981",
        "CS1701"
    ];
    const allNowarn = [...testNowarn, ...(args.nowarn || [])];

    const csInfo = CSharp.csharp_library({
        name: args.name,
        toolchain: csharpToolchain,
        srcs: args.srcs,
        refs: [...Defs.CORECLR_TEST_COMMON_DEPS, ...(args.deps || [])],
        fileRefs: Defs.CORECLR_TEST_COMMON_REFS,
        optimize: args.optimize !== undefined ? args.optimize : true,
        allowUnsafe: args.allowUnsafe !== undefined ? args.allowUnsafe : true,
        defines: args.defines,
        nowarn: allNowarn
    });

    return {
        kind: "CoreClrTestResult",
        binary: csInfo.binary,
        csInfo: csInfo,
        defaultInfo: csInfo.defaultInfo
    };
}
