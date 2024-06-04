// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisReflectionAccessPattern
	{
		public IMethodSymbol ReferencedMethod { get; init; }
		public IOperation Operation { get; init; }
		public ISymbol OwningSymbol { get; init; }
		public FeatureContext FeatureContext { get; init; }

		public TrimAnalysisReflectionAccessPattern (
			IMethodSymbol referencedMethod,
			IOperation operation,
			ISymbol owningSymbol,
			FeatureContext feature)
		{
			ReferencedMethod = referencedMethod;
			Operation = operation;
			OwningSymbol = owningSymbol;
			FeatureContext = feature.DeepCopy ();
		}

		public TrimAnalysisReflectionAccessPattern Merge (
			ValueSetLattice<SingleValue> lattice,
			FeatureContextLattice featureContextLattice,
			TrimAnalysisReflectionAccessPattern other)
		{
			Debug.Assert (SymbolEqualityComparer.Default.Equals (ReferencedMethod, other.ReferencedMethod));
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisReflectionAccessPattern (
				ReferencedMethod,
				Operation,
				OwningSymbol,
				featureContextLattice.Meet (FeatureContext, other.FeatureContext));
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			if (context.EnableTrimAnalyzer &&
				!OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope (out _) &&
				!FeatureContext.IsEnabled (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute)) {
				foreach (var diagnostic in ReflectionAccessAnalyzer.GetDiagnosticsForReflectionAccessToDAMOnMethod (diagnosticContext, ReferencedMethod))
					diagnosticContext.AddDiagnostic (diagnostic);
			}

			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers) {
				if (requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, ReferencedMethod, OwningSymbol, context, FeatureContext, out Diagnostic? diag))
					diagnosticContext.AddDiagnostic (diag);
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
