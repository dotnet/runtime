// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public class TrimDataFlowAnalysis
		: ForwardDataFlowAnalysis<
			LocalState<ValueSet<SingleValue>>,
			LocalDataFlowState<ValueSet<SingleValue>, ValueSetLattice<SingleValue>>,
			LocalStateLattice<ValueSet<SingleValue>, ValueSetLattice<SingleValue>>,
			BlockProxy,
			RegionProxy,
			ControlFlowGraphProxy,
			TrimAnalysisVisitor
		>
	{
		readonly ControlFlowGraphProxy ControlFlowGraph;

		readonly LocalStateLattice<ValueSet<SingleValue>, ValueSetLattice<SingleValue>> Lattice;

		readonly OperationBlockAnalysisContext Context;

		public TrimDataFlowAnalysis (OperationBlockAnalysisContext context, ControlFlowGraph cfg)
		{
			ControlFlowGraph = new ControlFlowGraphProxy (cfg);
			Lattice = new (new ValueSetLattice<SingleValue> ());
			Context = context;
		}

		public IEnumerable<TrimAnalysisPattern> ComputeTrimAnalysisPatterns ()
		{
			var visitor = new TrimAnalysisVisitor (Lattice, Context);
			Fixpoint (ControlFlowGraph, Lattice, visitor);
			return visitor.TrimAnalysisPatterns;
		}

#if DEBUG
#pragma warning disable CA1805 // Do not initialize unnecessarily
		// Set this to a method name to trace the analysis of the method.
		readonly string? traceMethod = null;

		bool trace = false;

		// Set this to true to print out the dataflow states encountered during the analysis.
		readonly bool showStates = false;
#pragma warning restore CA1805 // Do not initialize unnecessarily
		ControlFlowGraphProxy cfg;

		public override void TraceStart (ControlFlowGraphProxy cfg)
		{
			this.cfg = cfg;
			var blocks = cfg.Blocks.ToList ();
			string? methodName = null;
			foreach (var block in blocks) {
				if (block.Block.Operations.FirstOrDefault () is not IOperation op)
					continue;

				var method = op.Syntax.FirstAncestorOrSelf<MethodDeclarationSyntax> ();
				if (method is MethodDeclarationSyntax)
					methodName = method.Identifier.ValueText;

				break;
			}

			if (methodName?.Equals (traceMethod) == true)
				trace = true;
		}

		public override void TraceVisitBlock (BlockProxy block)
		{
			if (!trace)
				return;

			Console.Write ("block " + block.Block.Ordinal + ": ");
			if (block.Block.Operations.FirstOrDefault () is IOperation firstBlockOp) {
				Console.WriteLine (firstBlockOp.Syntax.ToString ());
			} else {
				Console.WriteLine ();
			}
			Console.Write ("predecessors: ");
			foreach (var predecessor in cfg.GetPredecessors (block)) {
				var predProxy = predecessor.Block;
				Console.Write (predProxy.Block.Ordinal + " ");
			}
			Console.WriteLine ();
		}

		static void WriteIndented (string? s, int level)
		{
			string[]? lines = s?.Trim ().Split (new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			if (lines == null)
				return;
			foreach (var line in lines) {
				Console.Write (new String ('\t', level));
				Console.WriteLine (line);
			}
		}

		public override void TraceBlockInput (
			LocalState<ValueSet<SingleValue>> normalState,
			LocalState<ValueSet<SingleValue>>? exceptionState,
			LocalState<ValueSet<SingleValue>>? exceptionFinallyState
		)
		{
			if (trace && showStates) {
				WriteIndented ("--- before transfer ---", 1);
				WriteIndented ("normal state:", 1);
				WriteIndented (normalState.ToString (), 2);
				WriteIndented ("exception state:", 1);
				WriteIndented (exceptionState?.ToString (), 2);
				WriteIndented ("finally exception state:", 1);
				WriteIndented (exceptionFinallyState?.ToString (), 2);
			}
		}

		public override void TraceBlockOutput (
			LocalState<ValueSet<SingleValue>> normalState,
			LocalState<ValueSet<SingleValue>>? exceptionState,
			LocalState<ValueSet<SingleValue>>? exceptionFinallyState
		)
		{
			if (trace && showStates) {
				WriteIndented ("--- after transfer ---", 1);
				WriteIndented ("normal state:", 1);
				WriteIndented (normalState.ToString (), 2);
				WriteIndented ("exception state:", 1);
				WriteIndented (exceptionState?.ToString (), 2);
				WriteIndented ("finally state:", 1);
				WriteIndented (exceptionFinallyState?.ToString (), 2);
			}
		}
#endif
	}
}