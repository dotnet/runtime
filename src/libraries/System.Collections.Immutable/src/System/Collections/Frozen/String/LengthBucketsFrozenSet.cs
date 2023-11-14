// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set implementation where strings are grouped by their lengths.</summary>
    internal sealed class LengthBucketsFrozenSet : FrozenSetInternalBase<string, LengthBucketsFrozenSet.GSW>
    {
        private readonly int[] _lengthBuckets;
        private readonly int _minLength;
        private readonly string[] _items;
        private readonly bool _ignoreCase;

        private LengthBucketsFrozenSet(
            string[] items, int[] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
            : base(comparer)
        {
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            _items = items;
            _lengthBuckets = lengthBuckets;
            _minLength = minLength;
            _ignoreCase = ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase);
        }

        internal static LengthBucketsFrozenSet? CreateLengthBucketsFrozenSetIfAppropriate(
            string[] items, IEqualityComparer<string> comparer, int minLength, int maxLength)
        {
            Debug.Assert(items.Length != 0);

            int[]? lengthBuckets = LengthBuckets.CreateLengthBucketsArrayIfAppropriate(items, comparer, minLength, maxLength);
            if (lengthBuckets is null)
            {
                return null;
            }

            return new LengthBucketsFrozenSet(items, lengthBuckets, minLength, comparer);
        }

        /// <inheritdoc />
        private protected override string[] ItemsCore => _items;

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);

        /// <inheritdoc />
        private protected override int CountCore => _items.Length;

        /// <inheritdoc />
        private protected override int FindItemIndex(string? item)
        {
            if (item is not null) // this implementation won't be constructed from null values, but Contains may still be called with one
            {
                // If the length doesn't have an associated bucket, the key isn't in the dictionary.
                int bucketIndex = (item.Length - _minLength) * LengthBuckets.MaxPerLength;
                int bucketEndIndex = bucketIndex + LengthBuckets.MaxPerLength;
                int[] lengthBuckets = _lengthBuckets;
                if (bucketIndex >= 0 && bucketEndIndex <= lengthBuckets.Length)
                {
                    string[] items = _items;

                    if (!_ignoreCase)
                    {
                        // within a bucket, strings are sorted in order 3102546
                        int jumpSize = LengthBuckets.MaxPerLength / 2;
                        int nextJump = 0;

                        while (jumpSize >= -1)
                        {
                            bucketIndex += nextJump;
                            int index = lengthBuckets[bucketIndex];
                            if ((uint)items.Length < (uint)index)
                            {
                                // -1 is used to indicate a null, when it's casted to unit it becomes > keys.Length
                                break;
                            }

                            int comparison = string.CompareOrdinal(item, items[index]);
                            if (comparison == 0)
                            {
                                return index;
                            }

                            if (comparison < 0)
                            {
                                nextJump = 1;
                            }
                            else
                            {
                                nextJump = 1 + jumpSize;
                            }

                            jumpSize -= 2;
                        }
                    }
                    else
                    {    // within a bucket, strings are sorted in order 3102546
                        int jumpSize = LengthBuckets.MaxPerLength / 2;
                        int nextJump = 0;

                        while (jumpSize >= -1)
                        {
                            bucketIndex += nextJump;
                            int index = lengthBuckets[bucketIndex];
                            if ((uint)items.Length < (uint)index)
                            {
                                // -1 is used to indicate a null, when it's casted to unit it becomes > keys.Length
                                break;
                            }

                            int comparison = StringComparer.OrdinalIgnoreCase.Compare(item, items[index]);
                            if (comparison == 0)
                            {
                                return index;
                            }

                            if (comparison < 0)
                            {
                                nextJump = 1;
                            }
                            else
                            {
                                nextJump = 1 + jumpSize;
                            }

                            jumpSize -= 2;
                        }
                    }
                }
            }

            return -1;
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private LengthBucketsFrozenSet _set;
            public void Store(FrozenSet<string> set) => _set = (LengthBucketsFrozenSet)set;

            public int Count => _set.Count;
            public IEqualityComparer<string> Comparer => _set.Comparer;
            public int FindItemIndex(string item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
