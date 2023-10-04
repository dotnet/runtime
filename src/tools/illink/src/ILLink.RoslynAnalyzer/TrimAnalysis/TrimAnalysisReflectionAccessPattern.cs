// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

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

			return new TrimAnalysisReflectionAccessPattern (
				ReferencedMethod,
				lattice.Meet (Instance, other.Instance),
				Operation,
				OwningSymbol);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics ()
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			foreach (var diagnostic in ReflectionAccessAnalyzer.GetDiagnosticsForReflectionAccessToDAMOnMethod (diagnosticContext, ReferencedMethod))
				yield return diagnostic;
		}
	}
}
