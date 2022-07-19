// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class CustomTypeMarshallerFixer : CodeFixProvider
    {
        private const string AddMissingCustomTypeMarshallerMembersKey = nameof(AddMissingCustomTypeMarshallerMembersKey);

        private sealed class CustomFixAllProvider : DocumentBasedFixAllProvider
        {
            protected override async Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                SyntaxNode? root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                if (root == null)
                    return document;

                DocumentEditor editor = await DocumentEditor.CreateAsync(document, fixAllContext.CancellationToken).ConfigureAwait(false);

                switch (fixAllContext.CodeActionEquivalenceKey)
                {
                    case AddMissingCustomTypeMarshallerMembersKey:
                        foreach (IGrouping<TextSpan, Diagnostic> diagnosticsBySpan in diagnostics.GroupBy(d => d.Location.SourceSpan))
                        {
                            SyntaxNode node = root.FindNode(diagnosticsBySpan.Key);

                            AddMissingMembers(editor, diagnosticsBySpan, node);
                        }
                        break;
                    default:
                        break;
                }

                return editor.GetChangedDocument();
            }

            private static void AddMissingMembers(DocumentEditor editor, IEnumerable<Diagnostic> diagnostics, SyntaxNode node)
            {
                var (missingMemberNames, _) = GetRequiredShapeMissingMemberNames(diagnostics);
                ITypeSymbol marshallerType = (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(node);
                editor.ReplaceNode(
                    node,
                    (node, gen) =>
                        CustomTypeMarshallerFixer.AddMissingMembers(
                            node,
                            marshallerType,
                            missingMemberNames,
                            editor.SemanticModel.Compilation,
                            gen));
            }
        }

        public override FixAllProvider? GetFixAllProvider() => new CustomFixAllProvider();

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(
                AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape,
                AnalyzerDiagnostics.Ids.MissingAllocatingMarshallingFallback,
                AnalyzerDiagnostics.Ids.ProvidedMethodsNotSpecifiedInFeatures);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Document doc = context.Document;
            SyntaxNode? root = await doc.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            SyntaxNode node = root.FindNode(context.Span);
            var (missingMemberNames, missingMembersDiagnostics) = GetRequiredShapeMissingMemberNames(context.Diagnostics);

            if (missingMembersDiagnostics.Count > 0)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        SR.AddMissingCustomTypeMarshallerMembers,
                        ct => AddMissingMembers(doc, node, missingMemberNames, ct),
                        AddMissingCustomTypeMarshallerMembersKey),
                    missingMembersDiagnostics);
            }
        }

        private static (List<string> missingMembers, List<Diagnostic> fixedDiagnostics) GetRequiredShapeMissingMemberNames(IEnumerable<Diagnostic> diagnostics)
        {
            List<string> missingMemberNames = new();
            List<Diagnostic> requiredShapeDiagnostics = new();
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Id == AnalyzerDiagnostics.Ids.CustomMarshallerTypeMustHaveRequiredShape)
                {
                    requiredShapeDiagnostics.Add(diagnostic);
                    if (diagnostic.Properties.TryGetValue(CustomTypeMarshallerAnalyzer.MissingMemberNames.Key, out string missingMembers))
                    {
                        missingMemberNames.AddRange(missingMembers.Split(CustomTypeMarshallerAnalyzer.MissingMemberNames.Delimiter));
                    }
                }
            }

            return (missingMemberNames, requiredShapeDiagnostics);
        }

        private static async Task<Document> AddMissingMembers(Document doc, SyntaxNode node, List<string> missingMemberNames, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(doc, ct).ConfigureAwait(false);
            var gen = editor.Generator;

            SyntaxNode updatedDeclaration = AddMissingMembers(node, (ITypeSymbol)editor.SemanticModel.GetDeclaredSymbol(node, ct), missingMemberNames, editor.SemanticModel.Compilation, gen);

            editor.ReplaceNode(node, updatedDeclaration);

            return editor.GetChangedDocument();
        }

        private static SyntaxNode AddMissingMembers(SyntaxNode node, ITypeSymbol
            marshallerType, List<string> missingMemberNames, Compilation compilation, SyntaxGenerator gen)
        {
            // TODO: Implement adding the missing members for the V2 shapes
            return node;
        }
    }
}
