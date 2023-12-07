// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

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

        /*
         * GetBidiCategory
         * ===============
         * Data derived from https://www.unicode.org/reports/tr9/#Bidirectional_Character_Types. This data
         * is encoded in DerivedBidiClass.txt. We map "L" to "strong left-to-right"; and we map "R" and "AL"
         * to "strong right-to-left". All other (non-strong) code points are "other" for our purposes.
         */

        internal static StrongBidiCategory GetBidiCategory(string s, int index)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if ((uint)index >= (uint)s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return GetBidiCategoryNoBoundsChecks((uint)GetCodePointFromString(s, index));
        }

        internal static StrongBidiCategory GetBidiCategory(StringBuilder s, int index)
        {
            Debug.Assert(s != null, "s != null");
            Debug.Assert(index >= 0 && index < s.Length, "index < s.Length");

            // The logic below follows Table 3-5 in the Unicode Standard, Sec. 3.9.
            // First char (high surrogate) = 110110wwwwxxxxxx
            // Second char (low surrogate) = 110111xxxxxxxxxx

            int c = (int)s[index];
            if (index < s.Length - 1)
            {
                int temp1 = c - HIGH_SURROGATE_START; // temp1 = 000000wwwwxxxxxx
                if ((uint)temp1 <= HIGH_SURROGATE_RANGE)
                {
                    int temp2 = (int)s[index + 1] - LOW_SURROGATE_START; // temp2 = 000000xxxxxxxxxx
                    if ((uint)temp2 <= HIGH_SURROGATE_RANGE)
                    {
                        // |--------temp1--||-temp2--|
                        // 00000uuuuuuxxxxxxxxxxxxxxxx (where uuuuu = wwww + 1)
                        c = (temp1 << 10) + temp2 + UNICODE_PLANE01_START;
                    }
                }
            }

            return GetBidiCategoryNoBoundsChecks((uint)c);
        }

        private static StrongBidiCategory GetBidiCategoryNoBoundsChecks(uint codePoint)
        {
            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks(codePoint);

            // Each entry of the 'CategoryValues' table uses bits 5 - 6 to store the strong bidi information.

            StrongBidiCategory bidiCategory = (StrongBidiCategory)(Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoriesValues), offset) & 0b_0110_0000);
            Debug.Assert(bidiCategory == StrongBidiCategory.Other || bidiCategory == StrongBidiCategory.StrongLeftToRight || bidiCategory == StrongBidiCategory.StrongRightToLeft, "Unknown StrongBidiCategory value.");

            return bidiCategory;
        }

        /*
         * GetDecimalDigitValue
         * ====================
         * Data derived from https://www.unicode.org/reports/tr44/#UnicodeData.txt. If Numeric_Type=Decimal,
         * then retrieves the Numeric_Value (0..9) for this code point. If Numeric_Type!=Decimal, returns -1.
         * This data is encoded in field 6 of UnicodeData.txt.
         */

        public static int GetDecimalDigitValue(char ch)
        {
            return GetDecimalDigitValueInternalNoBoundsCheck(ch);
        }

        public static int GetDecimalDigitValue(string s, int index)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if ((uint)index >= (uint)s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return GetDecimalDigitValueInternalNoBoundsCheck((uint)GetCodePointFromString(s, index));
        }

        private static int GetDecimalDigitValueInternalNoBoundsCheck(uint codePoint)
        {
            nuint offset = GetNumericGraphemeTableOffsetNoBoundsChecks(codePoint);
            uint rawValue = Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(DigitValues), offset);
            return (int)(rawValue >> 4) - 1; // return the high nibble of the result, minus 1 so that "not a decimal digit value" gets normalized to -1
        }

        /*
         * GetDigitValue
         * =============
         * Data derived from https://www.unicode.org/reports/tr44/#UnicodeData.txt. If Numeric_Type=Decimal
         * or Numeric_Type=Digit, then retrieves the Numeric_Value (0..9) for this code point. Otherwise
         * returns -1. This data is encoded in field 7 of UnicodeData.txt.
         */

        public static int GetDigitValue(char ch)
        {
            return GetDigitValueInternalNoBoundsCheck(ch);
        }

        public static int GetDigitValue(string s, int index)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if ((uint)index >= (uint)s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return GetDigitValueInternalNoBoundsCheck((uint)GetCodePointFromString(s, index));
        }

        private static int GetDigitValueInternalNoBoundsCheck(uint codePoint)
        {
            nuint offset = GetNumericGraphemeTableOffsetNoBoundsChecks(codePoint);
            int rawValue = Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(DigitValues), offset);
            return (rawValue & 0xF) - 1; // return the low nibble of the result, minus 1 so that "not a digit value" gets normalized to -1
        }

        /*
         * GetGraphemeBreakClusterType
         * ===========================
         * Data derived from https://unicode.org/reports/tr29/#Default_Grapheme_Cluster_Table. Represents
         * grapheme cluster boundary information for the given code point.
         */

        internal static GraphemeClusterBreakType GetGraphemeClusterBreakType(Rune rune)
        {
            nuint offset = GetNumericGraphemeTableOffsetNoBoundsChecks((uint)rune.Value);
            return (GraphemeClusterBreakType)Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(GraphemeSegmentationValues), offset);
        }

        /*
         * GetIsWhiteSpace
         * ===========================
         * Data derived from https://unicode.org/reports/tr44/#White_Space. Represents whether a code point
         * is listed as White_Space per PropList.txt.
         */

        internal static bool GetIsWhiteSpace(char ch)
        {
            // We don't need a (string, int) overload because all current white space chars are in the BMP.

            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks(ch);

            // High bit of each value in the 'CategoriesValues' array denotes whether this code point is white space.

            return (sbyte)Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoriesValues), offset) < 0;
        }

        /*
         * GetNumericValue
         * ===============
         * Data derived from https://www.unicode.org/reports/tr44/#UnicodeData.txt. If Numeric_Type=Decimal
         * or Numeric_Type=Digit or Numeric_Type=Numeric, then retrieves the Numeric_Value for this code point.
         * Otherwise returns -1. This data is encoded in field 8 of UnicodeData.txt.
         */

        public static double GetNumericValue(char ch)
        {
            return GetNumericValueNoBoundsCheck(ch);
        }

        internal static double GetNumericValue(int codePoint)
        {
            if (!UnicodeUtility.IsValidCodePoint((uint)codePoint))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.codePoint);
            }

            return GetNumericValueNoBoundsCheck((uint)codePoint);
        }

        public static double GetNumericValue(string s, int index)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if ((uint)index >= (uint)s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return GetNumericValueInternal(s, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double GetNumericValueInternal(string s, int index) => GetNumericValueNoBoundsCheck((uint)GetCodePointFromString(s, index));

        private static double GetNumericValueNoBoundsCheck(uint codePoint)
        {
            nuint offset = GetNumericGraphemeTableOffsetNoBoundsChecks(codePoint);
            ref byte refToValue = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(NumericValues), offset * 8 /* sizeof(double) */);

            // 'refToValue' points to a little-endian 64-bit double.

            if (BitConverter.IsLittleEndian)
            {
                return Unsafe.ReadUnaligned<double>(ref refToValue);
            }
            else
            {
                ulong temp = Unsafe.ReadUnaligned<ulong>(ref refToValue);
                temp = BinaryPrimitives.ReverseEndianness(temp);
                return BitConverter.UInt64BitsToDouble(temp);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpper(char codePoint)
        {
            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks((uint)codePoint);

            // The offset is specified in shorts:
            // Get the 'ref short' corresponding to where the addend is, read it as a signed 16-bit value, then add

            ref short rsStart = ref Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(UppercaseValues));
            ref short rsDelta = ref Unsafe.Add(ref rsStart, (nint)offset);
            int delta = (BitConverter.IsLittleEndian) ? rsDelta : BinaryPrimitives.ReverseEndianness(rsDelta);
            return (char)(delta + codePoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ToUpper(uint codePoint)
        {
            if (!UnicodeUtility.IsValidCodePoint(codePoint))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.codePoint);
            }

            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks(codePoint);

            // The mapped casing for the codePoint usually exists in the same plane as codePoint.
            // This is why we use 16-bit offsets to calculate the delta value from the codePoint.

            ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(UppercaseValues));
            ref ushort rsDelta = ref Unsafe.Add(ref rsStart, (nint)offset);
            int delta = (BitConverter.IsLittleEndian) ? rsDelta : BinaryPrimitives.ReverseEndianness(rsDelta);

            // We use the mask 0xFFFF0000u as we are sure the casing is in the same plane as codePoint.
            return (codePoint & 0xFFFF0000u) | (ushort)((uint)delta + codePoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToLower(char codePoint)
        {
            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks((uint)codePoint);

            // The offset is specified in shorts:
            // Get the 'ref short' corresponding to where the addend is, read it as a signed 16-bit value, then add

            ref short rsStart = ref Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(LowercaseValues));
            ref short rsDelta = ref Unsafe.Add(ref rsStart, (nint)offset);
            int delta = (BitConverter.IsLittleEndian) ? rsDelta : BinaryPrimitives.ReverseEndianness(rsDelta);
            return (char)(delta + codePoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ToLower(uint codePoint)
        {
            if (!UnicodeUtility.IsValidCodePoint(codePoint))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.codePoint);
            }

            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks(codePoint);

            // The mapped casing for the codePoint usually exists in the same plane as codePoint.
            // This is why we use 16-bit offsets to calculate the delta value from the codePoint.

            ref ushort rsStart = ref Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(LowercaseValues));
            ref ushort rsDelta = ref Unsafe.Add(ref rsStart, (nint)offset);
            int delta = (BitConverter.IsLittleEndian) ? rsDelta : BinaryPrimitives.ReverseEndianness(rsDelta);

            // We use the mask 0xFFFF0000u as we are sure the casing is in the same plane as codePoint.
            return (codePoint & 0xFFFF0000u) | (ushort)((uint)delta + codePoint);
        }

        /*
         * GetUnicodeCategory
         * ==================
         * Data derived from https://www.unicode.org/reports/tr44/#UnicodeData.txt. Returns the
         * General_Category of this code point as encoded in field 2 of UnicodeData.txt, or "Cn"
         * if the code point has not been assigned.
         */

        public static UnicodeCategory GetUnicodeCategory(char ch)
        {
            return GetUnicodeCategoryNoBoundsChecks(ch);
        }

        public static UnicodeCategory GetUnicodeCategory(int codePoint)
        {
            if (!UnicodeUtility.IsValidCodePoint((uint)codePoint))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.codePoint);
            }

            return GetUnicodeCategoryNoBoundsChecks((uint)codePoint);
        }

        public static UnicodeCategory GetUnicodeCategory(string s, int index)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if ((uint)index >= (uint)s.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return GetUnicodeCategoryInternal(s, index);
        }

        /// <summary>
        /// Similar to <see cref="GetUnicodeCategory(string, int)"/>, but skips argument checks.
        /// For internal use only.
        /// </summary>
        internal static UnicodeCategory GetUnicodeCategoryInternal(string value, int index)
        {
            Debug.Assert(value != null, "value can not be null");
            Debug.Assert(index < value.Length, "index < value.Length");

            return GetUnicodeCategoryNoBoundsChecks((uint)GetCodePointFromString(value, index));
        }

        /// <summary>
        /// Get the Unicode category of the character starting at index.  If the character is in BMP, charLength will return 1.
        /// If the character is a valid surrogate pair, charLength will return 2.
        /// </summary>
        internal static UnicodeCategory GetUnicodeCategoryInternal(string str, int index, out int charLength)
        {
            Debug.Assert(str != null, "str can not be null");
            Debug.Assert(str.Length > 0, "str.Length > 0");
            Debug.Assert(index >= 0 && index < str.Length, "index >= 0 && index < str.Length");

            uint codePoint = (uint)GetCodePointFromString(str, index);
            UnicodeDebug.AssertIsValidCodePoint(codePoint);

            charLength = (codePoint >= UNICODE_PLANE01_START) ? 2 /* surrogate pair */ : 1 /* BMP char */;
            return GetUnicodeCategoryNoBoundsChecks(codePoint);
        }

        private static UnicodeCategory GetUnicodeCategoryNoBoundsChecks(uint codePoint)
        {
            nuint offset = GetCategoryCasingTableOffsetNoBoundsChecks(codePoint);

            // Each entry of the 'CategoriesValues' table uses the low 5 bits to store the UnicodeCategory information.

            return (UnicodeCategory)(Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoriesValues), offset) & 0x1F);
        }

        /*
         * HELPER AND TABLE LOOKUP ROUTINES
         */

        /// <summary>
        /// Returns the code point pointed to by index, decoding any surrogate sequence if possible.
        /// This is similar to char.ConvertToUTF32, but the difference is that
        /// it does not throw exceptions when invalid surrogate characters are passed in.
        ///
        /// WARNING: since it doesn't throw an exception it CAN return a value
        /// in the surrogate range D800-DFFF, which is not a legal scalar value.
        /// </summary>
        private static int GetCodePointFromString(string s, int index)
        {
            Debug.Assert(s != null, "s != null");
            Debug.Assert((uint)index < (uint)s.Length, "index < s.Length");

            int codePoint = 0;

            // We know the 'if' block below will always succeed, but it allows the
            // JIT to optimize the codegen of this method.

            if ((uint)index < (uint)s.Length)
            {
                codePoint = s[index];
                int temp1 = codePoint - HIGH_SURROGATE_START;
                if ((uint)temp1 <= HIGH_SURROGATE_RANGE)
                {
                    index++;
                    if ((uint)index < (uint)s.Length)
                    {
                        int temp2 = s[index] - LOW_SURROGATE_START;
                        if ((uint)temp2 <= HIGH_SURROGATE_RANGE)
                        {
                            // Combine these surrogate code points into a supplementary code point
                            codePoint = (temp1 << 10) + temp2 + UNICODE_PLANE01_START;
                        }
                    }
                }
            }

            return codePoint;
        }

        /// <summary>
        /// Retrieves the offset into the "CategoryCasing" arrays where this code point's
        /// information is stored. Used for getting the Unicode category, bidi information,
        /// and whitespace information.
        /// </summary>
        private static nuint GetCategoryCasingTableOffsetNoBoundsChecks(uint codePoint)
        {
            UnicodeDebug.AssertIsValidCodePoint(codePoint);

            // The code below is written with the assumption that the backing store is 11:5:4.
            AssertCategoryCasingTableLevels(11, 5, 4);

            // Get the level index item from the high 11 bits of the code point.

            uint index = Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoryCasingLevel1Index), codePoint >> 9);

            // Get the level 2 WORD offset from the next 5 bits of the code point.
            // This provides the base offset of the level 3 table.
            // Note that & has lower precedence than +, so remember the parens.

            ref byte level2Ref = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoryCasingLevel2Index), (index << 6) + ((codePoint >> 3) & 0b_0011_1110));

            if (BitConverter.IsLittleEndian)
            {
                index = Unsafe.ReadUnaligned<ushort>(ref level2Ref);
            }
            else
            {
                index = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref level2Ref));
            }

            // Get the result from the low 4 bits of the code point.
            // This is the offset into the values table where the data is stored.

            return Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(CategoryCasingLevel3Index), (index << 4) + (codePoint & 0x0F));
        }

        /// <summary>
        /// Retrieves the offset into the "NumericGrapheme" arrays where this code point's
        /// information is stored. Used for getting numeric information and grapheme boundary
        /// information.
        /// </summary>
        private static nuint GetNumericGraphemeTableOffsetNoBoundsChecks(uint codePoint)
        {
            UnicodeDebug.AssertIsValidCodePoint(codePoint);

            // The code below is written with the assumption that the backing store is 11:5:4.
            AssertNumericGraphemeTableLevels(11, 5, 4);

            // Get the level index item from the high 11 bits of the code point.

            uint index = Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(NumericGraphemeLevel1Index), codePoint >> 9);

            // Get the level 2 WORD offset from the next 5 bits of the code point.
            // This provides the base offset of the level 3 table.
            // Note that & has lower precedence than +, so remember the parens.

            ref byte level2Ref = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(NumericGraphemeLevel2Index), (index << 6) + ((codePoint >> 3) & 0b_0011_1110));

            if (BitConverter.IsLittleEndian)
            {
                index = Unsafe.ReadUnaligned<ushort>(ref level2Ref);
            }
            else
            {
                index = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref level2Ref));
            }

            // Get the result from the low 4 bits of the code point.
            // This is the offset into the values table where the data is stored.

            return Unsafe.AddByteOffset(ref MemoryMarshal.GetReference(NumericGraphemeLevel3Index), (index << 4) + (codePoint & 0x0F));
        }
    }
}
