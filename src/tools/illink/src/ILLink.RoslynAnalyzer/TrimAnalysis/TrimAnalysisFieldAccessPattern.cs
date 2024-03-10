// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisFieldAccessPattern
	{
		public IFieldSymbol Field { get; init; }
		public IFieldReferenceOperation Operation { get; init; }
		public ISymbol OwningSymbol { get; init; }
		public FeatureContext FeatureContext { get; init; }

		public TrimAnalysisFieldAccessPattern (
			IFieldSymbol field,
			IFieldReferenceOperation operation,
			ISymbol owningSymbol,
			FeatureContext featureContext)
		{
			Field = field;
			Operation = operation;
			OwningSymbol = owningSymbol;
			FeatureContext = featureContext;
		}

		public TrimAnalysisFieldAccessPattern Merge (
			ValueSetLattice<SingleValue> lattice,
			FeatureContextLattice featureContextLattice,
			TrimAnalysisFieldAccessPattern other)
		{
			Debug.Assert (SymbolEqualityComparer.Default.Equals (Field, other.Field));
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisFieldAccessPattern (
				Field,
				Operation,
				OwningSymbol,
				featureContextLattice.Meet (FeatureContext, other.FeatureContext));
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers) {
				if (requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, Field, OwningSymbol, context, FeatureContext, out Diagnostic? diag))
					diagnosticContext.AddDiagnostic (diag);
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
