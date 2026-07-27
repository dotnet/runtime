// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.CodeFix
{
    /// <summary>
    /// Fixes compiler diagnostic <c>CS9389</c> by marking an unclassified extern member <c>unsafe</c>.
    /// The generated contract is intentionally conservative so developers can replace it with <c>safe</c> after audit.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeToExternCodeFixProvider)), Shared]
    public sealed class AddUnsafeToExternCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string ExternMemberRequiresUnsafeOrSafeDiagnosticId = "CS9389";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeToExternCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [ExternMemberRequiresUnsafeOrSafeDiagnosticId];

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
                    or LocalFunctionStatementSyntax);
    }
}
#endif
