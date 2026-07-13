// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeModifierMigrationCodeFixProvider)), Shared]
    public sealed class UnsafeModifierMigrationCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private static LocalizableString CodeFixTitle => new LocalizableResourceString(
            nameof(Resources.UnsafeModifierMigrationCodeFixTitle),
            Resources.ResourceManager,
            typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId.UnsafeModifierMigration.AsString()];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (!UnsafeCodeFixHelpers.IsMigrationEnabled(context.Document))
                return Task.CompletedTask;

            string title = CodeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => RemoveUnsafeModifiersAsync(context.Document, cancellationToken),
                    title),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        private static async Task<Document> RemoveUnsafeModifiersAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel ||
                await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
            {
                return document;
            }

            var removals = UnsafeMigrationAnalysis.GetModifierRemovals(semanticModel, cancellationToken);
            if (removals.IsEmpty)
                return document;

            var annotations = removals.ToDictionary(
                static removal => removal.Declaration,
                static _ => new SyntaxAnnotation());

            SyntaxNode changedRoot = root.ReplaceNodes(
                annotations.Keys,
                (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));

            SyntaxGenerator generator = SyntaxGenerator.GetGenerator(document);
            foreach (SyntaxAnnotation annotation in annotations.Values)
            {
                SyntaxNode declaration = changedRoot.GetAnnotatedNodes(annotation).Single();
                SyntaxNode replacement = generator.WithModifiers(
                    declaration,
                    generator.GetModifiers(declaration).WithIsUnsafe(false))
                    .WithLeadingTrivia(declaration.GetLeadingTrivia());

                changedRoot = changedRoot.ReplaceNode(declaration, replacement);
            }

            return document.WithSyntaxRoot(changedRoot);
        }
    }
}
#endif
