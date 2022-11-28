// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// This class is responsible for the interprocedural analysis of local variables.
	// It substitutes type arguments into the generic forward dataflow analysis,
	// creating a simpler abstraction that can track the values of local variables using Roslyn APIs.
	// The kinds of values tracked are still left as unspecified generic parameters TValue and TLattice.
	public abstract class LocalDataFlowAnalysis<TValue, TLattice, TTransfer>
		: ForwardDataFlowAnalysis<
			LocalState<TValue>,
			LocalDataFlowState<TValue, TLattice>,
			LocalStateLattice<TValue, TLattice>,
			BlockProxy,
			RegionProxy,
			ControlFlowGraphProxy,
			TTransfer
		>
		where TValue : struct, IEquatable<TValue>
		where TLattice : ILattice<TValue>, new()
		where TTransfer : LocalDataFlowVisitor<TValue, TLattice>
	{
		protected readonly LocalStateLattice<TValue, TLattice> Lattice;

		protected readonly OperationBlockAnalysisContext Context;

		readonly IOperation OperationBlock;

		protected LocalDataFlowAnalysis (OperationBlockAnalysisContext context, IOperation operationBlock)
		{
			Lattice = new (new TLattice ());
			Context = context;
			OperationBlock = operationBlock;
		}

		public void InterproceduralAnalyze ()
		{
			var methodGroupLattice = new ValueSetLattice<MethodBodyValue> ();
			var hoistedLocalLattice = new DictionaryLattice<LocalKey, Maybe<TValue>, MaybeLattice<TValue, TLattice>> ();
			var interproceduralStateLattice = new InterproceduralStateLattice<TValue, TLattice> (
				methodGroupLattice, hoistedLocalLattice);
			var interproceduralState = interproceduralStateLattice.Top;

			var oldInterproceduralState = interproceduralState.Clone ();

			if (Context.OwningSymbol is not IMethodSymbol owningMethod)
				return;

			Debug.Assert (owningMethod.MethodKind is not (MethodKind.LambdaMethod or MethodKind.LocalFunction));
			var startMethod = new MethodBodyValue (owningMethod, Context.GetControlFlowGraph (OperationBlock));
			interproceduralState.TrackMethod (startMethod);

			while (!interproceduralState.Equals (oldInterproceduralState)) {
				oldInterproceduralState = interproceduralState.Clone ();

				foreach (var method in oldInterproceduralState.Methods) {
					if (method.Method.IsInRequiresUnreferencedCodeAttributeScope ())
						continue;

					AnalyzeMethod (method, ref interproceduralState);
				}
			}
		}

		void AnalyzeMethod (MethodBodyValue method, ref InterproceduralState<TValue, TLattice> interproceduralState)
		{
			var cfg = method.ControlFlowGraph;
			var lValueFlowCaptures = LValueFlowCapturesProvider.CreateLValueFlowCaptures (cfg);
			var visitor = GetVisitor (method.Method, cfg, lValueFlowCaptures, interproceduralState);
			Fixpoint (new ControlFlowGraphProxy (cfg), Lattice, visitor);

			// The interprocedural state struct is stored as a field of the visitor and modified
			// in-place there, but we also need those modifications to be reflected here.
			interproceduralState = visitor.InterproceduralState;
		}

		protected abstract TTransfer GetVisitor (
			IMethodSymbol method,
			ControlFlowGraph methodCFG,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			InterproceduralState<TValue, TLattice> interproceduralState);
	}
}