// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Container for storing numbers with efficient range queries and insertions. The values are maintained in a list of ranges.
    /// </summary>
    internal class RangeSet : IEnumerable<RangeSet.Range>
    {
        private List<Range> _ranges;

        internal readonly struct Range
        {
            internal long Start { get; }
            internal long End { get; }

            internal long Length => End - Start + 1;

            internal Range(long start, long end)
            {
                Start = start;
                End = end;
            }

            internal Range WithStart(long start)
            {
                return new Range(start, End);
            }

            internal Range WithEnd(long end)
            {
                return new Range(Start, end);
            }

            internal bool Includes(Range other)
            {
                return Includes(other.Start, other.End);
            }

            internal bool Contains(long value)
            {
                return Start <= value && value <= End;
            }

            internal bool IsSubsetOf(long start, long end)
            {
                Debug.Assert(start <= end);
                return start <= Start && End <= end;
            }

            internal bool Includes(long start, long end)
            {
                Debug.Assert(start <= end);
                return Start <= start && end <= End;
            }

            public override string ToString() => $"[{Start}..{End}]";

            public void Deconstruct(out long start, out long end)
            {
                start = Start;
                end = End;
            }
        }

        internal RangeSet()
        {
            _ranges = new List<Range>();
        }

        /// <summary>
        ///     Removes all items from the set.
        /// </summary>
        internal void Clear()
        {
            _ranges.Clear();
        }

        /// <summary>
        ///     Adds given value to the set. Equivalent to adding range [value, value].
        /// </summary>
        /// <param name="value">Value to be added.</param>
        internal void Add(long value)
        {
            Add(value, value);
        }

        /// <summary>
        ///     Adds given range of values to the set, both bounds are inclusive.
        /// </summary>
        /// <param name="start">Minimum value to add.</param>
        /// <param name="end">Maximum value to add.</param>
        internal void Add(long start, long end)
        {
            Debug.Assert(start <= end);

            if (_ranges.Count == 0)
            {
                _ranges.Add(new Range(start, end));
                return;
            }

            int removeCount = 0;

            // check the range to the left, if exists
            int index = IndexOfPrevious(start);
            if (index >= 0)
            {
                var range = _ranges[index];
                // it overlaps with or touches the preceding range, merge them
                if (start <= range.End + 1)
                {
                    start = Math.Min(start, range.Start);
                    end = Math.Max(end, range.End);

                    removeCount = 1;
                }
                else // keep the range
                {
                    index++;
                }
            }

            index = Math.Max(0, index);

            // check if following ranges overlap with new one
            while (index + removeCount < _ranges.Count)
            {
                var range = _ranges[index + removeCount];

                if (range.Start - 1 > end || range.End + 1 < start)
                {
                    // no overlap or touch, we are done
                    break;
                }

                if (range.IsSubsetOf(start, end))
                {
                    // subset of new range, we can remove this range
                    removeCount++;
                    continue;
                }

                // overlap with the new range, merge it into the new one
                start = Math.Min(start, range.Start);
                end = Math.Max(end, range.End);

                removeCount++;
            }

            if (removeCount == 0)
            {
                // add the new range
                _ranges.Insert(index, new Range(start, end));
            }
            else
            {
                // remove all except one range, which will be overwritten
                _ranges.RemoveRange(index, removeCount - 1);
                _ranges[index] = new Range(start, end);
            }
        }

        /// <summary>
        ///     Adds all items from other instance of <see cref="RangeSet"/>.
        /// </summary>
        /// <param name="ranges">Ranges to be included.</param>
        internal void Add(RangeSet ranges)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                Add(ranges[i].Start, ranges[i].End);
            }
        }

        /// <summary>
        ///     Finds index of the largest range with start lesser than or equal to given value. The range at returned index does not necessarily contain the given value.
        /// </summary>
        /// <param name="value">The value to be searched for.</param>
        /// <returns>Index of the last range started before the value or -1 if no such range exists.</returns>
        private int IndexOfPrevious(long value)
        {
            if (_ranges.Count == 0 || _ranges[0].Start > value)
                return -1;

            // do binary search
            int hi = _ranges.Count - 1;
            int lo = 0;
            while (lo <= hi)
            {
                int median = lo + (hi - lo) / 2;
                int cmp = _ranges[median].Start.CompareTo(value);
                if (cmp == 0) return median;
                if (cmp < 0) lo = median + 1;
                else hi = median - 1;
            }

            return lo - 1;
        }

        /// <summary>
        ///     Checks if the given value is present in the set.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns></returns>
        internal bool Contains(long value)
        {
            return Includes(value, value);
        }

        /// <summary>
        ///     Checks if the given range of values is present in the set.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="end">Inclusive end of the range.</param>
        /// <returns></returns>
        internal bool Includes(long start, long end)
        {
            Debug.Assert(start <= end);

            // empty set does not intersect with anything
            int index = IndexOfPrevious(start);
            return index >= 0 && _ranges[index].Includes(start, end);
        }

        /// <summary>
        ///     Removes given value from the set.
        /// </summary>
        /// <param name="value"></param>
        internal void Remove(long value)
        {
            Remove(value, value);
        }

        /// <summary>
        ///     Removes given inclusive range of value from the set.
        /// </summary>
        /// <param name="start">Start of the range to remove.</param>
        /// <param name="end">End of the range to remove.</param>
        internal void Remove(long start, long end)
        {
            Debug.Assert(start <= end);
            if (_ranges.Count == 0) return;

            int removeCount = 0;
            int index = IndexOfPrevious(start);
            if (index >= 0)
            {
                var range = _ranges[index];
                Debug.Assert(range.Start <= start);
                if (range.Start == start)
                {
                    if (range.End <= end)
                    {
                        // remove completely
                        removeCount++;
                    }
                    else
                    {
                        // shorten from the left, and we are done
                        _ranges[index] = range.WithStart(end + 1);
                        return;
                    }
                }
                else if (end < range.End)
                {
                    // split the range and we are done
                    _ranges.Insert(index + 1, new Range(end + 1, range.End));
                    _ranges[index] = range.WithEnd(start - 1);
                    return;
                }
                else // shorten the range from the right
                {
                    _ranges[index] = range.WithEnd(Math.Min(range.End, start - 1));
                    index++;
                }
            }

            index = Math.Max(0, index);

            // check all following ranges
            while (index + removeCount < _ranges.Count)
            {
                var range = _ranges[index + removeCount];
                Debug.Assert(start < range.Start);

                if (end < range.End)
                {
                    // possible only partial overlap, adjust the range
                    _ranges[index] = range.WithStart(Math.Max(range.Start, end));
                    break;
                }

                // proper subset, remove entire range
                removeCount++;
            }

            _ranges.RemoveRange(index, removeCount);
        }

        /// <summary>
        ///     Removes items present in the provided ranges from the set.
        /// </summary>
        /// <param name="ranges">Ranges to be removed.</param>
        internal void Remove(RangeSet ranges)
        {
            for (int i = ranges.Count - 1; i >= 0; i--)
            {
                Remove(ranges[i].Start, ranges[i].End);
            }
        }

        /// <summary>
        ///     Gets minimal value in the set.
        /// </summary>
        internal long GetMin()
        {
            return _ranges[0].Start;
        }

        /// <summary>
        ///     Gets maximal value in the set.
        /// </summary>
        internal long GetMax()
        {
            return _ranges[^1].End;
        }

        internal Range this[int index] => _ranges[index];

        /// <summary>
        ///     Returns number of contiguous ranges.
        /// </summary>
        internal int Count => _ranges.Count;

        public IEnumerator<Range> GetEnumerator() => _ranges.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
