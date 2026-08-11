// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.IO;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class ReferenceCompatibilityTestUtils
    {
        public static CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test CreateTestWithReference<TAnalyzer, TCodeFix>(string mainSource, string referenceSource)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
        {
            var referencedMetadata = CreateReferencedMetadata(referenceSource);
            var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
            {
                TestCode = mainSource
            };
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project is null)
                    return solution;
                project = project.AddMetadataReference(referencedMetadata);
                return project.Solution;
            });
            return test;

            static MetadataReference CreateReferencedMetadata(string referencedSource)
            {
                var refs = SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences();
                var referencedCompilation = CSharpCompilation.Create(
                    "ReferencedAssembly",
                    new[] { SyntaxFactory.ParseSyntaxTree(referencedSource, new CSharpParseOptions(languageVersion: LanguageVersion.Preview)) },
                    refs,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                var referencedImage = new MemoryStream();
                referencedCompilation.Emit(referencedImage);
                referencedImage.Position = 0;
                return MetadataReference.CreateFromStream(referencedImage);
            }
        }

        public static CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test CreateTestWithCompilationReference<TAnalyzer, TCodeFix>(string mainSource, string referenceSource)
            where TAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
        {
            var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test
            {
                TestCode = mainSource
            };
            test.SolutionTransforms.Add((solution, projectId) =>
            {
                ProjectId referencedProjectId = ProjectId.CreateNewId();
                solution = solution
                    .AddProject(referencedProjectId, "ReferencedAssembly", "ReferencedAssembly", LanguageNames.CSharp)
                    .WithProjectParseOptions(referencedProjectId, new CSharpParseOptions(languageVersion: LanguageVersion.Preview))
                    .WithProjectCompilationOptions(referencedProjectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                Project referencedProject = solution.GetProject(referencedProjectId)!;
                solution = referencedProject
                    .AddMetadataReferences(SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences())
                    .AddDocument("ReferencedAssembly.cs", SourceText.From(referenceSource))
                    .Project
                    .Solution;

                Project project = solution.GetProject(projectId)!;
                return project.AddProjectReference(new ProjectReference(referencedProjectId)).Solution;
            });
            return test;
        }
    }
}
