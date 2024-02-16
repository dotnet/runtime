// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore
	{
		readonly Dictionary<(IOperation, bool), TrimAnalysisAssignmentPattern> AssignmentPatterns;
		readonly Dictionary<IOperation, TrimAnalysisFieldAccessPattern> FieldAccessPatterns;
		readonly Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern> GenericInstantiationPatterns;
		readonly Dictionary<IOperation, TrimAnalysisMethodCallPattern> MethodCallPatterns;
		readonly Dictionary<IOperation, TrimAnalysisReflectionAccessPattern> ReflectionAccessPatterns;
		readonly Dictionary<IOperation, TrimAnalysisReturnValuePattern> ReturnValuePatterns;
		readonly ValueSetLattice<SingleValue> Lattice;
		readonly FeatureContextLattice FeatureContextLattice;

		public TrimAnalysisPatternStore (
			ValueSetLattice<SingleValue> lattice,
			FeatureContextLattice featureContextLattice)
		{
			AssignmentPatterns = new Dictionary<(IOperation, bool), TrimAnalysisAssignmentPattern> ();
			FieldAccessPatterns = new Dictionary<IOperation, TrimAnalysisFieldAccessPattern> ();
			GenericInstantiationPatterns = new Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern> ();
			MethodCallPatterns = new Dictionary<IOperation, TrimAnalysisMethodCallPattern> ();
			ReflectionAccessPatterns = new Dictionary<IOperation, TrimAnalysisReflectionAccessPattern> ();
			ReturnValuePatterns = new Dictionary<IOperation, TrimAnalysisReturnValuePattern> ();
			Lattice = lattice;
			FeatureContextLattice = featureContextLattice;
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

			AssignmentPatterns[(trimAnalysisPattern.Operation, isReturnValue)] = trimAnalysisPattern.Merge (Lattice, FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisFieldAccessPattern pattern)
		{
			if (!FieldAccessPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				FieldAccessPatterns.Add (pattern.Operation, pattern);
				return;
			}

			FieldAccessPatterns[pattern.Operation] = pattern.Merge (Lattice, FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisGenericInstantiationPattern pattern)
		{
			if (!GenericInstantiationPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				GenericInstantiationPatterns.Add (pattern.Operation, pattern);
				return;
			}

			GenericInstantiationPatterns[pattern.Operation] = pattern.Merge (FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisMethodCallPattern pattern)
		{
			if (!MethodCallPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				MethodCallPatterns.Add (pattern.Operation, pattern);
				return;
			}

			MethodCallPatterns[pattern.Operation] = pattern.Merge (Lattice, FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisReflectionAccessPattern pattern)
		{
			if (!ReflectionAccessPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				ReflectionAccessPatterns.Add (pattern.Operation, pattern);
				return;
			}

			ReflectionAccessPatterns[pattern.Operation] = pattern.Merge (Lattice, FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisReturnValuePattern pattern)
		{
			if (!ReturnValuePatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				ReturnValuePatterns.Add (pattern.Operation, pattern);
				return;
			}

			Debug.Assert (existingPattern == pattern, "Return values should be identical");
		}

		public IEnumerable<Diagnostic> CollectDiagnostics (DataFlowAnalyzerContext context)
		{
			foreach (var assignmentPattern in AssignmentPatterns.Values) {
				foreach (var diagnostic in assignmentPattern.CollectDiagnostics (context))
					yield return diagnostic;
			}

			foreach (var fieldAccessPattern in FieldAccessPatterns.Values) {
				foreach (var diagnostic in fieldAccessPattern.CollectDiagnostics (context))
					yield return diagnostic;
			}

			foreach (var genericInstantiationPattern in GenericInstantiationPatterns.Values) {
				foreach (var diagnostic in genericInstantiationPattern.CollectDiagnostics (context))
					yield return diagnostic;
			}

			foreach (var methodCallPattern in MethodCallPatterns.Values) {
				foreach (var diagnostic in methodCallPattern.CollectDiagnostics (context))
					yield return diagnostic;
			}

			foreach (var reflectionAccessPattern in ReflectionAccessPatterns.Values) {
				foreach (var diagnostic in reflectionAccessPattern.CollectDiagnostics (context))
					yield return diagnostic;
			}

			foreach (var returnValuePattern in ReturnValuePatterns.Values) {
				foreach (var diagnostic in returnValuePattern.CollectDiagnostics (context))
					yield return diagnostic;
			}
		}
	}
}
