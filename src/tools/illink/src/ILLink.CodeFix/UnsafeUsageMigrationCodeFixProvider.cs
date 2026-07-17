// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RoslynCodeFixProvider = Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider;

namespace ILLink.CodeFix;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnsafeUsageMigrationCodeFixProvider)), Shared]
public sealed class UnsafeUsageMigrationCodeFixProvider : RoslynCodeFixProvider
{
    private static LocalizableString CodeFixTitle => new LocalizableResourceString(
        nameof(Resources.UnsafeUsageMigrationCodeFixTitle),
        Resources.ResourceManager,
        typeof(Resources));

    public override ImmutableArray<string> FixableDiagnosticIds =>
    [
        DiagnosticId.UnsafeUsageMigration.AsString(),
        "CS0764",
        "CS9360",
        "CS9361",
        "CS9362",
        "CS9363",
        "CS9389",
        "CS9392"
    ];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (!UnsafeCodeFixHelpers.IsMigrationEnabled(context.Document))
            return Task.CompletedTask;

        string title = CodeFixTitle.ToString();
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => UnsafeUsageDocumentFixer.FixDocumentAsync(
                    context.Document,
                    cancellationToken),
                title),
            context.Diagnostics);

        return Task.CompletedTask;
    }
}
#endif
