// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias ilc;

using System;
using System.Collections.Generic;
using System.Text;

using ilc::ILCompiler;
using ilc::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.IL;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

[ConditionalClass(typeof(TestPaths), nameof(TestPaths.IsXArchTarget))]
public class HardwareIntrinsicILProviderTests
{
    [Theory]
    [InlineData(TargetArchitecture.X86, "AvxVnni", InstructionSet.X64_AVXVNNI, InstructionSet.X64_AVX512v3)]
    [InlineData(TargetArchitecture.X64, "AvxVnni", InstructionSet.X64_AVXVNNI, InstructionSet.X64_AVX512v3)]
    [InlineData(TargetArchitecture.X86, "AvxVnniInt8", InstructionSet.X64_AVXVNNIINT, InstructionSet.X64_AVXVNNIINT_V512)]
    [InlineData(TargetArchitecture.X64, "AvxVnniInt8", InstructionSet.X64_AVXVNNIINT, InstructionSet.X64_AVXVNNIINT_V512)]
    [InlineData(TargetArchitecture.X86, "AvxVnniInt16", InstructionSet.X64_AVXVNNIINT, InstructionSet.X64_AVXVNNIINT_V512)]
    [InlineData(TargetArchitecture.X64, "AvxVnniInt16", InstructionSet.X64_AVXVNNIINT, InstructionSet.X64_AVXVNNIINT_V512)]
    public void IsSupportedMatchesCodeGenerator(TargetArchitecture architecture, string typeName, InstructionSet vexIsa, InstructionSet evexIsa)
    {
        CompilerTypeSystemContext context = new(
            new TargetDetails(architecture, TargetOS.Linux, TargetAbi.NativeAot),
            SharedGenericsMode.Disabled, delegateFeatures: 0)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath,
            },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
        context.SetSystemModule(context.GetModuleForSimpleName("System.Private.CoreLib"));

        FieldDesc isSupportedField = context.SystemModule.GetType("System"u8, "Environment"u8).GetField("s_processId"u8);
        MetadataType intrinsicType = context.SystemModule.GetType("System.Runtime.Intrinsics.X86"u8, Encoding.UTF8.GetBytes(typeName));

        foreach ((bool supportedVex, bool supportedEvex, bool optimisticVex, bool optimisticEvex) in new[]
        {
            (false, false, false, false),
            (true, false, true, false),
            (false, true, false, true),
            (true, true, true, true),
            (false, false, true, false),
            (false, false, false, true),
            (false, true, true, true),
            (true, false, true, true),
        })
        {
            InstructionSetFlags supported = default;
            InstructionSetFlags optimistic = default;
            if (supportedVex)
                supported.AddInstructionSet(vexIsa);
            if (supportedEvex)
                supported.AddInstructionSet(evexIsa);
            if (optimisticVex)
                optimistic.AddInstructionSet(vexIsa);
            if (optimisticEvex)
                optimistic.AddInstructionSet(evexIsa);

            InstructionSetSupport support = new(supported, default, optimistic, default, architecture);
            HardwareIntrinsicILProvider provider = new(support, isSupportedField, new ilc::Internal.IL.NativeAotILProvider());

            foreach (MetadataType type in architecture == TargetArchitecture.X64
                ? new[] { intrinsicType, intrinsicType.GetNestedType("X64"u8) }
                : new[] { intrinsicType })
            {
                MethodDesc method = type.GetMethod("get_IsSupported"u8, null);
                InstructionSet selectedIsa = optimisticVex ? vexIsa : evexIsa;
                byte[] expected = supported.HasInstructionSet(selectedIsa)
                    ? new[] { (byte)ILOpcode.ldc_i4_1, (byte)ILOpcode.ret }
                    : optimistic.HasInstructionSet(selectedIsa)
                        ? HardwareIntrinsicHelpers.EmitIsSupportedIL(method, isSupportedField, selectedIsa).GetILBytes()
                        : new[] { (byte)ILOpcode.ldc_i4_0, (byte)ILOpcode.ret };

                Assert.Equal(expected, provider.GetMethodIL(method).GetILBytes());
            }
        }
    }
}
