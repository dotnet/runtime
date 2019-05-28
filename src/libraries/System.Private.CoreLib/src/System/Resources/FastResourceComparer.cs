// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: A collection of quick methods for comparing 
**          resource keys (strings)
**
** 
===========================================================*/

#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Resources
{
    internal sealed class FastResourceComparer : IComparer, IEqualityComparer, IComparer<string?>, IEqualityComparer<string?>
    {
        internal static readonly FastResourceComparer Default = new FastResourceComparer();

        // Implements IHashCodeProvider too, due to Hashtable requirements.
        public int GetHashCode(object key)
        {
            string s = (string)key;
            return FastResourceComparer.HashFunction(s);
        }

        public int GetHashCode([DisallowNull] string? key)
        {
            return FastResourceComparer.HashFunction(key!);
        }

        // This hash function MUST be publically documented with the resource
        // file format, AND we may NEVER change this hash function's return 
        // value (without changing the file format).
        internal static int HashFunction(string key)
        {
            // Never change this hash function.  We must standardize it so that 
            // others can read & write our .resources files.  Additionally, we
            // have a copy of it in InternalResGen as well.
            uint hash = 5381;
            for (int i = 0; i < key.Length; i++)
                hash = ((hash << 5) + hash) ^ key[i];
            return (int)hash;
        }

        // Compares Strings quickly in a case-sensitive way
        public int Compare(object? a, object? b)
        {
            if (a == b) return 0;
            string? sa = (string?)a;
            string? sb = (string?)b;
            return string.CompareOrdinal(sa, sb);
        }

        public int Compare(string? a, string? b)
        {
            return string.CompareOrdinal(a, b);
        }

        public bool Equals(string? a, string? b)
        {
            return string.Equals(a, b);
        }

        public new bool Equals(object? a, object? b)
        {
            if (a == b) return true;
            string? sa = (string?)a;
            string? sb = (string?)b;
            return string.Equals(sa, sb);
        }

        // Input is one string to compare with, and a byte[] containing chars in 
        // little endian unicode.  Pass in the number of valid chars.
        public static unsafe int CompareOrdinal(string a, byte[] bytes, int bCharLength)
        {
            Debug.Assert(a != null && bytes != null, "FastResourceComparer::CompareOrdinal must have non-null params");
            Debug.Assert(bCharLength * 2 <= bytes.Length, "FastResourceComparer::CompareOrdinal - numChars is too big!");
            // This is a managed version of strcmp, but I can't take advantage
            // of a terminating 0, unlike strcmp in C.
            int i = 0;
            int r = 0;
            // Compare the min length # of characters, then return length diffs.
            int numChars = a.Length;
            if (numChars > bCharLength)
                numChars = bCharLength;
            if (bCharLength == 0)   // Can't use fixed on a 0-element array.
                return (a.Length == 0) ? 0 : -1;
            fixed (byte* pb = bytes)
            {
                byte* pChar = pb;
                while (i < numChars && r == 0)
                {
                    // little endian format
                    int b = pChar[0] | pChar[1] << 8;
                    r = a[i++] - b;
                    pChar += sizeof(char);
                }
            }
            if (r != 0) return r;
            return a.Length - bCharLength;
        }

        public static int CompareOrdinal(byte[] bytes, int aCharLength, string b)
        {
            return -CompareOrdinal(b, bytes, aCharLength);
        }

        // This method is to handle potentially misaligned data accesses.
        // The byte* must point to little endian Unicode characters.
        internal static unsafe int CompareOrdinal(byte* a, int byteLen, string b)
        {
            Debug.Assert((byteLen & 1) == 0, "CompareOrdinal is expecting a UTF-16 string length, which must be even!");
            Debug.Assert(a != null && b != null, "Null args not allowed.");
            Debug.Assert(byteLen >= 0, "byteLen must be non-negative.");

            int r = 0;
            int i = 0;
            // Compare the min length # of characters, then return length diffs.
            int numChars = byteLen >> 1;
            if (numChars > b.Length)
                numChars = b.Length;
            while (i < numChars && r == 0)
            {
                // Must compare character by character, not byte by byte.
                char aCh = (char)(*a++ | (*a++ << 8));
                r = aCh - b[i++];
            }
            if (r != 0) return r;
            return byteLen - b.Length * 2;
        }
    }
}

