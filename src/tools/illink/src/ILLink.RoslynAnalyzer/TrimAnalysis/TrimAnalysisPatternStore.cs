// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	internal readonly struct TrimAnalysisPatternStore
	{
		readonly Dictionary<IOperation, TrimAnalysisAssignmentPattern> AssignmentPatterns;
		readonly Dictionary<IOperation, TrimAnalysisFieldAccessPattern> FieldAccessPatterns;
		readonly Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern> GenericInstantiationPatterns;
		readonly Dictionary<IOperation, TrimAnalysisMethodCallPattern> MethodCallPatterns;
		readonly Dictionary<IOperation, TrimAnalysisReflectionAccessPattern> ReflectionAccessPatterns;
		readonly Dictionary<IOperation, FeatureCheckReturnValuePattern> FeatureCheckReturnValuePatterns;
		readonly ValueSetLattice<SingleValue> Lattice;
		readonly FeatureContextLattice FeatureContextLattice;

		public TrimAnalysisPatternStore (
			ValueSetLattice<SingleValue> lattice,
			FeatureContextLattice featureContextLattice)
		{
			AssignmentPatterns = new Dictionary<IOperation, TrimAnalysisAssignmentPattern> ();
			FieldAccessPatterns = new Dictionary<IOperation, TrimAnalysisFieldAccessPattern> ();
			GenericInstantiationPatterns = new Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern> ();
			MethodCallPatterns = new Dictionary<IOperation, TrimAnalysisMethodCallPattern> ();
			ReflectionAccessPatterns = new Dictionary<IOperation, TrimAnalysisReflectionAccessPattern> ();
			FeatureCheckReturnValuePatterns = new Dictionary<IOperation, FeatureCheckReturnValuePattern> ();
			Lattice = lattice;
			FeatureContextLattice = featureContextLattice;
		}

		public void Add (TrimAnalysisAssignmentPattern trimAnalysisPattern)
		{
			// Finally blocks will be analyzed multiple times, once for normal control flow and once
			// for exceptional control flow, and these separate analyses could produce different
			// trim analysis patterns.
			// The current algorithm always does the exceptional analysis last, so the final state for
			// an operation will include all analysis patterns (since the exceptional state is a superset)
			// of the normal control-flow state.
			// We still add patterns to the operation, rather than replacing, to make this resilient to
			// changes in the analysis algorithm.
			if (!AssignmentPatterns.TryGetValue (trimAnalysisPattern.Operation, out var existingPattern)) {
				AssignmentPatterns.Add (trimAnalysisPattern.Operation, trimAnalysisPattern);
				return;
			}

			AssignmentPatterns[trimAnalysisPattern.Operation] = trimAnalysisPattern.Merge (Lattice, FeatureContextLattice, existingPattern);
		}

		public void Add (TrimAnalysisFieldAccessPattern pattern)
		{
			if (!FieldAccessPatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				FieldAccessPatterns.Add (pattern.Operation, pattern);
				return;
			}

			FieldAccessPatterns[pattern.Operation] = pattern.Merge (FeatureContextLattice, existingPattern);
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

			ReflectionAccessPatterns[pattern.Operation] = pattern.Merge (FeatureContextLattice, existingPattern);
		}

		public void Add (FeatureCheckReturnValuePattern pattern)
		{
			if (!FeatureCheckReturnValuePatterns.TryGetValue (pattern.Operation, out var existingPattern)) {
				FeatureCheckReturnValuePatterns.Add (pattern.Operation, pattern);
				return;
			}

			Debug.Assert (existingPattern == pattern, "Return values should be identical");
		}

		public void ReportDiagnostics (DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
		{
			foreach (var assignmentPattern in AssignmentPatterns.Values)
				assignmentPattern.ReportDiagnostics (context, reportDiagnostic);

			foreach (var fieldAccessPattern in FieldAccessPatterns.Values)
				fieldAccessPattern.ReportDiagnostics (context, reportDiagnostic);

			foreach (var genericInstantiationPattern in GenericInstantiationPatterns.Values)
				genericInstantiationPattern.ReportDiagnostics (context, reportDiagnostic);

			foreach (var methodCallPattern in MethodCallPatterns.Values)
				methodCallPattern.ReportDiagnostics (context, reportDiagnostic);

			foreach (var reflectionAccessPattern in ReflectionAccessPatterns.Values)
				reflectionAccessPattern.ReportDiagnostics (context, reportDiagnostic);

			foreach (var returnValuePattern in FeatureCheckReturnValuePatterns.Values)
				returnValuePattern.ReportDiagnostics (context, reportDiagnostic);
		}
	}
}
