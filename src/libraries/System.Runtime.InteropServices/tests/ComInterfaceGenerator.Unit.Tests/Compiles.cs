// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class Compiles
    {
        public static IEnumerable<object[]> VTableIndexCodeSnippetsToCompile()
        {
            yield return new[] { CodeSnippets.SpecifiedMethodIndexNoExplicitParameters };
            yield return new[] { CodeSnippets.SpecifiedMethodIndexNoExplicitParametersNoImplicitThis };
            yield return new[] { CodeSnippets.SpecifiedMethodIndexNoExplicitParametersCallConvWithCallingConventions };

            // Basic marshalling validation
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<byte>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<sbyte>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<short>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<ushort>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<int>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<uint>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<long>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<ulong>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<float>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<double>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<IntPtr>() };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers<UIntPtr>() };

            // Attributed marshalling model validation
            yield return new[] { CodeSnippets.BasicParametersAndModifiers(CodeSnippets.CustomTypeMarshallingTestsTypeName) + CodeSnippets.SimpleCustomTypeMarshallingDeclaration };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers(CodeSnippets.CustomTypeMarshallingTestsTypeName) + CodeSnippets.TwoStageCustomTypeMarshallingDeclaration };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers(CodeSnippets.CustomTypeMarshallingTestsTypeName) + CodeSnippets.OptionalCallerAllocatedBufferMarshallingDeclaration };
            yield return new[] { CodeSnippets.BasicParametersAndModifiers(CodeSnippets.CustomTypeMarshallingTestsTypeName) + CodeSnippets.UnmanagedResourcesCustomTypeMarshallingDeclaration };

            // SafeHandles
            yield return new[] { CodeSnippets.BasicParametersAndModifiers("Microsoft.Win32.SafeHandles.SafeFileHandle") };
        }

        [Theory]
        [MemberData(nameof(VTableIndexCodeSnippetsToCompile))]
        public async Task ValidateVTableIndexSnippets(string source)
        {
            Compilation comp = await TestUtils.CreateCompilation(source);
            // Allow the Native nested type name to be missing in the pre-source-generator compilation
            TestUtils.AssertPreSourceGeneratorCompilation(comp, "CS0426");

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.VtableIndexStubGenerator());
            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }
    }
}
