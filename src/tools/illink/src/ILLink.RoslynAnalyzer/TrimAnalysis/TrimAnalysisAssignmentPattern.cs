// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisAssignmentPattern
	{
		public MultiValue Source { init; get; }
		public MultiValue Target { init; get; }
		public IOperation Operation { init; get; }
		public ISymbol OwningSymbol { init; get; }

		public TrimAnalysisAssignmentPattern (
			MultiValue source,
			MultiValue target,
			IOperation operation,
			ISymbol owningSymbol)
		{
			Source = source.DeepCopy ();
			Target = target.DeepCopy ();
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public TrimAnalysisAssignmentPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (OwningSymbol, other.OwningSymbol));

			return new TrimAnalysisAssignmentPattern (
				lattice.Meet (Source, other.Source),
				lattice.Meet (Target, other.Target),
				Operation,
				OwningSymbol);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
			if (context.EnableTrimAnalyzer && !OwningSymbol.IsInRequiresUnreferencedCodeAttributeScope (out _)) {
				foreach (var sourceValue in Source) {
					foreach (var targetValue in Target) {
						// The target should always be an annotated value, but the visitor design currently prevents
						// declaring this in the type system.
						if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
							throw new NotImplementedException ();

						var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, default (ReflectionAccessAnalyzer));
						requireDynamicallyAccessedMembersAction.Invoke (sourceValue, targetWithDynamicallyAccessedMembers);
					}
				}
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
