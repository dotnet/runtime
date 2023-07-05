// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary implementation where strings are grouped by their lengths.</summary>
    internal sealed class LengthBucketsFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
        /// <summary>Allowed ratio between buckets with values and total buckets.  Under this ratio, this implementation won't be used due to too much wasted space.</summary>
        private const double EmptyLengthsRatio = 0.2;
        /// <summary>The maximum number of items allowed per bucket.  The larger the value, the longer it can take to search a bucket, which is sequentially examined.</summary>
        private const int MaxPerLength = 5;

        private readonly int[] _lengthBuckets;
        private readonly int _minLength;
        private readonly string[] _keys;
        private readonly TValue[] _values;
        private readonly bool _ignoreCase;

        private LengthBucketsFrozenDictionary(
            string[] keys, TValue[] values, int[] lengthBuckets, int minLength, IEqualityComparer<string> comparer)
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
            string[] keys, TValue[] values, IEqualityComparer<string> comparer, int minLength, int maxLength)
        {
            Debug.Assert(keys.Length != 0 && keys.Length == values.Length);
            Debug.Assert(comparer == EqualityComparer<string>.Default || comparer == StringComparer.Ordinal || comparer == StringComparer.OrdinalIgnoreCase);
            Debug.Assert(minLength >= 0 && maxLength >= minLength);

            // If without even looking at the keys we know that some bucket will exceed the max per-bucket
            // limit (pigeon hole principle), we can early-exit out without doing any further work.
            int spread = maxLength - minLength + 1;
            if (keys.Length / spread > MaxPerLength)
            {
                return null;
            }

            int arraySize = spread * MaxPerLength;
#if NET6_0_OR_GREATER
            if (arraySize > Array.MaxLength)
#else
            if (arraySize > 0X7FFFFFC7)
#endif
            {
                // In the future we may lower the value, as it may be quite unlikely
                // to have a LOT of strings of different sizes.
                return null;
            }

            // Instead of creating a dictionary of lists or a multi-dimensional array
            // we rent a single dimension array, where every bucket has five slots.
            // The bucket starts at (key.Length - minLength) * 5.
            // Each value is an index of the key from _keys array
            // or just -1, which represents "null".
            int[] buckets = ArrayPool<int>.Shared.Rent(arraySize);
            buckets.AsSpan(0, arraySize).Fill(-1);

            int nonEmptyCount = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                int startIndex = (key.Length - minLength) * MaxPerLength;
                int endIndex = startIndex + MaxPerLength;
                int index = startIndex;

                while (index < endIndex)
                {
                    ref int bucket = ref buckets[index];
                    if (bucket < 0)
                    {
                        if (index == startIndex)
                        {
                            nonEmptyCount++;
                        }

                        bucket = i;
                        break;
                    }

                    index++;
                }

                if (index == endIndex)
                {
                    // If we've already hit the max per-bucket limit, bail.
                    ArrayPool<int>.Shared.Return(buckets);
                    return null;
                }
            }

            // If there would be too much empty space in the lookup array, bail.
            if (nonEmptyCount / (double)spread < EmptyLengthsRatio)
            {
                ArrayPool<int>.Shared.Return(buckets);
                return null;
            }

#if NET6_0_OR_GREATER
            // We don't need an array with every value initialized to zero if we are just about to overwrite every value anyway.
            int[] copy = GC.AllocateUninitializedArray<int>(arraySize);
            Array.Copy(buckets, copy, arraySize);
#else
            int[] copy = buckets.AsSpan(0, arraySize).ToArray();
#endif
            ArrayPool<int>.Shared.Return(buckets);

            return new LengthBucketsFrozenDictionary<TValue>(keys, values, copy, minLength, comparer);
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
            int bucketIndex = (key.Length - _minLength) * MaxPerLength;
            int bucketEndIndex = bucketIndex + MaxPerLength;
            int[] lengthBuckets = _lengthBuckets;
            if (bucketIndex >= 0 && bucketEndIndex <= lengthBuckets.Length)
            {
                string[] keys = _keys;
                TValue[] values = _values;

                if (!_ignoreCase)
                {
                    for (; bucketIndex < bucketEndIndex; bucketIndex++)
                    {
                        int index = lengthBuckets[bucketIndex];
                        if ((uint)index < (uint)keys.Length)
                        {
                            if (key == keys[index])
                            {
                                return ref values[index];
                            }
                        }
                        else
                        {
                            // -1 is used to indicate a null, when it's casted to unit it becomes > keys.Length
                            break;
                        }
                    }
                }
                else
                {
                    for (; bucketIndex < bucketEndIndex; bucketIndex++)
                    {
                        int index = lengthBuckets[bucketIndex];
                        if ((uint)index < (uint)keys.Length)
                        {
                            if (StringComparer.OrdinalIgnoreCase.Equals(key, keys[index]))
                            {
                                return ref values[index];
                            }
                        }
                        else
                        {
                            // -1 is used to indicate a null, when it's casted to unit it becomes > keys.Length
                            break;
                        }
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
