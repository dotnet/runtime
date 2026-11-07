// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFix.UnsafeContext;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Introduces inner unsafe contexts for operations that require one under the updated memory-safety rules
    /// (unsafe-v2): <c>unsafe { }</c> blocks preceded by a <c>// SAFETY: To be audited</c> line, or
    /// <c>/* SAFETY: To be audited */ unsafe(...)</c> expressions. It never changes member modifiers, signatures or
    /// project options.
    /// </summary>
    /// <remarks>
    /// Equivalence keys identify the planning policy rather than the generated form, so that Fix All (including
    /// <c>dotnet format</c>, which uses the first registered action) applies one policy to every occurrence while
    /// each occurrence still gets the form that policy selects for it.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeContextCodeFixProvider)), Shared]
    public sealed class UnsafeContextCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        internal const string BlockFirstEquivalenceKey = nameof(UnsafeContextCodeFixProvider) + "." + nameof(UnsafeContextPolicy.BlockFirst);
        internal const string ExpressionFirstEquivalenceKey = nameof(UnsafeContextCodeFixProvider) + "." + nameof(UnsafeContextPolicy.ExpressionFirst);
        internal const string BodyWideEquivalenceKey = nameof(UnsafeContextCodeFixProvider) + "." + nameof(UnsafeContextPolicy.BodyWide);

        private static readonly FixAllProvider s_fixAllProvider = FixAllProvider.Create(async (context, document, diagnostics) =>
        {
            var result = await UnsafeContextDocumentFixer.FixAsync(
                document,
                diagnostics,
                GetPolicy(context.CodeActionEquivalenceKey),
                focus: null,
                context.CancellationToken).ConfigureAwait(false);
            return result?.Document;
        });

        public override ImmutableArray<string> FixableDiagnosticIds => UnsafeContextFacts.FixableDiagnosticIds;

        public override FixAllProvider GetFixAllProvider() => s_fixAllProvider;

        /// <summary>Registers the preferred fix, plus the expression-first and body-wide variants when they differ.</summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                CancellationToken cancellationToken = context.CancellationToken;
                UnsafeContextFixResult? preferred = await FixAsync(context.Document, diagnostic, UnsafeContextPolicy.BlockFirst, cancellationToken).ConfigureAwait(false);
                if (preferred is null)
                    continue;

                string preferredTitle = preferred.Form == UnsafeContextForm.Block
                    ? Resources.WrapInUnsafeBlockCodeFixTitle
                    : Resources.WrapExpressionInUnsafeContextCodeFixTitle;
                Register(context, diagnostic, preferredTitle, BlockFirstEquivalenceKey, preferred.Document);

                if (preferred.Form == UnsafeContextForm.Block
                    && await FixAsync(context.Document, diagnostic, UnsafeContextPolicy.ExpressionFirst, cancellationToken).ConfigureAwait(false) is { Form: UnsafeContextForm.Expression } expression)
                {
                    Register(context, diagnostic, Resources.WrapExpressionInUnsafeContextCodeFixTitle, ExpressionFirstEquivalenceKey, expression.Document);
                }

                if (await FixAsync(context.Document, diagnostic, UnsafeContextPolicy.BodyWide, cancellationToken).ConfigureAwait(false) is { } body
                    && !await HaveSameTextAsync(body.Document, preferred.Document, cancellationToken).ConfigureAwait(false))
                {
                    Register(context, diagnostic, Resources.WrapBodyInUnsafeBlockCodeFixTitle, BodyWideEquivalenceKey, body.Document);
                }
            }
        }

        private static Task<UnsafeContextFixResult?> FixAsync(Document document, Diagnostic diagnostic, UnsafeContextPolicy policy, CancellationToken cancellationToken) =>
            UnsafeContextDocumentFixer.FixAsync(document, [diagnostic], policy, focus: diagnostic, cancellationToken);

        private static void Register(CodeFixContext context, Diagnostic diagnostic, string title, string equivalenceKey, Document document) =>
            context.RegisterCodeFix(CodeAction.Create(title, _ => Task.FromResult(document), equivalenceKey), diagnostic);

        private static async Task<bool> HaveSameTextAsync(Document left, Document right, CancellationToken cancellationToken) =>
            (await left.GetTextAsync(cancellationToken).ConfigureAwait(false))
                .ContentEquals(await right.GetTextAsync(cancellationToken).ConfigureAwait(false));

        private static UnsafeContextPolicy GetPolicy(string? equivalenceKey) =>
            equivalenceKey switch
            {
                ExpressionFirstEquivalenceKey => UnsafeContextPolicy.ExpressionFirst,
                BodyWideEquivalenceKey => UnsafeContextPolicy.BodyWide,
                _ => UnsafeContextPolicy.BlockFirst,
            };
    }
}
