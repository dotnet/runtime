// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisFieldAccessPattern
	{
		public IFieldSymbol Field { init; get; }
		public IFieldReferenceOperation Operation { init; get; }
		public ISymbol OwningSymbol { init; get; }

		public TrimAnalysisFieldAccessPattern (
			IFieldSymbol field,
			IFieldReferenceOperation operation,
			ISymbol owningSymbol)
		{
			Field = field;
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		// No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
		// and there's only one way to "access" a field.

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			foreach (var requiresAnalyzer in context.EnabledRequiresAnalyzers) {
				if (requiresAnalyzer.CheckAndCreateRequiresDiagnostic (Operation, Field, OwningSymbol, context, out Diagnostic? diag))
					diagnosticContext.AddDiagnostic (diag);
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
