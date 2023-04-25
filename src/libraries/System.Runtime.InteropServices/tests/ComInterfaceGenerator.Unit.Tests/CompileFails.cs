// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace ComInterfaceGenerator.Unit.Tests
{
    public class CompileFails
    {
        private static string ID(
            [CallerLineNumber] int lineNumber = 0,
            [CallerFilePath] string? filePath = null)
            => TestUtils.GetFileLineName(lineNumber, filePath);

        public static IEnumerable<object[]> ComInterfaceGeneratorSnippetsToCompile()
        {
            CodeSnippets codeSnippets = new(new GeneratedComInterfaceAttributeProvider());
            // Inheriting from multiple GeneratedComInterface-marked interfaces.
            yield return new object[] { ID(), codeSnippets.DerivedComInterfaceTypeMultipleComInterfaceBases, 1, 0 };
        }

        [Theory]
        [MemberData(nameof(ComInterfaceGeneratorSnippetsToCompile))]
        public async Task ValidateComInterfaceGeneratorSnippets(string id, string source, int expectedGeneratorErrors, int expectedCompilerErrors)
        {
            TestUtils.Use(id);
            Compilation comp = await TestUtils.CreateCompilation(source);
            TestUtils.AssertPreSourceGeneratorCompilation(comp);

            var newComp = TestUtils.RunGenerators(comp, out var generatorDiags, new Microsoft.Interop.ComInterfaceGenerator());

            // Verify the compilation failed with errors.
            IEnumerable<Diagnostic> generatorErrors = generatorDiags.Where(d => d.Severity == DiagnosticSeverity.Error);
            int generatorErrorCount = generatorErrors.Count();
            Assert.True(
                expectedGeneratorErrors == generatorErrorCount,
                $"Expected {expectedGeneratorErrors} errors, but encountered {generatorErrorCount}. Errors: {string.Join(Environment.NewLine, generatorErrors.Select(d => d.ToString()))}");

            IEnumerable<Diagnostic> compilerErrors = newComp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
            int compilerErrorCount = compilerErrors.Count();
            Assert.True(
                expectedCompilerErrors == compilerErrorCount,
                $"Expected {expectedCompilerErrors} errors, but encountered {compilerErrorCount}. Errors: {string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString()))}");
        }
    }
}
