// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            var newCompDiags = newComp.GetDiagnostics();
            Assert.Empty(newCompDiags);
        }
    }
}
