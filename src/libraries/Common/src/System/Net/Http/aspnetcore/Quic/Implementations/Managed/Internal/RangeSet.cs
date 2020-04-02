using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Security;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Container for storing numbers with efficient range queries and insertions. The values are maintained in a list of ranges.
    /// </summary>
    internal class RangeSet
    {
        private List<Range> _ranges;

        internal class Range
        {
            internal ulong Start;
            internal ulong End;

            public Range(ulong start, ulong end)
            {
                Start = start;
                End = end;
            }

            public override string ToString() => $"[{Start}..{End}]";
        }

        internal RangeSet()
        {
            _ranges = new List<Range>();
        }

        internal void Add(ulong value)
        {
            Add(value, value);
        }

        internal void Add(ulong start, ulong end)
        {
            Debug.Assert(start <= end);

            if (_ranges.Count == 0)
            {
                _ranges.Add(new Range(start, end));
                return;
            }

            // do binary search and find first preceding range
            int index = 0;
            int delta = _ranges.Count / 2;
            while (delta > 0)
            {
                if (_ranges[index + delta].Start < start)
                {
                    index += delta;
                }

                delta /= 2;
            }

            var range = _ranges[index];

            if (start < range.Start)
            {
                // if overlaps with preceding range, merge them
                if (end >= range.Start)
                {
                    start = Math.Min(start, range.Start);
                    end = Math.Max(end, range.End);

                    _ranges.RemoveAt(index);
                }
            }

            // check if following ranges overlap with new one
            while (index < _ranges.Count)
            {
                range = _ranges[index];

                if (range.Start - 1 > end || range.End + 1 < start)
                {
                    // no overlap or touch, we are done
                    break;
                }

                if (start <= range.Start && end >= range.End)
                {
                    // subset of new range, we can remove this range
                    _ranges.RemoveAt(index);
                    continue;
                }

                // overlap with new range, remove
                start = Math.Min(start, range.Start);
                end = Math.Max(end, range.End);

                _ranges.RemoveAt(index);
            }

            // finally, add the new range
            if (start > range.Start)
                index++;
            _ranges.Insert(index, new Range(start, end));

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
    }
}
