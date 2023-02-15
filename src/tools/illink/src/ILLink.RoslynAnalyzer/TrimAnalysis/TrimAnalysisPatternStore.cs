// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore
	{
		readonly Dictionary<(IOperation, bool), TrimAnalysisAssignmentPattern> AssignmentPatterns;
		readonly Dictionary<IOperation, TrimAnalysisMethodCallPattern> MethodCallPatterns;
		readonly ValueSetLattice<SingleValue> Lattice;

		public TrimAnalysisPatternStore (ValueSetLattice<SingleValue> lattice)
		{
			AssignmentPatterns = new Dictionary<(IOperation, bool), TrimAnalysisAssignmentPattern> ();
			MethodCallPatterns = new Dictionary<IOperation, TrimAnalysisMethodCallPattern> ();
			Lattice = lattice;
		}

		public void Add (TrimAnalysisAssignmentPattern trimAnalysisPattern, bool isReturnValue)
		{
			// Finally blocks will be analyzed multiple times, once for normal control flow and once
			// for exceptional control flow, and these separate analyses could produce different
			// trim analysis patterns.
			// The current algorithm always does the exceptional analysis last, so the final state for
			// an operation will include all analysis patterns (since the exceptional state is a superset)
			// of the normal control-flow state.
			// We still add patterns to the operation, rather than replacing, to make this resilient to
			// changes in the analysis algorithm.
			if (!AssignmentPatterns.TryGetValue ((trimAnalysisPattern.Operation, isReturnValue), out var existingPattern)) {
				AssignmentPatterns.Add ((trimAnalysisPattern.Operation, isReturnValue), trimAnalysisPattern);
				return;
			}

			AssignmentPatterns[(trimAnalysisPattern.Operation, isReturnValue)] = trimAnalysisPattern.Merge (Lattice, existingPattern);
		}

		public void Add (TrimAnalysisMethodCallPattern pattern)
		{
			if (!MethodCallPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				MethodCallPatterns.Add (pattern.Operation, pattern);
				return;
			}

			MethodCallPatterns[pattern.Operation] = pattern.Merge (Lattice, existingPattern);
		}

		public IEnumerable<Diagnostic> CollectDiagnostics ()
		{
			foreach (var assignmentPattern in AssignmentPatterns.Values) {
				foreach (var diagnostic in assignmentPattern.CollectDiagnostics ())
					yield return diagnostic;
			}

			foreach (var methodCallPattern in MethodCallPatterns.Values) {
				foreach (var diagnostic in methodCallPattern.CollectDiagnostics ())
					yield return diagnostic;
			}
		}
	}
}
