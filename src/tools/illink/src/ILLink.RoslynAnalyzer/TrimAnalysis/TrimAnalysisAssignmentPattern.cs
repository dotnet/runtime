// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisAssignmentPattern
	{
		public MultiValue Source { init; get; }
		public MultiValue Target { init; get; }
		public IOperation Operation { init; get; }

		public TrimAnalysisAssignmentPattern (MultiValue source, MultiValue target, IOperation operation)
		{
			Source = source.DeepCopy ();
			Target = target.DeepCopy ();
			Operation = operation;
		}

		public TrimAnalysisAssignmentPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
		{
			Debug.Assert (Operation == other.Operation);

			return new TrimAnalysisAssignmentPattern (
				lattice.Meet (Source, other.Source),
				lattice.Meet (Target, other.Target),
				Operation);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics ()
		{
			var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
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

			return diagnosticContext.Diagnostics;
		}
	}
}
