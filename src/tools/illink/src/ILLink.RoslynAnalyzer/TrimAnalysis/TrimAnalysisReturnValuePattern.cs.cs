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
		public FeatureChecksValue ReturnValue { get; init; }
		public ValueSet<string> FeatureCheckAnnotations { get; init; }
		public IOperation Operation { get; init; }
		public IPropertySymbol OwningSymbol { get; init; }

		public TrimAnalysisReturnValuePattern (
			FeatureChecksValue returnValue,
			ValueSet<string> featureCheckAnnotations,
			IOperation operation,
			IPropertySymbol owningSymbol)
		{
			ReturnValue = returnValue.DeepCopy ();
			FeatureCheckAnnotations = featureCheckAnnotations.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
			// For now, feature check validation is enabled only when trim analysis is enabled.
			if (context.EnableTrimAnalyzer) {
				if (!OwningSymbol.IsStatic || OwningSymbol.Type.SpecialType != SpecialType.System_Boolean) {
					// || OwningSymbol.SetMethod != null) { // TODO: checking for SetMethod doesn't work
					// because we never reach here for set-only properties that don't have a return-value pattern.
					// Warn about invalid feature checks (non-static, non-bool, or non-get-only properties)
					diagnosticContext.AddDiagnostic (
						DiagnosticId.InvalidFeatureCheck);
					return diagnosticContext.Diagnostics;
				}

				ValueSet<string> returnValueFeatures = ReturnValue.EnabledFeatures;
				// For any feature that this property is declared to guard,
				// the abstract return value must include that feature
				// (indicating it is known to be enabled when the return value is true).
				foreach (string feature in FeatureCheckAnnotations.GetKnownValues ()) {
					if (!returnValueFeatures.Contains (feature)) {
						diagnosticContext.AddDiagnostic (
							DiagnosticId.ReturnValueDoesNotMatchFeatureChecks,
							OwningSymbol.GetDisplayName (),
							feature);
					}
				}
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
