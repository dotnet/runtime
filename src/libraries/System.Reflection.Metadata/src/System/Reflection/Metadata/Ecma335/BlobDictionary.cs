// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Internal;
#if NET
using System.Runtime.InteropServices;
#endif

namespace System.Reflection.Metadata.Ecma335
{
    [DebuggerDisplay("Count = {Count}")]
    internal readonly struct BlobDictionary
    {
        private readonly Dictionary<int, KeyValuePair<ImmutableArray<byte>, BlobHandle>> _dictionary;

        // A simple LCG. Constants taken from
        // https://github.com/imneme/pcg-c/blob/83252d9c23df9c82ecb42210afed61a7b42402d7/include/pcg_variants.h#L276-L284
        private static int GetNextDictionaryKey(int dictionaryKey) =>
            (int)((uint)dictionaryKey * 747796405 + 2891336453);

#if NET
        private unsafe ref KeyValuePair<ImmutableArray<byte>, BlobHandle> GetValueRefOrAddDefault(ReadOnlySpan<byte> key, out bool exists)
        {
            int dictionaryKey = Hash.GetFNVHashCode(key);
            while (true)
            {
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, dictionaryKey, out exists);
                if (!exists || entry.Key.AsSpan().SequenceEqual(key))
                {
#pragma warning disable CS9082 // Local is returned by reference but was initialized to a value that cannot be returned by reference
                    // In .NET 6 the assembly of GetValueRefOrAddDefault was compiled with earlier ref safety rules
                    // and caused an error, which was turned into a warning because of unsafe and was suppressed.
                    return ref entry;
#pragma warning restore CS9082
                }
                dictionaryKey = GetNextDictionaryKey(dictionaryKey);
            }
        }

        public BlobHandle GetOrAdd(ReadOnlySpan<byte> key, ImmutableArray<byte> immutableKey, BlobHandle value, out bool exists)
        {
            ref var entry = ref GetValueRefOrAddDefault(key, out exists);
            if (exists)
            {
                return entry.Value;
            }

            // If we are given an immutable array, do not allocate a new one.
            if (immutableKey.IsDefault)
            {
                immutableKey = key.ToImmutableArray();
            }
            else
            {
                Debug.Assert(immutableKey.AsSpan().SequenceEqual(key));
            }

            entry = new(immutableKey, value);
            return value;
        }
#else
        public BlobHandle GetOrAdd(ReadOnlySpan<byte> key, ImmutableArray<byte> immutableKey, BlobHandle value, out bool exists)
        {
            int dictionarykey = Hash.GetFNVHashCode(key);
            KeyValuePair<ImmutableArray<byte>, BlobHandle> entry;
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

            // If we are given an immutable array, do not allocate a new one.
            if (immutableKey.IsDefault)
            {
                immutableKey = key.ToImmutableArray();
            }
            else
            {
                Debug.Assert(immutableKey.AsSpan().SequenceEqual(key));
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

        public Dictionary<int, KeyValuePair<ImmutableArray<byte>, BlobHandle>>.Enumerator GetEnumerator() =>
            _dictionary.GetEnumerator();
    }
}
