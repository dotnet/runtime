// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Reports <c>IL5010</c> for an explicit <c>safe</c> modifier that has no <c>&lt;safety&gt;</c> XML
    /// documentation.
    /// </summary>
    /// <remarks>
    /// This is the symmetric hole to <c>IL5005</c>. <c>safe</c> is a hand-written assertion the compiler cannot
    /// verify: on an <c>extern</c> member or a <c>LibraryImport</c> it claims the native boundary upholds its
    /// contract, and on a field in an explicit or extended layout it claims the overlap cannot be used to
    /// type-pun. Both deserve a recorded audit. No code fix is offered because only a developer can write the
    /// justification. The diagnostic is disabled by default while this migration tooling remains experimental.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SafeModifierMissingJustificationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.SafeModifierMissingJustification,
                isEnabledByDefault: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [s_rule];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            if (!System.Diagnostics.Debugger.IsAttached)
                context.EnableConcurrentExecution();

            context.RegisterSymbolAction(
                AnalyzeSymbol,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);
            context.RegisterOperationAction(AnalyzeLocalFunction, OperationKind.LocalFunction);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context) =>
            AnalyzeSymbol(context.Symbol, context.CancellationToken, context.ReportDiagnostic);

        private static void AnalyzeLocalFunction(OperationAnalysisContext context) =>
            AnalyzeSymbol(
                ((ILocalFunctionOperation)context.Operation).Symbol,
                context.CancellationToken,
                context.ReportDiagnostic);

        private static void AnalyzeSymbol(
            ISymbol symbol,
            System.Threading.CancellationToken cancellationToken,
            System.Action<Diagnostic> reportDiagnostic)
        {
            // Accessors take their contract from the containing property or event, which is analyzed separately.
            if (symbol is IMethodSymbol { AssociatedSymbol: not null })
                return;

            foreach (SyntaxNode declaration in UnsafeMigrationAnalyzerHelpers.GetDeclarations(symbol, cancellationToken))
            {
                if (!UnsafeMigrationSyntaxHelpers.HasSafeModifier(declaration)
                    || UnsafeMigrationAnalyzerHelpers.HasSafetyDocumentation(declaration, symbol, cancellationToken))
                {
                    continue;
                }

                SyntaxToken safeModifier = UnsafeMigrationSyntaxHelpers.GetModifier(
                    declaration,
                    UnsafeMigrationSyntaxHelpers.SafeKeywordKind);
                if (safeModifier == default)
                    continue;

                reportDiagnostic(Diagnostic.Create(s_rule, safeModifier.GetLocation()));
            }
        }
    }
}
#endif
