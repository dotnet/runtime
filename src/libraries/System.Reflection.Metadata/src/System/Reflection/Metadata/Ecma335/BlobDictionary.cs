// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Internal;
#if NET
using System.Runtime.InteropServices;
#endif

namespace System.Reflection.Metadata.Ecma335
{
    [DebuggerDisplay("Count = {Count}")]
    internal readonly struct BlobDictionary(BlobBuilder builder, int capacity = 0)
    {
#if NET
        private readonly Dictionary<BlobBuilder.Segment, BlobHandle> _dictionary = new(capacity, new Comparer(builder));

        public BlobHandle GetOrAdd<T>(T key, BlobHandle value) where T : notnull, allows ref struct
        {
            // Always use the alternate lookup; we do not support directly adding segments.
            ref BlobHandle entry =
                ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary.GetAlternateLookup<T>(), key, out bool exists);

            if (!exists)
            {
                entry = value;
            }

            return entry;
        }

        private sealed class Comparer(BlobBuilder builder) : IEqualityComparer<BlobBuilder.Segment>, IAlternateEqualityComparer<ReadOnlySpan<byte>, BlobBuilder.Segment>, IAlternateEqualityComparer<BlobBuilder, BlobBuilder.Segment>
        {
            public BlobBuilder.Segment Create(ReadOnlySpan<byte> alternate) => builder.WriteSegment(alternate, prependCompressedSize: true);
            public BlobBuilder.Segment Create(BlobBuilder alternate) => builder.WriteSegment(alternate, prependCompressedSize: true);

            public bool Equals(BlobBuilder.Segment x, BlobBuilder.Segment y) => x.ContentEquals(y);
            public bool Equals(ReadOnlySpan<byte> alternate, BlobBuilder.Segment other) => other.ContentEquals(alternate);
            public bool Equals(BlobBuilder alternate, BlobBuilder.Segment other) => other.ContentEquals(alternate);
            public int GetHashCode(BlobBuilder.Segment obj) => obj.GetContentFNVHashCode();
            public int GetHashCode(ReadOnlySpan<byte> alternate) => Hash.GetFNVHashCode(alternate);
            public int GetHashCode(BlobBuilder alternate) => alternate.GetContentFNVHashCode();
        }
#else
        private readonly BlobBuilder _builder = builder;

        private readonly Dictionary<int, KeyValuePair<BlobBuilder.Segment, BlobHandle>> _dictionary = new(capacity);

        // A simple LCG. Constants taken from
        // https://github.com/imneme/pcg-c/blob/83252d9c23df9c82ecb42210afed61a7b42402d7/include/pcg_variants.h#L276-L284
        private static int GetNextDictionaryKey(int dictionaryKey) =>
            (int)((uint)dictionaryKey * 747796405 + 2891336453);

        public BlobHandle GetOrAdd(BlobBuilder key, BlobHandle value)
        {
            int dictionaryKey = key.GetContentFNVHashCode();
            KeyValuePair<BlobBuilder.Segment, BlobHandle> entry;
            bool exists;
            while (true)
            {
                if (!(exists = _dictionary.TryGetValue(dictionaryKey, out entry))
                    || entry.Key.ContentEquals(key))
                {
                    break;
                }
                dictionaryKey = GetNextDictionaryKey(dictionaryKey);
            }

            if (exists)
            {
                return entry.Value;
            }

            _dictionary.Add(dictionaryKey, new(_builder.WriteSegment(key, prependCompressedSize: true), value));
            return value;
        }

        public BlobHandle GetOrAdd(ReadOnlySpan<byte> key, BlobHandle value)
        {
            int dictionaryKey = Hash.GetFNVHashCode(key);
            KeyValuePair<BlobBuilder.Segment, BlobHandle> entry;
            bool exists;
            while (true)
            {
                if (!(exists = _dictionary.TryGetValue(dictionaryKey, out entry))
                    || entry.Key.ContentEquals(key))
                {
                    break;
                }
                dictionaryKey = GetNextDictionaryKey(dictionaryKey);
            }

            if (exists)
            {
                return entry.Value;
            }

            _dictionary.Add(dictionaryKey, new(_builder.WriteSegment(key, prependCompressedSize: true), value));
            return value;
        }
#endif

        public int Count => _dictionary.Count;

        public void Clear() => _dictionary.Clear();
    }
}
