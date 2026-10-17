// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.Text;

using crossgen2::ILCompiler;
using crossgen2::Internal.JitInterface;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ReadyToRunLibraryRootProviderTests
{
    [Theory]
    [InlineData("System.Numerics", "Vector`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector64`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector128`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector256`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector512`1", false)]
    [InlineData("System.Runtime.Intrinsics", "Scalar`1", false)]
    [InlineData("System.Collections.Generic", "List`1", true)]
    [InlineData("System", "Span`1", true)]
    public void AutomaticTypeInstantiation(string namespaceName, string typeName, bool expected)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(typeName));

        InstantiatedType instantiated = ReadyToRunLibraryRootProvider.InstantiateIfPossible(type);
        Assert.Equal(expected, instantiated is not null);
        if (instantiated is not null)
        {
            Assert.Same(context.CanonType, instantiated.Instantiation[0]);
        }

        InstantiatedType concrete = type.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int32));
        Assert.Same(context.GetWellKnownType(WellKnownType.Int32), concrete.Instantiation[0]);
    }

    [Theory]
    [InlineData("System.Numerics", "Vector", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector64", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector128", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector256", false)]
    [InlineData("System.Runtime.Intrinsics", "Vector512", false)]
    [InlineData("System", "Array", true)]
    public void AutomaticMethodInstantiation(string namespaceName, string typeName, bool expected)
    {
        ReadyToRunCompilerContext context = CreateContext();
        MetadataType type = context.SystemModule.GetType(Encoding.UTF8.GetBytes(namespaceName), Encoding.UTF8.GetBytes(typeName));
        int methodsChecked = 0;
        int methodsInstantiated = 0;
        foreach (MethodDesc method in type.GetMethods())
        {
            if (!method.HasInstantiation)
                continue;

            MethodDesc instantiated = ReadyToRunLibraryRootProvider.InstantiateIfPossible(method);
            if (!expected)
            {
                Assert.Null(instantiated);
            }
            else if (instantiated is not null)
            {
                methodsInstantiated++;
                foreach (TypeDesc argument in instantiated.Instantiation)
                    Assert.Same(context.CanonType, argument);
            }

            methodsChecked++;
        }

        Assert.True(methodsChecked > 0);
        Assert.Equal(expected, methodsInstantiated > 0);
        if (!expected)
        {
            Assert.True(CheckNestedTypes(type) > 0);
        }
    }

    private static int CheckNestedTypes(MetadataType type)
    {
        int typesChecked = 0;
        foreach (MetadataType nested in type.GetNestedTypes())
        {
            if (nested.HasInstantiation)
            {
                Assert.Null(ReadyToRunLibraryRootProvider.InstantiateIfPossible(nested));
                typesChecked++;
            }

            typesChecked += CheckNestedTypes(nested);
        }

        return typesChecked;
    }

    private static ReadyToRunCompilerContext CreateContext()
    {
        InstructionSetSupport instructionSetSupport = new(default, default, TargetArchitecture.X64);
        TargetDetails target = new(TargetArchitecture.X64, TargetOS.Linux, TargetAbi.NativeAot);
        ReadyToRunCompilerContext context = new(target, SharedGenericsMode.CanonicalReferenceTypes,
            bubbleIncludesCoreModule: true, targetAllowsRuntimeCodeGeneration: true, instructionSetSupport, oldTypeSystemContext: null)
        {
            InputFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["System.Private.CoreLib"] = TestPaths.SystemPrivateCoreLibPath,
            },
            ReferenceFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        };
        context.SetSystemModule((EcmaModule)context.GetModuleForSimpleName("System.Private.CoreLib"));

        return context;
    }
}
