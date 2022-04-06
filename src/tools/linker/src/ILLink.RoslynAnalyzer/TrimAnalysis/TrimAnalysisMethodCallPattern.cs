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
	public readonly record struct TrimAnalysisMethodCallPattern (
		IMethodSymbol CalledMethod,
		MultiValue Instance,
		ImmutableArray<MultiValue> Arguments,
		IOperation Operation,
		ISymbol OwningSymbol)
	{
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
			if (!handleCallAction.Invoke (new MethodProxy (CalledMethod), Instance, Arguments, out _, out _)) {
				// If the intrinsic handling didn't work we have to:
				//   Handle the instance value
				//   Handle argument passing
				//   Construct the return value
				// Note: this is temporary as eventually the handling of all method calls should be done in the shared code (not just intrinsics)
				if (!CalledMethod.IsStatic) {
					var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, new ReflectionAccessAnalyzer ());
					requireDynamicallyAccessedMembersAction.Invoke (Instance, new MethodThisParameterValue (CalledMethod));
				}

				for (int argumentIndex = 0; argumentIndex < Arguments.Length; argumentIndex++) {
					var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction (diagnosticContext, new ReflectionAccessAnalyzer ());
					requireDynamicallyAccessedMembersAction.Invoke (Arguments[argumentIndex], new MethodParameterValue (CalledMethod.Parameters[argumentIndex]));
				}
			}

			return diagnosticContext.Diagnostics;
		}
	}
}
