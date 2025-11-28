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
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
    ILLink.RoslynAnalyzer.RequiresDynamicCodeAnalyzer,
    ILLink.CodeFix.RequiresDynamicCodeCodeFixProvider>;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ILLink.CodeFix;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ReferenceAotCompatibilityTests
    {
        [Fact]
        public async Task EmitsWarningForReferenceWithoutIsAotCompatible_WhenPropertyEnabled()
        {
            var referencedSource = "public class ReferencedClass { }";
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresDynamicCodeAnalyzer, RequiresDynamicCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableAotAnalyzer} = true
                build_property.VerifyReferenceAotCompatibility = true
                """)));
            test.ExpectedDiagnostics.Add(VerifyCS.Diagnostic(DiagnosticId.ReferenceNotMarkedIsAotCompatible).WithArguments("ReferencedAssembly"));
            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotEmitWarning_WhenVerifyReferenceAotCompatibilityDisabled()
        {
            var referencedSource = "public class ReferencedClass { }";
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresDynamicCodeAnalyzer, RequiresDynamicCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableAotAnalyzer} = true
                build_property.VerifyReferenceAotCompatibility = false
                """)));
            await test.RunAsync();
        }

        [Fact]
        public async Task DoesNotEmitWarning_WhenReferenceMarkedIsAotCompatible()
        {
            var referencedSource = """
                [assembly: System.Reflection.AssemblyMetadata("IsAotCompatible", "True")]
                public class ReferencedClass { }
                """;
            var testSource = "public class MainClass { ReferencedClass c; }";

            var test = ReferenceCompatibilityTestUtils.CreateTestWithReference<RequiresDynamicCodeAnalyzer, RequiresDynamicCodeCodeFixProvider>(
                testSource, referencedSource);
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Microsoft.CodeAnalysis.Text.SourceText.From($"""
                is_global = true
                build_property.{ILLink.RoslynAnalyzer.MSBuildPropertyOptionNames.EnableAotAnalyzer} = true
                build_property.VerifyReferenceAotCompatibility = true
                """)));
            await test.RunAsync();
        }
    }
}
