// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILVerify;

namespace Internal.IL
{
    partial class ILImporter
    {
        private readonly struct ExceptionRegionRange
        {
            public readonly int StartOffset;
            public readonly int EndOffset; // Exclusive.

            public ExceptionRegionRange(int startOffset, int endOffset)
            {
                StartOffset = startOffset;
                EndOffset = endOffset;
            }

            public bool IsEmpty => StartOffset >= EndOffset;

            public bool Overlaps(ExceptionRegionRange other) =>
                !IsEmpty && !other.IsEmpty && StartOffset < other.EndOffset && other.StartOffset < EndOffset;

            public bool Contains(ExceptionRegionRange other) =>
                StartOffset <= other.StartOffset && other.EndOffset <= EndOffset;

            public bool HasSameBounds(ExceptionRegionRange other) =>
                StartOffset == other.StartOffset && EndOffset == other.EndOffset;
        }

        private readonly struct ExceptionClauseLayout
        {
            public readonly ILExceptionRegionKind Kind;
            public readonly ExceptionRegionRange Try;
            public readonly ExceptionRegionRange Handler;
            public readonly ExceptionRegionRange Filter;

            public ExceptionClauseLayout(ILExceptionRegion region)
            {
                Kind = region.Kind;
                Try = new ExceptionRegionRange(region.TryOffset, region.TryOffset + region.TryLength);
                Handler = new ExceptionRegionRange(region.HandlerOffset, region.HandlerOffset + region.HandlerLength);
                Filter = Kind == ILExceptionRegionKind.Filter ?
                    new ExceptionRegionRange(region.FilterOffset, region.HandlerOffset) : default;
            }

            public bool HasFilter => Kind == ILExceptionRegionKind.Filter;

            public bool CanShareTry => Kind is ILExceptionRegionKind.Catch or ILExceptionRegionKind.Filter;

            // A filter immediately precedes its handler, so their union is one interval.
            public ExceptionRegionRange HandlerAndFilter =>
                new ExceptionRegionRange(HasFilter ? Filter.StartOffset : Handler.StartOffset, Handler.EndOffset);

            public bool IsDisjointFrom(ExceptionClauseLayout other) =>
                !Try.Overlaps(other.Try) && !Try.Overlaps(other.HandlerAndFilter) &&
                !HandlerAndFilter.Overlaps(other.Try) && !HandlerAndFilter.Overlaps(other.HandlerAndFilter);

            public bool IsContainedIn(ExceptionRegionRange region) =>
                region.Contains(Try) && region.Contains(Handler) && (!HasFilter || region.Contains(Filter));
        }

        private void VerifyExceptionRegions()
        {
            if (_exceptionRegions.Length == 0)
                return;

            var clauses = new ExceptionClauseLayout?[_exceptionRegions.Length];
            for (int i = 0; i < _exceptionRegions.Length; i++)
            {
                var clause = new ExceptionClauseLayout(_exceptionRegions[i].ILRegion);
                VerifyExceptionRegionRange(clause.Try);
                VerifyExceptionRegionRange(clause.Handler);
                if (clause.HasFilter)
                {
                    VerifyExceptionRegionRange(clause.Filter);
                    VerifyExceptionFilterEnd(clause.Filter);
                }

                // Empty or internally overlapping clauses cannot participate in containment.
                // Their diagnostics are independent of the remaining clauses and IL checks.
                if (clause.Try.IsEmpty || clause.Handler.IsEmpty)
                    continue;

                _currentInstructionOffset = clause.Try.StartOffset;
                if (!Check(!clause.Try.Overlaps(clause.HandlerAndFilter), VerifierError.EHClauseOverlap))
                    continue;

                clauses[i] = clause;
                // I.12.4.2.7 defines layout in terms of pairs of complete clauses.
                // Compare each pair independently, including unreachable clauses.
                for (int j = 0; j < i; j++)
                {
                    if (clauses[j] is ExceptionClauseLayout earlier)
                        VerifyExceptionRegionPair(earlier, clause);
                }
            }
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

        private void VerifyExceptionFilterEnd(ExceptionRegionRange filter)
        {
            _currentInstructionOffset = filter.StartOffset;
            // I.12.4.2.7 and III.3.34 require exactly one endfilter, lexically last.
            int lastInstruction = filter.EndOffset - 2;
            if (!Check(lastInstruction >= filter.StartOffset && _validTargetOffsets[lastInstruction] &&
                GetOpcodeAt(lastInstruction) == ILOpcode.endfilter, VerifierError.EHClauseFilterEnd))
                return;

            for (int offset = filter.StartOffset; offset < lastInstruction; offset++)
            {
                if (!Check(!_instructionBoundaries[offset] || GetOpcodeAt(offset) != ILOpcode.endfilter,
                    VerifierError.EHClauseFilterEnd))
                    break;
            }
        }

        // Arguments follow their original order in the EH table.
        private void VerifyExceptionRegionPair(ExceptionClauseLayout earlier, ExceptionClauseLayout later)
        {
            _currentInstructionOffset = later.Try.StartOffset;

            // II.19: no two filter or handler entries may have the same address,
            // even when one region is otherwise correctly nested in the other.
            bool sameEntry = earlier.Handler.StartOffset == later.Handler.StartOffset ||
                (earlier.HasFilter && earlier.Filter.StartOffset == later.Handler.StartOffset) ||
                (later.HasFilter && earlier.Handler.StartOffset == later.Filter.StartOffset) ||
                (earlier.HasFilter && later.HasFilter && earlier.Filter.StartOffset == later.Filter.StartOffset);
            if (!Check(!sameEntry, VerifierError.EHClauseOverlap))
                return;

            if (earlier.Try.HasSameBounds(later.Try))
            {
                Check(earlier.CanShareTry && later.CanShareTry &&
                    !earlier.HandlerAndFilter.Overlaps(later.HandlerAndFilter), VerifierError.EHClauseOverlap);
                return;
            }

            if (earlier.IsDisjointFrom(later))
                return;

            // A complete clause must fit inside a single non-filter region of the other.
            if (earlier.IsContainedIn(later.Try) ||
                earlier.IsContainedIn(later.Handler) || later.IsContainedIn(earlier.Handler))
                return;

            if (later.IsContainedIn(earlier.Try))
            {
                // I.12.4.2.5: inner try clauses must precede their enclosing try clause.
                VerificationError(VerifierError.EHClauseOrder);
                return;
            }

            // Distinguish crossing or shared regions from inconsistent clause nesting.
            ReadOnlySpan<ExceptionRegionRange> earlierRegions = [earlier.Try, earlier.Handler, earlier.Filter];
            ReadOnlySpan<ExceptionRegionRange> laterRegions = [later.Try, later.Handler, later.Filter];
            foreach (ExceptionRegionRange first in earlierRegions)
            {
                foreach (ExceptionRegionRange second in laterRegions)
                {
                    if (first.Overlaps(second) && (first.HasSameBounds(second) ||
                        (!first.Contains(second) && !second.Contains(first))))
                    {
                        _currentInstructionOffset = second.StartOffset;
                        VerificationError(VerifierError.EHClauseOverlap);
                        return;
                    }
                }
            }

            VerificationError(VerifierError.EHClauseNesting);
        }
    }
}
