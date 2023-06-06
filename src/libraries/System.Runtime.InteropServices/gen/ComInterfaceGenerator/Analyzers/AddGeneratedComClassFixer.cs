// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.Interop.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class AddGeneratedComClassFixer : CodeFixProvider
    {
        public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AnalyzerDiagnostics.Ids.AddGeneratedComClassAttribute);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                .ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var node = root.FindNode(diagnosticSpan);
            if (node == null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    SR.AddGeneratedComClassAttributeTitle,
                    ct => AddGeneratedComClassAsync(
                        context.Document,
                        node,
                        ct),
                    nameof(AddGeneratedComClassAsync)),
                diagnostic);
        }

        private static async Task<Document> AddGeneratedComClassAsync(Document document, SyntaxNode node, CancellationToken ct)
        {
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            editor.ReplaceNode(node, (node, gen) =>
            {
                var attribute = gen.Attribute(gen.TypeExpression(editor.SemanticModel.Compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComClassAttribute)));
                var updatedNode = gen.AddAttributes(node, attribute);
                var declarationModifiers = gen.GetModifiers(updatedNode);
                if (!declarationModifiers.IsPartial)
                {
                    updatedNode = gen.WithModifiers(updatedNode, declarationModifiers.WithPartial(true));
                }
                return updatedNode;
            });

            return editor.GetChangedDocument();
        }
    }
}
