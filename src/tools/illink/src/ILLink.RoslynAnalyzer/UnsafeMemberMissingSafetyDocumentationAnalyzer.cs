// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Reports <c>IL5005</c> when an explicit unsafe member contract has no <c>&lt;safety&gt;</c> XML documentation.
    /// The diagnostic is disabled by default while this migration tooling remains experimental.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnsafeMemberMissingSafetyDocumentationAnalyzer : DiagnosticAnalyzer
    {
        public const string PointerSignatureProperty = nameof(PointerSignatureProperty);
        public const string RequiresExplicitSafetyModifierProperty = nameof(RequiresExplicitSafetyModifierProperty);

        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.UnsafeMemberMissingSafetyDocumentation,
                isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeDeclaration,
                UnsafeMigrationAnalyzerHelpers.UnsafeContractDeclarationKinds);
        }

        private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode declaration = context.Node;
            SyntaxToken unsafeModifier = UnsafeMigrationAnalyzerHelpers.GetModifier(
                declaration,
                SyntaxKind.UnsafeKeyword);

            if (unsafeModifier == default)
                return;

            ISymbol? symbol = UnsafeMigrationAnalyzerHelpers.GetDeclaredSymbol(
                declaration,
                context.SemanticModel,
                context.CancellationToken);

            if (!UnsafeMigrationAnalyzerHelpers.IsUnsafeContractMember(declaration, symbol)
                || UnsafeMigrationAnalyzerHelpers.HasSafetyDocumentation(declaration, symbol, context.CancellationToken))
            {
                return;
            }

            var properties = ImmutableDictionary<string, string?>.Empty;
            // Pointer signatures were caller-unsafe under the legacy rules, so the fixer must retain unsafe.
            if (symbol is not null && UnsafeMigrationAnalyzerHelpers.HasPointerOrFunctionPointerSignature(symbol))
                properties = properties.Add(PointerSignatureProperty, bool.TrueString);
            // Explicit and extended layouts still require an explicit safe/unsafe marker after migration.
            if (UnsafeMigrationAnalyzerHelpers.RequiresExplicitSafetyModifier(declaration, symbol))
                properties = properties.Add(RequiresExplicitSafetyModifierProperty, bool.TrueString);

            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                unsafeModifier.GetLocation(),
                properties: properties));
        }
    }
}
#endif
