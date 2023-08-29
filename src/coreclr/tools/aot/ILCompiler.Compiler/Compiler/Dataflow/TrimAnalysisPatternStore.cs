// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.Logging;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Internal.TypeSystem;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly struct TrimAnalysisPatternStore
    {
        private readonly Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern> AssignmentPatterns;
        private readonly Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> MethodCallPatterns;
        private readonly Dictionary<(MessageOrigin, TypeSystemEntity), TrimAnalysisTokenAccessPattern> TokenAccessPatterns;
        private readonly Dictionary<(MessageOrigin, TypeSystemEntity), TrimAnalysisGenericInstantiationAccessPattern> GenericInstantiations;
        private readonly Dictionary<(MessageOrigin, FieldDesc), TrimAnalysisFieldAccessPattern> FieldAccessPatterns;
        private readonly ValueSetLattice<SingleValue> Lattice;
        private readonly Logger _logger;

        public TrimAnalysisPatternStore(ValueSetLattice<SingleValue> lattice, Logger logger)
        {
            AssignmentPatterns = new Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern>();
            MethodCallPatterns = new Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern>();
            TokenAccessPatterns = new Dictionary<(MessageOrigin, TypeSystemEntity), TrimAnalysisTokenAccessPattern>();
            GenericInstantiations = new Dictionary<(MessageOrigin, TypeSystemEntity), TrimAnalysisGenericInstantiationAccessPattern>();
            FieldAccessPatterns = new Dictionary<(MessageOrigin, FieldDesc), TrimAnalysisFieldAccessPattern>();
            Lattice = lattice;
            _logger = logger;
        }

        public void Add(TrimAnalysisAssignmentPattern pattern)
        {
            // While trimming, each pattern should have a unique origin (which has ILOffset)
            // but we don't track the correct ILOffset for return instructions.
            // https://github.com/dotnet/linker/issues/2778
            // For now, work around it with a separate bit.
            bool isReturnValue = pattern.Target.AsSingleValue() is MethodReturnValue;

            if (!AssignmentPatterns.TryGetValue((pattern.Origin, isReturnValue), out var existingPattern))
            {
                AssignmentPatterns.Add((pattern.Origin, isReturnValue), pattern);
                return;
            }

            AssignmentPatterns[(pattern.Origin, isReturnValue)] = pattern.Merge(Lattice, existingPattern);
        }

        public void Add(TrimAnalysisMethodCallPattern pattern)
        {
            if (!MethodCallPatterns.TryGetValue(pattern.Origin, out var existingPattern))
            {
                MethodCallPatterns.Add(pattern.Origin, pattern);
                return;
            }

            MethodCallPatterns[pattern.Origin] = pattern.Merge(Lattice, existingPattern);
        }

        public void Add(TrimAnalysisTokenAccessPattern pattern)
        {
            TokenAccessPatterns.TryAdd((pattern.Origin, pattern.Entity), pattern);

            // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
            // and there's only one way to "access" a generic instantiation.
        }

        public void Add(TrimAnalysisGenericInstantiationAccessPattern pattern)
        {
            GenericInstantiations.TryAdd((pattern.Origin, pattern.Entity), pattern);

            // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
            // and there's only one way to "access" a generic instantiation.
        }

        public void Add(TrimAnalysisFieldAccessPattern pattern)
        {
            FieldAccessPatterns.TryAdd((pattern.Origin, pattern.Field), pattern);

            // No Merge - there's nothing to merge since this pattern is uniquely identified by both the origin and the entity
            // and there's only one way to "access" a field.
        }

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker)
        {
            foreach (var pattern in AssignmentPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);

            foreach (var pattern in MethodCallPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);

            foreach (var pattern in TokenAccessPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);

            foreach (var pattern in GenericInstantiations.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);

            foreach (var pattern in FieldAccessPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);
        }
    }
}
