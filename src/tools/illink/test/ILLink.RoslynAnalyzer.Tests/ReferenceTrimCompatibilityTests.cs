// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.IO;
using ILLink.CodeFix;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer,
    ILLink.CodeFix.RequiresUnreferencedCodeCodeFixProvider>;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ReferenceTrimCompatibilityTests
    {
        [Fact]
        public async Task EmitsWarningForReferenceWithoutIsTrimmable_WhenPropertyEnabled()
        {
            var referencedSource = "public class ReferencedClass { }";
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresUnreferencedCodeAnalyzer, RequiresUnreferencedCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.VerifyReferenceTrimCompatibility} = true
                """)));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DiagnosticId.ReferenceNotMarkedIsTrimmable).WithArguments("ReferencedAssembly"));
            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotEmitWarning_WhenVerifyReferenceTrimCompatibilityDisabled()
        {
            var referencedSource = "public class ReferencedClass { }";
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresUnreferencedCodeAnalyzer, RequiresUnreferencedCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.VerifyReferenceTrimCompatibility} = false
                """)));

            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotEmitWarning_WhenReferenceMarkedIsTrimmable()
        {
            var referencedSource = """
                [assembly: System.Reflection.AssemblyMetadata("IsTrimmable", "True")]
                public class ReferencedClass { }
                """;
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresUnreferencedCodeAnalyzer, RequiresUnreferencedCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.VerifyReferenceTrimCompatibility} = true
                """)));

            await test.RunAsync();
        }
    }
}
