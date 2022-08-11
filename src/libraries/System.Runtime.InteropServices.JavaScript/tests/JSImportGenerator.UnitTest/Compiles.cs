// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System;
using Xunit;
using LibraryImportGenerator.UnitTests;

namespace JSImportGenerator.Unit.Tests
{
    public class Compiles
    {
        public static IEnumerable<object[]> CodeSnippetsToCompile()
        {
            yield return new object[] { CodeSnippets.TrivialClassDeclarations };
            yield return new object[] { CodeSnippets.AllDefault };
            yield return new object[] { CodeSnippets.AllAnnotated };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<int>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<byte>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<bool>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<char>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<string>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<JSObject>() };
            yield return new object[] { CodeSnippets.DefaultReturnMarshaler<Exception>() };
        }


        [Theory]
        [MemberData(nameof(CodeSnippetsToCompile))]
        public async Task ValidateSnippets(string source)
        {
            Compilation comp = await TestUtils.CreateCompilation(source, allowUnsafe: true);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags,
                new Microsoft.Interop.JavaScript.JSImportGenerator(),
                new Microsoft.Interop.JavaScript.JSExportGenerator());

            // uncomment for debugging JSTestUtils.DumpCode(source, newComp, generatorDiags);

            Assert.Empty(generatorDiags);

            TestUtils.AssertPostSourceGeneratorCompilation(newComp);
        }
    }
}
