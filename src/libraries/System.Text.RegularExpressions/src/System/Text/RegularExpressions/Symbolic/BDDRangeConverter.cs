// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Converts sets of integers expressed as BDDs into non-overlapping and ordered arrays of ranges.
    /// </summary>
    internal sealed class BDDRangeConverter
    {
        /// <summary>
        /// Cache for all range conversions. Having this cache is required to avoid exponential running time.
        /// </summary>
        private readonly Dictionary<BDD, (uint, uint)[]> _rangeCache = new Dictionary<BDD, (uint, uint)[]>();

        private BDDRangeConverter() { }

        /// <summary>
        /// Convert the set into an equivalent array of ranges.
        /// The ranges are nonoverlapping and ordered.
        /// </summary>
        public static (uint, uint)[] ToRanges(BDD set)
        {
            const int MaxBit = 15; // most significant bit of a 16-bit char

            if (set.IsEmpty)
                return [];

            if (set.IsFull)
                return [(0u, ((uint)1 << MaxBit << 1) - 1)];

            var rc = new BDDRangeConverter();
            return LiftRanges(MaxBit + 1, MaxBit - set.Ordinal, rc.ToRangesFromOrdinal(set));
        }

        /// <summary>
        /// Extends a set of ranges to include more significant bits. The new bits are allowed to be anything and all
        /// combinations of the new bits are included.
        /// e.g. if toBits = 6 and newBits = 2 and ranges = (in binary form) {[0000 1010, 0000 1110]} i.e. [x0A,x0E]
        /// then result = {[0000 1010, 0000 1110], [0001 1010, 0001 1110],
        ///                [0010 1010, 0010 1110], [0011 1010, 0011 1110]},
        /// </summary>
        private static (uint, uint)[] LiftRanges(int toBits, int newBits, (uint, uint)[] ranges)
        {
            // nothing happens if no new bits are added
            if (newBits == 0)
                return ranges;

            int fromBits = toBits - newBits;

            // Iterate through all combinations of the new bits
            var result = new (uint, uint)[(1 << newBits) * ranges.Length];
            int resultPos = 0;
            for (uint i = 0; i < (1 << newBits); i++)
            {
                // Shift the prefix to be past the existing range of bits, and
                // generate each range with this prefix added.
                uint prefix = i << fromBits;
                foreach ((uint, uint) range in ranges)
                {
                    result[resultPos++] = (range.Item1 | prefix, range.Item2 | prefix);
                }
            }

            // lifted ranges can wrap around like this [0...][...2^fromBits-1][2^fromBits...][...2^(fromBits+1)-1]
            uint maximal = ((uint)1 << fromBits) - 1;
            if (ranges[0].Item1 == 0 && ranges[ranges.Length - 1].Item2 == maximal)
            {
                // merge consequtive ranges, we know that res has at least two elements here
                var merged = new List<(uint, uint)>();
                uint from = result[0].Item1;
                uint to = result[0].Item2;
                for (int i = 1; i < result.Length; i++)
                {
                    if (to == result[i].Item1 - 1)
                    {
                        // merge into previous instead of adding a new range
                        to = result[i].Item2;
                    }
                    else
                    {
                        merged.Add((from, to));
                        from = result[i].Item1;
                        to = result[i].Item2;
                    }
                }
                merged.Add((from, to));
                result = merged.ToArray();
            }

            return result;
        }

        private (uint, uint)[] ToRangesFromOrdinal(BDD set)
        {
            if (!_rangeCache.TryGetValue(set, out (uint, uint)[]? ranges))
            {
                Debug.Assert(!set.IsLeaf);

                int b = set.Ordinal;
                uint mask = (uint)1 << b;
                if (set.Zero.IsEmpty)
                {
                    #region 0-case is empty
                    if (set.One.IsFull)
                    {
                        ranges = [(mask, (mask << 1) - 1)];
                    }
                    else //1-case is neither full nor empty
                    {
                        (uint, uint)[] ranges1 = LiftRanges(b, b - set.One.Ordinal - 1, ToRangesFromOrdinal(set.One));
                        ranges = new (uint, uint)[ranges1.Length];
                        for (int i = 0; i < ranges1.Length; i++)
                        {
                            ranges[i] = (ranges1[i].Item1 | mask, ranges1[i].Item2 | mask);
                        }
                    }
                    #endregion
                }
                else if (set.Zero.IsFull)
                {
                    #region 0-case is full
                    if (set.One.IsEmpty)
                    {
                        ranges = [(0u, mask - 1)];
                    }
                    else
                    {
                        (uint, uint)[] rangesR = LiftRanges(b, b - set.One.Ordinal - 1, ToRangesFromOrdinal(set.One));
                        (uint, uint) range = rangesR[0];
                        if (range.Item1 == 0)
                        {
                            ranges = new (uint, uint)[rangesR.Length];
                            ranges[0] = (0, range.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[i] = (rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                        else
                        {
                            ranges = new (uint, uint)[rangesR.Length + 1];
                            ranges[0] = (0, mask - 1);
                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[i + 1] = (rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region 0-case is neither full nor empty
                    (uint, uint)[] rangesL = LiftRanges(b, b - set.Zero.Ordinal - 1, ToRangesFromOrdinal(set.Zero));
                    (uint, uint) last = rangesL[rangesL.Length - 1];

                    if (set.One.IsEmpty)
                    {
                        ranges = rangesL;
                    }

                    else if (set.One.IsFull)
                    {
                        var ranges1 = new List<(uint, uint)>();
                        for (int i = 0; i < rangesL.Length - 1; i++)
                        {
                            ranges1.Add(rangesL[i]);
                        }

                        if (last.Item2 == (mask - 1))
                        {
                            ranges1.Add((last.Item1, (mask << 1) - 1));
                        }
                        else
                        {
                            ranges1.Add(last);
                            ranges1.Add((mask, (mask << 1) - 1));
                        }
                        ranges = ranges1.ToArray();
                    }
                    else //general case: neither 0-case, not 1-case is full or empty
                    {
                        (uint, uint)[] rangesR0 = ToRangesFromOrdinal(set.One);

                        (uint, uint)[] rangesR = LiftRanges(b, b - set.One.Ordinal - 1, rangesR0);

                        (uint, uint) first = rangesR[0];

                        if (last.Item2 == (mask - 1) && first.Item1 == 0) //merge together the last and first ranges
                        {
                            ranges = new (uint, uint)[rangesL.Length + rangesR.Length - 1];
                            for (int i = 0; i < rangesL.Length - 1; i++)
                            {
                                ranges[i] = rangesL[i];
                            }

                            ranges[rangesL.Length - 1] = (last.Item1, first.Item2 | mask);
                            for (int i = 1; i < rangesR.Length; i++)
                            {
                                ranges[rangesL.Length - 1 + i] = (rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }
                        else
                        {
                            ranges = new (uint, uint)[rangesL.Length + rangesR.Length];
                            for (int i = 0; i < rangesL.Length; i++)
                            {
                                ranges[i] = rangesL[i];
                            }

                            for (int i = 0; i < rangesR.Length; i++)
                            {
                                ranges[rangesL.Length + i] = (rangesR[i].Item1 | mask, rangesR[i].Item2 | mask);
                            }
                        }

                    }
                    #endregion
                }
                _rangeCache[set] = ranges;
            }

            return ranges;
        }
    }
}
