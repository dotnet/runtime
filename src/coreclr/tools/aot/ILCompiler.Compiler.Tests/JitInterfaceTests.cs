// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class JitInterfaceTests
    {
        private readonly MetadataType _testType;

        public JitInterfaceTests()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All)
            {
                InputFilePaths = new Dictionary<string, string>
                {
                    { "Test.CoreLib", @"Test.CoreLib.dll" },
                    { "ILCompiler.Compiler.Tests.Assets", @"ILCompiler.Compiler.Tests.Assets.dll" },
                },
                ReferenceFilePaths = new Dictionary<string, string>(),
            };

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));
            ModuleDesc testModule = context.GetModuleForSimpleName("ILCompiler.Compiler.Tests.Assets");
            _testType = testModule.GetType("ILCompiler.Compiler.Tests.Assets"u8, "JitInterface"u8);
        }

        [Theory]
        [InlineData("Primitive", true)]
        [InlineData("Instance", false)]
        [InlineData("Reference", true)]
        [InlineData("StructWithoutReference", true)]
        [InlineData("StructWithReference", false)]
        public void CanOmitPinning(string fieldName, bool expected)
        {
            FieldDesc field = _testType.GetField(System.Text.Encoding.UTF8.GetBytes(fieldName));

            Assert.Equal(expected, CorInfoImpl.CanOmitPinning(field));
        }

        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(false, false, true, true)]
        [InlineData(false, true, false, true)]
        [InlineData(false, true, true, false)]
        public void CanOmitPinningForStaticField(bool hasRva, bool isValueType, bool hasGCStaticBase, bool expected)
        {
            Assert.Equal(
                expected,
                CorInfoImpl.CanOmitPinningForStaticField(hasRva, isValueType, hasGCStaticBase));
        }
    }
}
