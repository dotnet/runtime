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
                if (type.Namespace == "ILCompiler.Compiler.Tests.Assets.SwiftTypes" && type.IsValueType)
                {
                    yield return new object[] { type.Name, type };
                }
            }
        }

        [Theory]
        [MemberData(nameof(DiscoverSwiftTypes))]
        public void VerifyLowering(string typeName, EcmaType type)
        {
            _ = typeName;
            var expectedLoweringAttribute = type.GetDecodedCustomAttribute("ILCompiler.Compiler.Tests.Assets.SwiftTypes", "ExpectedLoweringAttribute");

            if (expectedLoweringAttribute.Value.FixedArguments.Length == 0)
            {
                Assert.Equal(new CORINFO_SWIFT_LOWERING { byReference = true }, SwiftPhysicalLowering.LowerTypeForSwiftSignature(type), SwiftLoweringComparer.Instance);
            }
            else
            {
                CORINFO_SWIFT_LOWERING expectedLowering = default;
                expectedLowering.numLoweredElements = expectedLoweringAttribute.Value.FixedArguments.Length;
                expectedLoweringAttribute.Value.FixedArguments.Select(na => GetCorType((ExpectedLowering)(int)na.Value)).ToArray().AsSpan().CopyTo(expectedLowering.LoweredElements);
                Assert.Equal(expectedLowering, SwiftPhysicalLowering.LowerTypeForSwiftSignature(type));
            }
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

        private sealed class SwiftLoweringComparer : IEqualityComparer<CORINFO_SWIFT_LOWERING>
        {
            public static SwiftLoweringComparer Instance { get; } = new SwiftLoweringComparer();

            public bool Equals(CORINFO_SWIFT_LOWERING x, CORINFO_SWIFT_LOWERING y)
            {
                if (x.byReference != y.byReference)
                    return false;

                return x.LoweredElements.SequenceEqual(y.LoweredElements);
            }

            public int GetHashCode([DisallowNull] CORINFO_SWIFT_LOWERING obj)
            {
                HashCode code = default;
                code.Add(obj.byReference);
                if (obj.byReference)
                {
                    code.Add(obj.numLoweredElements);
                    foreach (var type in obj.LoweredElements[0..(int)obj.numLoweredElements])
                    {
                        code.Add(type);
                    }
                }
                return code.ToHashCode();
            }
        }
    }
}
