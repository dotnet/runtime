// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public readonly record struct TrimAnalysisMethodCallPattern
	{
		public readonly Instruction Operation;
		public readonly MethodReference CalledMethod;
		public readonly MultiValue Instance;
		public readonly ImmutableArray<MultiValue> Arguments;
		public readonly MessageOrigin Origin;

		public TrimAnalysisMethodCallPattern (
			Instruction operation,
			MethodReference calledMethod,
			MultiValue instance,
			ImmutableArray<MultiValue> arguments,
			MessageOrigin origin)
		{
			Debug.Assert (origin.Provider is MethodDefinition);
			Operation = operation;
			CalledMethod = calledMethod;
			Instance = instance.DeepCopy ();
			if (arguments.IsEmpty) {
				Arguments = ImmutableArray<MultiValue>.Empty;
			} else {
				var builder = ImmutableArray.CreateBuilder<MultiValue> ();
				foreach (var argument in arguments)
					builder.Add (argument.DeepCopy ());
				Arguments = builder.ToImmutableArray ();
			}
			Origin = origin;
		}

		public TrimAnalysisMethodCallPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisMethodCallPattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (Origin == other.Origin);
			Debug.Assert (CalledMethod == other.CalledMethod);
			Debug.Assert (Arguments.Length == other.Arguments.Length);

			var argumentsBuilder = ImmutableArray.CreateBuilder<MultiValue> ();
			for (int i = 0; i < Arguments.Length; i++)
				argumentsBuilder.Add (lattice.Meet (Arguments[i], other.Arguments[i]));

			return new TrimAnalysisMethodCallPattern (
				Operation,
				CalledMethod,
				lattice.Meet (Instance, other.Instance),
				argumentsBuilder.ToImmutable (),
				Origin);
		}

		public void MarkAndProduceDiagnostics (ReflectionMarker reflectionMarker, MarkStep markStep, LinkContext context)
		{
			bool diagnosticsEnabled = !context.Annotations.ShouldSuppressAnalysisWarningsForRequiresUnreferencedCode (Origin.Provider, out _);
			var diagnosticContext = new DiagnosticContext (Origin, diagnosticsEnabled, context);
			ReflectionMethodBodyScanner.HandleCall (Operation, CalledMethod, Instance, Arguments,
				diagnosticContext,
				reflectionMarker,
				context,
				markStep,
				out MultiValue _);
		}
	}
}
