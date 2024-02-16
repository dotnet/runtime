// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// This class is responsible for the interprocedural analysis of local variables.
	// It substitutes type arguments into the generic forward dataflow analysis,
	// creating a simpler abstraction that can track the values of local variables using Roslyn APIs.
	// The kinds of values tracked are still left as unspecified generic parameters TValue and TLattice.
	public abstract class LocalDataFlowAnalysis<TValue, TContext, TLattice, TContextLattice, TTransfer, TConditionValue>
		: ForwardDataFlowAnalysis<
			LocalStateAndContext<TValue, TContext>,
			LocalDataFlowState<TValue, TContext, TLattice, TContextLattice>,
			LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice>,
			BlockProxy,
			RegionProxy,
			ControlFlowGraphProxy,
			TTransfer,
			TConditionValue
		>
		where TValue : struct, IEquatable<TValue>
		where TContext : struct, IEquatable<TContext>
		where TLattice : ILattice<TValue>, new()
		where TContextLattice : ILattice<TContext>, new()
		where TTransfer : LocalDataFlowVisitor<TValue, TContext, TLattice, TContextLattice, TConditionValue>
		where TConditionValue : struct, INegate<TConditionValue>
	{
		protected readonly OperationBlockAnalysisContext Context;

		readonly IOperation OperationBlock;

		static LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice> GetLatticeAndEntryValue(
			TContext initialContext,
			out LocalStateAndContext<TValue, TContext> entryValue)
		{
			LocalStateAndContextLattice<TValue, TContext, TLattice, TContextLattice> lattice = new (new (new TLattice ()), new TContextLattice ());
			entryValue = new LocalStateAndContext<TValue, TContext> (default (LocalState<TValue>), initialContext);
			return lattice;
		}

		// The initial value of the local dataflow is the empty local state (no tracked assignments),
		// with an initial context that must be specified by the derived class.
		protected LocalDataFlowAnalysis (OperationBlockAnalysisContext context, IOperation operationBlock, TContext initialContext)
			: base (GetLatticeAndEntryValue (initialContext, out var entryValue), entryValue)
		{
			Context = context;
			OperationBlock = operationBlock;
		}

		public void InterproceduralAnalyze ()
		{
			ValueSetLattice<MethodBodyValue> methodGroupLattice = default;
			DictionaryLattice<LocalKey, Maybe<TValue>, MaybeLattice<TValue, TLattice>> hoistedLocalLattice = default;
			var interproceduralStateLattice = new InterproceduralStateLattice<TValue, TLattice> (
				methodGroupLattice, hoistedLocalLattice);
			var interproceduralState = interproceduralStateLattice.Top;

			var oldInterproceduralState = interproceduralState.Clone ();

			if (OperationBlock is IAttributeOperation attribute) {
				AnalyzeAttribute (Context.OwningSymbol, attribute);
				return;
			}

			Debug.Assert (Context.OwningSymbol is not IMethodSymbol methodSymbol ||
				methodSymbol.MethodKind is not (MethodKind.LambdaMethod or MethodKind.LocalFunction));
			var startMethod = new MethodBodyValue (Context.OwningSymbol, Context.GetControlFlowGraph (OperationBlock));
			interproceduralState.TrackMethod (startMethod);

			while (!interproceduralState.Equals (oldInterproceduralState)) {
				oldInterproceduralState = interproceduralState.Clone ();

				Debug.Assert (!oldInterproceduralState.Methods.IsUnknown ());
				foreach (var method in oldInterproceduralState.Methods.GetKnownValues ()) {
					AnalyzeMethod (method, ref interproceduralState);
				}
			}
		}

		void AnalyzeAttribute (ISymbol owningSymbol, IAttributeOperation attribute)
		{
			var cfg = Context.GetControlFlowGraph (attribute);
			var lValueFlowCaptures = LValueFlowCapturesProvider.CreateLValueFlowCaptures (cfg);
			var visitor = GetVisitor (owningSymbol, cfg, lValueFlowCaptures, default);
			Fixpoint (new ControlFlowGraphProxy (cfg), visitor);
		}

		void AnalyzeMethod (MethodBodyValue method, ref InterproceduralState<TValue, TLattice> interproceduralState)
		{
			var cfg = method.ControlFlowGraph;
			var lValueFlowCaptures = LValueFlowCapturesProvider.CreateLValueFlowCaptures (cfg);
			var visitor = GetVisitor (method.OwningSymbol, cfg, lValueFlowCaptures, interproceduralState);
			Fixpoint (new ControlFlowGraphProxy (cfg), visitor);

			// The interprocedural state struct is stored as a field of the visitor and modified
			// in-place there, but we also need those modifications to be reflected here.
			interproceduralState = visitor.InterproceduralState;
		}

		protected abstract TTransfer GetVisitor (
			ISymbol owningSymbol,
			ControlFlowGraph methodCFG,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			InterproceduralState<TValue, TLattice> interproceduralState);
	}
}
