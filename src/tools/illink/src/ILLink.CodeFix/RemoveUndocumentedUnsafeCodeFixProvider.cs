// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if UNSAFE_MIGRATION
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
    /// Pointer signatures and declarations that need an explicit marker (<c>extern</c>, <c>LibraryImport</c>,
    /// <c>CS9392</c> fields) retain <c>unsafe</c> for compatibility and safety.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUndocumentedUnsafeCodeFixProvider)), Shared]
    public sealed class RemoveUndocumentedUnsafeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private static LocalizableString RemoveCodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.RemoveUndocumentedUnsafeCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticId.UnsafeMemberMissingSafetyDocumentation.AsString()];

        public override FixAllProvider GetFixAllProvider() => UnsafeMigrationFixAllProvider.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics[0];
            // Pointer signatures were already caller-unsafe under legacy rules. Extern and LibraryImport methods and
            // CS9392 fields must keep a marker, and default to unsafe until manually audited.
            // Pointer operations in inactive #if code may still need the modifier's lexical unsafe context elsewhere.
            if (diagnostic.Properties.ContainsKey(UnsafeMemberMissingSafetyDocumentationAnalyzer.PointerSignatureProperty)
                || diagnostic.Properties.ContainsKey(UnsafeMemberMissingSafetyDocumentationAnalyzer.RequiresExplicitSafetyModifierProperty)
                || diagnostic.Properties.ContainsKey(UnsafeMemberMissingSafetyDocumentationAnalyzer.ContainsInactiveUnsafeCodeProperty))
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
                || !UnsafeMigrationSyntaxHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword))
            {
                return;
            }

            string title = RemoveCodeFixTitle.ToString();

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
