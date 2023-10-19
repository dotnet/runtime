// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.ComTypes;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisReflectionAccessPattern
	{
		public IMethodSymbol ReferencedMethod { init; get; }
		public MultiValue Instance { init; get; }
		public IOperation Operation { init; get; }
		public ISymbol OwningSymbol { init; get; }

		public TrimAnalysisReflectionAccessPattern (
			IMethodSymbol referencedMethod,
			MultiValue instance,
			IOperation operation,
			ISymbol owningSymbol)
		{
			ReferencedMethod = referencedMethod;
			Instance = instance.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public TrimAnalysisReflectionAccessPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisReflectionAccessPattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (ReferencedMethod, other.ReferencedMethod));
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisReflectionAccessPattern (
				ReferencedMethod,
				lattice.Meet (Instance, other.Instance),
				Operation,
				OwningSymbol);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (RequiresAnalyzerContext context)
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			if (!OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope (out _)) {
				foreach (var diagnostic in ReflectionAccessAnalyzer.GetDiagnosticsForReflectionAccessToDAMOnMethod (diagnosticContext, ReferencedMethod))
					diagnosticContext.AddDiagnostic (diagnostic);
			}

			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers) {
				if (requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, ReferencedMethod, OwningSymbol, context, out Diagnostic? diag))
					diagnosticContext.AddDiagnostic (diag);
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
