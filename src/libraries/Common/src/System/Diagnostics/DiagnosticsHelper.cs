// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal static class DiagnosticsHelper
    {
        /// <summary>
        /// Compares two tag collections for equality.
        /// </summary>
        /// <param name="sortedTags">The first collection of tags. it has to be a sorted List</param>
        /// <param name="tags2">The second collection of tags. This one doesn't have to be sorted nor be specific collection type</param>
        /// <returns>True if the two collections are equal, false otherwise</returns>
        /// <remarks>
        /// This method is used to compare two collections of tags for equality. The first collection is expected to be a sorted array
        /// of tags. The second collection can be any collection of tags.
        /// we avoid the allocation of a new array by using the second collection as is and not converting it to an array. the reason
        /// is we call this every time we try to create a meter or instrument and we don't want to allocate a new array every time.
        /// </remarks>
        internal static bool CompareTags(List<KeyValuePair<string, object?>>? sortedTags, IEnumerable<KeyValuePair<string, object?>>? tags2)
        {
            if (sortedTags == tags2)
            {
                return true;
            }

            if (sortedTags is null || tags2 is null)
            {
                return false;
            }

            int count = sortedTags.Count;
            int size = count / (sizeof(long) * 8) + 1;
            BitMapper bitMapper = new BitMapper(size <= 100 ? stackalloc ulong[size] : new ulong[size]);

            if (tags2 is ICollection<KeyValuePair<string, object?>> tagsCol)
            {
                if (tagsCol.Count != count)
                {
                    return false;
                }

                if (tagsCol is IList<KeyValuePair<string, object?>> secondList)
                {
                    for (int i = 0; i < count; i++)
                    {
                        KeyValuePair<string, object?> pair = secondList[i];

                        for (int j = 0; j < count; j++)
                        {
                            if (bitMapper.IsSet(j))
                            {
                                continue;
                            }

                            KeyValuePair<string, object?> pair1 = sortedTags[j];

                            int compareResult = string.CompareOrdinal(pair.Key, pair1.Key);
                            if (compareResult == 0 && object.Equals(pair.Value, pair1.Value))
                            {
                                bitMapper.SetBit(j);
                                break;
                            }

                            if (compareResult < 0 || j == count - 1)
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }

            int listCount = 0;
            using (IEnumerator<KeyValuePair<string, object?>> enumerator = tags2.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    listCount++;
                    if (listCount > sortedTags.Count)
                    {
                        return false;
                    }

                    KeyValuePair<string, object?> pair = enumerator.Current;
                    for (int j = 0; j < count; j++)
                    {
                        if (bitMapper.IsSet(j))
                        {
                            continue;
                        }

                        KeyValuePair<string, object?> pair1 = sortedTags[j];

                        int compareResult = string.CompareOrdinal(pair.Key, pair1.Key);
                        if (compareResult == 0 && object.Equals(pair.Value, pair1.Value))
                        {
                            bitMapper.SetBit(j);
                            break;
                        }

                        if (compareResult < 0 || j == count - 1)
                        {
                            return false;
                        }
                    }
                }

                return listCount == sortedTags.Count;
            }
        }
    }

    internal ref struct BitMapper
    {
        private int _maxIndex;
        private Span<ulong> _bitMap;

        public BitMapper(Span<ulong> bitMap)
        {
            _bitMap = bitMap;
            _bitMap.Clear();
            _maxIndex = bitMap.Length * sizeof(long) * 8;
        }

        public int MaxIndex => _maxIndex;

        private static void GetIndexAndMask(int index, out int bitIndex, out ulong mask)
        {
            bitIndex = index >> 6;
            int bit = index & (sizeof(long) * 8 - 1);
            mask = 1UL << bit;
        }

        public bool SetBit(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < _maxIndex);

            GetIndexAndMask(index, out int bitIndex, out ulong mask);
            ulong value = _bitMap[bitIndex];
            _bitMap[bitIndex] = value | mask;
            return true;
        }

        public bool IsSet(int index)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(index < _maxIndex);

            GetIndexAndMask(index, out int bitIndex, out ulong mask);
            ulong value = _bitMap[bitIndex];
            return ((value & mask) != 0);
        }
    }
}
