// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisMethodCallPattern
	{
		public IMethodSymbol CalledMethod { init; get; }
		public MultiValue Instance { init; get; }
		public ImmutableArray<MultiValue> Arguments { init; get; }
		public IOperation Operation { init; get; }
		public ISymbol OwningSymbol { init; get; }

		public TrimAnalysisMethodCallPattern (
			IMethodSymbol calledMethod,
			MultiValue instance,
			ImmutableArray<MultiValue> arguments,
			IOperation operation,
			ISymbol owningSymbol)
		{
			CalledMethod = calledMethod;
			Instance = instance.DeepCopy ();
			if (arguments.IsEmpty) {
				Arguments = ImmutableArray<MultiValue>.Empty;
			} else {
				var builder = ImmutableArray.CreateBuilder<MultiValue> ();
				foreach (var argument in arguments) {
					builder.Add (argument.DeepCopy ());
				}
				Arguments = builder.ToImmutableArray ();
			}
			Operation = operation;
			OwningSymbol = owningSymbol;
		}

		public TrimAnalysisMethodCallPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisMethodCallPattern other)
		{
			Debug.Assert (Operation == other.Operation);
			Debug.Assert (SymbolEqualityComparer.Default.Equals (CalledMethod, other.CalledMethod));
			Debug.Assert (Arguments.Length == other.Arguments.Length);

			var argumentsBuilder = ImmutableArray.CreateBuilder<MultiValue> ();
			for (int i = 0; i < Arguments.Length; i++) {
				argumentsBuilder.Add (lattice.Meet (Arguments[i], other.Arguments[i]));
			}

			return new TrimAnalysisMethodCallPattern (
				CalledMethod,
				lattice.Meet (Instance, other.Instance),
				argumentsBuilder.ToImmutable (),
				Operation,
				OwningSymbol);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics ()
		{
			DiagnosticContext diagnosticContext = new (Operation.Syntax.GetLocation ());
			HandleCallAction handleCallAction = new (diagnosticContext, OwningSymbol, Operation);
			MethodProxy method = new (CalledMethod);
			IntrinsicId intrinsicId = Intrinsics.GetIntrinsicIdForMethod (method);
			if (!handleCallAction.Invoke (method, Instance, Arguments, intrinsicId, out _)) {
				// If this returns false it means the intrinsic needs special handling:
				// case IntrinsicId.TypeDelegator_Ctor:
				//    No diagnostics to report - this is an "identity" operation for data flow, can't produce diagnostics on its own
				// case IntrinsicId.Array_Empty:
				//    No diagnostics to report - constant value
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
