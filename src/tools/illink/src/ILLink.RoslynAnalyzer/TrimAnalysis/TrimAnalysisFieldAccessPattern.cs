// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	internal readonly record struct TrimAnalysisFieldAccessPattern
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

		public void ReportDiagnostics (DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation (), reportDiagnostic);
			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers)
				requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, Field, OwningSymbol, context, FeatureContext, in diagnosticContext);
		}
	}
}
