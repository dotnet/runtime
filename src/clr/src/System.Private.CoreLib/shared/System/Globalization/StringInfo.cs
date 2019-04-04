// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    /// <summary>
    /// This class defines behaviors specific to a writing system.
    /// A writing system is the collection of scripts and orthographic rules
    /// required to represent a language as text.
    /// </summary>
    public class StringInfo
    {
        private string _str = null!; // initialized in helper called by ctors

        private int[]? _indexes;

        public StringInfo() : this(string.Empty)
        {
        }

        public StringInfo(string value)
        {
            this.String = value;
        }

        public override bool Equals(object? value)
        {
            return value is StringInfo otherStringInfo
                && _str.Equals(otherStringInfo._str);
        }

        public override int GetHashCode() => _str.GetHashCode();

        /// <summary>
        /// Our zero-based array of index values into the string. Initialize if
        /// our private array is not yet, in fact, initialized.
        /// </summary>
        private int[]? Indexes
        {
            get
            {
                if (_indexes == null && String.Length > 0)
                {
                    _indexes = StringInfo.ParseCombiningCharacters(String);
                }

                return _indexes;
            }
        }

        public string String
        {
            get => _str;
            set
            {
                _str = value ?? throw new ArgumentNullException(nameof(value));
                _indexes = null;
            }
        }

        public int LengthInTextElements => Indexes?.Length ?? 0;

        public string SubstringByTextElements(int startingTextElement)
        {
            // If the string is empty, no sense going further.
            if (Indexes == null)
            {
                if (startingTextElement < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startingTextElement), startingTextElement, SR.ArgumentOutOfRange_NeedPosNum);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(startingTextElement), startingTextElement, SR.Arg_ArgumentOutOfRangeException);
                }
            }

            return SubstringByTextElements(startingTextElement, Indexes.Length - startingTextElement);
        }

        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements)
        {
            if (startingTextElement < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTextElement), startingTextElement, SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (String.Length == 0 || startingTextElement >= Indexes!.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTextElement), startingTextElement, SR.Arg_ArgumentOutOfRangeException);
            }
            if (lengthInTextElements < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInTextElements), lengthInTextElements, SR.ArgumentOutOfRange_NeedPosNum);
            }
            if (startingTextElement > Indexes.Length - lengthInTextElements)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInTextElements), lengthInTextElements, SR.Arg_ArgumentOutOfRangeException);
            }

            int start = Indexes[startingTextElement];

            if (startingTextElement + lengthInTextElements == Indexes.Length)
            {
                // We are at the last text element in the string and because of that
                // must handle the call differently.
                return String.Substring(start);
            }
            else
            {
                return String.Substring(start, Indexes[lengthInTextElements + startingTextElement] - start);
            }
        }

        public static string GetNextTextElement(string str) => GetNextTextElement(str, 0);

        /// <summary>
        /// Get the code point count of the current text element.
        ///
        /// A combining class is defined as:
        ///      A character/surrogate that has the following Unicode category:
        ///      * NonSpacingMark (e.g. U+0300 COMBINING GRAVE ACCENT)
        ///      * SpacingCombiningMark (e.g. U+ 0903 DEVANGARI SIGN VISARGA)
        ///      * EnclosingMark (e.g. U+20DD COMBINING ENCLOSING CIRCLE)
        ///
        /// In the context of GetNextTextElement() and ParseCombiningCharacters(), a text element is defined as:
        ///  1. If a character/surrogate is in the following category, it is a text element.
        ///     It can NOT further combine with characters in the combinging class to form a text element.
        ///      * one of the Unicode category in the combinging class
        ///      * UnicodeCategory.Format
        ///      * UnicodeCateogry.Control
        ///      * UnicodeCategory.OtherNotAssigned
        ///  2. Otherwise, the character/surrogate can be combined with characters in the combinging class to form a text element.
        /// </summary>
        /// <returns>The length of the current text element</returns>
        internal static int GetCurrentTextElementLen(string str, int index, int len, ref UnicodeCategory ucCurrent, ref int currentCharCount)
        {
            Debug.Assert(index >= 0 && len >= 0, "StringInfo.GetCurrentTextElementLen() : index = " + index + ", len = " + len);
            Debug.Assert(index < len, "StringInfo.GetCurrentTextElementLen() : index = " + index + ", len = " + len);
            if (index + currentCharCount == len)
            {
                // This is the last character/surrogate in the string.
                return currentCharCount;
            }

            // Call an internal GetUnicodeCategory, which will tell us both the unicode category, and also tell us if it is a surrogate pair or not.
            int nextCharCount;
            UnicodeCategory ucNext = CharUnicodeInfo.InternalGetUnicodeCategory(str, index + currentCharCount, out nextCharCount);
            if (CharUnicodeInfo.IsCombiningCategory(ucNext))
            {
                // The next element is a combining class.
                // Check if the current text element to see if it is a valid base category (i.e. it should not be a combining category,
                // not a format character, and not a control character).
                if (CharUnicodeInfo.IsCombiningCategory(ucCurrent)
                    || (ucCurrent == UnicodeCategory.Format)
                    || (ucCurrent == UnicodeCategory.Control)
                    || (ucCurrent == UnicodeCategory.OtherNotAssigned)
                    || (ucCurrent == UnicodeCategory.Surrogate))    // An unpair high surrogate or low surrogate
                {
                    // Will fall thru and return the currentCharCount
                }
                else
                {
                    // Remember the current index.
                    int startIndex = index;

                    // We have a valid base characters, and we have a character (or surrogate) that is combining.
                    // Check if there are more combining characters to follow.
                    // Check if the next character is a nonspacing character.
                    index += currentCharCount + nextCharCount;

                    while (index < len)
                    {
                        ucNext = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out nextCharCount);
                        if (!CharUnicodeInfo.IsCombiningCategory(ucNext))
                        {
                            ucCurrent = ucNext;
                            currentCharCount = nextCharCount;
                            break;
                        }
                        index += nextCharCount;
                    }

                    return index - startIndex;
                }
            }

            // The return value will be the currentCharCount.
            int ret = currentCharCount;
            ucCurrent = ucNext;
            // Update currentCharCount.
            currentCharCount = nextCharCount;
            return ret;
        }

        /// <summary>
        /// Returns the str containing the next text element in str starting at
        /// index index. If index is not supplied, then it will start at the beginning
        /// of str. It recognizes a base character plus one or more combining
        /// characters or a properly formed surrogate pair as a text element.
        /// See also the ParseCombiningCharacters() and the ParseSurrogates() methods.
        /// </summary>
        public static string GetNextTextElement(string str, int index)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int len = str.Length;
            if (index < 0 || index >= len)
            {
                if (index == len)
                {
                    return string.Empty;
                }

                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            int charLen;
            UnicodeCategory uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            return str.Substring(index, GetCurrentTextElementLen(str, index, len, ref uc, ref charLen));
        }

        public static TextElementEnumerator GetTextElementEnumerator(string str)
        {
            return GetTextElementEnumerator(str, 0);
        }

        public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int len = str.Length;
            if (index < 0 || index > len)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            return new TextElementEnumerator(str, index, len);
        }

        /// <summary>
        /// Returns the indices of each base character or properly formed surrogate
        /// pair  within the str. It recognizes a base character plus one or more
        /// combining characters or a properly formed surrogate pair as a text
        /// element and returns the index of the base character or high surrogate.
        /// Each index is the beginning of a text element within a str. The length
        /// of each element is easily computed as the difference between successive
        /// indices. The length of the array will always be less than or equal to
        /// the length of the str. For example, given the str
        /// \u4f00\u302a\ud800\udc00\u4f01, this method would return the indices:
        /// 0, 2, 4.
        /// </summary>
        public static int[] ParseCombiningCharacters(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int len = str.Length;
            int[] result = new int[len];
            if (len == 0)
            {
                return (result);
            }

            int resultCount = 0;

            int i = 0;
            int currentCharLen;
            UnicodeCategory currentCategory = CharUnicodeInfo.InternalGetUnicodeCategory(str, 0, out currentCharLen);

            while (i < len)
            {
                result[resultCount++] = i;
                i += GetCurrentTextElementLen(str, i, len, ref currentCategory, ref currentCharLen);
            }

            if (resultCount < len)
            {
                int[] returnArray = new int[resultCount];
                Array.Copy(result, 0, returnArray, 0, resultCount);
                return (returnArray);
            }
            return result;
        }
    }
}
