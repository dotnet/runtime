// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public readonly record struct TrimAnalysisAssignmentPattern
	{
		public MultiValue Source { get; init; }
		public MultiValue Target { get; init; }
		public MessageOrigin Origin { get; init; }

		// For assignment of a method parameter, we store the parameter index to disambiguate
		// assignments from different out parameters of a single method call.
		public int? ParameterIndex { get; init; }

		public TrimAnalysisAssignmentPattern (MultiValue source, MultiValue target, MessageOrigin origin, int? parameterIndex)
		{
			Source = source.DeepCopy ();
			Target = target.DeepCopy ();
			Origin = origin;
			ParameterIndex = parameterIndex;
		}

		public TrimAnalysisAssignmentPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
		{
			Debug.Assert (Origin == other.Origin);
			Debug.Assert (ParameterIndex == other.ParameterIndex);

			return new TrimAnalysisAssignmentPattern (
				lattice.Meet (Source, other.Source),
				lattice.Meet (Target, other.Target),
				Origin,
				ParameterIndex);
		}

		public void MarkAndProduceDiagnostics (ReflectionMarker reflectionMarker, LinkContext context)
		{
			bool diagnosticsEnabled = !context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (Origin.Provider, out _);
			var diagnosticContext = new DiagnosticContext (Origin, diagnosticsEnabled, context);

			foreach (var sourceValue in Source.AsEnumerable ()) {
				foreach (var targetValue in Target.AsEnumerable ()) {
					if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
						throw new NotImplementedException ();

					var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (reflectionMarker, diagnosticContext);
					requireDynamicallyAccessedMembersAction.Invoke (sourceValue, targetWithDynamicallyAccessedMembers);
				}
			}
		}
	}
}
