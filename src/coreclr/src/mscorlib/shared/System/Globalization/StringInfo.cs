// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//
//  Purpose:  This class defines behaviors specific to a writing system.
//            A writing system is the collection of scripts and
//            orthographic rules required to represent a language as text.
//
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;

namespace System.Globalization
{
    public class StringInfo
    {
        private string _str;

        private int[] _indexes;

        // Legacy constructor
        public StringInfo() : this("") { }

        // Primary, useful constructor
        public StringInfo(string value)
        {
            this.String = value;
        }

        public override bool Equals(Object value)
        {
            StringInfo that = value as StringInfo;
            if (that != null)
            {
                return (_str.Equals(that._str));
            }
            return (false);
        }

        public override int GetHashCode()
        {
            return _str.GetHashCode();
        }


        // Our zero-based array of index values into the string. Initialize if 
        // our private array is not yet, in fact, initialized.
        private int[] Indexes
        {
            get
            {
                if ((null == _indexes) && (0 < this.String.Length))
                {
                    _indexes = StringInfo.ParseCombiningCharacters(this.String);
                }

                return (_indexes);
            }
        }

        public string String
        {
            get
            {
                return (_str);
            }
            set
            {
                if (null == value)
                {
                    throw new ArgumentNullException(nameof(String),
                        SR.ArgumentNull_String);
                }

                _str = value;
                _indexes = null;
            }
        }

        public int LengthInTextElements
        {
            get
            {
                if (null == this.Indexes)
                {
                    // Indexes not initialized, so assume length zero
                    return (0);
                }

                return (this.Indexes.Length);
            }
        }

        public string SubstringByTextElements(int startingTextElement)
        {
            // If the string is empty, no sense going further. 
            if (null == this.Indexes)
            {
                // Just decide which error to give depending on the param they gave us....
                if (startingTextElement < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(startingTextElement), SR.ArgumentOutOfRange_NeedPosNum);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(startingTextElement), SR.Arg_ArgumentOutOfRangeException);
                }
            }
            return (SubstringByTextElements(startingTextElement, Indexes.Length - startingTextElement));
        }

        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements)
        {
            if (startingTextElement < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTextElement), SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (this.String.Length == 0 || startingTextElement >= Indexes.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startingTextElement), SR.Arg_ArgumentOutOfRangeException);
            }

            if (lengthInTextElements < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInTextElements), SR.ArgumentOutOfRange_NeedPosNum);
            }

            if (startingTextElement > Indexes.Length - lengthInTextElements)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthInTextElements), SR.Arg_ArgumentOutOfRangeException);
            }

            int start = Indexes[startingTextElement];

            if (startingTextElement + lengthInTextElements == Indexes.Length)
            {
                // We are at the last text element in the string and because of that
                // must handle the call differently.
                return (this.String.Substring(start));
            }
            else
            {
                return (this.String.Substring(start, (Indexes[lengthInTextElements + startingTextElement] - start)));
            }
        }

        public static string GetNextTextElement(string str)
        {
            return (GetNextTextElement(str, 0));
        }


        ////////////////////////////////////////////////////////////////////////
        //
        // Get the code point count of the current text element.
        //
        // A combining class is defined as:
        //      A character/surrogate that has the following Unicode category:
        //      * NonSpacingMark (e.g. U+0300 COMBINING GRAVE ACCENT)
        //      * SpacingCombiningMark (e.g. U+ 0903 DEVANGARI SIGN VISARGA)
        //      * EnclosingMark (e.g. U+20DD COMBINING ENCLOSING CIRCLE)
        //
        // In the context of GetNextTextElement() and ParseCombiningCharacters(), a text element is defined as:
        //
        //  1. If a character/surrogate is in the following category, it is a text element.  
        //     It can NOT further combine with characters in the combinging class to form a text element.
        //      * one of the Unicode category in the combinging class
        //      * UnicodeCategory.Format
        //      * UnicodeCateogry.Control
        //      * UnicodeCategory.OtherNotAssigned
        //  2. Otherwise, the character/surrogate can be combined with characters in the combinging class to form a text element.
        //
        //  Return:
        //      The length of the current text element
        //
        //  Parameters:
        //      String str
        //      index   The starting index
        //      len     The total length of str (to define the upper boundary)
        //      ucCurrent   The Unicode category pointed by Index.  It will be updated to the uc of next character if this is not the last text element.
        //      currentCharCount    The char count of an abstract char pointed by Index.  It will be updated to the char count of next abstract character if this is not the last text element.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static int GetCurrentTextElementLen(string str, int index, int len, ref UnicodeCategory ucCurrent, ref int currentCharCount)
        {
            Debug.Assert(index >= 0 && len >= 0, "StringInfo.GetCurrentTextElementLen() : index = " + index + ", len = " + len);
            Debug.Assert(index < len, "StringInfo.GetCurrentTextElementLen() : index = " + index + ", len = " + len);
            if (index + currentCharCount == len)
            {
                // This is the last character/surrogate in the string.
                return (currentCharCount);
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
                    int startIndex = index; // Remember the current index.

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
                    return (index - startIndex);
                }
            }
            // The return value will be the currentCharCount.
            int ret = currentCharCount;
            ucCurrent = ucNext;
            // Update currentCharCount.
            currentCharCount = nextCharCount;
            return (ret);
        }

        // Returns the str containing the next text element in str starting at 
        // index index.  If index is not supplied, then it will start at the beginning 
        // of str.  It recognizes a base character plus one or more combining 
        // characters or a properly formed surrogate pair as a text element.  See also 
        // the ParseCombiningCharacters() and the ParseSurrogates() methods.
        public static string GetNextTextElement(string str, int index)
        {
            //
            // Validate parameters.
            //
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int len = str.Length;
            if (index < 0 || index >= len)
            {
                if (index == len)
                {
                    return (String.Empty);
                }
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            int charLen;
            UnicodeCategory uc = CharUnicodeInfo.InternalGetUnicodeCategory(str, index, out charLen);
            return (str.Substring(index, GetCurrentTextElementLen(str, index, len, ref uc, ref charLen)));
        }

        public static TextElementEnumerator GetTextElementEnumerator(string str)
        {
            return (GetTextElementEnumerator(str, 0));
        }

        public static TextElementEnumerator GetTextElementEnumerator(string str, int index)
        {
            //
            // Validate parameters.
            //
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            int len = str.Length;
            if (index < 0 || (index > len))
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            return (new TextElementEnumerator(str, index, len));
        }

        /*
         * Returns the indices of each base character or properly formed surrogate pair 
         * within the str.  It recognizes a base character plus one or more combining 
         * characters or a properly formed surrogate pair as a text element and returns 
         * the index of the base character or high surrogate.  Each index is the 
         * beginning of a text element within a str.  The length of each element is 
         * easily computed as the difference between successive indices.  The length of 
         * the array will always be less than or equal to the length of the str.  For 
         * example, given the str \u4f00\u302a\ud800\udc00\u4f01, this method would 
         * return the indices: 0, 2, 4.
         */

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
                Array.Copy(result, returnArray, resultCount);
                return (returnArray);
            }
            return (result);
        }
    }
}
