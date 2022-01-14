// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore : IEnumerable<TrimAnalysisPattern>
	{
		readonly Dictionary<(IOperation, bool), TrimAnalysisPattern> TrimAnalysisPatterns;

		readonly ValueSetLattice<SingleValue> Lattice;

		public TrimAnalysisPatternStore (ValueSetLattice<SingleValue> lattice)
		{
			TrimAnalysisPatterns = new Dictionary<(IOperation, bool), TrimAnalysisPattern> ();
			Lattice = lattice;
		}

		public void Add (TrimAnalysisPattern trimAnalysisPattern, bool isReturnValue)
		{
			// Finally blocks will be analyzed multiple times, once for normal control flow and once
			// for exceptional control flow, and these separate analyses could produce different
			// trim analysis patterns.
			// The current algorithm always does the exceptional analysis last, so the final state for
			// an operation will include all analysis patterns (since the exceptional state is a superset)
			// of the normal control-flow state.
			// We still add patterns to the operation, rather than replacing, to make this resilient to
			// changes in the analysis algorithm.
			if (!TrimAnalysisPatterns.TryGetValue ((trimAnalysisPattern.Operation, isReturnValue), out TrimAnalysisPattern existingPattern)) {
				TrimAnalysisPatterns.Add ((trimAnalysisPattern.Operation, isReturnValue), trimAnalysisPattern);
				return;
			}

			MultiValue source = Lattice.Meet (trimAnalysisPattern.Source, existingPattern.Source);
			MultiValue target = Lattice.Meet (trimAnalysisPattern.Target, existingPattern.Target);
			TrimAnalysisPatterns[(trimAnalysisPattern.Operation, isReturnValue)] = new TrimAnalysisPattern (source, target, trimAnalysisPattern.Operation);
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<TrimAnalysisPattern> GetEnumerator () => TrimAnalysisPatterns.Values.GetEnumerator ();
	}
}