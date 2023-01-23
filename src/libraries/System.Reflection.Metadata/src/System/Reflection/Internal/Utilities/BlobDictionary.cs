// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
#if NET
using System.Runtime.InteropServices;
#endif

namespace System.Reflection.Internal
{
    [DebuggerDisplay("Count = {Count}")]
    internal readonly struct BlobDictionary<TValue>
    {
        private readonly Dictionary<int, KeyValuePair<ImmutableArray<byte>, TValue>> _dictionary;

        // A simple LCG. Constants taken from
        // https://github.com/imneme/pcg-c/blob/83252d9c23df9c82ecb42210afed61a7b42402d7/include/pcg_variants.h#L276-L284
        private static int GetNextDictionaryKey(int dictionaryKey) =>
            (int)((uint)dictionaryKey * 747796405 + 2891336453);

#if NET
        private ref KeyValuePair<ImmutableArray<byte>, TValue> GetValueRefOrAddDefault(ReadOnlySpan<byte> key, out bool exists)
        {
            int dictionaryKey = Hash.GetFNVHashCode(key);
            while (true)
            {
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, dictionaryKey, out exists);
                if (!exists || entry.Key.AsSpan().SequenceEqual(key))
                {
                    return ref entry;
                }
                dictionaryKey = GetNextDictionaryKey(dictionaryKey);
            }
        }

        private TValue GetOrAdd(ReadOnlySpan<byte> key, ImmutableArray<byte> immutableKey, TValue value, out bool exists)
        {
            ref var entry = ref GetValueRefOrAddDefault(key, out exists);
            if (exists)
            {
                return entry.Value;
            }

            if (immutableKey.IsDefault)
            {
                immutableKey = key.ToImmutableArray();
            }
            entry = new(immutableKey, value);
            return value;
        }
#else
        private TValue GetOrAdd(ReadOnlySpan<byte> key, ImmutableArray<byte> immutableKey, TValue value, out bool exists)
        {
            int dictionarykey = Hash.GetFNVHashCode(key);
            KeyValuePair<ImmutableArray<byte>, TValue> entry;
            while (true)
            {
                if (!(exists = _dictionary.TryGetValue(dictionarykey, out entry))
                    || entry.Key.AsSpan().SequenceEqual(key))
                {
                    break;
                }
                dictionarykey = GetNextDictionaryKey(dictionarykey);
            }

            if (exists)
            {
                return entry.Value;
            }

            if (immutableKey.IsDefault)
            {
                immutableKey = key.ToImmutableArray();
            }
            _dictionary.Add(dictionarykey, new(immutableKey, value));
            return value;
        }
#endif

        public BlobDictionary(int capacity = 0)
        {
            _dictionary = new(capacity);
        }

        public int Count => _dictionary.Count;

        public IEnumerator<KeyValuePair<int, KeyValuePair<ImmutableArray<byte>, TValue>>> GetEnumerator() =>
            _dictionary.GetEnumerator();

        public TValue GetOrAdd(ReadOnlySpan<byte> key, TValue value, out bool exists) =>
            GetOrAdd(key, default, value, out exists);

        // If we are given an immutable array, do not allocate a new one.
        public TValue GetOrAdd(ImmutableArray<byte> key, TValue value, out bool exists) =>
            GetOrAdd(key.AsSpan(), key, value, out exists);
    }
}
