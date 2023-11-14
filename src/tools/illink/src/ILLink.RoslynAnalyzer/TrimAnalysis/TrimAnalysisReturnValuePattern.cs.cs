// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisReturnValuePattern
	{
		public FeatureChecksValue ReturnValue { init; get; }
		public IOperation Operation { init; get; }
		public ISymbol OwningSymbol { init; get; }

		public TrimAnalysisReturnValuePattern (
			FeatureChecksValue returnValue,
			IOperation operation,
			ISymbol owningSymbol)
		{
			ReturnValue = returnValue.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public TrimAnalysisReturnValuePattern Merge (
			FeatureChecksLattice lattice,
			TrimAnalysisReturnValuePattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisReturnValuePattern (
				lattice.Meet (ReturnValue, other.ReturnValue),
				Operation,
				OwningSymbol);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
            // For now, feature guard validation is enabled only when trim analysis is enabled.
			if (context.EnableTrimAnalyzer) {
                // Warn if the return value doesn't match the expected value based on the attribute.
                throw new NotImplementedException ();
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
