// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary implementation where strings are grouped by their lengths.</summary>
    internal sealed class LengthBucketsFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        /// <summary>Allowed ratio between buckets with values and total buckets.  Under this ratio, this implementation won't be used due to too much wasted space.</summary>
        private const double EmptyLengthsRatio = 0.2;
        /// <summary>The maximum number of items allowed per bucket.  The larger the value, the longer it can take to search a bucket, which is sequentially examined.</summary>
        private const int MaxPerLength = 5;

        private readonly KeyValuePair<string, int>[][] _lengthBuckets;
        private readonly int _minLength;
        private readonly string[] _keys;
        private readonly TValue[] _values;
        private readonly bool _ignoreCase;

        private LengthBucketsFrozenDictionary(
            string[] keys, TValue[] values, KeyValuePair<string, int>[][] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
            : base(comparer)
        {
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);

            _keys = keys;
            _values = values;
            _lengthBuckets = lengthBuckets;
            _minLength = minLength;
            _ignoreCase = ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase);
        }

        internal static LengthBucketsFrozenDictionary<TValue>? CreateLengthBucketsFrozenDictionaryIfAppropriate(
            Dictionary<string, TValue> source, IEqualityComparer<string> comparer, int minLength, int maxLength)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);
            Debug.Assert(minLength >= 0 && maxLength >= minLength);

            // If without even looking at the keys we know that some bucket will exceed the max per-bucket
            // limit (pigeon hole principle), we can early-exit out without doing any further work.
            int spread = maxLength - minLength + 1;
            if (source.Count / spread > MaxPerLength)
            {
                return null;
            }

            // Iterate through all of the inputs, bucketing them based on the length of the string.
            var groupedByLength = new Dictionary<int, List<KeyValuePair<string, TValue>>>();
            foreach (KeyValuePair<string, TValue> pair in source)
            {
                string s = pair.Key;
                Debug.Assert(s.Length >= minLength && s.Length <= maxLength);

#if NET6_0_OR_GREATER
                List<KeyValuePair<string, TValue>> list = CollectionsMarshal.GetValueRefOrAddDefault(groupedByLength, s.Length, out _) ??= new List<KeyValuePair<string, TValue>>(MaxPerLength);
#else
                if (!groupedByLength.TryGetValue(s.Length, out List<KeyValuePair<string, TValue>>? list))
                {
                    groupedByLength[s.Length] = list = new List<KeyValuePair<string, TValue>>(MaxPerLength);
                }
#endif

                // If we've already hit the max per-bucket limit, bail.
                if (list.Count == MaxPerLength)
                {
                    return null;
                }

                list.Add(pair);
            }

            // If there would be too much empty space in the lookup array, bail.
            if (groupedByLength.Count / (double)spread < EmptyLengthsRatio)
            {
                return null;
            }

            var keys = new string[source.Count];
            var values = new TValue[keys.Length];
            var lengthBuckets = new KeyValuePair<string, int>[spread][];

            // Iterate through each bucket, filling the keys/values arrays, and creating a lookup array such that
            // given a string length we can index into that array to find the array of string,int pairs: the string
            // is the key and the int is the index into the keys/values array for the corresponding entry.
            int index = 0;
            foreach (KeyValuePair<int, List<KeyValuePair<string, TValue>>> group in groupedByLength)
            {
                KeyValuePair<string, int>[] length = lengthBuckets[group.Key - minLength] = new KeyValuePair<string, int>[group.Value.Count];
                int i = 0;
                foreach (KeyValuePair<string, TValue> pair in group.Value)
                {
                    length[i] = new KeyValuePair<string, int>(pair.Key, index);
                    keys[index] = pair.Key;
                    values[index] = pair.Value;

                    i++;
                    index++;
                }
            }

            return new LengthBucketsFrozenDictionary<TValue>(keys, values, lengthBuckets, minLength, comparer);
        }

        /// <inheritdoc />
        private protected override string[] KeysCore => _keys;

        /// <inheritdoc />
        private protected override TValue[] ValuesCore => _values;

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

        /// <inheritdoc />
        private protected override int CountCore => _keys.Length;

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(string key)
        {
            // If the length doesn't have an associated bucket, the key isn't in the dictionary.
            int length = key.Length - _minLength;
            if (length >= 0)
            {
                // Get the bucket for this key's length.  If it's null, the key isn't in the dictionary.
                KeyValuePair<string, int>[][] lengths = _lengthBuckets;
                if ((uint)length < (uint)lengths.Length && lengths[length] is KeyValuePair<string, int>[] subset)
                {
                    // Now iterate through every key in the bucket to see whether this is a match.
                    if (_ignoreCase)
                    {
                        foreach (KeyValuePair<string, int> kvp in subset)
                        {
                            if (StringComparer.OrdinalIgnoreCase.Equals(key, kvp.Key))
                            {
                                return ref _values[kvp.Value];
                            }
                        }
                    }
                    else
                    {
                        foreach (KeyValuePair<string, int> kvp in subset)
                        {
                            if (key == kvp.Key)
                            {
                                return ref _values[kvp.Value];
                            }
                        }
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
