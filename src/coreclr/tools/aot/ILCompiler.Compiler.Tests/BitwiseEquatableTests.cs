// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.Text;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class BitwiseEquatableTests
    {
        private readonly CompilerTypeSystemContext _context;
        private readonly ModuleDesc _testModule;
        private readonly MethodDesc _objectEquals;

        public BitwiseEquatableTests()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            _context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            _context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "EquatableAssets", @"EquatableAssets.dll" },
                };
            _context.ReferenceFilePaths = new Dictionary<string, string>();

            _context.SetSystemModule(_context.GetModuleForSimpleName("Test.CoreLib"));
            _testModule = _context.GetModuleForSimpleName("EquatableAssets");
            _objectEquals = _context.GetWellKnownType(WellKnownType.Object).GetMethod("Equals"u8, null);
        }

        private MetadataType GetTestType(string name)
            => (MetadataType)_testModule.GetType(
                new Utf8Span(Encoding.UTF8.GetBytes("BitwiseEquatable")),
                new Utf8Span(Encoding.UTF8.GetBytes(name)));

        [Theory]
        [InlineData("OneField", true)]
        [InlineData("TwoFields", true)]
        [InlineData("MixedPrimitives", true)]
        [InlineData("ForwardsToOp", true)]
        [InlineData("FloatField", false)]
        [InlineData("PartialCompare", false)]
        [InlineData("OrCompare", false)]
        [InlineData("NestedField", false)]
        [InlineData("NotEquatable", false)]
        public void TestIsBitwiseEquatable(string typeName, bool expected)
        {
            MetadataType type = GetTestType(typeName);

            // This mirrors the decision RuntimeHelpersIntrinsics.EmitIL makes for a value type
            // that implements IEquatable<T> of self.
            bool result = ComparerIntrinsics.CanCompareValueTypeBits(type, _objectEquals)
                && ComparerIntrinsics.IsIEquatableEqualsFieldwise(type);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("PartialCompare")]
        [InlineData("OrCompare")]
        [InlineData("NestedField")]
        public void TestFieldwiseScanRejectsNonEquivalentEquals(string typeName)
        {
            // These types are bit-comparable at the field level, so the scan of the actual
            // Equals implementation is what proves they are not memcmp-equivalent.
            MetadataType type = GetTestType(typeName);

            Assert.True(ComparerIntrinsics.CanCompareValueTypeBits(type, _objectEquals));
            Assert.False(ComparerIntrinsics.IsIEquatableEqualsFieldwise(type));
        }
    }
}
