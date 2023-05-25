// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics
{
    internal static class DiagnosticsHelper
    {
        // This is similar to System.Linq ToArray. We are not using System.Linq here to avoid the dependency.
        internal static KeyValuePair<string, object?>[]? ToArray(IEnumerable<KeyValuePair<string, object?>>? tags)
        {
            if (tags is null)
            {
                return null;
            }

            KeyValuePair<string, object?>[]? array = null;
            if (tags is ICollection<KeyValuePair<string, object?>> tagsCol)
            {
                array = new KeyValuePair<string, object?>[tagsCol.Count];
                if (tagsCol is IList<KeyValuePair<string, object?>> secondList)
                {
                    for (int i = 0; i < tagsCol.Count; i++)
                    {
                        array[i] = secondList[i];
                    }

                    return array;
                }
            }

            if (array is null)
            {
                int count = 0;
                using (IEnumerator<KeyValuePair<string, object?>> enumerator = tags.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        count++;
                    }
                }

                array = new KeyValuePair<string, object?>[count];
            }

            Debug.Assert(array is not null);

            int index = 0;
            using (IEnumerator<KeyValuePair<string, object?>> enumerator = tags.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    array[index++] = enumerator.Current;
                }
            }

            return array;
        }

        /// <summary>
        /// Compares two tag collections for equality.
        /// </summary>
        /// <param name="sortedTags">The first collection of tags. it has to be a sorted array</param>
        /// <param name="tags2">The second collection of tags. This one doesn't have to be sorted nor be specific collection type</param>
        /// <returns>True if the two collections are equal, false otherwise</returns>
        /// <remarks>
        /// This method is used to compare two collections of tags for equality. The first collection is expected to be a sorted array
        /// of tags. The second collection can be any collection of tags.
        /// we avoid the allocation of a new array by using the second collection as is and not converting it to an array. the reason
        /// is we call this every time we try to create a meter or instrument and we don't want to allocate a new array every time.
        /// </remarks>
        internal static bool CompareTags(KeyValuePair<string, object?>[]? sortedTags, IEnumerable<KeyValuePair<string, object?>>? tags2)
        {
            if (sortedTags == tags2)
            {
                return true;
            }

            if (sortedTags is null || tags2 is null)
            {
                return false;
            }

            // creating with 2 longs which can initially handle 2 * 64 = 128 tags
            BitMapper bitMapper = new BitMapper(stackalloc ulong[2], true);

            int count = sortedTags.Length;
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
                    if (listCount > sortedTags.Length)
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

                return listCount == sortedTags.Length;
            }
        }
    }

    internal ref struct BitMapper
    {
        private int _maxIndex;
        private Span<ulong> _bitMap;

        public BitMapper(Span<ulong> bitMap, bool zeroInitialize = false)
        {
            _bitMap = bitMap;
            _maxIndex = bitMap.Length * sizeof(long) * 8;

            if (zeroInitialize)
            {
                for (int i = 0; i < _bitMap.Length; i++)
                {
                    _bitMap[i] = 0;
                }
            }
        }

        public int MaxIndex => _maxIndex;

        private void Expand(int index)
        {
            if (_maxIndex > index)
            {
                return;
            }

            int newMax = (index / sizeof(long)) + 10;

            Span<ulong> newBitMap = new ulong[newMax];
            _bitMap.CopyTo(newBitMap);
            _bitMap = newBitMap;
            _maxIndex = newMax * sizeof(long);
        }

        private static void GetIndexAndMask(int index, out int bitIndex, out ulong mask)
        {
            bitIndex = index >> sizeof(long);
            int bit = index & (sizeof(long) - 1);
            mask = 1UL << bit;
        }

        public bool SetBit(int index)
        {
            if (index < 0)
            {
                throw new OutOfMemoryException(nameof(index));
            }

            if (index >= _maxIndex)
            {
                Expand(index);
            }

            GetIndexAndMask(index, out int bitIndex, out ulong mask);
            ulong value = _bitMap[bitIndex];
            _bitMap[bitIndex] = value | mask;
            return true;
        }

        public bool IsSet(int index)
        {
            if (index < 0)
            {
                throw new OutOfMemoryException(nameof(index));
            }

            if (index >= _maxIndex)
            {
                return false;
            }

            GetIndexAndMask(index, out int bitIndex, out ulong mask);
            ulong value = _bitMap[bitIndex];
            return ((value & mask) != 0);
        }
    }
}
