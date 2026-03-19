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
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeMethodMissingRequiresUnsafeCodeFixProvider)), Shared]
    public sealed class UnsafeMethodMissingRequiresUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.UnsafeMethodMissingRequiresUnsafe));

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            SupportedDiagnostics.Select(dd => dd.Id).ToImmutableArray();

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            if (await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var declarationNode = node.AncestorsAndSelf().FirstOrDefault(
                n => n is Microsoft.CodeAnalysis.CSharp.Syntax.BaseMethodDeclarationSyntax
                  or Microsoft.CodeAnalysis.CSharp.Syntax.LocalFunctionStatementSyntax
                  or Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax
                  or Microsoft.CodeAnalysis.CSharp.Syntax.IndexerDeclarationSyntax);
            if (declarationNode is null)
                return;

            var title = new LocalizableResourceString(
                nameof(Resources.UnsafeMethodMissingRequiresUnsafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources)).ToString();

            context.RegisterCodeFix(CodeAction.Create(
                title: title,
                createChangedDocument: ct => AddRequiresUnsafeAttributeAsync(document, declarationNode, ct),
                equivalenceKey: title), diagnostic);
        }

        private static async Task<Document> AddRequiresUnsafeAttributeAsync(
            Document document,
            SyntaxNode declarationNode,
            CancellationToken cancellationToken)
        {
            if (await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } model)
                return document;

            if (model.Compilation.GetBestTypeByMetadataName(RequiresUnsafeAnalyzer.FullyQualifiedRequiresUnsafeAttribute) is not { } attributeSymbol)
                return document;

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var attribute = generator.Attribute(generator.TypeExpression(attributeSymbol))
                .WithAdditionalAnnotations(Simplifier.Annotation, Simplifier.AddImportsAnnotation);

            editor.AddAttribute(declarationNode, attribute);

            return editor.GetChangedDocument();
        }
    }
}
#endif
