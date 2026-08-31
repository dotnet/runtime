// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Debug = System.Diagnostics.Debug;

using Internal.NativeFormat;

namespace Internal.TypeSystem.Ecma
{
    /// <summary>
    /// MetadataReader string decoder that caches and reuses strings rather than allocating
    /// on each call to MetadataReader.GetString(handle).
    /// Safe to use from multiple threads and lock free.
    /// </summary>
    public sealed class CachingMetadataStringDecoder : MetadataStringDecoder
    {
        private struct Entry
        {
            // hash code of the entry
            public int HashCode;

            // full text of the item
            public string Text;
        }

        // TODO: Tune the bucket size
        private const int BucketSize = 4;

        // The table of cached entries. The size of the table has to be power of 2.
        private Entry[] _table;

        // The next candidate in the bucket range for eviction
        private int _evictionHint;

        public CachingMetadataStringDecoder(int size)
            : base(System.Text.Encoding.UTF8)
        {
            Debug.Assert((size & (size - 1)) == 0, "The cache size must be power of 2");

            _table = new Entry[size];
        }

        private string Find(int hashCode, string s)
        {
            var arr = _table;
            int mask = _table.Length - 1;

            int idx = hashCode & mask;

            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < BucketSize + 1; i++)
            {
                string e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e == null)
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                if (hash == hashCode && s == e)
                {
                    return e;
                }

                idx = (idx + i) & mask;
            }
            return null;
        }

        private unsafe string FindASCII(int hashCode, byte* bytes, int byteCount)
        {
            var arr = _table;
            int mask = _table.Length - 1;

            int idx = hashCode & mask;

            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < BucketSize + 1; i++)
            {
                string e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e == null)
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                if (hash == hashCode && TextEqualsASCII(e, bytes, byteCount))
                {
                    return e;
                }

                idx = (idx + i) & mask;
            }
            return null;
        }

        private static unsafe bool TextEqualsASCII(string text, byte* ascii, int length)
        {
#if DEBUG
            for (var i = 0; i < length; i++)
            {
                Debug.Assert((ascii[i] & 0x80) == 0, "The byte* input to this method must be valid ASCII.");
            }
#endif

            if (length != text.Length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (ascii[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        private string Add(int hashCode, string s)
        {
            var arr = _table;
            int mask = _table.Length - 1;

            int idx = hashCode & mask;

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < BucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & BucketSize;
            }

            // or pick a victim within the bucket range
            // and replace with new entry
            var i1 = _evictionHint++ & (BucketSize - 1);
            idx = (idx + ((i1 * i1 + i1) / 2)) & mask;

        foundIdx:
            arr[idx].HashCode = hashCode;
            arr[idx].Text = s;

            return s;
        }

        public string Lookup(string s)
        {
            int hashCode = TypeHashingAlgorithms.ComputeNameHashCode(s);

            string existing = Find(hashCode, s);
            if (existing != null)
                return existing;

            return Add(hashCode, s);
        }

        public override unsafe string GetString(byte* bytes, int byteCount)
        {
            bool isAscii;
            int hashCode = TypeHashingAlgorithms.ComputeASCIINameHashCode(bytes, byteCount, out isAscii);

            if (isAscii)
            {
                string existing = FindASCII(hashCode, bytes, byteCount);
                if (existing != null)
                    return existing;
                return Add(hashCode, Encoding.GetString(bytes, byteCount));
            }
            else
            {
                return Lookup(Encoding.GetString(bytes, byteCount));
            }
        }
    }
}
