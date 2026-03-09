// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using ILLink.RoslynAnalyzer.DataFlow;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
    internal readonly record struct TrimAnalysisBackingFieldAccessPattern
    {
        public IPropertySymbol Property { get; init; }
        public IPropertyReferenceOperation Operation { get; init; }
        public ISymbol OwningSymbol { get; init; }
        public FeatureContext FeatureContext { get; init; }

        public TrimAnalysisBackingFieldAccessPattern(
            IPropertySymbol property,
            IPropertyReferenceOperation operation,
            ISymbol owningSymbol,
            FeatureContext featureContext)
        {
            Property = property;
            Operation = operation;
            OwningSymbol = owningSymbol;
            FeatureContext = featureContext;
        }

        public TrimAnalysisBackingFieldAccessPattern Merge(
            FeatureContextLattice featureContextLattice,
            TrimAnalysisBackingFieldAccessPattern other)
        {
            Debug.Assert(SymbolEqualityComparer.Default.Equals(Property, other.Property));
            Debug.Assert(Operation == other.Operation);
            Debug.Assert(SymbolEqualityComparer.Default.Equals(OwningSymbol, other.OwningSymbol));

            return new TrimAnalysisBackingFieldAccessPattern(
                Property,
                Operation,
                OwningSymbol,
                featureContextLattice.Meet(FeatureContext, other.FeatureContext));
        }

        public void ReportDiagnostics(DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
        {
            DiagnosticContext diagnosticContext = new(Operation.Syntax.GetLocation(), reportDiagnostic);
            foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers)
                requiresAnalyzer.CheckAndCreateRequiresDiagnostic(Operation, Property, OwningSymbol, context, FeatureContext, in diagnosticContext);
        }
    }
}
