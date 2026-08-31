// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal readonly struct IncrementalDependencyEntry
    {
        internal IncrementalDependencyEntry(object node, string reason, bool marked)
        {
            Node = node;
            Reason = reason;
            Marked = marked;
        }

        internal object Node { get; }
        internal string Reason { get; }
        internal bool Marked { get; }
    }

    internal readonly struct IncrementalConditionalDependencyEntry
    {
        internal IncrementalConditionalDependencyEntry(
            object node,
            object otherReasonNode,
            string reason,
            bool nodeMarked,
            bool otherReasonNodeMarked)
        {
            Node = node;
            OtherReasonNode = otherReasonNode;
            Reason = reason;
            NodeMarked = nodeMarked;
            OtherReasonNodeMarked = otherReasonNodeMarked;
        }

        internal object Node { get; }
        internal object OtherReasonNode { get; }
        internal string Reason { get; }
        internal bool NodeMarked { get; }
        internal bool OtherReasonNodeMarked { get; }
    }

    internal static class IncrementalDependencyValidator
    {
        internal static bool Matches(
            IReadOnlyList<IncrementalDependencyEntry> baselineStatic,
            IReadOnlyList<IncrementalConditionalDependencyEntry> baselineConditional,
            IReadOnlyList<IncrementalDependencyEntry> currentStatic,
            IReadOnlyList<IncrementalConditionalDependencyEntry> currentConditional,
            out string reason)
        {
            for (int i = 0; i < currentStatic.Count; i++)
            {
                IncrementalDependencyEntry current = currentStatic[i];
                if (!current.Marked)
                {
                    reason = $"unmarked-static-dependency:{i}";
                    return false;
                }
                if (i >= baselineStatic.Count)
                {
                    reason = $"static-dependency:{i}";
                    return false;
                }

                IncrementalDependencyEntry baseline = baselineStatic[i];
                if (!ReferenceEquals(baseline.Node, current.Node) ||
                    !string.Equals(baseline.Reason, current.Reason, StringComparison.Ordinal))
                {
                    reason = $"static-dependency:{i}";
                    return false;
                }
            }
            if (baselineStatic.Count != currentStatic.Count)
            {
                reason = $"static-dependency-count:{baselineStatic.Count}:{currentStatic.Count}";
                return false;
            }

            for (int i = 0; i < currentConditional.Count; i++)
            {
                IncrementalConditionalDependencyEntry current = currentConditional[i];
                if (!current.NodeMarked || !current.OtherReasonNodeMarked)
                {
                    reason = $"unmarked-conditional-dependency:{i}";
                    return false;
                }
                if (i >= baselineConditional.Count)
                {
                    reason = $"conditional-dependency:{i}";
                    return false;
                }

                IncrementalConditionalDependencyEntry baseline = baselineConditional[i];
                if (!ReferenceEquals(baseline.Node, current.Node) ||
                    !ReferenceEquals(baseline.OtherReasonNode, current.OtherReasonNode) ||
                    !string.Equals(baseline.Reason, current.Reason, StringComparison.Ordinal))
                {
                    reason = $"conditional-dependency:{i}";
                    return false;
                }
            }
            if (baselineConditional.Count != currentConditional.Count)
            {
                reason =
                    $"conditional-dependency-count:{baselineConditional.Count}:{currentConditional.Count}";
                return false;
            }

            reason = null;
            return true;
        }
    }

    internal readonly struct IncrementalCodeState
    {
        internal IncrementalCodeState(MethodCodeNode node)
            : this(
                node.FrameInfos,
                node.GCInfo,
                node.EHInfo is not null,
                node.DebugLocInfos,
                node.DebugVarInfos,
                node.DebugEHClauseInfos,
                node.DebugInfoForIncrementalCompilation,
                node.LocalTypesForIncrementalCompilation)
        {
        }

        internal IncrementalCodeState(
            FrameInfo[] frameInfos,
            byte[] gcInfo,
            bool hasEhInfo,
            DebugLocInfo[] debugLocInfos,
            DebugVarInfo[] debugVarInfos,
            DebugEHClauseInfo[] debugEhClauseInfos,
            MethodDebugInformation debugInfo,
            TypeDesc[] localTypes)
        {
            FrameInfos = frameInfos;
            GcInfo = gcInfo;
            HasEhInfo = hasEhInfo;
            DebugLocInfos = debugLocInfos;
            DebugVarInfos = debugVarInfos;
            DebugEhClauseInfos = debugEhClauseInfos;
            DebugInfo = debugInfo;
            LocalTypes = localTypes;
        }

        internal FrameInfo[] FrameInfos { get; }
        internal byte[] GcInfo { get; }
        internal bool HasEhInfo { get; }
        internal DebugLocInfo[] DebugLocInfos { get; }
        internal DebugVarInfo[] DebugVarInfos { get; }
        internal DebugEHClauseInfo[] DebugEhClauseInfos { get; }
        internal MethodDebugInformation DebugInfo { get; }
        internal TypeDesc[] LocalTypes { get; }
    }

    internal static class IncrementalCodeStateValidator
    {
        internal static bool Matches(
            in IncrementalCodeState baseline,
            MethodCodeNode current,
            out string reason) =>
            Matches(baseline, new IncrementalCodeState(current), out reason);

        internal static bool Matches(
            in IncrementalCodeState baseline,
            in IncrementalCodeState current,
            out string reason)
        {
            if (baseline.HasEhInfo || current.HasEhInfo)
            {
                reason = "exception-handling-info-present";
                return false;
            }
            if (!((ReadOnlySpan<byte>)baseline.GcInfo).SequenceEqual(current.GcInfo))
            {
                reason = "gc-info";
                return false;
            }

            FrameInfo[] currentFrames = current.FrameInfos;
            int oldFrameCount = baseline.FrameInfos?.Length ?? 0;
            int newFrameCount = currentFrames?.Length ?? 0;
            if (oldFrameCount != newFrameCount)
            {
                reason = "frame-count";
                return false;
            }
            for (int i = 0; i < oldFrameCount; i++)
            {
                if (!baseline.FrameInfos[i].Equals(currentFrames[i]))
                {
                    reason = $"frame:{i}";
                    return false;
                }
            }

            if (!DebugLocationsMatch(baseline.DebugLocInfos, current.DebugLocInfos) ||
                !DebugVariablesMatch(baseline.DebugVarInfos, current.DebugVarInfos) ||
                !DebugEhClausesMatch(baseline.DebugEhClauseInfos, current.DebugEhClauseInfos) ||
                !ReferenceEquals(baseline.DebugInfo, current.DebugInfo) ||
                !TypesMatch(baseline.LocalTypes, current.LocalTypes))
            {
                reason = "debug-info";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool DebugLocationsMatch(DebugLocInfo[] left, DebugLocInfo[] right)
        {
            int count = left?.Length ?? 0;
            if (count != (right?.Length ?? 0))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (left[i].NativeOffset != right[i].NativeOffset ||
                    left[i].ILOffset != right[i].ILOffset)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool DebugVariablesMatch(DebugVarInfo[] left, DebugVarInfo[] right)
        {
            int count = left?.Length ?? 0;
            if (count != (right?.Length ?? 0))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (left[i].VarNumber != right[i].VarNumber)
                    return false;

                DebugVarRangeInfo[] leftRanges = left[i].Ranges;
                DebugVarRangeInfo[] rightRanges = right[i].Ranges;
                int rangeCount = leftRanges?.Length ?? 0;
                if (rangeCount != (rightRanges?.Length ?? 0))
                    return false;

                for (int j = 0; j < rangeCount; j++)
                {
                    DebugVarRangeInfo leftRange = leftRanges[j];
                    DebugVarRangeInfo rightRange = rightRanges[j];
                    VarLoc leftLocation = leftRange.VarLoc;
                    VarLoc rightLocation = rightRange.VarLoc;
                    if (leftRange.StartOffset != rightRange.StartOffset ||
                        leftRange.EndOffset != rightRange.EndOffset ||
                        leftLocation.A != rightLocation.A ||
                        leftLocation.B != rightLocation.B ||
                        leftLocation.C != rightLocation.C ||
                        leftLocation.D != rightLocation.D)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool DebugEhClausesMatch(DebugEHClauseInfo[] left, DebugEHClauseInfo[] right)
        {
            int count = left?.Length ?? 0;
            if (count != (right?.Length ?? 0))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (left[i].TryOffset != right[i].TryOffset ||
                    left[i].TryLength != right[i].TryLength ||
                    left[i].HandlerOffset != right[i].HandlerOffset ||
                    left[i].HandlerLength != right[i].HandlerLength)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TypesMatch(TypeDesc[] left, TypeDesc[] right)
        {
            int count = left?.Length ?? 0;
            if (count != (right?.Length ?? 0))
                return false;

            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(left[i], right[i]))
                    return false;
            }

            return true;
        }
    }
}
