// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using LocalStateValue = ILLink.RoslynAnalyzer.DataFlow.LocalStateAndContext<
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.RoslynAnalyzer.DataFlow.FeatureContext
>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public class TrimDataFlowAnalysis : LocalDataFlowAnalysis<
		MultiValue,
		FeatureContext,
		ValueSetLattice<SingleValue>,
		FeatureContextLattice,
		TrimAnalysisVisitor,
		FeatureChecksValue>
	{
		public TrimAnalysisPatternStore TrimAnalysisPatterns { get; }

		DataFlowAnalyzerContext _dataFlowAnalyzerContext;

		// The initial state of the feature context is None, meaning that
		// no features are enabled at the beginning of the entry block.
		// This way, calls to all Requires-annotated APIs will warn unless
		// guarded by a feature check.
		public TrimDataFlowAnalysis (
			OperationBlockAnalysisContext context,
			DataFlowAnalyzerContext dataFlowAnalyzerContext,
			IOperation operationBlock)
			: base (context, operationBlock, initialContext: FeatureContext.None)
		{
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (lattice.LocalStateLattice.Lattice.ValueLattice, lattice.ContextLattice);
			_dataFlowAnalyzerContext = dataFlowAnalyzerContext;
		}

		public IEnumerable<Diagnostic> CollectDiagnostics ()
		{
			return TrimAnalysisPatterns.CollectDiagnostics (_dataFlowAnalyzerContext);
		}

		protected override TrimAnalysisVisitor GetVisitor (
			ISymbol owningSymbol,
			ControlFlowGraph methodCFG,
			ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures,
			InterproceduralState<MultiValue, ValueSetLattice<SingleValue>> interproceduralState)
		 => new (Context.Compilation, lattice, owningSymbol, methodCFG, lValueFlowCaptures, TrimAnalysisPatterns, interproceduralState, _dataFlowAnalyzerContext);

#if DEBUG
#pragma warning disable CA1805 // Do not initialize unnecessarily
		// Set this to a method name to trace the analysis of the method.
		readonly string? traceMethod = null;

		bool trace = false;

		// Set this to true to print out the dataflow states encountered during the analysis.
		readonly bool showStates = false;

		static readonly TracingType tracingMechanism = Debugger.IsAttached ? TracingType.Debug : TracingType.Console;
#pragma warning restore CA1805 // Do not initialize unnecessarily
		ControlFlowGraphProxy cfg;

		private enum TracingType
		{
			Console,
			Debug
		}

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
			if (trace)
				TraceWriteLine("Tracing method " + methodName);
		}

		public override void TraceVisitBlock (BlockProxy block)
		{
			if (!trace)
				return;

			TraceWrite ("block " + block.Block.Ordinal + ": ");
			if (block.Block.Operations.FirstOrDefault () is IOperation firstBlockOp) {
				TraceWriteLine (firstBlockOp.Syntax.ToString ());
			} else if (block.Block.BranchValue is IOperation branchOp) {
				TraceWriteLine (branchOp.Syntax.ToString ());
			} else {
				TraceWriteLine ("");
			}
			TraceWrite ("predecessors: ");
			foreach (var predecessor in cfg.GetPredecessors (block)) {
				var predProxy = predecessor.Source;
				TraceWrite (predProxy.Block.Ordinal + " ");
			}
			TraceWriteLine ("");
		}

		private static void TraceWriteLine (string tracingInfo)
		{
			switch (tracingMechanism) {
			case TracingType.Console:
// Analyzers should not be writing to the console,
// but this is only used for debugging purposes and is off by default.
#pragma warning disable RS1035
				Console.WriteLine (tracingInfo);
#pragma warning restore RS1035
				break;
			case TracingType.Debug:
				Debug.WriteLine (tracingInfo);
				break;
			default:
				throw new NotImplementedException (message: "invalid TracingType is being used");
			}
		}

		private static void TraceWrite (string tracingInfo)
		{
			switch (tracingMechanism) {
			case TracingType.Console:
// Analyzers should not be writing to the console,
// but this is only used for debugging purposes and is off by default.
#pragma warning disable RS1035
				Console.Write (tracingInfo);
#pragma warning restore RS1035
				break;
			case TracingType.Debug:
				Debug.Write (tracingInfo);
				break;
			default:
				throw new NotImplementedException (message: "invalid TracingType is being used");
			}
		}

		static void WriteIndented (string? s, int level)
		{
			if (s is not null) {
				var reader = new StringReader (s);
				string? line;
				while ((line = reader.ReadLine ()) != null) {
					if (line.Length != 0) {
						TraceWrite (new string ('\t', level));
						TraceWriteLine (line);
					}
				}
			}
		}

		public override void TraceBlockInput (
			LocalStateValue normalState,
			LocalStateValue? exceptionState,
			LocalStateValue? exceptionFinallyState
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
			LocalStateValue normalState,
			LocalStateValue? exceptionState,
			LocalStateValue? exceptionFinallyState
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
