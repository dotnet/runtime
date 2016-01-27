// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
//  Class:    CharacterInfo
//
//  Purpose:  This class implements a set of methods for retrieving
//            character type information.  Character type information is
//            independent of culture and region.
//
//  Date:     August 12, 1998
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security;
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public static partial class CharUnicodeInfo
    {
        //--------------------------------------------------------------------//
        //                        Internal Information                        //
        //--------------------------------------------------------------------//

        //
        // Native methods to access the Unicode category data tables in charinfo.nlp.
        //
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';

        internal const int UNICODE_CATEGORY_OFFSET = 0;
        internal const int BIDI_CATEGORY_OFFSET = 1;



        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
        internal const int UNICODE_PLANE01_START = 0x10000;


        ////////////////////////////////////////////////////////////////////////
        //
        // Actions:
        // Convert the BMP character or surrogate pointed by index to a UTF32 value.
        // This is similar to Char.ConvertToUTF32, but the difference is that
        // it does not throw exceptions when invalid surrogate characters are passed in.
        //
        // WARNING: since it doesn't throw an exception it CAN return a value
        //          in the surrogate range D800-DFFF, which are not legal unicode values.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static int InternalConvertToUtf32(String s, int index)
        {
            Contract.Assert(s != null, "s != null");
            Contract.Assert(index >= 0 && index < s.Length, "index < s.Length");
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if (temp1 >= 0 && temp1 <= 0x3ff)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if (temp2 >= 0 && temp2 <= 0x3ff)
                    {
                        // Convert the surrogate to UTF32 and get the result.
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }
            return ((int)s[index]);
        }
        ////////////////////////////////////////////////////////////////////////
        //
        // Convert a character or a surrogate pair starting at index of string s
        // to UTF32 value.
        //
        //  Parameters:
        //      s       The string
        //      index   The starting index.  It can point to a BMP character or
        //              a surrogate pair.
        //      len     The length of the string.
        //      charLength  [out]   If the index points to a BMP char, charLength
        //              will be 1.  If the index points to a surrogate pair,
        //              charLength will be 2.
        //
        // WARNING: since it doesn't throw an exception it CAN return a value
        //          in the surrogate range D800-DFFF, which are not legal unicode values.
        //
        //  Returns:
        //      The UTF32 value
        //
        ////////////////////////////////////////////////////////////////////////

        internal static int InternalConvertToUtf32(String s, int index, out int charLength)
        {
            Contract.Assert(s != null, "s != null");
            Contract.Assert(s.Length > 0, "s.Length > 0");
            Contract.Assert(index >= 0 && index < s.Length, "index >= 0 && index < s.Length");
            charLength = 1;
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if (temp1 >= 0 && temp1 <= 0x3ff)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if (temp2 >= 0 && temp2 <= 0x3ff)
                    {
                        // Convert the surrogate to UTF32 and get the result.
                        charLength++;
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }
            return ((int)s[index]);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsWhiteSpace
        //
        //  Determines if the given character is a white space character.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static bool IsWhiteSpace(String s, int index)
        {
            Contract.Assert(s != null, "s!=null");
            Contract.Assert(index >= 0 && index < s.Length, "index >= 0 && index < s.Length");

            UnicodeCategory uc = GetUnicodeCategory(s, index);
            // In Unicode 3.0, U+2028 is the only character which is under the category "LineSeparator".
            // And U+2029 is th eonly character which is under the category "ParagraphSeparator".
            switch (uc)
            {
                case (UnicodeCategory.SpaceSeparator):
                case (UnicodeCategory.LineSeparator):
                case (UnicodeCategory.ParagraphSeparator):
                    return (true);
            }
            return (false);
        }


        internal static bool IsWhiteSpace(char c)
        {
            UnicodeCategory uc = GetUnicodeCategory(c);
            // In Unicode 3.0, U+2028 is the only character which is under the category "LineSeparator".
            // And U+2029 is th eonly character which is under the category "ParagraphSeparator".
            switch (uc)
            {
                case (UnicodeCategory.SpaceSeparator):
                case (UnicodeCategory.LineSeparator):
                case (UnicodeCategory.ParagraphSeparator):
                    return (true);
            }

            return (false);
        }


        //
        // This is called by the public char and string, index versions
        //
        // Note that for ch in the range D800-DFFF we just treat it as any other non-numeric character
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static double InternalGetNumericValue(int ch)
        {
            Contract.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            // Get the level 2 item from the highest 12 bit (8 - 19) of ch.
            ushort index = s_pNumericLevel1Index[ch >> 8];
            // Get the level 2 WORD offset from the 4 - 7 bit of ch.  This provides the base offset of the level 3 table.
            // The offset is referred to an float item in m_pNumericFloatData.
            // Note that & has the lower precedence than addition, so don't forget the parathesis.
            index = s_pNumericLevel1Index[index + ((ch >> 4) & 0x000f)];

            fixed (ushort* pUshortPtr = &(s_pNumericLevel1Index[index]))
            {
                byte* pBytePtr = (byte*)pUshortPtr;
                fixed (byte* pByteNum = s_pNumericValues)
                {
                    double* pDouble = (double*)pByteNum;
                    return pDouble[pBytePtr[(ch & 0x000f)]];
                }
            }
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //Returns the numeric value associated with the character c. If the character is a fraction,
        // the return value will not be an integer. If the character does not have a numeric value, the return value is -1.
        //
        //Returns:
        //  the numeric value for the specified Unicode character.  If the character does not have a numeric value, the return value is -1.
        //Arguments:
        //      ch  a Unicode character
        //Exceptions:
        //      ArgumentNullException
        //      ArgumentOutOfRangeException
        //
        ////////////////////////////////////////////////////////////////////////


        public static double GetNumericValue(char ch)
        {
            return (InternalGetNumericValue(ch));
        }


        public static double GetNumericValue(String s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_Index);
            }
            Contract.EndContractBlock();
            return (InternalGetNumericValue(InternalConvertToUtf32(s, index)));
        }

        public static UnicodeCategory GetUnicodeCategory(char ch)
        {
            return (InternalGetUnicodeCategory(ch));
        }

        public static UnicodeCategory GetUnicodeCategory(String s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            Contract.EndContractBlock();
            return InternalGetUnicodeCategory(s, index);
        }

        internal unsafe static UnicodeCategory InternalGetUnicodeCategory(int ch)
        {
            return ((UnicodeCategory)InternalGetCategoryValue(ch, UNICODE_CATEGORY_OFFSET));
        }


        ////////////////////////////////////////////////////////////////////////
        //
        //Action: Returns the Unicode Category property for the character c.
        //Returns:
        //  an value in UnicodeCategory enum
        //Arguments:
        //  ch  a Unicode character
        //Exceptions:
        //  None
        //
        //Note that this API will return values for D800-DF00 surrogate halves.
        //
        ////////////////////////////////////////////////////////////////////////

        [System.Security.SecuritySafeCritical]  // auto-generated
        internal unsafe static byte InternalGetCategoryValue(int ch, int offset)
        {
            Contract.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            // Get the level 2 item from the highest 12 bit (8 - 19) of ch.
            ushort index = s_pCategoryLevel1Index[ch >> 8];
            // Get the level 2 WORD offset from the 4 - 7 bit of ch.  This provides the base offset of the level 3 table.
            // Note that & has the lower precedence than addition, so don't forget the parathesis.
            index = s_pCategoryLevel1Index[index + ((ch >> 4) & 0x000f)];

            fixed (ushort* pUshortPtr = &(s_pCategoryLevel1Index[index]))
            {
                byte* pBytePtr = (byte*)pUshortPtr;
                // Get the result from the 0 -3 bit of ch.
                byte valueIndex = pBytePtr[(ch & 0x000f)];
                byte uc = s_pCategoriesValue[valueIndex * 2 + offset];
                //
                // Make sure that OtherNotAssigned is the last category in UnicodeCategory.
                // If that changes, change the following assertion as well.
                //
                //Contract.Assert(uc >= 0 && uc <= UnicodeCategory.OtherNotAssigned, "Table returns incorrect Unicode category");
                return (uc);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //Action: Returns the Unicode Category property for the character c.
        //Returns:
        //  an value in UnicodeCategory enum
        //Arguments:
        //  value  a Unicode String
        //  index  Index for the specified string.
        //Exceptions:
        //  None
        //
        ////////////////////////////////////////////////////////////////////////

        internal static UnicodeCategory InternalGetUnicodeCategory(String value, int index)
        {
            Contract.Assert(value != null, "value can not be null");
            Contract.Assert(index < value.Length, "index < value.Length");

            return (InternalGetUnicodeCategory(InternalConvertToUtf32(value, index)));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // Get the Unicode category of the character starting at index.  If the character is in BMP, charLength will return 1.
        // If the character is a valid surrogate pair, charLength will return 2.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static UnicodeCategory InternalGetUnicodeCategory(String str, int index, out int charLength)
        {
            Contract.Assert(str != null, "str can not be null");
            Contract.Assert(str.Length > 0, "str.Length > 0"); ;
            Contract.Assert(index >= 0 && index < str.Length, "index >= 0 && index < str.Length");

            return (InternalGetUnicodeCategory(InternalConvertToUtf32(str, index, out charLength)));
        }

        internal static bool IsCombiningCategory(UnicodeCategory uc)
        {
            Contract.Assert(uc >= 0, "uc >= 0");
            return (
                uc == UnicodeCategory.NonSpacingMark ||
                uc == UnicodeCategory.SpacingCombiningMark ||
                uc == UnicodeCategory.EnclosingMark
            );
        }
    }
}
