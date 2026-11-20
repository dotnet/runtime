// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILVerify;

namespace Internal.IL
{
    partial class ILImporter
    {
        private struct ExceptionRegionRange
        {
            public readonly int StartOffset;
            public readonly int EndOffset; // Exclusive
            public readonly ExceptionRegionKind Kind;
            public readonly int ClauseIndex;
            public int ParentIndex;
            public int EnclosingTryIndex;

            public ExceptionRegionRange(int startOffset, int endOffset, ExceptionRegionKind kind, int clauseIndex)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
                Kind = kind;
                ClauseIndex = clauseIndex;
                ParentIndex = -1;
                EnclosingTryIndex = int.MaxValue;
            }

            public bool IsEmpty => StartOffset >= EndOffset;

            public bool Overlaps(ExceptionRegionRange other) =>
                !IsEmpty && !other.IsEmpty && StartOffset < other.EndOffset && other.StartOffset < EndOffset;

            public bool HasSameBounds(ExceptionRegionRange other) =>
                StartOffset == other.StartOffset && EndOffset == other.EndOffset;
        }

        private enum ExceptionRegionKind
        {
            Try,
            Handler,
            Filter
        }

        private struct ExceptionClauseLayout
        {
            public readonly ILExceptionRegionKind Kind;
            public int TryIndex;
            public int HandlerIndex;
            public int FilterIndex;
            public bool HasOverlap;

            public ExceptionClauseLayout(ILExceptionRegionKind kind)
            {
                Kind = kind;
                TryIndex = -1;
                HandlerIndex = -1;
                FilterIndex = -1;
                HasOverlap = false;
            }

            public bool HasFilter => Kind == ILExceptionRegionKind.Filter;

            public bool CanShareTry => Kind is ILExceptionRegionKind.Catch or ILExceptionRegionKind.Filter;
        }

        private void VerifyExceptionRegions()
        {
            if (_exceptionRegions.Length == 0)
                return;

            var clauses = new ExceptionClauseLayout[_exceptionRegions.Length];
            var regions = new List<ExceptionRegionRange>(_exceptionRegions.Length);
            List<int> endfilterOffsets = null;
            for (int i = 0; i < _exceptionRegions.Length; i++)
            {
                ILExceptionRegion region = _exceptionRegions[i].ILRegion;
                var clause = new ExceptionClauseLayout(region.Kind);
                clauses[i] = clause;
                var tryRegion = new ExceptionRegionRange(region.TryOffset, region.TryOffset + region.TryLength, ExceptionRegionKind.Try, i);
                var handler = new ExceptionRegionRange(region.HandlerOffset, region.HandlerOffset + region.HandlerLength, ExceptionRegionKind.Handler, i);
                var filter = clause.HasFilter ?
                    new ExceptionRegionRange(region.FilterOffset, region.HandlerOffset, ExceptionRegionKind.Filter, i) : default;
                VerifyExceptionRegionRange(tryRegion);
                VerifyExceptionRegionRange(handler);
                if (clause.HasFilter)
                {
                    VerifyExceptionRegionRange(filter);
                    endfilterOffsets ??= FindEndfilterOffsets();
                    VerifyExceptionFilterEnd(filter, endfilterOffsets);
                }

                if (tryRegion.IsEmpty || handler.IsEmpty)
                    continue;

                _currentInstructionOffset = tryRegion.StartOffset;
                if (!Check(!tryRegion.Overlaps(handler) && (!clause.HasFilter || !tryRegion.Overlaps(filter)),
                    VerifierError.EHClauseOverlap))
                    continue;

                regions.Add(tryRegion);
                regions.Add(handler);
                if (clause.HasFilter)
                    regions.Add(filter);
            }

            FindExceptionRegionParents(clauses, regions);
            VerifyExceptionClauseRelations(clauses, regions);
        }

        private void VerifyExceptionRegionRange(ExceptionRegionRange region)
        {
            _currentInstructionOffset = region.StartOffset;
            if (!Check(!region.IsEmpty, VerifierError.EHClauseEmpty))
                return;

            // FindEHTargets has already checked the bounds against the method size.
            Check(_validTargetOffsets[region.StartOffset] &&
                (region.EndOffset == _ilBytes.Length || _validTargetOffsets[region.EndOffset]),
                VerifierError.EHClauseBoundary);
        }

        private List<int> FindEndfilterOffsets()
        {
            var offsets = new List<int>();
            for (int offset = 0; offset < _ilBytes.Length; offset++)
            {
                if (_instructionBoundaries[offset] && GetOpcodeAt(offset) == ILOpcode.endfilter)
                    offsets.Add(offset);
            }
            return offsets;
        }

        private void VerifyExceptionFilterEnd(ExceptionRegionRange filter, List<int> endfilterOffsets)
        {
            _currentInstructionOffset = filter.StartOffset;
            // I.12.4.2.7 and III.3.34 require exactly one endfilter, lexically last.
            int lastInstruction = filter.EndOffset - 2;
            if (!Check(lastInstruction >= filter.StartOffset && _validTargetOffsets[lastInstruction] &&
                GetOpcodeAt(lastInstruction) == ILOpcode.endfilter, VerifierError.EHClauseFilterEnd))
                return;

            int firstEndfilter = endfilterOffsets.BinarySearch(filter.StartOffset);
            if (firstEndfilter < 0)
                firstEndfilter = ~firstEndfilter;
            Check(firstEndfilter < endfilterOffsets.Count && endfilterOffsets[firstEndfilter] == lastInstruction,
                VerifierError.EHClauseFilterEnd);
        }

