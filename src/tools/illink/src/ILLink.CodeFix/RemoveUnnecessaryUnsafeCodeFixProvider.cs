// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Removes an <c>unsafe</c> modifier flagged as unnecessary by IL5005.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUnnecessaryUnsafeCodeFixProvider)), Shared]
    public sealed class RemoveUnnecessaryUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public static string DiagnosticId => ILLink.Shared.DiagnosticId.UnnecessaryUnsafeModifier.AsString();

        public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private static string Title => new LocalizableResourceString(
            nameof(Resources.RemoveUnnecessaryUnsafeCodeFixTitle), Resources.ResourceManager, typeof(Resources)).ToString();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            var unsafeToken = root.FindToken(diagnostic.Location.SourceSpan.Start);
            if (!unsafeToken.IsKind(SyntaxKind.UnsafeKeyword) || unsafeToken.Parent is not { } declaration)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(Title, ct => RemoveModifierAsync(context.Document, declaration, ct), equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> RemoveModifierAsync(Document document, SyntaxNode declaration, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var modifiers = editor.Generator.GetModifiers(declaration);
            editor.SetModifiers(declaration, modifiers.WithIsUnsafe(false));
            return editor.GetChangedDocument();
        }
    }
}
#endif
