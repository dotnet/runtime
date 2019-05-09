// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    /// <summary>
    /// This class implements a set of methods for retrieving character type
    /// information. Character type information is independent of culture
    /// and region.
    /// </summary>
    public static partial class CharUnicodeInfo
    {
        internal const char HIGH_SURROGATE_START = '\ud800';
        internal const char HIGH_SURROGATE_END = '\udbff';
        internal const char LOW_SURROGATE_START = '\udc00';
        internal const char LOW_SURROGATE_END = '\udfff';
        internal const int  HIGH_SURROGATE_RANGE = 0x3FF;

        internal const int UNICODE_CATEGORY_OFFSET = 0;
        internal const int BIDI_CATEGORY_OFFSET = 1;

        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
        internal const int UNICODE_PLANE01_START = 0x10000;

        /// <summary>
        /// Convert the BMP character or surrogate pointed by index to a UTF32 value.
        /// This is similar to char.ConvertToUTF32, but the difference is that
        /// it does not throw exceptions when invalid surrogate characters are passed in.
        ///
        /// WARNING: since it doesn't throw an exception it CAN return a value
        /// in the surrogate range D800-DFFF, which are not legal unicode values.
        /// </summary>
        internal static int InternalConvertToUtf32(string s, int index)
        {
            Debug.Assert(s != null, "s != null");
            Debug.Assert(index >= 0 && index < s.Length, "index < s.Length");
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if ((uint)temp1 <= HIGH_SURROGATE_RANGE)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if ((uint)temp2 <= HIGH_SURROGATE_RANGE)
                    {
                        // Convert the surrogate to UTF32 and get the result.
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }
            return (int)s[index];
        }

        internal static int InternalConvertToUtf32(StringBuilder s, int index)
        {
            Debug.Assert(s != null, "s != null");
            Debug.Assert(index >= 0 && index < s.Length, "index < s.Length");

            int c = (int)s[index];
            if (index < s.Length - 1)
            {
                int temp1 = c - HIGH_SURROGATE_START;
                if ((uint)temp1 <= HIGH_SURROGATE_RANGE)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if ((uint)temp2 <= HIGH_SURROGATE_RANGE)
                    {
                        // Convert the surrogate to UTF32 and get the result.
                        return (temp1 * 0x400) + temp2 + UNICODE_PLANE01_START;
                    }
                }
            }
            return c;
        }

        /// <summary>
        /// Convert a character or a surrogate pair starting at index of string s
        /// to UTF32 value.
        /// WARNING: since it doesn't throw an exception it CAN return a value
        /// in the surrogate range D800-DFFF, which are not legal unicode values.
        /// </summary>
        internal static int InternalConvertToUtf32(string s, int index, out int charLength)
        {
            Debug.Assert(s != null, "s != null");
            Debug.Assert(s.Length > 0, "s.Length > 0");
            Debug.Assert(index >= 0 && index < s.Length, "index >= 0 && index < s.Length");
            charLength = 1;
            if (index < s.Length - 1)
            {
                int temp1 = (int)s[index] - HIGH_SURROGATE_START;
                if ((uint)temp1 <= HIGH_SURROGATE_RANGE)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START;
                    if ((uint)temp2 <= HIGH_SURROGATE_RANGE)
                    {
                        // Convert the surrogate to UTF32 and get the result.
                        charLength++;
                        return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
                    }
                }
            }
            return ((int)s[index]);
        }

        /// <summary>
        /// This is called by the public char and string, index versions
        /// Note that for ch in the range D800-DFFF we just treat it as any
        /// other non-numeric character
        /// </summary>
        internal static double InternalGetNumericValue(int ch)
        {
            Debug.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            // Get the level 2 item from the highest 12 bit (8 - 19) of ch.
            int index = ch >> 8;
            if ((uint)index < (uint)NumericLevel1Index.Length)
            {
                index = NumericLevel1Index[index];
                // Get the level 2 offset from the 4 - 7 bit of ch.  This provides the base offset of the level 3 table.
                // Note that & has the lower precedence than addition, so don't forget the parathesis.
                index = NumericLevel2Index[(index << 4) + ((ch >> 4) & 0x000f)];
                index = NumericLevel3Index[(index << 4) + (ch & 0x000f)];
                ref var value = ref Unsafe.AsRef(in NumericValues[index * 8]);

                if (BitConverter.IsLittleEndian)
                {
                    return Unsafe.ReadUnaligned<double>(ref value);
                }

                return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(ref value)));
            }
            return -1;
        }

        internal static byte InternalGetDigitValues(int ch, int offset)
        {
            Debug.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            // Get the level 2 item from the highest 12 bit (8 - 19) of ch.
            int index = ch >> 8;
            if ((uint)index < (uint)NumericLevel1Index.Length)
            {
                index = NumericLevel1Index[index];
                // Get the level 2 offset from the 4 - 7 bit of ch.  This provides the base offset of the level 3 table.
                // Note that & has the lower precedence than addition, so don't forget the parathesis.
                index = NumericLevel2Index[(index << 4) + ((ch >> 4) & 0x000f)];
                index = NumericLevel3Index[(index << 4) + (ch & 0x000f)];
                return DigitValues[index * 2 + offset];
            }
            return 0xff;
        }

        /// <summary>
        /// Returns the numeric value associated with the character c.
        /// If the character is a fraction,  the return value will not be an
        /// integer. If the character does not have a numeric value, the return
        /// value is -1.
        /// </summary>
        public static double GetNumericValue(char ch)
        {
            return InternalGetNumericValue(ch);
        }

        public static double GetNumericValue(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            return InternalGetNumericValue(InternalConvertToUtf32(s, index));
        }

        public static int GetDecimalDigitValue(char ch)
        {
            return (sbyte)InternalGetDigitValues(ch, 0);
        }

        public static int GetDecimalDigitValue(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            return (sbyte)InternalGetDigitValues(InternalConvertToUtf32(s, index), 0);
        }

        public static int GetDigitValue(char ch)
        {
            return (sbyte)InternalGetDigitValues(ch, 1);
        }

        public static int GetDigitValue(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            }

            return (sbyte)InternalGetDigitValues(InternalConvertToUtf32(s, index), 1);
        }

        public static UnicodeCategory GetUnicodeCategory(char ch)
        {
            return GetUnicodeCategory((int)ch);
        }

        public static UnicodeCategory GetUnicodeCategory(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return InternalGetUnicodeCategory(s, index);
        }

        public static UnicodeCategory GetUnicodeCategory(int codePoint)
        {
            return (UnicodeCategory)InternalGetCategoryValue(codePoint, UNICODE_CATEGORY_OFFSET);
        }

        /// <summary>
        /// Returns the Unicode Category property for the character c.
        /// Note that this API will return values for D800-DF00 surrogate halves.
        /// </summary>
        internal static byte InternalGetCategoryValue(int ch, int offset)
        {
            Debug.Assert(ch >= 0 && ch <= 0x10ffff, "ch is not in valid Unicode range.");
            // Get the level 2 item from the highest 11 bits of ch.
            int index = CategoryLevel1Index[ch >> 9];
            // Get the level 2 WORD offset from the next 5 bits of ch.  This provides the base offset of the level 3 table.
            // Note that & has the lower precedence than addition, so don't forget the parathesis.
            index = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in CategoryLevel2Index[(index << 6) + ((ch >> 3) & 0b111110)]));
            if (!BitConverter.IsLittleEndian)
            {
                index = BinaryPrimitives.ReverseEndianness((ushort)index);
            }

            // Get the result from the 0 -3 bit of ch.
            index = CategoryLevel3Index[(index << 4) + (ch & 0x000f)];
            return CategoriesValue[index * 2 + offset];
        }

        /// <summary>
        /// Returns the Unicode Category property for the character c.
        /// </summary>
        internal static UnicodeCategory InternalGetUnicodeCategory(string value, int index)
        {
            Debug.Assert(value != null, "value can not be null");
            Debug.Assert(index < value.Length, "index < value.Length");

            return (GetUnicodeCategory(InternalConvertToUtf32(value, index)));
        }

        internal static BidiCategory GetBidiCategory(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return ((BidiCategory) InternalGetCategoryValue(InternalConvertToUtf32(s, index), BIDI_CATEGORY_OFFSET));
        }

        internal static BidiCategory GetBidiCategory(StringBuilder s, int index)
        {
            Debug.Assert(s != null, "s can not be null");
            Debug.Assert(index >= 0 && index < s.Length, "invalid index"); ;

            return ((BidiCategory) InternalGetCategoryValue(InternalConvertToUtf32(s, index), BIDI_CATEGORY_OFFSET));
        }

        /// <summary>
        /// Get the Unicode category of the character starting at index.  If the character is in BMP, charLength will return 1.
        /// If the character is a valid surrogate pair, charLength will return 2.
        /// </summary>
        internal static UnicodeCategory InternalGetUnicodeCategory(string str, int index, out int charLength)
        {
            Debug.Assert(str != null, "str can not be null");
            Debug.Assert(str.Length > 0, "str.Length > 0"); ;
            Debug.Assert(index >= 0 && index < str.Length, "index >= 0 && index < str.Length");

            return GetUnicodeCategory(InternalConvertToUtf32(str, index, out charLength));
        }

        internal static bool IsCombiningCategory(UnicodeCategory uc)
        {
            Debug.Assert(uc >= 0, "uc >= 0");
            return (
                uc == UnicodeCategory.NonSpacingMark ||
                uc == UnicodeCategory.SpacingCombiningMark ||
                uc == UnicodeCategory.EnclosingMark
            );
        }
    }
}
