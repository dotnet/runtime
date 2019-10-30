// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CompilerInfoTests
    {
        [Theory]
        [InlineData("cs", new string[] { "c#", "cs", "csharp" })]
        [InlineData("vb", new string[] { "vb", "vbs", "visualbasic", "vbscript" })]
        public void GetLanguages_Invoke_ReturnsExpected(string language, string[] languages)
        {
            CompilerInfo compilerInfo = CodeDomProvider.GetCompilerInfo(language);
            Assert.Equal(languages, compilerInfo.GetLanguages());
            Assert.NotSame(compilerInfo.GetLanguages(), compilerInfo.GetLanguages());
        }

        [Theory]
        [InlineData("cs", new string[] { ".cs", "cs" })]
        [InlineData("vb", new string[] { ".vb", "vb" })]
        public void GetExtensions_Invoke_ReturnsExpected(string language, string[] extensions)
        {
            CompilerInfo compilerInfo = CodeDomProvider.GetCompilerInfo(language);
            Assert.Equal(extensions, compilerInfo.GetExtensions());
            Assert.NotSame(compilerInfo.GetExtensions(), compilerInfo.GetExtensions());
        }

        [Theory]
        [InlineData("cs")]
        [InlineData("vb")]
        public void CreateCompilerParameters_Invoke_ReturnsExpected(string language)
        {
            CompilerInfo compilerInfo = CodeDomProvider.GetCompilerInfo(language);
            CompilerParameters parameters = compilerInfo.CreateDefaultCompilerParameters();
            Assert.Null(parameters.CompilerOptions);
            Assert.Empty(parameters.CoreAssemblyFileName);
            Assert.Empty(parameters.EmbeddedResources);
            Assert.False(parameters.GenerateExecutable);
            Assert.False(parameters.GenerateInMemory);
            Assert.False(parameters.IncludeDebugInformation);
            Assert.Empty(parameters.LinkedResources);
            Assert.Null(parameters.MainClass);
            Assert.Null(parameters.OutputAssembly);
            Assert.Empty(parameters.ReferencedAssemblies);
            Assert.False(parameters.TreatWarningsAsErrors);
            Assert.Equal(IntPtr.Zero, parameters.UserToken);
            Assert.Equal(4, parameters.WarningLevel);
            Assert.Null(parameters.Win32Resource);

            Assert.NotSame(compilerInfo.CreateDefaultCompilerParameters(), compilerInfo.CreateDefaultCompilerParameters());
        }

        [Fact]
        public void CreateProvider_NullProviderOptions_ThrowsArgumentNullException()
        {
            CompilerInfo compilerInfo = CodeDomProvider.GetCompilerInfo("cs");
            AssertExtensions.Throws<ArgumentNullException>("providerOptions", () => compilerInfo.CreateProvider(null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            CompilerInfo compilerInfo = CodeDomProvider.GetCompilerInfo("cs");
            yield return new object[] { compilerInfo, compilerInfo, true };
            yield return new object[] { compilerInfo, CodeDomProvider.GetCompilerInfo("cs"), true };
            yield return new object[] { compilerInfo, CodeDomProvider.GetCompilerInfo("vb"), false };

            // .NET Core fixes a typo in .NET Full Framework and validates that the casted object
            // instead of validating the object typed parameter.
            if (!PlatformDetection.IsFullFramework)
            {
                yield return new object[] { compilerInfo, new object(), false };
            }
            yield return new object[] { compilerInfo, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Invoke_ReturnsExpected(CompilerInfo compilerInfo, object other, bool expected)
        {
            Assert.Equal(expected, compilerInfo.Equals(other));
            if (other is CompilerInfo)
            {
                Assert.Equal(expected, compilerInfo.GetHashCode().Equals(other.GetHashCode()));
            }
        }
    }
}
