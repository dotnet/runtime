// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void CompiledAssembly_GetWithPathToAssemblySet_ReturnsExpectedAssembly()
        {
            var results = new CompilerResults(null) { PathToAssembly = (PlatformDetection.IsBrowser ? "/" : string.Empty) + typeof(CompilerResultsTests).Assembly.Location };

            Assert.NotNull(results.CompiledAssembly);
            Assert.Equal(typeof(CompilerResultsTests).Assembly.FullName, results.CompiledAssembly.FullName);
            Assert.Same(results.CompiledAssembly, results.CompiledAssembly);
        }

        [Fact]
        public void CompiledAssembly_GetWithPathToAssemblyNotSet_ReturnsNull()
        {
            var results = new CompilerResults(null);

            Assert.Null(results.CompiledAssembly);
        }

        public static IEnumerable<object[]> CompiledAssembly_Set_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { typeof(CompilerResultsTests).Assembly };
        }

        [Theory]
        [MemberData(nameof(CompiledAssembly_Set_TestData))]
        public void CompiledAssembly_Set_GetReturnsExpected(Assembly value)
        {
            var results = new CompilerResults(null) { CompiledAssembly = value };
            Assert.Same(value, results.CompiledAssembly);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void PathToAssembly_Set_GetReturnsExpected(string value)
        {
            var results = new CompilerResults(null) { PathToAssembly = value };
            Assert.Same(value, results.PathToAssembly);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void NativeCompilerReturnValue_Set_GetReturnsExpected(int value)
        {
            var results = new CompilerResults(null) { NativeCompilerReturnValue = value };
            Assert.Equal(value, results.NativeCompilerReturnValue);
        }

        public static IEnumerable<object[]> TempFiles_Set_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new TempFileCollection() };
        }

        [Theory]
        [MemberData(nameof(TempFiles_Set_TestData))]
        public void TempFiles_Set_GetReturnsExpected(TempFileCollection value)
        {
            var results = new CompilerResults(null) { TempFiles = value };
            Assert.Same(value, results.TempFiles);
        }
    }
}
