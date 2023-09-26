// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	public struct RequiresAnalyzerContext
	{
		private Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> _enabledAnalyzers;

		public IEnumerable<RequiresAnalyzerBase> EnabledRequiresAnalyzers => _enabledAnalyzers.Keys;

		public ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (RequiresAnalyzerBase analyzer)
		{
			if (!_enabledAnalyzers.TryGetValue (analyzer, out var members))
				throw new System.ArgumentException ($"Analyzer {analyzer.GetType ().Name} is not in the cache");
			return members;
		}

		RequiresAnalyzerContext (Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> enabledAnalyzers)
		{
			_enabledAnalyzers = enabledAnalyzers;
		}

		public static RequiresAnalyzerContext Create (OperationBlockAnalysisContext context, ImmutableArray<RequiresAnalyzerBase> requiresAnalyzers)
		{
			var enabledAnalyzers = new Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> ();
			foreach (var analyzer in requiresAnalyzers) {
				if (analyzer.IsAnalyzerEnabled (context.Options)) {
					var incompatibleMembers = analyzer.GetSpecialIncompatibleMembers (context.Compilation);
					enabledAnalyzers.Add (analyzer, incompatibleMembers);
				}
			}
			return new RequiresAnalyzerContext (enabledAnalyzers);
		}
	}
}
