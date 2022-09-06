// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public ref partial struct SequenceReader<T> where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="span">The read data, if any.</param>
        /// <param name="delimiter">The delimiter to look for.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = remaining.IndexOf(delimiter);

            if (index != -1)
            {
                span = index == 0 ? default : remaining.Slice(0, index);
                AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
                return true;
            }

            return TryReadToSlow(out span, delimiter, advancePastDelimiter);
        }

        private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter)
        {
            if (!TryReadToInternal(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
            {
                span = default;
                return false;
            }

            span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
            return true;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiter"/>, ignoring delimiters that are
        /// preceded by <paramref name="delimiterEscape"/>.
        /// </summary>
        /// <param name="span">The read data, if any.</param>
        /// <param name="delimiter">The delimiter to look for.</param>
        /// <param name="delimiterEscape">If found prior to <paramref name="delimiter"/> it will skip that occurrence.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = remaining.IndexOf(delimiter);

            if ((index > 0 && !remaining[index - 1].Equals(delimiterEscape)) || index == 0)
            {
                span = remaining.Slice(0, index);
                AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
                return true;
            }

            // This delimiter might be skipped, go down the slow path
            return TryReadToSlow(out span, delimiter, delimiterEscape, index, advancePastDelimiter);
        }

        private bool TryReadToSlow(out ReadOnlySpan<T> span, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
        {
            if (!TryReadToSlow(out ReadOnlySequence<T> sequence, delimiter, delimiterEscape, index, advancePastDelimiter))
            {
                span = default;
                return false;
            }

            Debug.Assert(sequence.Length > 0);
            span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
            return true;
        }

        private bool TryReadToSlow(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, int index, bool advancePastDelimiter)
        {
            SequenceReader<T> copy = this;

            ReadOnlySpan<T> remaining = UnreadSpan;
            bool priorEscape = false;

            do
            {
                if (index >= 0)
                {
                    if (index == 0 && priorEscape)
                    {
                        // We were in the escaped state, so skip this delimiter
                        priorEscape = false;
                        Advance(index + 1);
                        remaining = UnreadSpan;
                        goto Continue;
                    }
                    else if (index > 0 && remaining[index - 1].Equals(delimiterEscape))
                    {
                        // This delimiter might be skipped

                        // Count our escapes
                        int escapeCount = 1;
                        int i = index - 2;
                        for (; i >= 0; i--)
                        {
                            if (!remaining[i].Equals(delimiterEscape))
                                break;
                        }
                        if (i < 0 && priorEscape)
                        {
                            // Started and ended with escape, increment once more
                            escapeCount++;
                        }
                        escapeCount += index - 2 - i;

                        if ((escapeCount & 1) != 0)
                        {
                            // An odd escape count means we're currently escaped,
                            // skip the delimiter and reset escaped state.
                            Advance(index + 1);
                            priorEscape = false;
                            remaining = UnreadSpan;
                            goto Continue;
                        }
                    }

                    // Found the delimiter. Move to it, slice, then move past it.
                    AdvanceCurrentSpan(index);

                    sequence = Sequence.Slice(copy.Position, Position);
                    if (advancePastDelimiter)
                    {
                        Advance(1);
                    }
                    return true;
                }
                else
                {
                    // No delimiter, need to check the end of the span for odd number of escapes then advance
                    if (remaining.Length > 0 && remaining[remaining.Length - 1].Equals(delimiterEscape))
                    {
                        int escapeCount = 1;
                        int i = remaining.Length - 2;
                        for (; i >= 0; i--)
                        {
                            if (!remaining[i].Equals(delimiterEscape))
                                break;
                        }

                        escapeCount += remaining.Length - 2 - i;
                        if (i < 0 && priorEscape)
                            priorEscape = (escapeCount & 1) == 0;   // equivalent to incrementing escapeCount before setting priorEscape
                        else
                            priorEscape = (escapeCount & 1) != 0;
                    }
                    else
                    {
                        priorEscape = false;
                    }
                }

                // Nothing in the current span, move to the end, checking for the skip delimiter
                AdvanceCurrentSpan(remaining.Length);
                remaining = CurrentSpan;

            Continue:
                index = remaining.IndexOf(delimiter);
            } while (!End);

            // Didn't find anything, reset our original state.
            this = copy;
            sequence = default;
            return false;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="sequence">The read data, if any.</param>
        /// <param name="delimiter">The delimiter to look for.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter = true)
        {
            return TryReadToInternal(out sequence, delimiter, advancePastDelimiter);
        }

        private bool TryReadToInternal(out ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter, int skip = 0)
        {
            Debug.Assert(skip >= 0);
            SequenceReader<T> copy = this;
            if (skip > 0)
                Advance(skip);
            ReadOnlySpan<T> remaining = UnreadSpan;

            while (_moreData)
            {
                int index = remaining.IndexOf(delimiter);
                if (index != -1)
                {
                    // Found the delimiter. Move to it, slice, then move past it.
                    if (index > 0)
                    {
                        AdvanceCurrentSpan(index);
                    }

                    sequence = Sequence.Slice(copy.Position, Position);
                    if (advancePastDelimiter)
                    {
                        Advance(1);
                    }
                    return true;
                }

                AdvanceCurrentSpan(remaining.Length);
                remaining = CurrentSpan;
            }

            // Didn't find anything, reset our original state.
            this = copy;
            sequence = default;
            return false;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiter"/>, ignoring delimiters that are
        /// preceded by <paramref name="delimiterEscape"/>.
        /// </summary>
        /// <param name="sequence">The read data, if any.</param>
        /// <param name="delimiter">The delimiter to look for.</param>
        /// <param name="delimiterEscape">If found prior to <paramref name="delimiter"/> it will skip that occurrence.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, bool advancePastDelimiter = true)
        {
            SequenceReader<T> copy = this;

            ReadOnlySpan<T> remaining = UnreadSpan;
            bool priorEscape = false;

            while (_moreData)
            {
                int index = remaining.IndexOf(delimiter);
                if (index != -1)
                {
                    if (index == 0 && priorEscape)
                    {
                        // We were in the escaped state, so skip this delimiter
                        priorEscape = false;
                        Advance(index + 1);
                        remaining = UnreadSpan;
                        continue;
                    }
                    else if (index > 0 && remaining[index - 1].Equals(delimiterEscape))
                    {
                        // This delimiter might be skipped

                        // Count our escapes
                        int escapeCount = 0;
                        for (int i = index; i > 0 && remaining[i - 1].Equals(delimiterEscape); i--, escapeCount++)
                            ;
                        if (escapeCount == index && priorEscape)
                        {
                            // Started and ended with escape, increment once more
                            escapeCount++;
                        }

                        priorEscape = false;
                        if ((escapeCount & 1) != 0)
                        {
                            // Odd escape count means we're in the escaped state, so skip this delimiter
                            Advance(index + 1);
                            remaining = UnreadSpan;
                            continue;
                        }
                    }

                    // Found the delimiter. Move to it, slice, then move past it.
                    if (index > 0)
                    {
                        Advance(index);
                    }

                    sequence = Sequence.Slice(copy.Position, Position);
                    if (advancePastDelimiter)
                    {
                        Advance(1);
                    }
                    return true;
                }

                // No delimiter, need to check the end of the span for odd number of escapes then advance
                {
                    int escapeCount = 0;
                    for (int i = remaining.Length; i > 0 && remaining[i - 1].Equals(delimiterEscape); i--, escapeCount++)
                        ;
                    if (priorEscape && escapeCount == remaining.Length)
                    {
                        escapeCount++;
                    }
                    priorEscape = escapeCount % 2 != 0;
                }

                // Nothing in the current span, move to the end, checking for the skip delimiter
                Advance(remaining.Length);
                remaining = CurrentSpan;
            }

            // Didn't find anything, reset our original state.
            this = copy;
            sequence = default;
            return false;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiters"/>.
        /// </summary>
        /// <param name="span">The read data, if any.</param>
        /// <param name="delimiters">The delimiters to look for.</param>
        /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
        /// <returns>True if any of the <paramref name="delimiters"/> were found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadToAny(out ReadOnlySpan<T> span, scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = delimiters.Length == 2
                ? remaining.IndexOfAny(delimiters[0], delimiters[1])
                : remaining.IndexOfAny(delimiters);

            if (index != -1)
            {
                span = remaining.Slice(0, index);
                Advance(index + (advancePastDelimiter ? 1 : 0));
                return true;
            }

            return TryReadToAnySlow(out span, delimiters, advancePastDelimiter);
        }

        private bool TryReadToAnySlow(out ReadOnlySpan<T> span, scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter)
        {
            if (!TryReadToAnyInternal(out ReadOnlySequence<T> sequence, delimiters, advancePastDelimiter, CurrentSpan.Length - CurrentSpanIndex))
            {
                span = default;
                return false;
            }

            span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
            return true;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiters"/>.
        /// </summary>
        /// <param name="sequence">The read data, if any.</param>
        /// <param name="delimiters">The delimiters to look for.</param>
        /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
        /// <returns>True if any of the <paramref name="delimiters"/> were found.</returns>
        public bool TryReadToAny(out ReadOnlySequence<T> sequence, scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
        {
            return TryReadToAnyInternal(out sequence, delimiters, advancePastDelimiter);
        }

        private bool TryReadToAnyInternal(out ReadOnlySequence<T> sequence, scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter, int skip = 0)
        {
            SequenceReader<T> copy = this;
            if (skip > 0)
                Advance(skip);
            ReadOnlySpan<T> remaining = UnreadSpan;

            while (!End)
            {
                int index = delimiters.Length == 2
                    ? remaining.IndexOfAny(delimiters[0], delimiters[1])
                    : remaining.IndexOfAny(delimiters);

                if (index != -1)
                {
                    // Found one of the delimiters. Move to it, slice, then move past it.
                    if (index > 0)
                    {
                        AdvanceCurrentSpan(index);
                    }

                    sequence = Sequence.Slice(copy.Position, Position);
                    if (advancePastDelimiter)
                    {
                        Advance(1);
                    }
                    return true;
                }

                Advance(remaining.Length);
                remaining = CurrentSpan;
            }

            // Didn't find anything, reset our original state.
            this = copy;
            sequence = default;
            return false;
        }

        /// <summary>
        /// Try to read everything up to the given <paramref name="delimiter"/>.
        /// </summary>
        /// <param name="span">The read data, if any.</param>
        /// <param name="delimiter">The delimiter to look for.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySpan<T> span, scoped ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = remaining.IndexOf(delimiter);

            if (index >= 0)
            {
                span = remaining.Slice(0, index);
                AdvanceCurrentSpan(index + (advancePastDelimiter ? delimiter.Length : 0));
                return true;
            }

            // This delimiter might be skipped, go down the slow path
            return TryReadToSlow(out span, delimiter, advancePastDelimiter);
        }

        private bool TryReadToSlow(out ReadOnlySpan<T> span, scoped ReadOnlySpan<T> delimiter, bool advancePastDelimiter)
        {
            if (!TryReadTo(out ReadOnlySequence<T> sequence, delimiter, advancePastDelimiter))
            {
                span = default;
                return false;
            }

            Debug.Assert(sequence.Length > 0);
            span = sequence.IsSingleSegment ? sequence.First.Span : sequence.ToArray();
            return true;
        }

        /// <summary>
        /// Try to read data until the entire given <paramref name="delimiter"/> matches.
        /// </summary>
        /// <param name="sequence">The read data, if any.</param>
        /// <param name="delimiter">The multi (T) delimiter.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the <paramref name="delimiter"/> was found.</returns>
        public bool TryReadTo(out ReadOnlySequence<T> sequence, scoped ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true)
        {
            if (delimiter.Length == 0)
            {
                sequence = default;
                return true;
            }

            SequenceReader<T> copy = this;

            bool advanced = false;
            while (!End)
            {
                if (!TryReadTo(out sequence, delimiter[0], advancePastDelimiter: false))
                {
                    this = copy;
                    return false;
                }

                if (delimiter.Length == 1)
                {
                    if (advancePastDelimiter)
                    {
                        Advance(1);
                    }
                    return true;
                }

                if (IsNext(delimiter))
                {
                    // Probably a faster way to do this, potentially by avoiding the Advance in the previous TryReadTo call
                    if (advanced)
                    {
                        sequence = copy.Sequence.Slice(copy.Consumed, Consumed - copy.Consumed);
                    }

                    if (advancePastDelimiter)
                    {
                        Advance(delimiter.Length);
                    }
                    return true;
                }
                else
                {
                    Advance(1);
                    advanced = true;
                }
            }

            this = copy;
            sequence = default;
            return false;
        }

        /// <summary>
        /// Try to read data with given <paramref name="count"/>.
        /// </summary>
        /// <param name="count">Read count.</param>
        /// <param name="sequence">The read data, if successfully read requested <paramref name="count"/> data.</param>
        /// <returns><c>true</c> if remaining items in current <see cref="SequenceReader{T}" /> is enough for <paramref name="count"/>.</returns>
        public bool TryReadExact(int count, out ReadOnlySequence<T> sequence)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }
            if (count > Remaining)
            {
                sequence = default;
                return false;
            }

            sequence = Sequence.Slice(Position, count);
            if (count != 0)
            {
                Advance(count);
            }
            return true;
        }

        /// <summary>
        /// Advance until the given <paramref name="delimiter"/>, if found.
        /// </summary>
        /// <param name="delimiter">The delimiter to search for.</param>
        /// <param name="advancePastDelimiter">True to move past the <paramref name="delimiter"/> if found.</param>
        /// <returns>True if the given <paramref name="delimiter"/> was found.</returns>
        public bool TryAdvanceTo(T delimiter, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = remaining.IndexOf(delimiter);
            if (index != -1)
            {
                Advance(advancePastDelimiter ? index + 1 : index);
                return true;
            }

            return TryReadToInternal(out _, delimiter, advancePastDelimiter);
        }

        /// <summary>
        /// Advance until any of the given <paramref name="delimiters"/>, if found.
        /// </summary>
        /// <param name="delimiters">The delimiters to search for.</param>
        /// <param name="advancePastDelimiter">True to move past the first found instance of any of the given <paramref name="delimiters"/>.</param>
        /// <returns>True if any of the given <paramref name="delimiters"/> were found.</returns>
        public bool TryAdvanceToAny(scoped ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true)
        {
            ReadOnlySpan<T> remaining = UnreadSpan;
            int index = remaining.IndexOfAny(delimiters);
            if (index != -1)
            {
                AdvanceCurrentSpan(index + (advancePastDelimiter ? 1 : 0));
                return true;
            }

            return TryReadToAnyInternal(out _, delimiters, advancePastDelimiter);
        }

        /// <summary>
        /// Advance past consecutive instances of the given <paramref name="value"/>.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePast(T value)
        {
            long start = Consumed;

            do
            {
                // Advance past all matches in the current span
                int i;
                for (i = CurrentSpanIndex; i < CurrentSpan.Length && CurrentSpan[i].Equals(value); i++)
                {
                }

                int advanced = i - CurrentSpanIndex;
                if (advanced == 0)
                {
                    // Didn't advance at all in this span, exit.
                    break;
                }

                AdvanceCurrentSpan(advanced);

                // If we're at position 0 after advancing and not at the End,
                // we're in a new span and should continue the loop.
            } while (CurrentSpanIndex == 0 && !End);

            return Consumed - start;
        }

        /// <summary>
        /// Skip consecutive instances of any of the given <paramref name="values"/>.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(scoped ReadOnlySpan<T> values)
        {
            long start = Consumed;

            do
            {
                // Advance past all matches in the current span
                int i;
                for (i = CurrentSpanIndex; i < CurrentSpan.Length && values.IndexOf(CurrentSpan[i]) != -1; i++)
                {
                }

                int advanced = i - CurrentSpanIndex;
                if (advanced == 0)
                {
                    // Didn't advance at all in this span, exit.
                    break;
                }

                AdvanceCurrentSpan(advanced);

                // If we're at position 0 after advancing and not at the End,
                // we're in a new span and should continue the loop.
            } while (CurrentSpanIndex == 0 && !End);

            return Consumed - start;
        }

        /// <summary>
        /// Advance past consecutive instances of any of the given values.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(T value0, T value1, T value2, T value3)
        {
            long start = Consumed;

            do
            {
                // Advance past all matches in the current span
                int i;
                for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
                {
                    T value = CurrentSpan[i];
                    if (!value.Equals(value0) && !value.Equals(value1) && !value.Equals(value2) && !value.Equals(value3))
                    {
                        break;
                    }
                }

                int advanced = i - CurrentSpanIndex;
                if (advanced == 0)
                {
                    // Didn't advance at all in this span, exit.
                    break;
                }

                AdvanceCurrentSpan(advanced);

                // If we're at position 0 after advancing and not at the End,
                // we're in a new span and should continue the loop.
            } while (CurrentSpanIndex == 0 && !End);

            return Consumed - start;
        }

        /// <summary>
        /// Advance past consecutive instances of any of the given values.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(T value0, T value1, T value2)
        {
            long start = Consumed;

            do
            {
                // Advance past all matches in the current span
                int i;
                for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
                {
                    T value = CurrentSpan[i];
                    if (!value.Equals(value0) && !value.Equals(value1) && !value.Equals(value2))
                    {
                        break;
                    }
                }

                int advanced = i - CurrentSpanIndex;
                if (advanced == 0)
                {
                    // Didn't advance at all in this span, exit.
                    break;
                }

                AdvanceCurrentSpan(advanced);

                // If we're at position 0 after advancing and not at the End,
                // we're in a new span and should continue the loop.
            } while (CurrentSpanIndex == 0 && !End);

            return Consumed - start;
        }

        /// <summary>
        /// Advance past consecutive instances of any of the given values.
        /// </summary>
        /// <returns>How many positions the reader has been advanced.</returns>
        public long AdvancePastAny(T value0, T value1)
        {
            long start = Consumed;

            do
            {
                // Advance past all matches in the current span
                int i;
                for (i = CurrentSpanIndex; i < CurrentSpan.Length; i++)
                {
                    T value = CurrentSpan[i];
                    if (!value.Equals(value0) && !value.Equals(value1))
                    {
                        break;
                    }
                }

                int advanced = i - CurrentSpanIndex;
                if (advanced == 0)
                {
                    // Didn't advance at all in this span, exit.
                    break;
                }

                AdvanceCurrentSpan(advanced);

                // If we're at position 0 after advancing and not at the End,
                // we're in a new span and should continue the loop.
            } while (CurrentSpanIndex == 0 && !End);

            return Consumed - start;
        }

        /// <summary>
        /// Moves the reader to the end of the sequence.
        /// </summary>
        public void AdvanceToEnd()
        {
            if (_moreData)
            {
                Consumed = Length;
                CurrentSpan = default;
                CurrentSpanIndex = 0;
                _currentPosition = Sequence.End;
                _nextPosition = default;
                _moreData = false;
            }
        }

        /// <summary>
        /// Check to see if the given <paramref name="next"/> value is next.
        /// </summary>
        /// <param name="next">The value to compare the next items to.</param>
        /// <param name="advancePast">Move past the <paramref name="next"/> value if found.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNext(T next, bool advancePast = false)
        {
            if (End)
                return false;

            if (CurrentSpan[CurrentSpanIndex].Equals(next))
            {
                if (advancePast)
                {
                    AdvanceCurrentSpan(1);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check to see if the given <paramref name="next"/> values are next.
        /// </summary>
        /// <param name="next">The span to compare the next items to.</param>
        /// <param name="advancePast">Move past the <paramref name="next"/> values if found.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNext(scoped ReadOnlySpan<T> next, bool advancePast = false)
        {
            ReadOnlySpan<T> unread = UnreadSpan;
            if (unread.StartsWith(next))
            {
                if (advancePast)
                {
                    AdvanceCurrentSpan(next.Length);
                }
                return true;
            }

            // Only check the slow path if there wasn't enough to satisfy next
            return unread.Length < next.Length && IsNextSlow(next, advancePast);
        }

        private bool IsNextSlow(scoped ReadOnlySpan<T> next, bool advancePast)
        {
            ReadOnlySpan<T> currentSpan = UnreadSpan;

            // We should only come in here if we need more data than we have in our current span
            Debug.Assert(currentSpan.Length < next.Length);

            int fullLength = next.Length;
            SequencePosition nextPosition = _nextPosition;

            while (next.StartsWith(currentSpan))
            {
                if (next.Length == currentSpan.Length)
                {
                    // Fully matched
                    if (advancePast)
                    {
                        Advance(fullLength);
                    }
                    return true;
                }

                // Need to check the next segment
                while (true)
                {
                    if (!Sequence.TryGet(ref nextPosition, out ReadOnlyMemory<T> nextSegment, advance: true))
                    {
                        // Nothing left
                        return false;
                    }

                    if (nextSegment.Length > 0)
                    {
                        next = next.Slice(currentSpan.Length);
                        currentSpan = nextSegment.Span;
                        if (currentSpan.Length > next.Length)
                        {
                            currentSpan = currentSpan.Slice(0, next.Length);
                        }
                        break;
                    }
                }
            }

            return false;
        }
    }
}
