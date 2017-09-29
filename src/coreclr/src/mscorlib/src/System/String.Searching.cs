// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class String
    {
        public bool Contains(string value)
        {
            return (IndexOf(value, StringComparison.Ordinal) >= 0);
        }

        public bool Contains(string value, StringComparison comparisonType)
        {
            return (IndexOf(value, comparisonType) >= 0);
        }

        // Returns the index of the first occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs thorough the next count characters.
        //
        public int IndexOf(char value)
        {
            return IndexOf(value, 0, this.Length);
        }

        public int IndexOf(char value, int startIndex)
        {
            return IndexOf(value, startIndex, this.Length - startIndex);
        }

        public unsafe int IndexOf(char value, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (count < 0 || count > Length - startIndex)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            fixed (char* pChars = &_firstChar)
            {
                char* pCh = pChars + startIndex;

                while (count >= 4)
                {
                    if (*pCh == value) goto ReturnIndex;
                    if (*(pCh + 1) == value) goto ReturnIndex1;
                    if (*(pCh + 2) == value) goto ReturnIndex2;
                    if (*(pCh + 3) == value) goto ReturnIndex3;

                    count -= 4;
                    pCh += 4;
                }

                while (count > 0)
                {
                    if (*pCh == value)
                        goto ReturnIndex;

                    count--;
                    pCh++;
                }

                return -1;

            ReturnIndex3: pCh++;
            ReturnIndex2: pCh++;
            ReturnIndex1: pCh++;
            ReturnIndex:
                return (int)(pCh - pChars);
            }
        }

        // Returns the index of the first occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs to startIndex + count -1.
        //
        public int IndexOfAny(char[] anyOf)
        {
            return IndexOfAny(anyOf, 0, this.Length);
        }

        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            return IndexOfAny(anyOf, startIndex, this.Length - startIndex);
        }

        public int IndexOfAny(char[] anyOf, int startIndex, int count)
        {
            if (anyOf == null)
            {
                throw new ArgumentNullException(nameof(anyOf));
            }

            if ((uint)startIndex > (uint)Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            if ((uint)count > (uint)(Length - startIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            if (anyOf.Length == 2)
            {
                // Very common optimization for directory separators (/, \), quotes (", '), brackets, etc
                return IndexOfAny(anyOf[0], anyOf[1], startIndex, count);
            }
            else if (anyOf.Length == 3)
            {
                return IndexOfAny(anyOf[0], anyOf[1], anyOf[2], startIndex, count);
            }
            else if (anyOf.Length > 3)
            {
                return IndexOfCharArray(anyOf, startIndex, count);
            }
            else if (anyOf.Length == 1)
            {
                return IndexOf(anyOf[0], startIndex, count);
            }
            else // anyOf.Length == 0
            {
                return -1;
            }
        }

        private unsafe int IndexOfAny(char value1, char value2, int startIndex, int count)
        {
            fixed (char* pChars = &_firstChar)
            {
                char* pCh = pChars + startIndex;

                while (count > 0)
                {
                    char c = *pCh;

                    if (c == value1 || c == value2)
                        return (int)(pCh - pChars);

                    // Possibly reads outside of count and can include null terminator
                    // Handled in the return logic
                    c = *(pCh + 1);

                    if (c == value1 || c == value2)
                        return (count == 1 ? -1 : (int)(pCh - pChars) + 1);

                    pCh += 2;
                    count -= 2;
                }

                return -1;
            }
        }

        private unsafe int IndexOfAny(char value1, char value2, char value3, int startIndex, int count)
        {
            fixed (char* pChars = &_firstChar)
            {
                char* pCh = pChars + startIndex;

                while (count > 0)
                {
                    char c = *pCh;

                    if (c == value1 || c == value2 || c == value3)
                        return (int)(pCh - pChars);

                    pCh++;
                    count--;
                }

                return -1;
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int IndexOfCharArray(char[] anyOf, int startIndex, int count);


        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // the first character of this string, it is case-sensitive and the current culture
        // comparison is used.
        //
        public int IndexOf(String value)
        {
            return IndexOf(value, StringComparison.CurrentCulture);
        }

        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // startIndex, it is case-sensitive and the current culture comparison is used.
        //
        public int IndexOf(String value, int startIndex)
        {
            return IndexOf(value, startIndex, StringComparison.CurrentCulture);
        }

        // Determines the position within this string of the first occurrence of the specified
        // string, according to the specified search criteria.  The search begins at
        // startIndex, ends at endIndex and the current culture comparison is used.
        //
        public int IndexOf(String value, int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > this.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            }

            if (count < 0 || count > this.Length - startIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            return IndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int IndexOf(String value, StringComparison comparisonType)
        {
            return IndexOf(value, 0, this.Length, comparisonType);
        }

        public int IndexOf(String value, int startIndex, StringComparison comparisonType)
        {
            return IndexOf(value, startIndex, this.Length - startIndex, comparisonType);
        }

        public int IndexOf(String value, int startIndex, int count, StringComparison comparisonType)
        {
            // Validate inputs
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (count < 0 || startIndex > this.Length - count)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.Ordinal);

                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.IndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);

                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }

        // Returns the index of the last occurrence of a specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        public int LastIndexOf(char value)
        {
            return LastIndexOf(value, this.Length - 1, this.Length);
        }

        public int LastIndexOf(char value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1);
        }

        public unsafe int LastIndexOf(char value, int startIndex, int count)
        {
            if (Length == 0)
                return -1;

            if (startIndex < 0 || startIndex >= Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            if (count < 0 || count - 1 > startIndex)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);

            fixed (char* pChars = &_firstChar)
            {
                char* pCh = pChars + startIndex;

                //We search [startIndex..EndIndex]
                while (count >= 4)
                {
                    if (*pCh == value) goto ReturnIndex;
                    if (*(pCh - 1) == value) goto ReturnIndex1;
                    if (*(pCh - 2) == value) goto ReturnIndex2;
                    if (*(pCh - 3) == value) goto ReturnIndex3;

                    count -= 4;
                    pCh -= 4;
                }

                while (count > 0)
                {
                    if (*pCh == value)
                        goto ReturnIndex;

                    count--;
                    pCh--;
                }

                return -1;

            ReturnIndex3: pCh--;
            ReturnIndex2: pCh--;
            ReturnIndex1: pCh--;
            ReturnIndex:
                return (int)(pCh - pChars);
            }
        }

        // Returns the index of the last occurrence of any specified character in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //

        //ForceInline ... Jit can't recognize String.get_Length to determine that this is "fluff"
        public int LastIndexOfAny(char[] anyOf)
        {
            return LastIndexOfAny(anyOf, this.Length - 1, this.Length);
        }

        public int LastIndexOfAny(char[] anyOf, int startIndex)
        {
            return LastIndexOfAny(anyOf, startIndex, startIndex + 1);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int LastIndexOfAny(char[] anyOf, int startIndex, int count);


        // Returns the index of the last occurrence of any character in value in the current instance.
        // The search starts at startIndex and runs backwards to startIndex - count + 1.
        // The character at position startIndex is included in the search.  startIndex is the larger
        // index within the string.
        //
        public int LastIndexOf(String value)
        {
            return LastIndexOf(value, this.Length - 1, this.Length, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, int startIndex)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, int startIndex, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            }

            return LastIndexOf(value, startIndex, count, StringComparison.CurrentCulture);
        }

        public int LastIndexOf(String value, StringComparison comparisonType)
        {
            return LastIndexOf(value, this.Length - 1, this.Length, comparisonType);
        }

        public int LastIndexOf(String value, int startIndex, StringComparison comparisonType)
        {
            return LastIndexOf(value, startIndex, startIndex + 1, comparisonType);
        }

        public int LastIndexOf(String value, int startIndex, int count, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Special case for 0 length input strings
            if (this.Length == 0 && (startIndex == -1 || startIndex == 0))
                return (value.Length == 0) ? 0 : -1;

            // Now after handling empty strings, make sure we're not out of range
            if (startIndex < 0 || startIndex > this.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);

            // Make sure that we allow startIndex == this.Length
            if (startIndex == this.Length)
            {
                startIndex--;
                if (count > 0)
                    count--;

                // If we are looking for nothing, just return 0
                if (value.Length == 0 && count >= 0 && startIndex - count + 1 >= 0)
                    return startIndex;
            }

            // 2nd half of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);


            switch (comparisonType)
            {
                case StringComparison.CurrentCulture:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.CurrentCultureIgnoreCase:
                    return CultureInfo.CurrentCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);

                case StringComparison.InvariantCulture:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.None);

                case StringComparison.InvariantCultureIgnoreCase:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                case StringComparison.Ordinal:
                    return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.Ordinal);

                case StringComparison.OrdinalIgnoreCase:
                    if (value.IsAscii() && this.IsAscii())
                        return CultureInfo.InvariantCulture.CompareInfo.LastIndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    else
                        return TextInfo.LastIndexOfStringOrdinalIgnoreCase(this, value, startIndex, count);
                default:
                    throw new ArgumentException(SR.NotSupported_StringComparison, nameof(comparisonType));
            }
        }
    }
}
