// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * il_coreclr_test — repo-specific build macro for CoreCLR IL tests.
 *
 * Assembles one or more .il source files into a test assembly using the
 * pre-built native ilasm from Core_Root, then wires test execution through
 * an internal `kind: "test"` rule that runs the binary via corerun.
 */

import * as Rules from "Sdk.Rules";
import {Cmd} from "Sdk.Transformers";
import * as Defs from "Defs";

// ============================================================================
//  Internal ilasm-compile rule
// ============================================================================

interface IlCompileAttrs {
    name: string;
    srcs: Rules.Label[];
    debugType?: string;
    optimize?: boolean;
}

interface IlCompileResolved {
    name: string;
    srcs: Rules.Artifact[];
    debugType?: string;
    optimize?: boolean;
}

interface IlCompileResult extends Rules.Provider {
    binary: File;
    defaultInfo: Rules.DefaultInfo;
}

const ilCompile = Rules.rule<IlCompileAttrs, IlCompileResolved, Rules.Toolchain, IlCompileResult>({
    doc: "Assemble .il source files into a .dll using ilasm.",
    resolve: (attrs, resolver) => <IlCompileResolved>{
        name: attrs.name,
        srcs: resolver.resolveAll(attrs.srcs),
        debugType: attrs.debugType,
        optimize: attrs.optimize,
    },
    impl: (ctx) => {
        const dll = ctx.actions.declareOutput(`${ctx.args.name}.dll`);

        let cmdArgs: Argument[] = [Cmd.argument("-quiet"), Cmd.argument("-dll")];
        if (ctx.args.debugType === "full") {
            cmdArgs = cmdArgs.push(Cmd.argument("-debug"));
        } else if (ctx.args.debugType === "pdbonly") {
            cmdArgs = cmdArgs.push(Cmd.argument("-debug=opt"));
        }
        if (ctx.args.optimize === true) {
            cmdArgs = cmdArgs.push(Cmd.argument("-optimize"));
        }
        cmdArgs = cmdArgs.push(Cmd.option("-output=", Rules.cmdOutput(dll)));
        for (const s of ctx.args.srcs) {
            cmdArgs = cmdArgs.push(Cmd.argument(Rules.cmdInput(s)));
        }

        const produced = ctx.actions.run({
            tool: Rules.sourceArtifact(Defs.CORE_ROOT_ILASM),
            arguments: cmdArgs,
            outputs: [dll],
            description: `ilasm ${ctx.args.name}`,
        });

        const binaryFile = Rules.getFile(produced[0]);
        return {
            kind: "IlCompileResult",
            binary: binaryFile,
            defaultInfo: Rules.defaultInfo({ files: [binaryFile] }),
        };
    },
});

// ============================================================================
//  Internal test runner rule (kind: "test" → auto-tagged bxl-kind:test)
// ============================================================================

interface IlTestRunnerAttrs {
    name: string;
    binary: File;
    env?: {name: string, value: string}[];
    flaky?: boolean;
    tags?: string[];
}

interface IlTestRunnerResult extends Rules.Provider {
    testInfo: Rules.TestInfo;
    defaultInfo: Rules.DefaultInfo;
}

const ilTestRunner = Rules.rule<IlTestRunnerAttrs, IlTestRunnerAttrs, Rules.Toolchain, IlTestRunnerResult>({
    doc: "Run an IL test DLL via corerun.",
    kind: "test",
    resolve: (attrs, _resolver) => attrs,
    impl: (ctx) => {
        const corerunPath = Defs.CORE_ROOT_CORERUN.path.toDiagnosticString();
        const dllName = ctx.args.binary.name.toString();

        // Generate the runner script via ctx.actions (build-time, untagged)
        // so it is produced by `bxl build` and available for Helix staging.
        // Test *execution* stays on ctx.runActions (tagged bxl-kind:test).
        const dllPath = ctx.args.binary.path.toDiagnosticString();
        const envLines = (ctx.args.env || []).map(e => `# env: ${e.name}=${e.value}`);
        const runner = ctx.actions.writeFile(
            ctx.actions.declareOutput(`${ctx.args.name}.runner.sh`),
            [
                "#!/usr/bin/env bash",
                `# dll-source: ${dllPath}`,
                ...envLines,
                `exec "${corerunPath}" "$(dirname "$0")/${dllName}" "$@"`,
            ]);

        const ti = Rules.scheduleTestRunner(ctx.args.name, Rules.testRunInfo({
            executable: runner,
            successExitCodes: [100],
            env: ctx.args.env,
            deps: [Rules.sourceArtifact(ctx.args.binary)],
            size: "small",
            flaky: ctx.args.flaky,
            tags: ctx.args.tags,
        }), ctx.runActions);

        return {
            kind: "IlTestRunnerResult",
            testInfo: ti,
            defaultInfo: Rules.defaultInfo({ files: [] }),
        };
    },
});

// ============================================================================
//  il_coreclr_test public API
// ============================================================================

@@public
export interface IlCoreClrTestArguments {
    name: string;
    srcs: Rules.Label[];
    debugType?: string;
    optimize?: boolean;
    env?: {name: string, value: string}[];
    run?: boolean;
    // ------------------------------------------------------------------
    // Bazel-compat attributes, accepted for 1:1 pass-through from the
    // port script. Most are no-ops at this layer.
    // ------------------------------------------------------------------
    pri?: number;
    size?: string;
    tags?: string[];
    flaky?: boolean;
    visibility?: string[];
    targetCompatibleWith?: string[];
}

@@public
export interface IlCoreClrTestResult extends Rules.Provider {
    binary: File;
    testInfo?: Rules.TestInfo;
    defaultInfo: Rules.DefaultInfo;
}

@@public
export function il_coreclr_test(args: IlCoreClrTestArguments): IlCoreClrTestResult {
    const ilResult = ilCompile({
        name: args.name,
        srcs: args.srcs,
        debugType: args.debugType,
        optimize: args.optimize,
    });

    // Tests carrying the bazel "manual" tag are compiled but not run by default.
    const taggedManual = (args.tags || []).filter(t => t === "manual").length > 0;
    const shouldRun = args.run !== false && !taggedManual;
    const testResult = !shouldRun
        ? undefined
        : ilTestRunner({
            name: `${args.name}_test`,
            binary: ilResult.binary,
            env: args.env,
            flaky: args.flaky,
            tags: args.tags,
        });

    return {
        kind: "IlCoreClrTestResult",
        binary: ilResult.binary,
        testInfo: testResult !== undefined ? testResult.testInfo : undefined,
        defaultInfo: ilResult.defaultInfo,
    };
}
