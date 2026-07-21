// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Immutable;
using System.Threading;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
    /// <summary>
    /// Reports <c>IL5006</c> for pointer or function-pointer signatures that lost their legacy caller-unsafe contract.
    /// The diagnostic is disabled by default while this migration tooling remains experimental.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PointerSignatureRequiresUnsafeAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule =
            DiagnosticDescriptors.GetDiagnosticDescriptor(
                DiagnosticId.PointerSignatureRequiresUnsafe,
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

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            // Roslyn does not invoke symbol actions for local functions.
            if (context.Symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction })
                return;

            AnalyzeSymbol(context.Symbol, context.CancellationToken, context.ReportDiagnostic);
        }

        private static void AnalyzeLocalFunction(OperationAnalysisContext context) =>
            AnalyzeSymbol(
                ((ILocalFunctionOperation)context.Operation).Symbol,
                context.CancellationToken,
                context.ReportDiagnostic);

        private static void AnalyzeSymbol(
            ISymbol symbol,
            CancellationToken cancellationToken,
            Action<Diagnostic> reportDiagnostic)
        {
            // Property and event accessors are represented by method symbols, but their containing declaration
            // already carries the pointer-signature contract.
            if (symbol is IMethodSymbol { AssociatedSymbol: not null }
                || !UnsafeMigrationAnalyzerHelpers.HasPointerOrFunctionPointerSignature(symbol))
            {
                return;
            }

            foreach (SyntaxNode declaration in UnsafeMigrationAnalyzerHelpers.GetDeclarations(symbol, cancellationToken))
            {
                // safe is an explicit audited contract on the limited declaration kinds where Roslyn permits it.
                if (UnsafeMigrationAnalyzerHelpers.HasModifier(declaration, SyntaxKind.UnsafeKeyword)
                    || UnsafeMigrationAnalyzerHelpers.HasSafeModifier(declaration)
                    || UnsafeMigrationAnalyzerHelpers.HasSafetyDocumentation(declaration, symbol, cancellationToken))
                {
                    continue;
                }

                reportDiagnostic(Diagnostic.Create(
                    s_rule,
                    UnsafeMigrationAnalyzerHelpers.GetIdentifierLocation(declaration)));
            }
        }
    }
}
#endif
