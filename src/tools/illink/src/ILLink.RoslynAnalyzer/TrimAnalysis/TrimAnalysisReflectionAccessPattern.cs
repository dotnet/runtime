// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	internal readonly record struct TrimAnalysisReflectionAccessPattern
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

		public void ReportDiagnostics (DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
		{
			var location = Operation.Syntax.GetLocation ();
			var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer (reportDiagnostic);
			if (context.EnableTrimAnalyzer &&
				!OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope (out _) &&
				!FeatureContext.IsEnabled (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute)) {
				reflectionAccessAnalyzer.GetDiagnosticsForReflectionAccessToDAMOnMethod (location, ReferencedMethod);
			}

			DiagnosticContext diagnosticContext = new (location, reportDiagnostic);
			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers)
				requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, ReferencedMethod, OwningSymbol, context, FeatureContext, diagnosticContext);
		}
	}
}
