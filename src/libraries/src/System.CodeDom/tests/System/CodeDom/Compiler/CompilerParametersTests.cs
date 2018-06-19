// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CompilerParametersTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var parameters = new CompilerParameters();
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
            Assert.Equal(-1, parameters.WarningLevel);
            Assert.Null(parameters.Win32Resource);
        }

        public static IEnumerable<object[]> Ctor_StringArray_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "a", "b" } };
        }

        [Theory]
        [MemberData(nameof(Ctor_StringArray_TestData))]
        public void Ctor_StringArray(string[] assemblyNames)
        {
            var parameters = new CompilerParameters(assemblyNames);
            Assert.Null(parameters.CompilerOptions);
            Assert.Empty(parameters.CoreAssemblyFileName);
            Assert.Empty(parameters.EmbeddedResources);
            Assert.False(parameters.GenerateExecutable);
            Assert.False(parameters.GenerateInMemory);
            Assert.False(parameters.IncludeDebugInformation);
            Assert.Empty(parameters.LinkedResources);
            Assert.Null(parameters.MainClass);
            Assert.Null(parameters.OutputAssembly);
            Assert.Equal(assemblyNames ?? Array.Empty<string>(), parameters.ReferencedAssemblies.Cast<string>());
            Assert.False(parameters.TreatWarningsAsErrors);
            Assert.Equal(IntPtr.Zero, parameters.UserToken);
            Assert.Equal(-1, parameters.WarningLevel);
            Assert.Null(parameters.Win32Resource);
        }

        public static IEnumerable<object[]> Ctor_StringArray_String_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new string[0], "" };
            yield return new object[] { new string[] { "a", "b" }, "outputName" };
        }

        [Theory]
        [MemberData(nameof(Ctor_StringArray_String_TestData))]
        public void Ctor_StringArray_String(string[] assemblyNames, string outputName)
        {
            var parameters = new CompilerParameters(assemblyNames, outputName);
            Assert.Null(parameters.CompilerOptions);
            Assert.Empty(parameters.CoreAssemblyFileName);
            Assert.Empty(parameters.EmbeddedResources);
            Assert.False(parameters.GenerateExecutable);
            Assert.False(parameters.GenerateInMemory);
            Assert.False(parameters.IncludeDebugInformation);
            Assert.Empty(parameters.LinkedResources);
            Assert.Null(parameters.MainClass);
            Assert.Equal(outputName, parameters.OutputAssembly);
            Assert.Equal(assemblyNames ?? Array.Empty<string>(), parameters.ReferencedAssemblies.Cast<string>());
            Assert.False(parameters.TreatWarningsAsErrors);
            Assert.Equal(IntPtr.Zero, parameters.UserToken);
            Assert.Equal(-1, parameters.WarningLevel);
            Assert.Null(parameters.Win32Resource);
        }

        public static IEnumerable<object[]> Ctor_StringArray_String_Bool_TestData()
        {
            yield return new object[] { null, null, true };
            yield return new object[] { new string[0], "", false };
            yield return new object[] { new string[] { "a", "b" }, "outputName", true };
        }

        [Theory]
        [MemberData(nameof(Ctor_StringArray_String_Bool_TestData))]
        public void Ctor_StringArray_String_Bool(string[] assemblyNames, string outputName, bool includeDebugInformation)
        {
            var parameters = new CompilerParameters(assemblyNames, outputName, includeDebugInformation);
            Assert.Null(parameters.CompilerOptions);
            Assert.Empty(parameters.CoreAssemblyFileName);
            Assert.Empty(parameters.EmbeddedResources);
            Assert.False(parameters.GenerateExecutable);
            Assert.False(parameters.GenerateInMemory);
            Assert.Equal(includeDebugInformation, parameters.IncludeDebugInformation);
            Assert.Empty(parameters.LinkedResources);
            Assert.Null(parameters.MainClass);
            Assert.Equal(outputName, parameters.OutputAssembly);
            Assert.Equal(assemblyNames ?? Array.Empty<string>(), parameters.ReferencedAssemblies.Cast<string>());
            Assert.False(parameters.TreatWarningsAsErrors);
            Assert.Equal(IntPtr.Zero, parameters.UserToken);
            Assert.Equal(-1, parameters.WarningLevel);
            Assert.Null(parameters.Win32Resource);
        }

        [Fact]
        public void TempFiles_Set_GetReturnsExpected()
        {
            var newValue = new TempFileCollection();
            var parameters = new CompilerParameters() { TempFiles = newValue };
            Assert.Same(newValue, parameters.TempFiles);

            parameters.TempFiles = null;
            Assert.Empty(parameters.TempFiles);
            Assert.Same(parameters.TempFiles, parameters.TempFiles);
        }
    }
}
