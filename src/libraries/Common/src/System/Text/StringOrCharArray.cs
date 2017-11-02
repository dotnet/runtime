// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text
{
    /// <summary>
    /// Discriminated union of a string and a char array/offset/count.  Enables looking up
    /// a portion of a char array as a key in a dictionary of string keys without having to
    /// allocate/copy the chars into a new string.  This comes at the expense of an extra
    /// reference field + two Int32s per key in the size of the dictionary's Entry array.
    /// </summary>
    internal readonly struct StringOrCharArray : IEquatable<StringOrCharArray>
    {
        public readonly string String;

        public readonly char[] CharArray;
        public readonly int CharArrayOffset;
        public readonly int CharArrayCount;

        public StringOrCharArray(string s)
        {
            String = s;

            CharArray = null;
            CharArrayOffset = 0;
            CharArrayCount = 0;

            DebugValidate();
        }

        public StringOrCharArray(char[] charArray, int charArrayIndex, int charArrayOffset)
        {
            String = null;

            CharArray = charArray;
            CharArrayOffset = charArrayIndex;
            CharArrayCount = charArrayOffset;

            DebugValidate();
        }

        public static implicit operator StringOrCharArray(string value)
        {
            return new StringOrCharArray(value);
        }

        public int Length
        {
            get
            {
                DebugValidate();
                return String != null ? String.Length : CharArrayCount;
            }
        }

        public override bool Equals(object obj)
        {
            return
                obj is StringOrCharArray &&
                Equals((StringOrCharArray)obj);
        }

        public unsafe bool Equals(StringOrCharArray other)
        {
            this.DebugValidate();
            other.DebugValidate();

            if (this.String != null)
            {
                // String vs String
                if (other.String != null)
                {
                    return StringComparer.Ordinal.Equals(this.String, other.String);
                }

                // String vs CharArray
                if (this.String.Length != other.CharArrayCount)
                    return false;

                for (int i = 0; i < this.String.Length; i++)
                {
                    if (this.String[i] != other.CharArray[other.CharArrayOffset + i])
                        return false;
                }

                return true;
            }

            // CharArray vs CharArray
            if (other.CharArray != null)
            {
                if (this.CharArrayCount != other.CharArrayCount)
                    return false;

                for (int i = 0; i < this.CharArrayCount; i++)
                {
                    if (this.CharArray[this.CharArrayOffset + i] != other.CharArray[other.CharArrayOffset + i])
                        return false;
                }

                return true;
            }

            // CharArray vs String
            if (this.CharArrayCount != other.String.Length)
                return false;

            for (int i = 0; i < this.CharArrayCount; i++)
            {
                if (this.CharArray[this.CharArrayOffset + i] != other.String[i])
                    return false;
            }

            return true;
        }

        public override unsafe int GetHashCode()
        {
            DebugValidate();

            if (String != null)
            {
                fixed (char* s = String)
                {
                    return GetHashCode(s, String.Length);
                }
            }
            else
            {
                fixed (char* s = CharArray)
                {
                    return GetHashCode(s + CharArrayOffset, CharArrayCount);
                }
            }
        }

        private static unsafe int GetHashCode(char* s, int count)
        {
            // This hash code is a simplified version of some of the code in String, 
            // when not using randomized hash codes.  We don't use string's GetHashCode
            // because we need to be able to use the exact same algorithms on a char[].
            // As such, this should not be used anywhere there are concerns around
            // hash-based attacks that would require a better code.

            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;

            for (int i = 0; i < count; ++i)
            {
                int c = *s++;
                hash1 = unchecked((hash1 << 5) + hash1) ^ c;

                if (++i >= count)
                    break;

                c = *s++;
                hash2 = unchecked((hash2 << 5) + hash2) ^ c;
            }

            return unchecked(hash1 + (hash2 * 1566083941));
        }

        [Conditional("DEBUG")]
        private void DebugValidate()
        {
            Debug.Assert((String != null) ^ (CharArray != null));

            if (CharArray != null)
            {
                Debug.Assert(CharArrayCount >= 0);
                Debug.Assert(CharArrayOffset >= 0);
                Debug.Assert(CharArrayOffset <= CharArray.Length - CharArrayCount);
            }
        }
    }
}
