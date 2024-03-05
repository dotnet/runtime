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
	public readonly record struct FeatureCheckReturnValuePattern
	{
		public FeatureChecksValue ReturnValue { get; init; }
		public ValueSet<string> FeatureCheckAnnotations { get; init; }
		public IOperation Operation { get; init; }
		public IPropertySymbol OwningSymbol { get; init; }

		public FeatureCheckReturnValuePattern (
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
			if (!context.EnableTrimAnalyzer)
				return diagnosticContext.Diagnostics;

			if (!OwningSymbol.IsStatic || OwningSymbol.Type.SpecialType != SpecialType.System_Boolean) {
				// Warn about invalid feature checks (non-static or non-bool properties)
				diagnosticContext.AddDiagnostic (
					DiagnosticId.InvalidFeatureCheck);
				return diagnosticContext.Diagnostics;
			}

			if (ReturnValue == FeatureChecksValue.All)
				return diagnosticContext.Diagnostics;

			ValueSet<string> returnValueFeatures = ReturnValue.EnabledFeatures;
			// For any analyzer-supported feature that this property is declared to guard,
			// the abstract return value must include that feature
			// (indicating it is known to be enabled when the return value is true).
			foreach (string feature in FeatureCheckAnnotations.GetKnownValues ()) {
				foreach (var analyzer in context.EnabledRequiresAnalyzers) {
					if (feature != analyzer.RequiresAttributeFullyQualifiedName)
						continue;

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
