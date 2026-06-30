// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.IL;
using Internal.TypeSystem;

using Xunit;

namespace ILCompiler.Compiler.Tests
{
    public class NameManglerTests
    {
        [Fact]
        public void SanitizedAssemblyNameCollisionsAreDisambiguatedDeterministically()
        {
            var target = new TargetDetails(TargetArchitecture.X64, TargetOS.Windows, TargetAbi.NativeAot);
            var context = new CompilerTypeSystemContext(target, SharedGenericsMode.CanonicalReferenceTypes, DelegateFeature.All);

            context.InputFilePaths = new Dictionary<string, string> {
                { "Test.CoreLib", @"Test.CoreLib.dll" },
                { "A_B", @"A_B.dll" },
                { "A.B", @"A.B.dll" },
                };
            context.ReferenceFilePaths = new Dictionary<string, string>();

            context.SetSystemModule(context.GetModuleForSimpleName("Test.CoreLib"));

            ModuleDesc moduleWithDot = context.GetModuleForSimpleName("A.B");
            ModuleDesc moduleWithUnderscore = context.GetModuleForSimpleName("A_B");

            var nameMangler = new NativeAotNameMangler(new UnixNodeMangler());

            string typeNameWithUnderscoreAssembly = nameMangler.GetMangledTypeName(moduleWithUnderscore.GetType("ManglerCollision"u8, "TestType"u8)).ToString();
            string typeNameWithDotAssembly = nameMangler.GetMangledTypeName(moduleWithDot.GetType("ManglerCollision"u8, "TestType"u8)).ToString();

            Assert.Equal("A_B_ManglerCollision_TestType", typeNameWithDotAssembly);
            Assert.Equal("A_B_0_ManglerCollision_TestType", typeNameWithUnderscoreAssembly);
        }
    }
}
