// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes analyzer diagnostic <c>IL5006</c> by adding <c>unsafe</c> to a pointer or function-pointer signature.
    /// This preserves the caller-unsafe behavior that legacy memory-safety rules inferred from those signatures.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeToPointerSignatureCodeFixProvider)), Shared]
    public sealed class AddUnsafeToPointerSignatureCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeToPointerSignatureCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticId.PointerSignatureRequiresUnsafe.AsString()];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) =>
            UnsafeModifierCodeFixHelpers.RegisterAddUnsafeCodeFixAsync(
                context,
                CodeFixTitle,
                static declaration => declaration is BaseMethodDeclarationSyntax
                    or PropertyDeclarationSyntax
                    or IndexerDeclarationSyntax
                    or EventDeclarationSyntax
                    or EventFieldDeclarationSyntax
                    or FieldDeclarationSyntax
                    or LocalFunctionStatementSyntax);
    }
}
#endif
