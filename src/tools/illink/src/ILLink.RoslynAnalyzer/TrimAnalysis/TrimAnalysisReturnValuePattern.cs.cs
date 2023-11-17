// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisReturnValuePattern
	{
		public FeatureChecksValue ReturnValue { init; get; }
		public IOperation Operation { init; get; }
		public IPropertySymbol OwningSymbol { init; get; }

		public TrimAnalysisReturnValuePattern (
			FeatureChecksValue returnValue,
			IOperation operation,
			IPropertySymbol owningSymbol)
		{
			ReturnValue = returnValue.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
			// For now, feature guard validation is enabled only when trim analysis is enabled.
			if (context.EnableTrimAnalyzer) {
				ValueSet<string> featureGuards = OwningSymbol.GetFeatureGuards (context.Compilation, context.EnabledRequiresAnalyzers);
				ValueSet<string> returnValueFeatures = ReturnValue.EnabledFeatures;
				// For any feature that this property is declared to guard,
				// the abstract return value must include that feature
				// (indicating it is known to be enabled when the return value is true).
				foreach (string featureGuard in featureGuards) {
					if (!returnValueFeatures.Contains (featureGuard)) {
						diagnosticContext.AddDiagnostic (
							DiagnosticId.ReturnValueDoesNotMatchFeatureGuards,
							OwningSymbol.GetDisplayName (),
							featureGuard);
					}
				}
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
