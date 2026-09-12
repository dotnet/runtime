// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using ILCompiler.ReadyToRun.Tests.TestCasesRunner;
using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using crossgen2::ILCompiler;
using crossgen2::Internal.IL;
using crossgen2::Internal.JitInterface;
using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class InstanceCalliHelperTests
{
    [Fact]
    public void CallOverloadsUseExplicitThis()
    {
        TargetArchitecture architecture = TestPaths.TargetArchitecture switch
        {
            "wasm" => TargetArchitecture.Wasm32,
            "armel" => TargetArchitecture.ARM,
            _ => Enum.Parse<TargetArchitecture>(TestPaths.TargetArchitecture, ignoreCase: true)
        };
        TargetOS operatingSystem = Enum.Parse<TargetOS>(TestPaths.TargetOS, ignoreCase: true);
        var instructionSets = new InstructionSetSupport(default, default, architecture);
        var target = new TargetDetails(architecture, operatingSystem, TargetAbi.NativeAot, instructionSets.GetVectorTSimdVector());
        var context = new ReadyToRunCompilerContext(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: !TestPaths.IsWasmTarget,
            instructionSets, oldTypeSystemContext: null)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath
            },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
        var coreLib = (EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib");
        context.SetSystemModule(coreLib);
        MetadataType helper = coreLib.GetType("System.Reflection"u8, "InstanceCalliHelper"u8);
        int overloadCount = 0;
        foreach (MethodDesc method in helper.GetMethods())
        {
            if (method.Name != "Call"u8)
            {
                continue;
            }

            overloadCount++;
            MethodSignature expected = Assert.IsType<FunctionPointerType>(method.Signature[0]).Signature;
            MethodIL il = InstanceCalliHelperIntrinsics.EmitIL(method);
            var reader = new ILReader(il.GetILBytes());
            int callCount = 0;
            while (reader.HasNext)
            {
                ILOpcode opcode = reader.ReadILOpcode();
                if (opcode == ILOpcode.calli)
                {
                    callCount++;
                    MethodSignature actual = Assert.IsType<MethodSignature>(il.GetObject(reader.ReadILToken()));
                    Assert.False(actual.IsStatic);
                    Assert.True((actual.Flags & MethodSignatureFlags.ExplicitThis) != 0);
                    Assert.Equal(expected.ReturnType, actual.ReturnType);
                    Assert.Equal(expected.Length, actual.Length);
                    for (int i = 0; i < expected.Length; i++)
                    {
                        Assert.Equal(expected[i], actual[i]);
                    }
                }
                else
                {
                    reader.Skip(opcode);
                }
            }

            Assert.Equal(1, callCount);
        }

        Assert.NotEqual(0, overloadCount);
    }
}
