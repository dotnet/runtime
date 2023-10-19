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

		// No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
		// and there's only one way to access the referenced method.

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
