// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	public readonly struct TrimAnalysisPatternStore
	{
		readonly Dictionary<(MessageOrigin, int?), TrimAnalysisAssignmentPattern> AssignmentPatterns;
		readonly Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> MethodCallPatterns;
		readonly ValueSetLattice<SingleValue> Lattice;
		readonly LinkContext _context;

		public TrimAnalysisPatternStore (ValueSetLattice<SingleValue> lattice, LinkContext context)
		{
			AssignmentPatterns = new Dictionary<(MessageOrigin, int?), TrimAnalysisAssignmentPattern> ();
			MethodCallPatterns = new Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> ();
			Lattice = lattice;
			_context = context;
		}

		public void Add (TrimAnalysisAssignmentPattern pattern)
		{
			var key = (pattern.Origin, pattern.ParameterIndex);
			if (!AssignmentPatterns.TryGetValue (key, out var existingPattern)) {
				AssignmentPatterns.Add (key, pattern);
				return;
			}

			AssignmentPatterns[key] = pattern.Merge (Lattice, existingPattern);
		}

		public void Add (TrimAnalysisMethodCallPattern pattern)
		{
			if (!MethodCallPatterns.TryGetValue (pattern.Origin, out var existingPattern)) {
				MethodCallPatterns.Add (pattern.Origin, pattern);
				return;
			}

			MethodCallPatterns[pattern.Origin] = pattern.Merge (Lattice, existingPattern);
		}

		public void MarkAndProduceDiagnostics (ReflectionMarker reflectionMarker, MarkStep markStep)
		{
			foreach (var pattern in AssignmentPatterns.Values)
				pattern.MarkAndProduceDiagnostics (reflectionMarker, _context);

			foreach (var pattern in MethodCallPatterns.Values)
				pattern.MarkAndProduceDiagnostics (reflectionMarker, markStep, _context);
		}
	}
}
