using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Security;
using System.Numerics;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Container for storing numbers with efficient range queries and insertions. The values are maintained in a list of ranges.
    /// </summary>
    internal class RangeSet : IEnumerable<RangeSet.Range>
    {
        private List<Range> _ranges;

        internal class Range
        {
            internal ulong Start;
            internal ulong End;

            internal ulong Length => End - Start + 1;

            internal Range(ulong start, ulong end)
            {
                Start = start;
                End = end;
            }

            internal bool Includes(Range other)
            {
                return Includes(other.Start, other.End);
            }

            internal bool Contains(ulong value)
            {
                return Start <= value && value <= End;
            }

            internal bool IsSubset(ulong start, ulong end)
            {
                Debug.Assert(start <= end);
                return start <= Start && End <= end;
            }

            internal bool Includes(ulong start, ulong end)
            {
                Debug.Assert(start <= end);
                return Start <= start && end <= End;
            }

            public override string ToString() => $"[{Start}..{End}]";
        }

        internal RangeSet()
        {
            _ranges = new List<Range>();
        }

        /// <summary>
        ///     Adds given value to the set. Equivalent to adding range [value, value].
        /// </summary>
        /// <param name="value">Value to be added.</param>
        internal void Add(ulong value)
        {
            Add(value, value);
        }

        /// <summary>
        ///     Adds given range of values to the set, both bounds are inclusive.
        /// </summary>
        /// <param name="start">Minimum value to add.</param>
        /// <param name="end">Maximum value to add.</param>
        internal void Add(ulong start, ulong end)
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

                if (range.IsSubset(start, end))
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
                // remove all except one range, which will be reused
                _ranges.RemoveRange(index, removeCount - 1);
                _ranges[index].Start = start;
                _ranges[index].End = end;
            }
        }

        /// <summary>
        ///     Finds index of the largest range with start lesser than or equal to given value. The range at returned index does not necessarily contain the given value.
        /// </summary>
        /// <param name="value">The value to be searched for.</param>
        /// <returns>Index of the last range started before the value or -1 if no such range exists.</returns>
        private int IndexOfPrevious(ulong value)
        {
            if (_ranges[0].Start > value)
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
        internal bool Contains(ulong value)
        {
            return Includes(value, value);
        }

        /// <summary>
        ///     Checks if the given range of values is present in the set.
        /// </summary>
        /// <param name="start">Start of the range.</param>
        /// <param name="end">Inclusive end of the range.</param>
        /// <returns></returns>
        internal bool Includes(ulong start, ulong end)
        {
            Debug.Assert(start <= end);

            // empty set does not intersect with anything
            if (_ranges.Count == 0) return false;
            return _ranges[IndexOfPrevious(start)].Includes(start, end);
        }

        /// <summary>
        ///     Removes given value from the set.
        /// </summary>
        /// <param name="value"></param>
        internal void Remove(ulong value)
        {
            Remove(value, value);
        }

        /// <summary>
        ///     Removes given inclusive range of value from the set.
        /// </summary>
        /// <param name="start">Start of the range to remove.</param>
        /// <param name="end">End of the range to remove.</param>
        internal void Remove(ulong start, ulong end)
        {
            Debug.Assert(start <= end);
            if (_ranges.Count == 0) return;

            int removeCount = 0;
            int index = IndexOfPrevious(start);
            if (index >= 0)
            {
                var range = _ranges[index];
                Debug.Assert(range.Start <= start);
                if (range.IsSubset(start, end))
                {
                    // remove completely
                    removeCount++;
                }
                else if (end < range.End)
                {
                    // split the range
                    _ranges.Insert(index, new Range(end + 1, range.End));
                    range.End = start - 1;
                    return;
                }
                else // reduce the range if needed and keep it
                {
                    range.End = Math.Min(range.End, start - 1);
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
                    range.Start = Math.Max(range.Start, end);
                    break;
                }

                // proper subset, remove entire range
                removeCount++;
            }

            _ranges.RemoveRange(index, removeCount);
        }

        internal ulong GetMin()
        {
            return _ranges[0].Start;
        }

        internal ulong GetMax()
        {
            return _ranges[^1].End;
        }

        internal Range this[int index] => _ranges[index];

        internal int Count => _ranges.Count;

        public IEnumerator<Range> GetEnumerator() => _ranges.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
