// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal sealed class NameTable
    {
        private sealed class Entry
        {
            public readonly Name Name;
            public readonly int HashCode;
            public Entry Next;

            public Entry(Name name, int hashCode, Entry next)
            {
                Name = name;
                HashCode = hashCode;
                Next = next;
            }
        }

        private Entry[] _entries;
        private int _count;
        private int _mask;

        internal NameTable()
        {
            _mask = 31;
            _entries = new Entry[_mask + 1];
        }

        public Name Add(string key)
        {
            int hashCode = ComputeHashCode(key);
            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.Next)
            {
                if (e.HashCode == hashCode && e.Name.Text.Equals(key))
                {
                    return e.Name;
                }
            }

            return AddEntry(new Name(key), hashCode);
        }

        public Name Add(string key, int length)
        {
            int hashCode = ComputeHashCode(key, length);
            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.Next)
            {
                if (e.HashCode == hashCode && Equals(e.Name.Text, key, length))
                {
                    return e.Name;
                }
            }

            return AddEntry(new Name(key.Substring(0, length)), hashCode);
        }

        internal void Add(Name name)
        {
            int hashCode = ComputeHashCode(name.Text);
            // make sure it doesn't already exist
            Debug.Assert(_entries.All(e => e?.Name.Text != name.Text));

            AddEntry(name, hashCode);
        }

        private static int ComputeHashCode(string key)
        {
            unchecked
            {
                int hashCode = key.Length;
                // use key.Length to eliminate the range check
                for (int i = 0; i < key.Length; i++)
                {
                    hashCode += (hashCode << 7) ^ key[i];
                }

                // mix it a bit more
                hashCode -= hashCode >> 17;
                hashCode -= hashCode >> 11;
                hashCode -= hashCode >> 5;

                return hashCode;
            }
        }

        private static int ComputeHashCode(string key, int length)
        {
            Debug.Assert(key != null);
            Debug.Assert(length <= key.Length);
            unchecked
            {
                int hashCode = length;
                for (int i = 0; i < length; i++)
                {
                    hashCode += (hashCode << 7) ^ key[i];
                }

                // mix it a bit more
                hashCode -= hashCode >> 17;
                hashCode -= hashCode >> 11;
                hashCode -= hashCode >> 5;

                return hashCode;
            }
        }

        private static bool Equals(string candidate, string key, int length)
        {
            Debug.Assert(candidate != null);
            Debug.Assert(key != null);
            Debug.Assert(length <= key.Length);
            if (candidate.Length != length)
            {
                return false;
            }

            for (int i = 0; i < candidate.Length; i++)
            {
                if (candidate[i] != key[i])
                {
                    return false;
                }
            }

            return true;
        }

        private Name AddEntry(Name name, int hashCode)
        {
            int index = hashCode & _mask;
            Entry e = new Entry(name, hashCode, _entries[index]);
            _entries[index] = e;
            if (_count++ == _mask)
            {
                Grow();
            }

            return e.Name;
        }

        private void Grow()
        {
            int newMask = _mask * 2 + 1;
            Entry[] oldEntries = _entries;
            Entry[] newEntries = new Entry[newMask + 1];

            // use oldEntries.Length to eliminate the range check            
            for (int i = 0; i < oldEntries.Length; i++)
            {
                Entry e = oldEntries[i];
                while (e != null)
                {
                    int newIndex = e.HashCode & newMask;
                    Entry tmp = e.Next;
                    e.Next = newEntries[newIndex];
                    newEntries[newIndex] = e;
                    e = tmp;
                }
            }

            _entries = newEntries;
            _mask = newMask;
        }
    }
}
