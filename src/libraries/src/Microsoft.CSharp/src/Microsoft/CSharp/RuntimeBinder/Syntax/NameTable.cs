// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal class NameTable
    {
        private class Entry
        {
            internal readonly Name name;
            internal readonly int hashCode;
            internal Entry next;

            internal Entry(Name name, int hashCode, Entry next)
            {
                this.name = name;
                this.hashCode = hashCode;
                this.next = next;
            }
        }

        private Entry[] _entries;
        private int _count;
        private int _mask;
        private readonly int _hashCodeRandomizer;

        internal NameTable()
        {
            _mask = 31;
            _entries = new Entry[_mask + 1];
            //hashCodeRandomizer = Environment.TickCount;
            _hashCodeRandomizer = 0;
        }

        public Name Add(string key)
        {
            int hashCode = ComputeHashCode(key);
            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && e.name.Text.Equals(key))
                {
                    return e.name;
                }
            }
            return AddEntry(new Name(key), hashCode);
        }

        internal void Add(Name name)
        {
            int hashCode = ComputeHashCode(name.Text);
            // make sure it doesn't already exist
            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && e.name.Text.Equals(name.Text))
                {
                    throw Error.InternalCompilerError();
                }
            }
            AddEntry(name, hashCode);
        }

        public Name Lookup(string key)
        {
            int hashCode = ComputeHashCode(key);
            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && e.name.Text.Equals(key))
                {
                    return e.name;
                }
            }
            return null;
        }

        private int ComputeHashCode(string key)
        {
            int len = key.Length;
            int hashCode = len + _hashCodeRandomizer;
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

        private Name AddEntry(Name name, int hashCode)
        {
            int index = hashCode & _mask;
            Entry e = new Entry(name, hashCode, _entries[index]);
            _entries[index] = e;
            if (_count++ == _mask)
            {
                Grow();
            }
            return e.name;
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
                    int newIndex = e.hashCode & newMask;
                    Entry tmp = e.next;
                    e.next = newEntries[newIndex];
                    newEntries[newIndex] = e;
                    e = tmp;
                }
            }

            _entries = newEntries;
            _mask = newMask;
        }
    }
}
