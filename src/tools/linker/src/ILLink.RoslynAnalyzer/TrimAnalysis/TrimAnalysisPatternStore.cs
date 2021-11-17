// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore : IEnumerable<TrimAnalysisPattern>
	{
		readonly Dictionary<IOperation, TrimAnalysisPattern> TrimAnalysisPatterns;

		public TrimAnalysisPatternStore () => TrimAnalysisPatterns = new Dictionary<IOperation, TrimAnalysisPattern> ();

		public void Add (TrimAnalysisPattern trimAnalysisPattern)
		{
			// If we already stored a trim analysis pattern for this operation,
			// it needs to be updated. The dataflow analysis should result in purely additive
			// changes to the trim analysis patterns generated for a given operation,
			// so we can just replace the original analysis pattern here.
#if DEBUG
			// Validate this in debug mode.
			if (TrimAnalysisPatterns.TryGetValue (trimAnalysisPattern.Operation, out var existingTrimAnalysisPattern)) {
				// The existing pattern source/target should be a subset of the new source/target.
				foreach (SingleValue source in existingTrimAnalysisPattern.Source)
					Debug.Assert (trimAnalysisPattern.Source.Contains (source));

				foreach (SingleValue target in existingTrimAnalysisPattern.Target)
					Debug.Assert (trimAnalysisPattern.Target.Contains (target));
			}
#endif
			TrimAnalysisPatterns[trimAnalysisPattern.Operation] = trimAnalysisPattern;
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<TrimAnalysisPattern> GetEnumerator () => TrimAnalysisPatterns.Values.GetEnumerator ();
	}
}