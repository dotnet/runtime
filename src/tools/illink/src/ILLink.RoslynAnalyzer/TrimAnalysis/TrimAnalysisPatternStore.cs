// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
    internal readonly struct TrimAnalysisPatternStore
    {
        private readonly Dictionary<IOperation, TrimAnalysisAssignmentPattern> AssignmentPatterns;
        private readonly Dictionary<IOperation, TrimAnalysisFieldAccessPattern> FieldAccessPatterns;
        private readonly Dictionary<IOperation, TrimAnalysisBackingFieldAccessPattern> BackingFieldAccessPatterns;
        private readonly Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern> GenericInstantiationPatterns;
        private readonly Dictionary<IOperation, TrimAnalysisMethodCallPattern> MethodCallPatterns;
        private readonly Dictionary<IOperation, TrimAnalysisReflectionAccessPattern> ReflectionAccessPatterns;
        private readonly Dictionary<IOperation, FeatureCheckReturnValuePattern> FeatureCheckReturnValuePatterns;
        private readonly ValueSetLattice<SingleValue> Lattice;
        private readonly FeatureContextLattice FeatureContextLattice;

        public TrimAnalysisPatternStore(
            ValueSetLattice<SingleValue> lattice,
            FeatureContextLattice featureContextLattice)
        {
            AssignmentPatterns = new Dictionary<IOperation, TrimAnalysisAssignmentPattern>();
            FieldAccessPatterns = new Dictionary<IOperation, TrimAnalysisFieldAccessPattern>();
            BackingFieldAccessPatterns = new Dictionary<IOperation, TrimAnalysisBackingFieldAccessPattern>();
            GenericInstantiationPatterns = new Dictionary<IOperation, TrimAnalysisGenericInstantiationPattern>();
            MethodCallPatterns = new Dictionary<IOperation, TrimAnalysisMethodCallPattern>();
            ReflectionAccessPatterns = new Dictionary<IOperation, TrimAnalysisReflectionAccessPattern>();
            FeatureCheckReturnValuePatterns = new Dictionary<IOperation, FeatureCheckReturnValuePattern>();
            Lattice = lattice;
            FeatureContextLattice = featureContextLattice;
        }

        public void Add(TrimAnalysisBackingFieldAccessPattern pattern)
        {
            if (!BackingFieldAccessPatterns.TryGetValue(pattern.Operation, out var existingPattern))
            {
                BackingFieldAccessPatterns.Add(pattern.Operation, pattern);
                return;
            }

            BackingFieldAccessPatterns[pattern.Operation] = pattern.Merge(FeatureContextLattice, existingPattern);
        }

        public void AddAssignment(ValueSet<SingleValue> source, ValueSet<SingleValue> target,
            IOperation operation, ISymbol owningSymbol, FeatureContext featureContext)
        {
            if (!HasAssignmentRequirements(target))
            {
                return;
            }

            // Finally blocks will be analyzed multiple times, once for normal control flow and once
            // for exceptional control flow, and these separate analyses could produce different
            // trim analysis patterns.
            // The current algorithm always does the exceptional analysis last, so the final state for
            // an operation will include all analysis patterns (since the exceptional state is a superset)
            // of the normal control-flow state.
            // We still add patterns to the operation, rather than replacing, to make this resilient to
            // changes in the analysis algorithm.
            if (!AssignmentPatterns.TryGetValue(operation, out TrimAnalysisAssignmentPattern existingPattern))
            {
                AssignmentPatterns.Add(operation, new TrimAnalysisAssignmentPattern(source, target, operation, owningSymbol, featureContext));
                return;
            }

            Debug.Assert(SymbolEqualityComparer.Default.Equals(existingPattern.OwningSymbol, owningSymbol));
            AssignmentPatterns[operation] = existingPattern.Merge(Lattice, FeatureContextLattice, source, target, featureContext);
        }

        private static bool HasAssignmentRequirements(ValueSet<SingleValue> target)
        {
            if (target.TryGetSingleValue(out SingleValue? single))
            {
                return single is not ValueWithDynamicallyAccessedMembers annotated || annotated.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None;
            }

            foreach (SingleValue value in target.AsEnumerable())
            {
                if (value is not ValueWithDynamicallyAccessedMembers annotated || annotated.DynamicallyAccessedMemberTypes != DynamicallyAccessedMemberTypes.None)
                {
                    return true;
                }
            }

            return false;
        }

        public void Add(TrimAnalysisFieldAccessPattern pattern)
        {
            if (!FieldAccessPatterns.TryGetValue(pattern.Operation, out var existingPattern))
            {
                FieldAccessPatterns.Add(pattern.Operation, pattern);
                return;
            }

            FieldAccessPatterns[pattern.Operation] = pattern.Merge(FeatureContextLattice, existingPattern);
        }

        public void Add(TrimAnalysisGenericInstantiationPattern pattern)
        {
            if (!GenericInstantiationPatterns.TryGetValue(pattern.Operation, out var existingPattern))
            {
                GenericInstantiationPatterns.Add(pattern.Operation, pattern);
                return;
            }

            GenericInstantiationPatterns[pattern.Operation] = pattern.Merge(FeatureContextLattice, existingPattern);
        }

        public void AddMethodCall(IMethodSymbol calledMethod, ValueSet<SingleValue> instance, ImmutableArray<ValueSet<SingleValue>> arguments,
            IOperation operation, ISymbol owningSymbol, FeatureContext featureContext)
        {
            if (!MethodCallPatterns.TryGetValue(operation, out TrimAnalysisMethodCallPattern existingPattern))
            {
                MethodCallPatterns.Add(operation, new TrimAnalysisMethodCallPattern(calledMethod, instance, arguments, operation, owningSymbol, featureContext));
                return;
            }

            Debug.Assert(SymbolEqualityComparer.Default.Equals(existingPattern.CalledMethod, calledMethod));
            Debug.Assert(SymbolEqualityComparer.Default.Equals(existingPattern.OwningSymbol, owningSymbol));
            MethodCallPatterns[operation] = existingPattern.Merge(Lattice, FeatureContextLattice, instance, arguments, featureContext);
        }

        public void Add(TrimAnalysisReflectionAccessPattern pattern)
        {
            if (!ReflectionAccessPatterns.TryGetValue(pattern.Operation, out var existingPattern))
            {
                ReflectionAccessPatterns.Add(pattern.Operation, pattern);
                return;
            }

            ReflectionAccessPatterns[pattern.Operation] = pattern.Merge(FeatureContextLattice, existingPattern);
        }

        public void Add(FeatureCheckReturnValuePattern pattern)
        {
            if (!FeatureCheckReturnValuePatterns.TryGetValue(pattern.Operation, out var existingPattern))
            {
                FeatureCheckReturnValuePatterns.Add(pattern.Operation, pattern);
                return;
            }

            Debug.Assert(existingPattern == pattern, "Return values should be identical");
        }

        public void ReportDiagnostics(DataFlowAnalyzerContext context, Action<Diagnostic> reportDiagnostic)
        {
            foreach (var assignmentPattern in AssignmentPatterns.Values)
                assignmentPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var fieldAccessPattern in FieldAccessPatterns.Values)
                fieldAccessPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var backingFieldAccessPattern in BackingFieldAccessPatterns.Values)
                backingFieldAccessPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var genericInstantiationPattern in GenericInstantiationPatterns.Values)
                genericInstantiationPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var methodCallPattern in MethodCallPatterns.Values)
                methodCallPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var reflectionAccessPattern in ReflectionAccessPatterns.Values)
                reflectionAccessPattern.ReportDiagnostics(context, reportDiagnostic);

            foreach (var returnValuePattern in FeatureCheckReturnValuePatterns.Values)
                returnValuePattern.ReportDiagnostics(context, reportDiagnostic);
        }
    }
}
