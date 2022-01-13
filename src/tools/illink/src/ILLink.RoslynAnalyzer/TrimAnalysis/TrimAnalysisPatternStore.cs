// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore : IEnumerable<TrimAnalysisPattern>
	{
		readonly Dictionary<IOperation, TrimAnalysisPattern> TrimAnalysisPatterns;

		public TrimAnalysisPatternStore () => TrimAnalysisPatterns = new Dictionary<IOperation, TrimAnalysisPattern> ();

		public void Add (TrimAnalysisPattern trimAnalysisPattern)
		{
			// TODO: check that this doesn't lose warnings (or add instead of replace)
			TrimAnalysisPatterns[trimAnalysisPattern.Operation] = trimAnalysisPattern;
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<TrimAnalysisPattern> GetEnumerator () => TrimAnalysisPatterns.Values.GetEnumerator ();
	}
}