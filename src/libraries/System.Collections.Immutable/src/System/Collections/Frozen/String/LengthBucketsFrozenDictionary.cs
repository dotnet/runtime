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
            if (arraySize >= Array.MaxLength)
#else
            if (arraySize >= 0X7FFFFFC7)
#endif
            {
                // This size check prevents OOM.
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

            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];

                int index = (key.Length - minLength) * MaxPerLength;

                if (buckets[index] < 0) buckets[index] = i;
                else if (buckets[index + 1] < 0) buckets[index + 1] = i;
                else if (buckets[index + 2] < 0) buckets[index + 2] = i;
                else if (buckets[index + 3] < 0) buckets[index + 3] = i;
                else if (buckets[index + 4] < 0) buckets[index + 4] = i;
                else
                {
                    // If we've already hit the max per-bucket limit, bail.
                    ArrayPool<int>.Shared.Return(buckets);
                    return null;
                }
            }

#if NET6_0_OR_GREATER
            // We don't need an array with every value initialized to zero if we are just about to overwrite every value anyway.
            // AllocateUninitializedArray is slower for small inputs, hence the size check.
            int[] copy = arraySize < 1000
                ? new int[arraySize]
                : GC.AllocateUninitializedArray<int>(arraySize);
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
            if (bucketIndex >= 0 && bucketIndex < _lengthBuckets.Length)
            {
                ReadOnlySpan<int> bucket = _lengthBuckets.AsSpan(bucketIndex, MaxPerLength);
                string[] keys = _keys;
                TValue[] values = _values;

                foreach (int index in bucket)
                {
                    // -1 is used to indicate a null
                    if (index < 0)
                    {
                        break;
                    }

                    if (_ignoreCase)
                    {
                        if (StringComparer.OrdinalIgnoreCase.Equals(key, keys[index]))
                        {
                            return ref values[index];
                        }
                    }
                    else
                    {
                        if (key == keys[index])
                        {
                            return ref values[index];
                        }
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
