// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * coreclr_test — repo-specific build macro for CoreCLR xunit tests.
 *
 * The macro builds a standalone test assembly via `csharp_binary`, then
 * wires test execution through an internal `kind: "test"` rule that
 * generates a runner script, stages the test DLL + deps, and runs via
 * corerun. The test runner pip is auto-tagged `bxl-kind:test` by the
 * framework so `bxl /f:tag='bxl-kind:test'` selects test execution
 * while a plain build only compiles.
 */

import * as Rules from "Sdk.Rules";
import * as CSharp from "Sdk.Rules.CSharp";
import {Cmd} from "Sdk.Transformers";
import * as Defs from "Defs";

// ============================================================================
//  write_file — local rule for generating text files
//
//  Analogous to bazel-skylib's write_file rule. Produces a text file
//  from a list of lines via ctx.actions.writeFile, returning the bound
//  Artifact so it can flow through the label system.
// ============================================================================

interface WriteFileAttrs {
    name: string;
    out: string;
    content: string[];
}

interface WriteFileResult extends Rules.Provider {
    out: Rules.Artifact;
    defaultInfo: Rules.DefaultInfo;
}

const write_file = Rules.rule<WriteFileAttrs, WriteFileAttrs, Rules.Toolchain>({
    doc: "Generate a text file from a list of lines.",
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const output = ctx.actions.declareOutput(ctx.args.out);
        const bound = ctx.actions.writeFile(output, ctx.args.content);
        return [
            <WriteFileResult>{
                kind: "WriteFileResult",
                out: bound,
                defaultInfo: Rules.defaultInfo({ files: [bound] }),
            },
        ];
    },
});

// ============================================================================
//  XUnitWrapperGenerator analyzer config
//
//  Generated via write_file so the build_property.* values flow through
//  the label-resolution safety model as an Artifact rather than a raw
//  Transformer.writeAllLines File.
// ============================================================================

const generatorGlobalConfigTarget = write_file({
    name: "coreclr_globalconfig",
    out: "coreclr.globalconfig",
    content: [
        "is_global = true",
        "",
        "build_property.TargetOS = linux",
        "build_property.TargetArchitecture = x64",
        "build_property.RuntimeFlavor = CoreCLR",
    ],
});
const generatorGlobalConfig = Rules.getProvider<WriteFileResult>(generatorGlobalConfigTarget, "WriteFileResult");

// ============================================================================
//  coreclr_test arguments and result
// ============================================================================

export interface CoreClrTestArguments {
    name: string;
    srcs: Rules.Label[];
    deps?: Rules.Target[];
    optimize?: boolean;
    allowUnsafe?: boolean;
    defines?: string[];
    nowarn?: string[];
    env?: {name: string, value: string}[];
    referenceXunitWrapperGenerator?: boolean;
    run?: boolean;
    // ------------------------------------------------------------------
    // Bazel-compat attributes, mostly absorbed as no-ops at this layer.
    // Kept on the interface so the generator can pass them through
    // 1:1 without having to know which ones the BXL macro acts on.
    // ------------------------------------------------------------------
    pri?: number;
    size?: string;
    debugType?: string;
    nullable?: string;
    async_?: boolean;
    flaky?: boolean;
    visibility?: string[];
    compilerOptions?: string[];
    testDeps?: Rules.Target[];
    // `tags` participates in skip-running ("manual") but is otherwise opaque.
    tags?: string[];
    // `targetCompatibleWith` is enforced at generation time (incompatible
    // targets are not emitted on this host); accepted here for completeness.
    targetCompatibleWith?: string[];
}

export interface CoreClrTestResult extends Rules.Provider {
    binary: Rules.Artifact;
    testInfo?: Rules.TestInfo;
    target: Rules.Target;
    defaultInfo: Rules.DefaultInfo;
}

// ============================================================================
//  Shared test runner rule (kind: "test" → auto-tagged bxl-kind:test)
//
//  Used by both coreclr_test (C#) and il_coreclr_test (IL) to run a
//  compiled test DLL via corerun.
// ============================================================================

export interface CorerunTestRunnerAttrs {
    name: string;
    binary: Rules.Artifact;
    runtimeFiles?: Rules.Artifact[];
    env?: {name: string, value: string}[];
    flaky?: boolean;
    tags?: string[];
}

export interface CorerunTestRunnerResult extends Rules.Provider {
    testInfo: Rules.TestInfo;
    defaultInfo: Rules.DefaultInfo;
}

