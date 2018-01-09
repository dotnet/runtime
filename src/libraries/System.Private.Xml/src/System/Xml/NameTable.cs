// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Xml
{
    /// <devdoc>
    ///    <para>
    ///       XmlNameTable implemented as a simple hash table.
    ///    </para>
    /// </devdoc>
    public class NameTable : XmlNameTable
    {
        //
        // Private types
        //
        private class Entry
        {
            internal string str;
            internal int hashCode;
            internal Entry next;

            internal Entry(string str, int hashCode, Entry next)
            {
                this.str = str;
                this.hashCode = hashCode;
                this.next = next;
            }
        }

        //
        // Fields
        //
        private Entry[] _entries;
        private int _count;
        private int _mask;

        //
        // Constructor
        /// <devdoc>
        ///      Public constructor.
        /// </devdoc>
        public NameTable()
        {
            _mask = 31;
            _entries = new Entry[_mask + 1];
        }

        //
        // XmlNameTable public methods
        /// <devdoc>
        ///      Add the given string to the NameTable or return
        ///      the existing string if it is already in the NameTable.
        /// </devdoc>
        public override string Add(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }
            int len = key.Length;
            if (len == 0)
            {
                return string.Empty;
            }

            int hashCode = ComputeHash32(key);

            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && e.str.Equals(key))
                {
                    return e.str;
                }
            }
            return AddEntry(key, hashCode);
        }

        /// <devdoc>
        ///      Add the given string to the NameTable or return
        ///      the existing string if it is already in the NameTable.
        /// </devdoc>
        public override string Add(char[] key, int start, int len)
        {
            if (len == 0)
            {
                return string.Empty;
            }

            // Compatibility check to ensure same exception as previous versions
            // independently of any exceptions throw by the hashing function.
            // note that NullReferenceException is the first one if key is null.
            if (start >= key.Length || start < 0 || (long)start + len > (long)key.Length)
            {
                throw new IndexOutOfRangeException();
            }

            // Compatibility check for len < 0, just throw the same exception as new string(key, start, len)
            if (len < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            int hashCode = ComputeHash32(key, start, len);

            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && TextEquals(e.str, key, start, len))
                {
                    return e.str;
                }
            }
            return AddEntry(new string(key, start, len), hashCode);
        }

        /// <devdoc>
        ///      Find the matching string in the NameTable.
        /// </devdoc>
        public override string Get(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value.Length == 0)
            {
                return string.Empty;
            }

            int hashCode = ComputeHash32(value);

            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && e.str.Equals(value))
                {
                    return e.str;
                }
            }
            return null;
        }

        /// <devdoc>
        ///      Find the matching string atom given a range of
        ///      characters.
        /// </devdoc>
        public override string Get(char[] key, int start, int len)
        {
            if (len == 0)
            {
                return string.Empty;
            }

            if (start >= key.Length || start < 0 || (long)start + len > (long)key.Length)
            {
                throw new IndexOutOfRangeException();
            }

            // Compatibility check for len < 0, just return null
            if (len < 0)
            {
                return null;
            }

            int hashCode = ComputeHash32(key, start, len);

            for (Entry e = _entries[hashCode & _mask]; e != null; e = e.next)
            {
                if (e.hashCode == hashCode && TextEquals(e.str, key, start, len))
                {
                    return e.str;
                }
            }
            return null;
        }

        //
        // Private methods
        //

        private string AddEntry(string str, int hashCode)
        {
            int index = hashCode & _mask;
            Entry e = new Entry(str, hashCode, _entries[index]);
            _entries[index] = e;
            if (_count++ == _mask)
            {
                Grow();
            }
            return e.str;
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

        private static bool TextEquals(string str1, char[] str2, int str2Start, int str2Length)
        {
            if (str1.Length != str2Length)
            {
                return false;
            }
            // use array.Length to eliminate the range check
            for (int i = 0; i < str1.Length; i++)
            {
                if (str1[i] != str2[str2Start + i])
                {
                    return false;
                }
            }
            return true;
        }

        private static int ComputeHash32(string key)
        {
            ReadOnlySpan<byte> bytes = key.AsReadOnlySpan().AsBytes();
            return Marvin.ComputeHash32(bytes, Marvin.DefaultSeed);
        }

        private static int ComputeHash32(char[] key, int start, int len)
        {
            ReadOnlySpan<byte> bytes = key.AsReadOnlySpan().Slice(start, len).AsBytes();
            return Marvin.ComputeHash32(bytes, Marvin.DefaultSeed);
        }
    }
}
