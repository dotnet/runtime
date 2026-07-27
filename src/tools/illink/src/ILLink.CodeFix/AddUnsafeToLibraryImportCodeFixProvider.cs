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
    /// Fixes analyzer diagnostic <c>IL5007</c> by marking a <c>LibraryImportAttribute</c> method <c>unsafe</c>.
    /// The generated contract is intentionally conservative so developers can replace it with <c>safe</c> after
    /// auditing the interop boundary.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeToLibraryImportCodeFixProvider)), Shared]
    public sealed class AddUnsafeToLibraryImportCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeToLibraryImportCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [DiagnosticId.LibraryImportRequiresExplicitSafety.AsString()];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) =>
            UnsafeModifierCodeFixHelpers.RegisterAddUnsafeCodeFixAsync(
                context,
                CodeFixTitle,
                static declaration => declaration is MethodDeclarationSyntax);
    }
}
#endif
