// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop.UnitTests.Verifiers
{
    internal static class CSharpVerifierHelper
    {
        /// <summary>
        /// By default, the compiler reports diagnostics for nullable reference types at
        /// <see cref="DiagnosticSeverity.Warning"/>, and the analyzer test framework defaults to only validating
        /// diagnostics at <see cref="DiagnosticSeverity.Error"/>. This map contains all compiler diagnostic IDs
        /// related to nullability mapped to <see cref="ReportDiagnostic.Error"/>, which is then used to enable all
        /// of these warnings for default validation during analyzer and code fix tests.
        /// </summary>
        internal static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } = GetNullableWarningsFromCompiler();

        private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
        {
            string[] args = { "/warnaserror:nullable" };
            var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
            return commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;
        }

        internal static Func<Solution, ProjectId, Solution> GetAllDiagonsticsEnabledTransform(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            return (solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                var compilationOptions = project.CompilationOptions!;
                var diagnosticOptions = compilationOptions.SpecificDiagnosticOptions.SetItems(NullableWarnings);

                // Explicitly enable diagnostics that are not enabled by default
                var enableAnalyzersOptions = new Dictionary<string, ReportDiagnostic>();
                foreach (var analyzer in analyzers)
                {
                    foreach (var diagnostic in analyzer.SupportedDiagnostics)
                    {
                        if (diagnostic.IsEnabledByDefault)
                            continue;

                        // Map the default severity to the reporting behaviour.
                        // We cannot simply use ReportDiagnostic.Default here, as diagnostics that are not enabled by default
                        // are treated as suppressed (regardless of their default severity).
                        var report = diagnostic.DefaultSeverity switch
                        {
                            DiagnosticSeverity.Error => ReportDiagnostic.Error,
                            DiagnosticSeverity.Warning => ReportDiagnostic.Warn,
                            DiagnosticSeverity.Info => ReportDiagnostic.Info,
                            DiagnosticSeverity.Hidden => ReportDiagnostic.Hidden,
                            _ => ReportDiagnostic.Default
                        };
                        enableAnalyzersOptions.Add(diagnostic.Id, report);
                    }
                }

                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                    compilationOptions.SpecificDiagnosticOptions
                        .SetItems(NullableWarnings)
                        .AddRange(enableAnalyzersOptions)
                        .AddRange(TestUtils.BindingRedirectWarnings));
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                return solution;
            };
        }

        internal static Func<Solution, ProjectId, Solution> GetTargetFrameworkAnalyzerOptionsProviderTransform(TestTargetFramework targetFramework)
        {
            return (solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;
                string tfmEditorConfig = targetFramework switch
                {
                    TestTargetFramework.Framework => """
                        is_global = true
                        build_property.TargetFrameworkIdentifier = .NETFramework
                        build_property.TargetFrameworkVersion = v4.8
                        """,
                    TestTargetFramework.Standard => """
                        is_global = true
                        build_property.TargetFrameworkIdentifier = .NETStandard
                        build_property.TargetFrameworkVersion = v2.0
                        """,
                    TestTargetFramework.Core => """
                        is_global = true
                        build_property.TargetFrameworkIdentifier = .NETCoreApp
                        build_property.TargetFrameworkVersion = v3.1
                        """,
                    TestTargetFramework.Net6 => """
                        is_global = true
                        build_property.TargetFrameworkIdentifier = .NETCoreApp
                        build_property.TargetFrameworkVersion = v6.0
                        """,
                    // Replicate the product case where we don't have these properties
                    // since we don't have a good mechanism to ship MSBuild files from dotnet/runtime
                    // in the SDK.
                    TestTargetFramework.Net => string.Empty,
                    _ => throw new System.Diagnostics.UnreachableException()
                };
                return solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(projectId),
                    "TargetFrameworkConfig.editorconfig",
                    SourceText.From(tfmEditorConfig, encoding: System.Text.Encoding.UTF8),
                    filePath: "/TargetFrameworkConfig.editorconfig");
            };
        }
    }
}
