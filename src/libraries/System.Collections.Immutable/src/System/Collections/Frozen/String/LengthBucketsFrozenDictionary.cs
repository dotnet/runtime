// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary implementation where strings are grouped by their lengths.</summary>
    internal sealed partial class LengthBucketsFrozenDictionary<TValue> : FrozenDictionary<string, TValue>
    {
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

            int[]? lengthBuckets = LengthBuckets.CreateLengthBucketsArrayIfAppropriate(keys, comparer, minLength, maxLength);
            if (lengthBuckets is null)
            {
                return null;
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
            int bucketIndex = (key.Length - _minLength) * LengthBuckets.MaxPerLength;
            int bucketEndIndex = bucketIndex + LengthBuckets.MaxPerLength;
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
