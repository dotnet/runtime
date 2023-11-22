// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	public readonly struct DataFlowAnalyzerContext
	{
		private readonly Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> _enabledAnalyzers;

		public IEnumerable<RequiresAnalyzerBase> EnabledRequiresAnalyzers => _enabledAnalyzers.Keys;

		public ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (RequiresAnalyzerBase analyzer)
		{
			if (!_enabledAnalyzers.TryGetValue (analyzer, out var members))
				throw new System.ArgumentException ($"Analyzer {analyzer.GetType ().Name} is not in the cache");
			return members;
		}

		public Compilation Compilation { get; }

		public readonly bool EnableTrimAnalyzer { get; }

		public readonly bool AnyAnalyzersEnabled => EnableTrimAnalyzer || _enabledAnalyzers.Count > 0;

		DataFlowAnalyzerContext (
			Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> enabledAnalyzers,
			bool enableTrimAnalyzer,
			Compilation compilation)
		{
			_enabledAnalyzers = enabledAnalyzers;
			EnableTrimAnalyzer = enableTrimAnalyzer;
			Compilation = compilation;
		}

		public static DataFlowAnalyzerContext Create (AnalyzerOptions options, Compilation compilation, ImmutableArray<RequiresAnalyzerBase> requiresAnalyzers)
		{
			var enabledAnalyzers = new Dictionary<RequiresAnalyzerBase, ImmutableArray<ISymbol>> ();
			foreach (var analyzer in requiresAnalyzers) {
				if (analyzer.IsAnalyzerEnabled (options)) {
					var incompatibleMembers = analyzer.GetSpecialIncompatibleMembers (compilation);
					enabledAnalyzers.Add (analyzer, incompatibleMembers);
				}
			}
			return new DataFlowAnalyzerContext (
				enabledAnalyzers,
				options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				compilation);
		}
	}
}
