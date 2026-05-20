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
import * as Common from "Tests.Common";

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

const write_file = Rules.rule<WriteFileAttrs, WriteFileAttrs, Rules.Toolchain, WriteFileResult>({
    doc: "Generate a text file from a list of lines.",
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const output = ctx.actions.declareOutput(ctx.args.out);
        const bound = ctx.actions.writeFile(output, ctx.args.content);
        return {
            kind: "WriteFileResult",
            out: bound,
            defaultInfo: Rules.defaultInfo({ files: [Rules.getFile(bound)] }),
        };
    },
});

// ============================================================================
//  XUnitWrapperGenerator analyzer config
//
//  Generated via write_file so the build_property.* values flow through
//  the label-resolution safety model as an Artifact rather than a raw
//  Transformer.writeAllLines File.
// ============================================================================

const generatorGlobalConfig = write_file({
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

// ============================================================================
//  coreclr_test arguments and result
// ============================================================================

@@public
export interface CoreClrTestArguments {
    name: string;
    srcs: Rules.Label[];
    deps?: CSharp.CSharpInfo[];
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
    testDeps?: CSharp.CSharpInfo[];
    // `tags` participates in skip-running ("manual") but is otherwise opaque.
    tags?: string[];
    // `targetCompatibleWith` is enforced at generation time (incompatible
    // targets are not emitted on this host); accepted here for completeness.
    targetCompatibleWith?: string[];
}

@@public
export interface CoreClrTestResult extends Rules.Provider {
    binary: File;
    testInfo?: Rules.TestInfo;
    csInfo: CSharp.CSharpInfo;
    defaultInfo: Rules.DefaultInfo;
}

// ============================================================================
//  Internal test runner rule (kind: "test" → auto-tagged bxl-kind:test)
// ============================================================================

interface CoreClrTestRunnerAttrs {
    name: string;
    binary: File;
    runtimeFiles: File[];
    env?: {name: string, value: string}[];
    flaky?: boolean;
    tags?: string[];
}

interface CoreClrTestRunnerResult extends Rules.Provider {
    testInfo: Rules.TestInfo;
    defaultInfo: Rules.DefaultInfo;
}

const coreclrTestRunner = Rules.rule<CoreClrTestRunnerAttrs, CoreClrTestRunnerAttrs, Rules.Toolchain, CoreClrTestRunnerResult>({
    doc: "Run a CoreCLR test DLL via corerun.",
    kind: "test",
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const corerunPath = Defs.CORE_ROOT_CORERUN.path.toDiagnosticString();
        const dllName = ctx.args.binary.name.toString();

        // Generate the runner script via ctx.actions (build-time, untagged)
        // so it is produced by `bxl build` and available for Helix staging.
        // Test *execution* stays on ctx.runActions (tagged bxl-kind:test).
        const dllPath = ctx.args.binary.path.toDiagnosticString();
        const runner = ctx.actions.writeFile(
            ctx.actions.declareOutput(`${ctx.args.name}.runner.sh`),
            [
                "#!/usr/bin/env bash",
                `# dll-source: ${dllPath}`,
                `exec "${corerunPath}" "$(dirname "$0")/${dllName}" "$@"`,
            ]);

        // Let the framework handle timeout, stamp, runat, success codes.
        const ti = Rules.scheduleTestRunner(ctx.args.name, Rules.testRunInfo({
            executable: runner,
            successExitCodes: [100],
            env: ctx.args.env,
            deps: [
                Rules.sourceArtifact(ctx.args.binary),
                ...ctx.args.runtimeFiles.map(f => Rules.sourceArtifact(f)),
            ],
            size: "small",
            flaky: ctx.args.flaky,
            tags: ctx.args.tags,
        }), ctx.runActions);

        return {
            kind: "CoreClrTestRunnerResult",
            testInfo: ti,
            defaultInfo: Rules.defaultInfo({ files: [] }),
        };
    },
});

// ============================================================================
//  coreclr_test — public macro
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
    const referenceXunitWrapperGenerator = args.referenceXunitWrapperGenerator !== false;
    const analyzerConfigs = referenceXunitWrapperGenerator
        ? [generatorGlobalConfig.out]
        : undefined;

    const deps = [Common.testLibrary, ...(referenceXunitWrapperGenerator ? [Common.xunitWrapperLibrary] : []), ...(args.deps || []), ...(args.testDeps || [])];
    const csInfo = CSharp.csharp_binary({
        name: args.name,
        toolchain: Common.csharpToolchain,
        srcs: args.srcs,
        refs: Defs.CORECLR_TEST_COMMON_DEPS,
        externalPackages: Defs.EXTERNAL_PACKAGES,
        deps: deps,
        optimize: args.optimize !== undefined ? args.optimize : true,
        allowUnsafe: args.allowUnsafe !== undefined ? args.allowUnsafe : true,
        defines: args.defines,
        nowarn: allNowarn,
        analyzers: referenceXunitWrapperGenerator ? [Rules.sourceArtifact(Common.xunitWrapperGenerator.binary)] : undefined,
        analyzerConfigs: analyzerConfigs,
    });

    const runtimeFiles = [
        Common.testLibrary.binary,
        ...(referenceXunitWrapperGenerator ? [Common.xunitWrapperLibrary.binary] : []),
        ...Defs.XUNIT_RUNTIME_DEPS,
    ];

    // Tests carrying the bazel "manual" tag are compiled but not run by default.
    const taggedManual = (args.tags || []).filter(t => t === "manual").length > 0;
    const shouldRun = args.run !== false && !taggedManual;
    const testResult = !shouldRun
        ? undefined
        : coreclrTestRunner({
            name: `${args.name}_test`,
            binary: csInfo.binary,
            runtimeFiles: runtimeFiles,
            env: args.env,
            flaky: args.flaky,
            tags: args.tags,
        });

    return {
        kind: "CoreClrTestResult",
        binary: csInfo.binary,
        testInfo: testResult !== undefined ? testResult.testInfo : undefined,
        csInfo: csInfo,
        defaultInfo: csInfo.defaultInfo,
    };
}