export const corerunTestRunner = Rules.rule<CorerunTestRunnerAttrs, CorerunTestRunnerAttrs, Rules.Toolchain>({
    doc: "Run a test DLL via corerun.",
    kind: "test",
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const corerunPath = Defs.CORE_ROOT_CORERUN.path.toDiagnosticString();
        const dllName = ctx.args.binary.shortPath;
        const dllPath = ctx.args.binary.path.toDiagnosticString();
        const runtimeFiles = ctx.args.runtimeFiles || [];
        const runtimeSources = runtimeFiles.map(f => `# runtime-source: ${f.path.toDiagnosticString()}`);
        const envLines = (ctx.args.env || []).map(e => `export "${e.name}=${e.value}"`);
        const runner = ctx.actions.writeFile(
            ctx.actions.declareOutput(`${ctx.args.name}.runner.sh`),
            [
                "#!/usr/bin/env bash",
                `# dll-source: ${dllPath}`,
                ...runtimeSources,
                "core_root=\"${CORE_ROOT:-${HELIX_CORRELATION_PAYLOAD:-}}\"",
                "corerun=\"${core_root:+$core_root/corerun}\"",
                "corerun=\"${corerun:-" + corerunPath + "}\"",
                ...envLines,
                `exec "$corerun" "$(dirname "$0")/${dllName}" "$@"`,
            ]);

        const ti = Rules.scheduleTestRunner(ctx.args.name, Rules.testRunInfo({
            executable: runner,
            successExitCodes: [100],
            env: ctx.args.env,
            deps: [
                ctx.args.binary,
                ...runtimeFiles,
            ],
            size: "small",
            flaky: ctx.args.flaky,
            tags: ctx.args.tags,
        }), ctx.runActions);

        return [
            <CorerunTestRunnerResult>{
                kind: "CorerunTestRunnerResult",
                testInfo: ti,
                defaultInfo: Rules.defaultInfo({ files: [] }),
            },
        ];
    },
});

// ============================================================================
//  coreclr_test — public macro
// ============================================================================

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
    const referenceXunitWrapperGenerator = args.referenceXunitWrapperGenerator !== false;
    const analyzerConfigs = referenceXunitWrapperGenerator
        ? [generatorGlobalConfig.out]
        : undefined;

    const deps = [testLibrary, ...(referenceXunitWrapperGenerator ? [xunitWrapperLibrary] : []), ...(args.deps || []), ...(args.testDeps || [])];
    const xunitWrapperGeneratorCompileInfo = Rules.getProvider<CSharp.DotnetAssemblyCompileInfo>(xunitWrapperGenerator, "DotnetAssemblyCompileInfo");
    const binaryTarget = tc.csharp_binary({
        name: args.name,
        tfm: "net11.0",
        srcs: args.srcs,
        deps: [...COMMON_TEST_IMPORTS, ...deps],
        optimize: args.optimize !== undefined ? args.optimize : true,
        allowUnsafe: args.allowUnsafe !== undefined ? args.allowUnsafe : true,
        defines: args.defines,
        nowarn: allNowarn,
        useSharedCompilation: true,
        disableImplicitFrameworkRefs: true,
        analyzers: referenceXunitWrapperGenerator ? [xunitWrapperGeneratorCompileInfo.binary] : undefined,
        analyzerConfigs: analyzerConfigs,
    });
    const binaryCompileInfo = Rules.getProvider<CSharp.DotnetAssemblyCompileInfo>(binaryTarget, "DotnetAssemblyCompileInfo");
    const binaryDefaultInfo = Rules.getProvider<Rules.DefaultInfo>(binaryTarget, "DefaultInfo");

    const testLibraryCompileInfo = Rules.getProvider<CSharp.DotnetAssemblyCompileInfo>(testLibrary, "DotnetAssemblyCompileInfo");
    const xunitWrapperLibraryCompileInfo = Rules.getProvider<CSharp.DotnetAssemblyCompileInfo>(xunitWrapperLibrary, "DotnetAssemblyCompileInfo");
    const runtimeFiles = [
        testLibraryCompileInfo.binary,
        ...(referenceXunitWrapperGenerator ? [xunitWrapperLibraryCompileInfo.binary] : []),
        ...Defs.XUNIT_RUNTIME_DEPS,
    ];

    const taggedManual = (args.tags || []).filter(t => t === "manual").length > 0;
    const shouldRun = args.run !== false && !taggedManual;
    const testRunnerTarget = !shouldRun
        ? undefined
        : corerunTestRunner({
            name: `${args.name}_test`,
            binary: binaryCompileInfo.binary,
            runtimeFiles: runtimeFiles,
            env: args.env,
            flaky: args.flaky,
            tags: args.tags,
        });
    const testResult = testRunnerTarget !== undefined
        ? Rules.getProvider<CorerunTestRunnerResult>(testRunnerTarget, "CorerunTestRunnerResult")
        : undefined;

    return {
        kind: "CoreClrTestResult",
        binary: binaryCompileInfo.binary,
        testInfo: testResult !== undefined ? testResult.testInfo : undefined,
        target: binaryTarget,
        defaultInfo: binaryDefaultInfo,
    };
}
