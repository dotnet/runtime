// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace System.Text.RegularExpressions.Tests
{
    public static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static async Task VerifyAnalyzerAsync(string source, ReferenceAssemblies? references = null)
        {
            await VerifyCodeFixAsync(source, source, references);
        }

        public static async Task VerifyCodeFixAsync(string source, string fixedSource, ReferenceAssemblies? references = null)
        {
            Test test = new Test(references)
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            await test.RunAsync(CancellationToken.None);
        }

        public class Test : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        {
            public Test(ReferenceAssemblies? references = null)
            {
                if (references != null)
                {
                    ReferenceAssemblies = references;
                }
                else
                {
                    // Clear out the default reference assemblies. We explicitly add references from the live ref pack,
                    // so we don't want the Roslyn test infrastructure to resolve/add any default reference assemblies
                    ReferenceAssemblies = new ReferenceAssemblies(string.Empty);
                    TestState.AdditionalReferences.AddRange(RegexGeneratorHelper.References);
                }
            }

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Preview;

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }

            // CS8795: Partial method '{0}' must have an implementation part because it has accessibility modifiers.
            protected override bool IsCompilerDiagnosticIncluded(Diagnostic diagnostic, CompilerDiagnostics compilerDiagnostics) => base.IsCompilerDiagnosticIncluded(diagnostic, compilerDiagnostics) && diagnostic.Id != "CS8795";
        }
    }
}
