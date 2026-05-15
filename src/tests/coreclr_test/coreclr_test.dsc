// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * coreclr_test — repo-specific build macro for CoreCLR xunit tests.
 *
 * The macro builds a standalone test assembly with the same common CoreCLR
 * test references used by the repo and optionally wires a BuildXL-backed
 * execution pip that runs the resulting test via corerun.
 */

import * as Rules from "Sdk.Rules";
import * as CSharp from "Sdk.Rules.CSharp";
import {Cmd, Transformer} from "Sdk.Transformers";
import * as Defs from "Defs";
import * as Common from "Tests.Common";

const supportToolchain: Rules.Toolchain = { kind: "Toolchain", name: "coreclr-test-support" };
const bashExe = f`/bin/bash`;

// ============================================================================
//  XUnitWrapperGenerator analyzer config
//
//  Generated rather than checked in so the build_property.* values can be
//  derived from the build configuration. The generator reads these via
//  csc /analyzerconfig: to emit correct platform/runtime conditionals
//  for [ActiveIssue]/[SkipOnPlatform]/etc.
// ============================================================================

const generatorGlobalConfig: File = Transformer.writeAllLines({
    outputPath: p`${Context.getMount("ObjectRoot").path}/coreclr_test/coreclr.globalconfig`,
    lines: [
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
}

@@public
export interface CoreClrTestResult extends Rules.Provider {
    binary: File;
    buildStamp: File;
    testStamp?: File;
    csInfo: CSharp.CSharpInfo;
    defaultInfo: Rules.DefaultInfo;
}

interface BuildStampAttrs {
    name: string;
    binary: CSharp.CSharpInfo;
}

interface BuildStampResult extends Rules.Provider {
    stamp: File;
    defaultInfo: Rules.DefaultInfo;
}

interface RunCoreClrTestAttrs {
    name: string;
    binary: CSharp.CSharpInfo;
    runtimeFiles: File[];
    environmentVariables: {name: string, value: string}[];
}

interface RunCoreClrTestResult extends Rules.Provider {
    stamp: File;
    defaultInfo: Rules.DefaultInfo;
}

function stageFile(actions: Rules.Actions, file: File): Rules.Artifact {
    return actions.copyFile(
        Rules.sourceArtifact(file),
        actions.declareOutput(file.path.name.toString()));
}

const emitBuildStamp = Rules.rule<BuildStampAttrs, BuildStampAttrs, Rules.Toolchain, BuildStampResult>({
    doc: "Mark a CoreCLR test assembly as built.",
    toolchain: supportToolchain,
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const stamp = ctx.actions.declareOutput(`${ctx.args.binary.binary.nameWithoutExtension}.build.stamp`);
        const script = ctx.actions.writeFile(
            ctx.actions.declareOutput(`${ctx.args.name}.build.sh`),
            [
                "#!/usr/bin/env bash",
                "set -euo pipefail",
                "printf 'built\\n' > \"$1\"",
            ]);

        const produced = ctx.actions.run({
            tool: bashExe,
            arguments: [
                Cmd.argument(Rules.cmdInput(script)),
                Cmd.argument(Rules.cmdOutput(stamp)),
            ],
            outputs: [stamp],
            dependencies: [Rules.sourceArtifact(ctx.args.binary.binary)],
            description: `mark coreclr test build: ${ctx.args.binary.binary.nameWithoutExtension}`,
        });

        const stampFile = Rules.getFile(produced[0]);
        return {
            kind: "BuildStampResult",
            stamp: stampFile,
            defaultInfo: Rules.defaultInfo({ files: [stampFile] }),
        };
    },
});

const runCoreClrTest = Rules.rule<RunCoreClrTestAttrs, RunCoreClrTestAttrs, Rules.Toolchain, RunCoreClrTestResult>({
    doc: "Run a CoreCLR standalone test via corerun and capture a BuildXL-visible stamp.",
    toolchain: supportToolchain,
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const stagedBinary = stageFile(ctx.actions, ctx.args.binary.binary);
        const stagedRuntimeFiles = ctx.args.runtimeFiles.map(file => stageFile(ctx.actions, file));

        const stamp = ctx.actions.declareOutput(`${ctx.args.binary.binary.nameWithoutExtension}.test.stamp`);
        const script = ctx.actions.writeFile(
            ctx.actions.declareOutput(`${ctx.args.name}.run.sh`),
            [
                "#!/usr/bin/env bash",
                "set -euo pipefail",
                "set +e",
                "\"$2\" \"$3\"",
                "exit_code=$?",
                "set -e",
                "if [[ \"$exit_code\" -eq 100 ]]; then",
                "  printf 'passed\\n' > \"$1\"",
                "  exit 0",
                "fi",
                "echo \"Expected exit code 100, got $exit_code\" >&2",
                "exit \"$exit_code\"",
            ]);

        const produced = ctx.actions.run({
            tool: bashExe,
            arguments: [
                Cmd.argument(Rules.cmdInput(script)),
                Cmd.argument(Rules.cmdOutput(stamp)),
                Cmd.argument(Rules.cmdInput(Rules.sourceArtifact(Defs.CORE_ROOT_CORERUN))),
                Cmd.argument(Rules.cmdInput(stagedBinary)),
            ],
            outputs: [stamp],
            dependencies: [
                stagedBinary,
                ...stagedRuntimeFiles,
            ],
            environmentVariables: ctx.args.environmentVariables,
            workingDirectory: Defs.CORE_ROOT_DIR,
            description: `run coreclr test: ${ctx.args.binary.binary.nameWithoutExtension}`,
        });

        const stampFile = Rules.getFile(produced[0]);
        return {
            kind: "RunCoreClrTestResult",
            stamp: stampFile,
            defaultInfo: Rules.defaultInfo({ files: [stampFile] }),
        };
    },
});

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
        ? [generatorGlobalConfig]
        : undefined;

    const deps = [Common.testLibrary, ...(referenceXunitWrapperGenerator ? [Common.xunitWrapperLibrary] : []), ...(args.deps || [])];
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
        analyzers: referenceXunitWrapperGenerator ? [Common.xunitWrapperGenerator.binary] : undefined,
        analyzerConfigs: analyzerConfigs,
    });

    const buildStamp = emitBuildStamp({
        name: `${args.name}_build`,
        binary: csInfo,
    }).stamp;

    const runtimeFiles = [
        Common.testLibrary.binary,
        ...(referenceXunitWrapperGenerator ? [Common.xunitWrapperLibrary.binary] : []),
        ...Defs.XUNIT_RUNTIME_DEPS,
    ];
    const testStamp = args.run === false
        ? undefined
        : runCoreClrTest({
            name: `${args.name}_test`,
            binary: csInfo,
            runtimeFiles: runtimeFiles,
            environmentVariables: args.env || [],
        }).stamp;

    return {
        kind: "CoreClrTestResult",
        binary: csInfo.binary,
        buildStamp: buildStamp,
        testStamp: testStamp,
        csInfo: csInfo,
        defaultInfo: Rules.defaultInfo({
            files: testStamp !== undefined ? [buildStamp, testStamp] : [buildStamp],
            runfiles: csInfo.defaultInfo.runfiles,
        }),
    };
}
