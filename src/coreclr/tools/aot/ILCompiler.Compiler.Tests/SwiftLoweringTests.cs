// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Xunit;
using System.Reflection.Metadata;

namespace ILCompiler.Compiler.Tests
{
    public class SwiftLoweringTests
    {
        // Keep in sync with ExpectedLoweringAttribute in SwiftTypesSupport.cs
        enum ExpectedLowering
        {
            Float,
            Double,
            Int8,
            Int16,
            Int32,
            Int64
        }

        public static IEnumerable<object[]> DiscoverSwiftTypes()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context  = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                };
            context.ReferenceFilePaths = new Dictionary<string, string>();

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            var testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");
            foreach (var type in testModule.GetAllTypes())
            {
                if (type is EcmaType { Namespace: "ILCompiler.Compiler.Tests.Assets.SwiftTypes", IsValueType: true } ecmaType
                    && ecmaType.GetDecodedCustomAttribute("ILCompiler.Compiler.Tests.Assets.SwiftTypes", "ExpectedLoweringAttribute") is { } expectedLoweringAttribute)
                {
                    // By default, we assume that our lowered representation is meant to be naturally aligned.
                    // For types that are not naturally aligned, the test can specify the offsets.
                    int[]? offsets = null;
                    if (expectedLoweringAttribute.NamedArguments.FirstOrDefault(a => a.Name == "Offsets").Value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> offsetsArgument)
                    {
                        offsets = new int[offsetsArgument.Length];
                        for (int i = 0; i < offsetsArgument.Length; i++)
                        {
                            offsets[i] = (int)offsetsArgument[i].Value;
                        }
                    }

                    CORINFO_SWIFT_LOWERING expected;
                    if (expectedLoweringAttribute.FixedArguments.Length == 0)
                    {
                        expected = new CORINFO_SWIFT_LOWERING { byReference = true };
                    }
                    else
                    {
                        expected = new CORINFO_SWIFT_LOWERING
                        {
                            numLoweredElements = expectedLoweringAttribute.FixedArguments.Length,
                        };
                        int naturalOffset = 0;
                        for (int i = 0; i < expectedLoweringAttribute.FixedArguments.Length; i++)
                        {
                            ExpectedLowering lowering = (ExpectedLowering)(int)expectedLoweringAttribute.FixedArguments[i].Value;
                            expected.LoweredElements[i] = GetCorType(lowering);
                            if (offsets is not null)
                            {
                                expected.Offsets[i] = (uint)offsets[i];
                            }
                            else
                            {
                                // For all types that we lower to, alignment == size
                                int size = GetSize(lowering);
                                if (size > 1)
                                {
                                    expected.Offsets[i] = (uint)naturalOffset.AlignUp(size);
                                }
                                else
                                {
                                    expected.Offsets[i] = (uint)naturalOffset;
                                }
                                naturalOffset += size;
                            }
                        }
                    }
                    yield return new object[] { type.Name, type, expected };
                }
            }
        }

        [Theory]
        [MemberData(nameof(DiscoverSwiftTypes))]
        public void VerifyLowering(string typeName, EcmaType type, CORINFO_SWIFT_LOWERING expectedLowering)
        {
            _ = typeName;

            Assert.Equal(expectedLowering, SwiftPhysicalLowering.LowerTypeForSwiftSignature(type));
        }

        private static int GetSize(ExpectedLowering expectedLowering)
        {
            return expectedLowering switch
            {
                ExpectedLowering.Float => 4,
                ExpectedLowering.Double => 8,
                ExpectedLowering.Int8 => 1,
                ExpectedLowering.Int16 => 2,
                ExpectedLowering.Int32 => 4,
                ExpectedLowering.Int64 => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(expectedLowering))
            };
        }

        private static CorInfoType GetCorType(ExpectedLowering expectedLowering)
        {
            return expectedLowering switch
            {
                ExpectedLowering.Float => CorInfoType.CORINFO_TYPE_FLOAT,
                ExpectedLowering.Double => CorInfoType.CORINFO_TYPE_DOUBLE,
                ExpectedLowering.Int8 => CorInfoType.CORINFO_TYPE_BYTE,
                ExpectedLowering.Int16 => CorInfoType.CORINFO_TYPE_SHORT,
                ExpectedLowering.Int32 => CorInfoType.CORINFO_TYPE_INT,
                ExpectedLowering.Int64 => CorInfoType.CORINFO_TYPE_LONG,
                _ => throw new ArgumentOutOfRangeException(nameof(expectedLowering))
            };
        }
    }
}
