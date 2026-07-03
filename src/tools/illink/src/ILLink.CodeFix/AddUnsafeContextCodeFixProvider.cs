// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Introduces an <c>unsafe</c> context (block or <c>unsafe(...)</c> expression) for operations flagged by
    /// IL5006 while migrating to the updated memory-safety rules.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeContextCodeFixProvider)), Shared]
    public sealed class AddUnsafeContextCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public static string DiagnosticId => ILLink.Shared.DiagnosticId.OperationRequiresUnsafeContext.AsString();

        public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private static string Title => new LocalizableResourceString(
            nameof(Resources.AddUnsafeContextCodeFixTitle), Resources.ResourceManager, typeof(Resources)).ToString();

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(
                CodeAction.Create(Title, ct => FixAsync(context.Document, diagnostic, ct), equivalenceKey: Title),
                diagnostic);
            return Task.CompletedTask;
        }

        private static async Task<Document> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root ||
                await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false) is not { } semanticModel)
                return document;

            return UnsafeContextStrategy.Fix(root, semanticModel, diagnostic.Location.SourceSpan) is { } newRoot
                ? document.WithSyntaxRoot(newRoot)
                : document;
        }
    }
}
#endif
