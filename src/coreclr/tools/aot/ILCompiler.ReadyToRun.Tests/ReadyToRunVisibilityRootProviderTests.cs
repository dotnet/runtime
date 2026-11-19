// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;

using crossgen2::ILCompiler;
using crossgen2::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunVisibilityRootProviderTests
{
    [Fact]
    public void StringConstructorFactoriesAreVisibilityRoots()
    {
        string targetArchitecture = TestPaths.TargetArchitecture.ToLowerInvariant();
        string targetOS = TestPaths.TargetOS.ToLowerInvariant();
        TargetArchitecture architecture = targetArchitecture switch
        {
            "armel" => TargetArchitecture.ARM,
            "wasm" => TargetArchitecture.Wasm32,
            _ => Enum.Parse<TargetArchitecture>(targetArchitecture, ignoreCase: true),
        };
        TargetOS operatingSystem = targetOS switch
        {
            "android" => TargetOS.Linux,
            "win" => TargetOS.Windows,
            _ => Enum.Parse<TargetOS>(targetOS, ignoreCase: true),
        };
        TargetAbi abi = targetArchitecture == "armel" ||
            (targetOS == "android" && architecture == TargetArchitecture.ARM)
            ? TargetAbi.NativeAotArmel : TargetAbi.NativeAot;
        bool targetAllowsRuntimeCodeGeneration = operatingSystem is not
            (TargetOS.iOS or TargetOS.iOSSimulator or TargetOS.MacCatalyst or TargetOS.tvOS or
             TargetOS.tvOSSimulator or TargetOS.Browser or TargetOS.Wasi)
            && architecture is not TargetArchitecture.Wasm32;
        bool targetIsSingleThreaded = architecture is TargetArchitecture.Wasm32;
        InstructionSetSupport instructionSetSupport = new(default, default, architecture);
        TargetDetails target = new(architecture, operatingSystem, abi, instructionSetSupport.GetVectorTSimdVector());
        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration, targetIsSingleThreaded,
            instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath,
            },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
        EcmaModule coreLib = (EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib");
        context.SetSystemModule(coreLib);
        context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
        {
            Context = context,
            IsInputBubble = true,
            CompilationModuleSet = [coreLib],
            VersionBubbleModuleSet = [coreLib],
            CrossModuleInlineable = [],
            InstructionSetSupport = instructionSetSupport,
        }));
        Assert.True(ReadyToRunVisibilityRootProvider.UseVisibilityBasedRootProvider(coreLib));

        RecordingRootingServiceProvider roots = new();
        new ReadyToRunVisibilityRootProvider(coreLib).AddCompilationRoots(roots);

        MetadataType stringType = (MetadataType)context.GetWellKnownType(WellKnownType.String);
        List<MethodDesc> constructors = new();
        foreach (MethodDesc method in stringType.GetMethods())
        {
            if (method.IsConstructor && method.IsInternalCall)
            {
                constructors.Add(method);
            }
        }
        Assert.NotEmpty(constructors);
        foreach (MethodDesc constructor in constructors)
        {
            MethodSignatureBuilder signature = new(constructor.Signature)
            {
                Flags = MethodSignatureFlags.Static,
                ReturnType = stringType,
            };
            MethodDesc? factory = stringType.GetMethod("Ctor"u8, signature.ToSignature());
            Assert.NotNull(factory);
            Assert.Contains(factory, roots.Methods);
        }

        MethodDesc? privateHelper = stringType.GetMethod("CreateStringForSByteConstructor"u8, null);
        Assert.NotNull(privateHelper);
        Assert.DoesNotContain(privateHelper, roots.Methods);
    }

    private sealed class RecordingRootingServiceProvider : IRootingServiceProvider
    {
        public HashSet<MethodDesc> Methods { get; } = new();

        public void AddCompilationRoot(MethodDesc method, bool rootMinimalDependencies, string reason, bool isJitHelper = false)
        {
            Methods.Add(method);
        }
    }
}
