// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes <c>IL5005</c> by removing undocumented legacy unsafe scopes that became caller contracts under unsafe-v2.
    /// Pointer signatures retain <c>unsafe</c>, while field-like <c>CS9392</c> declarations are changed to <c>safe</c>.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUndocumentedUnsafeCodeFixProvider)), Shared]
    public sealed class RemoveUndocumentedUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private static LocalizableString RemoveCodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.RemoveUndocumentedUnsafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        private static LocalizableString ReplaceCodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.ReplaceUndocumentedUnsafeWithSafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticId.UnsafeMemberMissingSafetyDocumentation.AsString()];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            // These members were already caller-unsafe under the legacy pointer compatibility rules.
            if (diagnostic.Properties.ContainsKey(
                UnsafeMemberMissingSafetyDocumentationAnalyzer.PointerSignatureProperty))
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            SyntaxNode targetNode = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
            if (UnsafeModifierCodeFixHelpers.FindDeclaration(targetNode) is not { } declaration
                || !UnsafeModifierCodeFixHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
            {
                return;
            }

            bool replaceWithSafe = diagnostic.Properties.ContainsKey(
                    UnsafeMemberMissingSafetyDocumentationAnalyzer.RequiresExplicitSafetyModifierProperty)
                && !UnsafeModifierCodeFixHelpers.HasSafeModifier(declaration);
            // Bare removal would recreate CS9392 for field-backed explicit or extended layout members.
            string title = (replaceWithSafe ? ReplaceCodeFixTitle : RemoveCodeFixTitle).ToString();

            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    cancellationToken => replaceWithSafe
                        ? UnsafeModifierCodeFixHelpers.ReplaceUnsafeWithSafeModifierAsync(
                            context.Document,
                            declaration,
                            cancellationToken)
                        : UnsafeModifierCodeFixHelpers.RemoveUnsafeModifierAsync(
                            context.Document,
                            declaration,
                            cancellationToken),
                    title),
                diagnostic);
        }
    }
}
#endif
