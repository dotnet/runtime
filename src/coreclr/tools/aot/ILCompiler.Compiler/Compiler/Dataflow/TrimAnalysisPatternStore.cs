// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILCompiler.Logging;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;

#nullable enable

namespace ILCompiler.Dataflow
{
    public readonly struct TrimAnalysisPatternStore
    {
        readonly Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern> AssignmentPatterns;
        readonly Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> MethodCallPatterns;
        readonly ValueSetLattice<SingleValue> Lattice;
        readonly Logger _logger;

        public TrimAnalysisPatternStore(ValueSetLattice<SingleValue> lattice, Logger logger)
        {
            AssignmentPatterns = new Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern>();
            MethodCallPatterns = new Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern>();
            Lattice = lattice;
            _logger = logger;
        }

        public void Add(TrimAnalysisAssignmentPattern pattern)
        {
            // In the linker, each pattern should have a unique origin (which has ILOffset)
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

        public void MarkAndProduceDiagnostics(ReflectionMarker reflectionMarker)
        {
            foreach (var pattern in AssignmentPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);

            foreach (var pattern in MethodCallPatterns.Values)
                pattern.MarkAndProduceDiagnostics(reflectionMarker, _logger);
        }
    }
}
