// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
    internal readonly record struct TrimAnalysisGenericInstantiationPattern
    {
        public ISymbol GenericInstantiation { get; init; }
        public IOperation Operation { get; init; }
        public ISymbol OwningSymbol { get; init; }
        public FeatureContext FeatureContext { get; init; }

        public TrimAnalysisGenericInstantiationPattern(
            ISymbol genericInstantiation,
            IOperation operation,
            ISymbol owningSymbol,
            FeatureContext featureContext)
        {
            GenericInstantiation = genericInstantiation;
            Operation = operation;
            OwningSymbol = owningSymbol;
            FeatureContext = featureContext.DeepCopy();
        }

        public TrimAnalysisGenericInstantiationPattern Merge(
            FeatureContextLattice featureContextLattice,
            TrimAnalysisGenericInstantiationPattern other)
        {
            Debug.Assert(Operation == other.Operation);
            Debug.Assert(SymbolEqualityComparer.Default.Equals(GenericInstantiation, other.GenericInstantiation));
            Debug.Assert(SymbolEqualityComparer.Default.Equals(OwningSymbol, other.OwningSymbol));

            return new TrimAnalysisGenericInstantiationPattern(
                GenericInstantiation,
                Operation,
                OwningSymbol,
                featureContextLattice.Meet(FeatureContext, other.FeatureContext));
        }

        public void ReportDiagnostics(DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
        {
            var location = Operation.Syntax.GetLocation();
            var typeNameResolver = new TypeNameResolver(context.Compilation);

            foreach (var analyzer in context.EnabledRequiresAnalyzers)
            {
                var genericArgumentDataFlow = new GenericArgumentDataFlow(analyzer, FeatureContext, typeNameResolver, OwningSymbol, location, reportDiagnostic);

                switch (GenericInstantiation)
                {
                    case INamedTypeSymbol type:
                        genericArgumentDataFlow.ProcessGenericArgumentDataFlow(type);
                        break;

                    case IMethodSymbol method:
                        genericArgumentDataFlow.ProcessGenericArgumentDataFlow(method);
                        break;

                    case IFieldSymbol field:
                        genericArgumentDataFlow.ProcessGenericArgumentDataFlow(field);
                        break;
                }
            }
        }
    }
}
