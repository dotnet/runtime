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
import * as CSharp from "Sdk.Rules.CSharp";
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

const ilCompile = Rules.rule<IlCompileAttrs, IlCompileResolved, Rules.Toolchain>({
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
            tool: Defs.CORE_ROOT_ILASM,
            arguments: cmdArgs,
            outputs: [dll],
            description: `ilasm ${ctx.args.name}`,
        });

        const binary = produced[0];
        return [
            <CSharp.DotnetAssemblyCompileInfo>{
                kind: "DotnetAssemblyCompileInfo",
                binary: binary,
                tfm: "net11.0",
                refs: [],
            },
            <CSharp.DotnetAssemblyRuntimeInfo>{
                kind: "DotnetAssemblyRuntimeInfo",
                binary: binary,
            },
        ];
    },
});

// ============================================================================
//  il_coreclr_test public API
// ============================================================================

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

export interface IlCoreClrTestResult extends Rules.Provider {
    binary: Rules.Artifact;
    testInfo?: Rules.TestInfo;
    defaultInfo: Rules.DefaultInfo;
}

export function il_coreclr_test(args: IlCoreClrTestArguments): IlCoreClrTestResult {
    const ilTarget = ilCompile({
        name: args.name,
        srcs: args.srcs,
        debugType: args.debugType,
        optimize: args.optimize,
    });
    const compileInfo = Rules.getProvider<CSharp.DotnetAssemblyCompileInfo>(ilTarget, "DotnetAssemblyCompileInfo");

    const taggedManual = (args.tags || []).filter(t => t === "manual").length > 0;
    const shouldRun = args.run !== false && !taggedManual;
    const testRunnerTarget = !shouldRun
        ? undefined
        : corerunTestRunner({
            name: `${args.name}_test`,
            binary: compileInfo.binary,
            env: args.env,
            flaky: args.flaky,
            tags: args.tags,
        });
    const testResult = testRunnerTarget !== undefined
        ? Rules.getProvider<CorerunTestRunnerResult>(testRunnerTarget, "CorerunTestRunnerResult")
        : undefined;

    return {
        kind: "IlCoreClrTestResult",
        binary: compileInfo.binary,
        testInfo: testResult !== undefined ? testResult.testInfo : undefined,
        defaultInfo: Rules.defaultInfo({ files: [compileInfo.binary] }),
    };
}
