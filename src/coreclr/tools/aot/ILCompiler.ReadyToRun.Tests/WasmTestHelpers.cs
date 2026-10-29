// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;

using crossgen2::ILCompiler;
using crossgen2::ILCompiler.DependencyAnalysis.ReadyToRun;
using crossgen2::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

internal static class WasmTestHelpers
{
    private const string CoreLibSimpleName = "System.Private.CoreLib";

    /// <summary>
    /// Configures a type system context the way crossgen2 does for
    /// <c>--targetarch wasm --targetos browser</c>. Extra input assemblies stand in for the rest of an
    /// app closure, which a real build always supplies alongside CoreLib.
    /// </summary>
    public static ReadyToRunCompilerContext CreateContext(params string[] extraInputAssemblyPaths)
    {
        string coreLibPath = TestPaths.SystemPrivateCoreLibPath;
        Assert.True(File.Exists(coreLibPath), $"System.Private.CoreLib.dll not found at '{coreLibPath}'");

        InstructionSetSupport instructionSetSupport = new(default, default, TargetArchitecture.Wasm32);
        TargetDetails target = new(TargetArchitecture.Wasm32, TargetOS.Browser, TargetAbi.NativeAot, instructionSetSupport.GetVectorTSimdVector());

        Dictionary<string, string> inputFilePaths = new(StringComparer.OrdinalIgnoreCase) { { CoreLibSimpleName, coreLibPath } };
        foreach (string path in extraInputAssemblyPaths)
        {
            inputFilePaths.Add(Path.GetFileNameWithoutExtension(path), path);
        }

        // Wasm cannot generate code at runtime, matching what crossgen2's Program computes for this target.
        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes, bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: false, targetIsSingleThreaded: true, instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = inputFilePaths,
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };

        EcmaModule coreLib = (EcmaModule)context.GetModuleForSimpleName(CoreLibSimpleName);
        context.SetSystemModule(coreLib);

        // The R2R field layout algorithm reaches into the compilation group to decide whether base
        // offsets need aligning, so a context without one throws before computing any layout.
        context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
        {
            Context = context,
            IsInputBubble = true,
            CompilationModuleSet = new[] { coreLib },
            VersionBubbleModuleSet = new ModuleDesc[] { coreLib },
            CrossModuleInlineable = Array.Empty<ModuleDesc>(),
            InstructionSetSupport = instructionSetSupport,
        }));

        return context;
    }
}
