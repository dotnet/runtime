// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Interop.UnitTests;
using Xunit;

namespace JSImportGenerator.Unit.Tests
{
    /// <summary>
    /// Verifies that the generated output compiles under the updated memory safety rules ("unsafe evolution"),
    /// where an <c>unsafe</c> modifier on a type establishes no context for the members inside it.
    /// </summary>
    public class UnsafeCodeGeneration
    {
        public static IEnumerable<object[]> Snippets()
        {
            yield return new object[] { nameof(CodeSnippets.AllDefault), CodeSnippets.AllDefault };
            yield return new object[] { nameof(CodeSnippets.AllAnnotated), CodeSnippets.AllAnnotated };
            yield return new object[] { nameof(CodeSnippets.AllAnnotatedExport), CodeSnippets.AllAnnotatedExport };
        }

        [Theory]
        [MemberData(nameof(Snippets))]
        public void GeneratedOutputCompilesUnderUpdatedRules(string name, string source)
        {
            _ = name;

            Compilation comp = TestUtils.CreateCompilation(source, allowUnsafe: true);

            // Roslyn does not expose the memory safety rules version through a public API yet, so opt in through
            // the same feature flag the compiler uses. It lives on the parse options, so every tree is re-parsed.
            var parseOptions = ((CSharpParseOptions)comp.SyntaxTrees.First().Options)
                .WithFeatures([new KeyValuePair<string, string>("updated-memory-safety-rules", "")]);
            comp = comp.RemoveAllSyntaxTrees().AddSyntaxTrees(
                comp.SyntaxTrees.Select(t => CSharpSyntaxTree.ParseText(t.GetText(), parseOptions, t.FilePath)));

            // CS9377 ("the 'unsafe' modifier does not have any effect here") sits above the default warning
            // level, so it has to be raised or the assertion below could never observe it.
            comp = comp.WithOptions(((CSharpCompilationOptions)comp.Options).WithWarningLevel(9999));

            Compilation newComp = TestUtils.RunGenerators(comp, out var generatorDiags,
                new Microsoft.Interop.JavaScript.JSImportGenerator(),
                new Microsoft.Interop.JavaScript.JSExportGenerator());

            Assert.Empty(generatorDiags);

            // CS9377 reports an 'unsafe' modifier that has no effect under these rules. It is suppressed in
            // generated files by default, so it is asserted on explicitly rather than left to the error check.
            var unexpected = newComp.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error || d.Id is "CS9377")
                .Select(d => $"{d.Id}: {d.GetMessage()} @ {d.Location.GetLineSpan()}")
                .ToList();

            Assert.Empty(unexpected);
        }
    }
}
