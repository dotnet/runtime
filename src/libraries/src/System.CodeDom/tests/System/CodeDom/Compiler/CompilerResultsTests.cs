// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CompilerResultsTests
    {
        public static IEnumerable<object[]> Ctor_TempFileCollection_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new TempFileCollection() };
        }

        [Theory]
        [MemberData(nameof(Ctor_TempFileCollection_TestData))]
        public void Ctor_TempFileCollection(TempFileCollection tempFiles)
        {
            var results = new CompilerResults(tempFiles);
            Assert.Null(results.CompiledAssembly);
            Assert.Empty(results.Errors);
            Assert.Equal(0, results.NativeCompilerReturnValue);
            Assert.Null(results.PathToAssembly);
            Assert.Empty(results.Output);
            Assert.Same(tempFiles, results.TempFiles);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Assembly.Load ignores CodeBase by design, except on netfx. See https://github.com/dotnet/coreclr/issues/10561")]
        public void CompiledAssembly_ValidPathToAssembly_ReturnsExpected()
        {
            var results = new CompilerResults(null) { PathToAssembly = typeof(int).Assembly.EscapedCodeBase };
            Assert.Equal(typeof(int).Assembly, results.CompiledAssembly);
            Assert.Same(results.CompiledAssembly, results.CompiledAssembly);
        }

        [Fact]
        public void CompiledAssembly_Set_GetReturnsExpecte()
        {
            Assembly assembly = typeof(CompilerResultsTests).Assembly;
            var results = new CompilerResults(null) { CompiledAssembly = assembly };
            Assert.Same(assembly, results.CompiledAssembly);
        }
    }
}
