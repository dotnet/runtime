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
	internal readonly record struct TrimAnalysisAssignmentPattern
	{
		public MultiValue Source { get; init; }
		public MultiValue Target { get; init; }
		public IOperation Operation { get; init; }
		public ISymbol OwningSymbol { get; init; }
		public FeatureContext FeatureContext { get; init; }

		public TrimAnalysisAssignmentPattern (
			MultiValue source,
			MultiValue target,
			IOperation operation,
			ISymbol owningSymbol,
			FeatureContext featureContext)
		{
			Source = source.DeepCopy ();
			Target = target.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
			FeatureContext = featureContext;
		}

		public TrimAnalysisAssignmentPattern Merge (
			ValueSetLattice<SingleValue> lattice,
			FeatureContextLattice featureContextLattice,
			TrimAnalysisAssignmentPattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisAssignmentPattern (
				lattice.Meet (Source, other.Source),
				lattice.Meet (Target, other.Target),
				Operation,
				OwningSymbol,
				featureContextLattice.Meet (FeatureContext, other.FeatureContext));
		}

		public void ReportDiagnostics (DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation (), reportDiagnostic);
			if (context.EnableTrimAnalyzer &&
				!OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope (out _) &&
				!FeatureContext.IsEnabled (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute)) {
				foreach (var sourceValue in Source.AsEnumerable ()) {
					foreach (var targetValue in Target.AsEnumerable ()) {
						// The target should always be an annotated value, but the visitor design currently prevents
						// declaring this in the type system.
						if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
							throw new NotImplementedException ();

						var reflectionAccessAnalyzer = new ReflectionAccessAnalyzer (reportDiagnostic);
						var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, reflectionAccessAnalyzer);
						requireDynamicallyAccessedMembersAction.Invoke (sourceValue, targetWithDynamicallyAccessedMembers);
					}
				}
			}
		}
	}
}