        private void FindExceptionRegionParents(ExceptionClauseLayout[] clauses, List<ExceptionRegionRange> regions)
        {
            regions.Sort(static (first, second) =>
            {
                int result = first.StartOffset.CompareTo(second.StartOffset);
                if (result == 0)
                    result = second.EndOffset.CompareTo(first.EndOffset);
                if (result == 0)
                    result = ((int)first.Kind).CompareTo((int)second.Kind);
                if (result == 0)
                    result = first.ClauseIndex.CompareTo(second.ClauseIndex);
                return result;
            });

            var stack = new Stack<int>();
            int previousEntry = -1;
            for (int i = 0; i < regions.Count; i++)
            {
                ExceptionRegionRange region = regions[i];
                switch (region.Kind)
                {
                    case ExceptionRegionKind.Try:
                        clauses[region.ClauseIndex].TryIndex = i;
                        break;
                    case ExceptionRegionKind.Handler:
                        clauses[region.ClauseIndex].HandlerIndex = i;
                        break;
                    case ExceptionRegionKind.Filter:
                        clauses[region.ClauseIndex].FilterIndex = i;
                        break;
                }

                // II.19 forbids shared filter/handler entries even if an intervening
                // try makes the two entry regions non-adjacent in the nesting tree.
                if (region.Kind != ExceptionRegionKind.Try)
                {
                    if (previousEntry >= 0 && regions[previousEntry].StartOffset == region.StartOffset)
                        ReportExceptionRegionOverlap(clauses, region, regions[previousEntry]);
                    previousEntry = i;
                }

                while (stack.Count != 0 && region.StartOffset >= regions[stack.Peek()].EndOffset)
                    stack.Pop();

                while (stack.Count != 0 && region.EndOffset > regions[stack.Peek()].EndOffset)
                    ReportExceptionRegionOverlap(clauses, region, regions[stack.Pop()]);

                if (stack.Count != 0)
                {
                    int parentIndex = stack.Peek();
                    ExceptionRegionRange parent = regions[parentIndex];
                    if (region.HasSameBounds(parent))
                    {
                        if (region.Kind == ExceptionRegionKind.Try && parent.Kind == ExceptionRegionKind.Try)
                        {
                            if (!clauses[region.ClauseIndex].CanShareTry || !clauses[parent.ClauseIndex].CanShareTry)
                                ReportExceptionRegionOverlap(clauses, region, parent);
                            clauses[region.ClauseIndex].TryIndex = parentIndex;
                        }
                        else
                        {
                            ReportExceptionRegionOverlap(clauses, region, parent);
                        }
                        continue;
                    }

                    region.ParentIndex = parentIndex;
                    // Propagate the ordering constraint through handlers too, without
                    // walking every ancestor for deeply nested clauses (I.12.4.2.5).
                    region.EnclosingTryIndex = parent.EnclosingTryIndex;
                    if (parent.Kind == ExceptionRegionKind.Try)
                        region.EnclosingTryIndex = Math.Min(region.EnclosingTryIndex, parent.ClauseIndex);
                }
                regions[i] = region;
                stack.Push(i);
            }
        }

        private void ReportExceptionRegionOverlap(ExceptionClauseLayout[] clauses,
            ExceptionRegionRange region, ExceptionRegionRange other)
        {
            if (!clauses[region.ClauseIndex].HasOverlap)
            {
                _currentInstructionOffset = region.StartOffset;
                VerificationError(VerifierError.EHClauseOverlap);
            }
            clauses[region.ClauseIndex].HasOverlap = true;
            clauses[other.ClauseIndex].HasOverlap = true;
        }

        private void VerifyExceptionClauseRelations(ExceptionClauseLayout[] clauses, List<ExceptionRegionRange> regions)
        {
            for (int i = 0; i < clauses.Length; i++)
            {
                ExceptionClauseLayout clause = clauses[i];
                if (clause.TryIndex < 0 || clause.HasOverlap)
                    continue;

                _currentInstructionOffset = regions[clause.TryIndex].StartOffset;
                int parent = regions[clause.TryIndex].ParentIndex;
                int handlerParent = regions[clause.HandlerIndex].ParentIndex;
                int filterParent = clause.HasFilter ? regions[clause.FilterIndex].ParentIndex : -1;

                // I.12.4.2.7 requires all parts of a clause to be contained in the
                // same region. Sorting whole-clause spans would reject valid interleaving.
                if (parent != handlerParent || (clause.HasFilter && parent != filterParent))
                {
                    bool sharedTryOverlap =
                        (handlerParent >= 0 && clauses[regions[handlerParent].ClauseIndex].TryIndex == clause.TryIndex) ||
                        (filterParent >= 0 && clauses[regions[filterParent].ClauseIndex].TryIndex == clause.TryIndex);
                    VerificationError(sharedTryOverlap ? VerifierError.EHClauseOverlap : VerifierError.EHClauseNesting);
                    continue;
                }

                if (!Check(parent < 0 || regions[parent].Kind != ExceptionRegionKind.Filter, VerifierError.EHClauseNesting))
                    continue;

                Check(i < regions[clause.TryIndex].EnclosingTryIndex, VerifierError.EHClauseOrder);
            }
        }
    }
}
