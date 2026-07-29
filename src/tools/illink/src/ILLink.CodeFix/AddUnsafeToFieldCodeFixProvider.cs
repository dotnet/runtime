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
    /// Fixes compiler diagnostic <c>CS9392</c> by marking field-like members in explicit or extended layouts <c>unsafe</c>.
    /// Primary-constructor parameters are intentionally unsupported because C# cannot place the modifier on them.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddUnsafeToFieldCodeFixProvider)), Shared]
    public sealed class AddUnsafeToFieldCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
    {
        public const string FieldRequiresUnsafeOrSafeDiagnosticId = "CS9392";

        private static LocalizableString CodeFixTitle =>
            new LocalizableResourceString(
                nameof(Resources.AddUnsafeToFieldCodeFixTitle),
                Resources.ResourceManager,
                typeof(Resources));

        public override ImmutableArray<string> FixableDiagnosticIds =>
            [FieldRequiresUnsafeOrSafeDiagnosticId];

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context) =>
            UnsafeModifierCodeFixHelpers.RegisterAddUnsafeCodeFixAsync(
                context,
                CodeFixTitle,
                static declaration => declaration is FieldDeclarationSyntax
                    or PropertyDeclarationSyntax
                    or EventFieldDeclarationSyntax);
    }
}
#endif
