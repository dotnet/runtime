// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes <c>CS9377</c> and unsafe-specific <c>CS0106</c> diagnostics by removing the invalid <c>unsafe</c> modifier.
    /// The shared <c>CS0106</c> ID is filtered so modifiers unrelated to unsafe are left to their own fixes.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveInvalidUnsafeCodeFixProvider)), Shared]
    public sealed class RemoveInvalidUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string UnsafeModifierHasNoEffectDiagnosticId = "CS9377";
        public const string InvalidModifierDiagnosticId = "CS0106";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.RemoveInvalidUnsafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [UnsafeModifierHasNoEffectDiagnosticId, InvalidModifierDiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            // CS0106 is shared by every invalid modifier, so only handle the unsafe-specific form.
            if (diagnostic.Id == InvalidModifierDiagnosticId
                && diagnostic.GetMessage(CultureInfo.InvariantCulture)
                    .IndexOf("'unsafe'", System.StringComparison.Ordinal) < 0)
            {
                return;
            }

            if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
                return;

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (UnsafeModifierCodeFixHelpers.FindDeclaration(targetNode) is not { } declaration
                || !UnsafeModifierCodeFixHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
            {
                return;
            }

            string title = CodeFixTitle.ToString();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => UnsafeModifierCodeFixHelpers.RemoveUnsafeModifierAsync(
                        context.Document,
                        declaration,
                        cancellationToken),
                    title),
                diagnostic);
        }
    }
}
#endif
