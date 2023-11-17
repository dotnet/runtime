// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using ILLink.Shared;
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

		// TODO: is Merge actually needed? If not, remove TConditionValue and lattice from change.
		// Can we ever see same opration/owningsymbol Merge with different return value?
		// Can the FeatureChecksValue ever be different?
		// Can it ever change after the initial analysis? Probably not if we don't do const prop.
		// public TrimAnalysisReturnValuePattern Merge (
		// 	FeatureChecksLattice lattice,
		// 	TrimAnalysisReturnValuePattern other)
		// {
		// 	Debug.Assert (Operation == other.Operation);
		// 	Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

		// 	// NOTE: all patterns should use Meet. Not union/intersection!
		// 	return new TrimAnalysisReturnValuePattern (
		// 		lattice.Meet (ReturnValue, other.ReturnValue),
		// 		Operation,
		// 		OwningSymbol);
		// }

		// TODO: avoid creating patterns for everything other than property symbols?
		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
			// For now, feature guard validation is enabled only when trim analysis is enabled.
			if (context.EnableTrimAnalyzer) {
				// Warn if the return value doesn't match the expected value based on the attribute.
				if (OwningSymbol is not IMethodSymbol methodSymbol || methodSymbol.MethodKind is not MethodKind.PropertyGet)
					return ImmutableArray<Diagnostic>.Empty;

				var propertySymbol = (IPropertySymbol) methodSymbol.AssociatedSymbol!;

				// TODO: represent features as attribute references rather than strings,
				// and report the attribute name instead of the string feature name in the diagnostic.
				FeatureContext featureGuards = propertySymbol.GetFeatureGuards (context.Compilation, context.EnabledRequiresAnalyzers);
				// Warn if the return value doesn't match the expected feature guards.
				// Each guard value must also be a return value, but not vice versa.
				ValueSet<string>? featureGuardSet = featureGuards.FeatureSet;
				if (featureGuardSet == null)
					throw new InvalidOperationException ();

				ValueSet<string>? returnValueSet = ReturnValue.EnabledFeatures.FeatureSet;
				// If all features are enabled? Should never happen...
				if (returnValueSet == null)
					throw new InvalidOperationException ();
				foreach (string expectedGuard in featureGuardSet) {
					// TODO: what if it contains both enabled and disabled features?
					if (!returnValueSet.Value.Contains (expectedGuard)) {
						diagnosticContext.AddDiagnostic (
							DiagnosticId.ReturnValueDoesNotMatchFeatureGuards,
							OwningSymbol.GetDisplayName (),
							expectedGuard);
					}
				}
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
