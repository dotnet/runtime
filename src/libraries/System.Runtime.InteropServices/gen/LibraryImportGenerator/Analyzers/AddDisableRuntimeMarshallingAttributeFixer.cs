// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class AddDisableRuntimeMarshallingAttributeFixer : CodeFixProvider
    {
        private const string EquivalenceKey = nameof(AddDisableRuntimeMarshallingAttributeFixer);

        private const string PropertiesFolderName = "Properties";
        private const string AssemblyInfoFileName = "AssemblyInfo.cs";

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(GeneratorDiagnostics.Ids.TypeNotSupported);

        // TODO: Write a custom fix all provider
        public override FixAllProvider? GetFixAllProvider() => null;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            List<Diagnostic> fixedDiagnostics = new(context.Diagnostics.Where(IsRequiresDiableRuntimeMarshallingDiagnostic));

            if (fixedDiagnostics.Count > 0)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add DisableRuntimeMarshallingAttribute to the assembly.",
                        ct => AddDisableRuntimeMarshallingAttributeApplicationToProject(context.Document.Project, ct),
                        EquivalenceKey),
                    fixedDiagnostics);
            }

            return Task.CompletedTask;

            static bool IsRequiresDiableRuntimeMarshallingDiagnostic(Diagnostic diagnostic)
            {
                return diagnostic.Properties.ContainsKey(GeneratorDiagnosticProperties.AddDisableRuntimeMarshallingAttribute);
            }
        }

        private static async Task<Solution> AddDisableRuntimeMarshallingAttributeApplicationToProject(Project project, CancellationToken cancellationToken)
        {
            Document? assemblyInfo =
                project.Documents.FirstOrDefault(IsPropertiesAssemblyInfo) ??
                project.AddDocument(AssemblyInfoFileName, "", folders: new[] { PropertiesFolderName });

            DocumentEditor editor = await DocumentEditor.CreateAsync(assemblyInfo, cancellationToken).ConfigureAwait(false);

            var syntaxRoot = await assemblyInfo.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(
                syntaxRoot,
                editor.Generator.AddAttributes(
                    syntaxRoot,
                    editor.Generator.Attribute(editor.Generator.DottedName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute))));

            return editor.GetChangedDocument().Project.Solution;

            static bool IsPropertiesAssemblyInfo(Document document)
            {
                // We specifically want to match a file in the Properties folder with the provided name (AssemblyInfo.cs) to match other VS templates that add this file.
                // We are very strict about this to ensure that we discover the correct file when it is already created and added to the project.
                return document.Name == AssemblyInfoFileName
                    && document.Folders.Count == 1
                    && document.Folders[0] == PropertiesFolderName;
            }
        }
    }
}
