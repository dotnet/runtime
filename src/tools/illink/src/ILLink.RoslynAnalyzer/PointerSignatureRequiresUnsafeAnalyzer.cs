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
    /// Reports <c>IL5006</c> for pointer or function-pointer signatures that lost their legacy caller-unsafe contract.
    /// An existing <c>unsafe</c>/<c>safe</c> modifier or a <c>&lt;safety&gt;</c> XML comment suppresses the diagnostic.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PointerSignatureRequiresUnsafeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(DiagnosticId.PointerSignatureRequiresUnsafe);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                AnalyzeDeclaration,
                UnsafeMigrationAnalyzerHelpers.PointerSignatureDeclarationKinds);
        }

        private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode declaration = context.Node;
            // safe is an explicit audited contract on the limited declaration kinds where Roslyn permits it.
            if (UnsafeMigrationAnalyzerHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                || UnsafeMigrationAnalyzerHelpers.HasSafeModifier(declaration))
            {
                return;
            }

            ISymbol? symbol = UnsafeMigrationAnalyzerHelpers.GetDeclaredSymbol(
                declaration,
                context.SemanticModel,
                context.CancellationToken);

            if (symbol is null
                || !UnsafeMigrationAnalyzerHelpers.HasPointerOrFunctionPointerSignature(symbol)
                || UnsafeMigrationAnalyzerHelpers.HasSafetyDocumentation(declaration, symbol, context.CancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                s_rule,
                UnsafeMigrationAnalyzerHelpers.GetIdentifierLocation(declaration)));
        }
    }
}
#endif
