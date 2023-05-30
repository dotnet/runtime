// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen set implementation where strings are grouped by their lengths.</summary>
    internal sealed class LengthBucketsFrozenSet : FrozenSetInternalBase<string, LengthBucketsFrozenSet.GSW>
    {
        /// <summary>Allowed ratio between buckets with values and total buckets.  Under this ratio, this implementation won't be used due to too much wasted space.</summary>
        private const double EmptyLengthsRatio = 0.2;

        /// <summary>The maximum number of items allowed per bucket.  The larger the value, the longer it can take to search a bucket, which is sequentially examined.</summary>
        private const int MaxPerLength = 5;

        private readonly KeyValuePair<string, int>[][] _lengthBuckets;
        private readonly int _minLength;
        private readonly string[] _items;
        private readonly bool _ignoreCase;

        private LengthBucketsFrozenSet(string[] items, KeyValuePair<string, int>[][] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
            : base(comparer)
        {
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            _items = items;
            _lengthBuckets = lengthBuckets;
            _minLength = minLength;
            _ignoreCase = ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase);
        }

        internal static LengthBucketsFrozenSet? CreateLengthBucketsFrozenSetIfAppropriate(string[] entries, IEqualityComparer<string> comparer, int minLength, int maxLength)
        {
            Debug.Assert(entries.Length != 0);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);
            Debug.Assert(minLength >= 0 && maxLength >= minLength);

            // If without even looking at the keys we know that some bucket will exceed the max per-bucket
            // limit (pigeon hole principle), we can early-exit out without doing any further work.
            int spread = maxLength - minLength + 1;
            if (entries.Length / spread > MaxPerLength)
            {
                return null;
            }

            // Iterate through all of the inputs, bucketing them based on the length of the string.
            var groupedByLength = new Dictionary<int, List<string>>();
            foreach (string s in entries)
            {
                Debug.Assert(s is not null, "This implementation should not be used with null source values.");
                Debug.Assert(s.Length >= minLength && s.Length <= maxLength);

#if NET6_0_OR_GREATER
                List<string> list = CollectionsMarshal.GetValueRefOrAddDefault(groupedByLength, s.Length, out _) ??= new List<string>(MaxPerLength);
#else
                if (!groupedByLength.TryGetValue(s.Length, out List<string>? list))
                {
                    groupedByLength[s.Length] = list = new List<string>(MaxPerLength);
                }
#endif

                // If we've already hit the max per-bucket limit, bail.
                if (list.Count == MaxPerLength)
                {
                    return null;
                }

                list.Add(s);
            }

            // If there would be too much empty space in the lookup array, bail.
            if (groupedByLength.Count / (double)spread < EmptyLengthsRatio)
            {
                return null;
            }

            var lengthBuckets = new KeyValuePair<string, int>[spread][];

            // Iterate through each bucket, filling the items array, and creating a lookup array such that
            // given a string length we can index into that array to find the array of string,int pairs: the string
            // is the key and the int is the index into the items array for the corresponding entry.
            int index = 0;
            foreach (KeyValuePair<int, List<string>> group in groupedByLength)
            {
                KeyValuePair<string, int>[] length = lengthBuckets[group.Key - minLength] = new KeyValuePair<string, int>[group.Value.Count];
                int i = 0;
                foreach (string value in group.Value)
                {
                    length[i] = new KeyValuePair<string, int>(value, index);
                    entries[index] = value;

                    i++;
                    index++;
                }
            }

            return new LengthBucketsFrozenSet(entries, lengthBuckets, minLength, comparer);
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
                // If the length doesn't have an associated bucket, the key isn't in the set.
                int length = item.Length - _minLength;
                if (length >= 0)
                {
                    // Get the bucket for this key's length.  If it's null, the key isn't in the set.
                    KeyValuePair<string, int>[][] lengths = _lengthBuckets;
                    if ((uint)length < (uint)lengths.Length && lengths[length] is KeyValuePair<string, int>[] subset)
                    {
                        // Now iterate through every key in the bucket to see whether this is a match.
                        if (_ignoreCase)
                        {
                            foreach (KeyValuePair<string, int> kvp in subset)
                            {
                                if (StringComparer.OrdinalIgnoreCase.Equals(item, kvp.Key))
                                {
                                    return kvp.Value;
                                }
                            }
                        }
                        else
                        {
                            foreach (KeyValuePair<string, int> kvp in subset)
                            {
                                if (item == kvp.Key)
                                {
                                    return kvp.Value;
                                }
                            }
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
