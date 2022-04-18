// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: This is the value class representing a Unicode character
** Char methods until we create this functionality.
**
**
===========================================================*/

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public readonly struct Char
        : IComparable,
          IComparable<char>,
          IEquatable<char>,
          IConvertible,
          ISpanFormattable,
          IBinaryInteger<char>,
          IMinMaxValue<char>,
          IUnsignedNumber<char>
    {
        //
        // Member Variables
        //
        private readonly char m_value; // Do not rename (binary serialization)

        //
        // Public Constants
        //
        // The maximum character value.
        public const char MaxValue = (char)0xFFFF;
        // The minimum character value.
        public const char MinValue = (char)0x00;

        private const byte IsWhiteSpaceFlag = 0x80;
        private const byte IsUpperCaseLetterFlag = 0x40;
        private const byte IsLowerCaseLetterFlag = 0x20;
        private const byte UnicodeCategoryMask = 0x1F;

        // Contains information about the C0, Basic Latin, C1, and Latin-1 Supplement ranges [ U+0000..U+00FF ], with:
        // - 0x80 bit if set means 'is whitespace'
        // - 0x40 bit if set means 'is uppercase letter'
        // - 0x20 bit if set means 'is lowercase letter'
        // - bottom 5 bits are the UnicodeCategory of the character
        private static ReadOnlySpan<byte> Latin1CharInfo => new byte[]
        {
        //  0     1     2     3     4     5     6     7     8     9     A     B     C     D     E     F
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x8E, 0x8E, 0x8E, 0x8E, 0x8E, 0x0E, 0x0E, // U+0000..U+000F
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, // U+0010..U+001F
            0x8B, 0x18, 0x18, 0x18, 0x1A, 0x18, 0x18, 0x18, 0x14, 0x15, 0x18, 0x19, 0x18, 0x13, 0x18, 0x18, // U+0020..U+002F
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x18, 0x18, 0x19, 0x19, 0x19, 0x18, // U+0030..U+003F
            0x18, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, // U+0040..U+004F
            0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x14, 0x18, 0x15, 0x1B, 0x12, // U+0050..U+005F
            0x1B, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, // U+0060..U+006F
            0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x14, 0x19, 0x15, 0x19, 0x0E, // U+0070..U+007F
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x8E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, // U+0080..U+008F
            0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, // U+0090..U+009F
            0x8B, 0x18, 0x1A, 0x1A, 0x1A, 0x1A, 0x1C, 0x18, 0x1B, 0x1C, 0x04, 0x16, 0x19, 0x0F, 0x1C, 0x1B, // U+00A0..U+00AF
            0x1C, 0x19, 0x0A, 0x0A, 0x1B, 0x21, 0x18, 0x18, 0x1B, 0x0A, 0x04, 0x17, 0x0A, 0x0A, 0x0A, 0x18, // U+00B0..U+00BF
            0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, // U+00C0..U+00CF
            0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x19, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x40, 0x21, // U+00D0..U+00DF
            0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, // U+00E0..U+00EF
            0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x19, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, 0x21, // U+00F0..U+00FF
        };

        // Return true for all characters below or equal U+00ff, which is ASCII + Latin-1 Supplement.
        private static bool IsLatin1(char c) => (uint)c < (uint)Latin1CharInfo.Length;

        // Return true for all characters below or equal U+007f, which is ASCII.

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="c"/> is an ASCII
        /// character ([ U+0000..U+007F ]).
        /// </summary>
        /// <remarks>
        /// Per http://www.unicode.org/glossary/#ASCII, ASCII is only U+0000..U+007F.
        /// </remarks>
        public static bool IsAscii(char c) => (uint)c <= '\x007f';

        // Return the Unicode category for Unicode character <= 0x00ff.
        private static UnicodeCategory GetLatin1UnicodeCategory(char c)
        {
            Debug.Assert(IsLatin1(c), "char.GetLatin1UnicodeCategory(): c should be <= 00ff");
            return (UnicodeCategory)(Latin1CharInfo[c] & UnicodeCategoryMask);
        }

        //
        // Private Constants
        //

        //
        // Overridden Instance Methods
        //

        // Calculate a hashcode for a 2 byte Unicode character.
        public override int GetHashCode()
        {
            return (int)m_value | ((int)m_value << 16);
        }

        // Used for comparing two boxed Char objects.
        //
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is char))
            {
                return false;
            }
            return m_value == ((char)obj).m_value;
        }

        [NonVersionable]
        public bool Equals(char obj)
        {
            return m_value == obj;
        }

        // Compares this object to another object, returning an integer that
        // indicates the relationship.
        // Returns a value less than zero if this  object
        // null is considered to be less than any instance.
        // If object is not of type Char, this method throws an ArgumentException.
        //
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }
            if (!(value is char))
            {
                throw new ArgumentException(SR.Arg_MustBeChar);
            }

            return m_value - ((char)value).m_value;
        }

        public int CompareTo(char value)
        {
            return m_value - value;
        }

        // Overrides System.Object.ToString.
        public override string ToString()
        {
            return char.ToString(m_value);
        }

        public string ToString(IFormatProvider? provider)
        {
            return char.ToString(m_value);
        }

        //
        // Formatting Methods
        //

        /*===================================ToString===================================
        **This static methods takes a character and returns the String representation of it.
        ==============================================================================*/
        // Provides a string representation of a character.
        public static string ToString(char c) => string.CreateFromChar(c);

        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            if (!destination.IsEmpty)
            {
                destination[0] = m_value;
                charsWritten = 1;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(m_value);

        public static char Parse(string s)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            if (s.Length != 1)
            {
                throw new FormatException(SR.Format_NeedSingleChar);
            }
            return s[0];
        }

        public static bool TryParse([NotNullWhen(true)] string? s, out char result)
        {
            result = '\0';
            if (s == null)
            {
                return false;
            }
            if (s.Length != 1)
            {
                return false;
            }
            result = s[0];
            return true;
        }

        //
        // Static Methods
        //
        /*=================================IsDigit======================================
        **A wrapper for char.  Returns a boolean indicating whether    **
        **character c is considered to be a digit.                                    **
        ==============================================================================*/
        // Determines whether a character is a digit.
        public static bool IsDigit(char c)
        {
            if (IsLatin1(c))
            {
                return IsInRange(c, '0', '9');
            }
            return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber;
        }

        internal static bool IsInRange(char c, char min, char max) => (uint)(c - min) <= (uint)(max - min);

        private static bool IsInRange(UnicodeCategory c, UnicodeCategory min, UnicodeCategory max) => (uint)(c - min) <= (uint)(max - min);

        /*=================================CheckLetter=====================================
        ** Check if the specified UnicodeCategory belongs to the letter categories.
        ==============================================================================*/
        internal static bool CheckLetter(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.UppercaseLetter, UnicodeCategory.OtherLetter);
        }

        /*=================================IsLetter=====================================
        **A wrapper for char.  Returns a boolean indicating whether    **
        **character c is considered to be a letter.                                   **
        ==============================================================================*/
        // Determines whether a character is a letter.
        public static bool IsLetter(char c)
        {
            if (IsAscii(c))
            {
                // For the version of the Unicode standard the Char type is locked to, the
                // ASCII range doesn't include letters in categories other than "upper" and "lower".
                return (Latin1CharInfo[c] & (IsUpperCaseLetterFlag | IsLowerCaseLetterFlag)) != 0;
            }
            return CheckLetter(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        private static bool IsWhiteSpaceLatin1(char c)
        {
            Debug.Assert(IsLatin1(c));
            return (Latin1CharInfo[c] & IsWhiteSpaceFlag) != 0;
        }

        /*===============================IsWhiteSpace===================================
        **A wrapper for char.  Returns a boolean indicating whether    **
        **character c is considered to be a whitespace character.                     **
        ==============================================================================*/
        // Determines whether a character is whitespace.
        public static bool IsWhiteSpace(char c)
        {
            if (IsLatin1(c))
            {
                return IsWhiteSpaceLatin1(c);
            }

            return CharUnicodeInfo.GetIsWhiteSpace(c);
        }

        /*===================================IsUpper====================================
        **Arguments: c -- the character to be checked.
        **Returns:  True if c is an uppercase character.
        ==============================================================================*/
        // Determines whether a character is upper-case.
        public static bool IsUpper(char c)
        {
            if (IsLatin1(c))
            {
                return (Latin1CharInfo[c] & IsUpperCaseLetterFlag) != 0;
            }
            return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter;
        }

        /*===================================IsLower====================================
        **Arguments: c -- the character to be checked.
        **Returns:  True if c is an lowercase character.
        ==============================================================================*/
        // Determines whether a character is lower-case.
        public static bool IsLower(char c)
        {
            if (IsLatin1(c))
            {
                return (Latin1CharInfo[c] & IsLowerCaseLetterFlag) != 0;
            }
            return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter;
        }

        internal static bool CheckPunctuation(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.OtherPunctuation);
        }

        /*================================IsPunctuation=================================
        **Arguments: c -- the character to be checked.
        **Returns:  True if c is an punctuation mark
        ==============================================================================*/
        // Determines whether a character is a punctuation mark.
        public static bool IsPunctuation(char c)
        {
            if (IsLatin1(c))
            {
                return CheckPunctuation(GetLatin1UnicodeCategory(c));
            }
            return CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        /*=================================CheckLetterOrDigit=====================================
        ** Check if the specified UnicodeCategory belongs to the letter or digit categories.
        ==============================================================================*/
        internal static bool CheckLetterOrDigit(UnicodeCategory uc)
        {
            return CheckLetter(uc) || uc == UnicodeCategory.DecimalDigitNumber;
        }

        // Determines whether a character is a letter or a digit.
        public static bool IsLetterOrDigit(char c)
        {
            if (IsLatin1(c))
            {
                return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
            }
            return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        /*===================================ToUpper====================================
        **
        ==============================================================================*/
        // Converts a character to upper-case for the specified culture.
        // <;<;Not fully implemented>;>;
        public static char ToUpper(char c, CultureInfo culture)
        {
            if (culture == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            return culture.TextInfo.ToUpper(c);
        }

        /*=================================ToUpper======================================
        **A wrapper for char.ToUpperCase.  Converts character c to its **
        **uppercase equivalent.  If c is already an uppercase character or is not an  **
        **alphabetic, nothing happens.                                                **
        ==============================================================================*/
        // Converts a character to upper-case for the default culture.
        //
        public static char ToUpper(char c)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToUpper(c);
        }

        // Converts a character to upper-case for invariant culture.
        public static char ToUpperInvariant(char c) => TextInfo.ToUpperInvariant(c);

        /*===================================ToLower====================================
        **
        ==============================================================================*/
        // Converts a character to lower-case for the specified culture.
        // <;<;Not fully implemented>;>;
        public static char ToLower(char c, CultureInfo culture)
        {
            if (culture == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            return culture.TextInfo.ToLower(c);
        }

        /*=================================ToLower======================================
        **A wrapper for char.ToLowerCase.  Converts character c to its **
        **lowercase equivalent.  If c is already a lowercase character or is not an   **
        **alphabetic, nothing happens.                                                **
        ==============================================================================*/
        // Converts a character to lower-case for the default culture.
        public static char ToLower(char c)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToLower(c);
        }

        // Converts a character to lower-case for invariant culture.
        public static char ToLowerInvariant(char c) => TextInfo.ToLowerInvariant(c);

        //
        // IConvertible implementation
        //
        public TypeCode GetTypeCode()
        {
            return TypeCode.Char;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Boolean"));
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return m_value;
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return Convert.ToSByte(m_value);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return Convert.ToByte(m_value);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return Convert.ToInt16(m_value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return Convert.ToUInt16(m_value);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return Convert.ToInt32(m_value);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return Convert.ToUInt32(m_value);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return Convert.ToInt64(m_value);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return Convert.ToUInt64(m_value);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Single"));
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Double"));
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "Decimal"));
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Char", "DateTime"));
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }

        public static bool IsControl(char c)
        {
            // This works because 'c' can never be -1.
            // See comments in Rune.IsControl for more information.

            return (((uint)c + 1) & ~0x80u) <= 0x20u;
        }

        public static bool IsControl(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            // Control chars are always in the BMP, so don't need to worry about surrogate handling.
            return IsControl(s[index]);
        }

        public static bool IsDigit(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return IsInRange(c, '0', '9');
            }

            return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.DecimalDigitNumber;
        }

        public static bool IsLetter(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsAscii(c))
            {
                // The ASCII range doesn't include letters in categories other than "upper" and "lower"
                return (Latin1CharInfo[c] & (IsUpperCaseLetterFlag | IsLowerCaseLetterFlag)) != 0;
            }

            return CheckLetter(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        public static bool IsLetterOrDigit(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
            }

            return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        public static bool IsLower(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return (Latin1CharInfo[c] & IsLowerCaseLetterFlag) != 0;
            }

            return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.LowercaseLetter;
        }

        /*=================================CheckNumber=====================================
        ** Check if the specified UnicodeCategory belongs to the number categories.
        ==============================================================================*/

        internal static bool CheckNumber(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.DecimalDigitNumber, UnicodeCategory.OtherNumber);
        }

        public static bool IsNumber(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, '0', '9');
                }
                return CheckNumber(GetLatin1UnicodeCategory(c));
            }
            return CheckNumber(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        public static bool IsNumber(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, '0', '9');
                }

                return CheckNumber(GetLatin1UnicodeCategory(c));
            }

            return CheckNumber(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        ////////////////////////////////////////////////////////////////////////
        //
        //  IsPunctuation
        //
        //  Determines if the given character is a punctuation character.
        //
        ////////////////////////////////////////////////////////////////////////

        public static bool IsPunctuation(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return CheckPunctuation(GetLatin1UnicodeCategory(c));
            }

            return CheckPunctuation(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        /*================================= CheckSeparator ============================
        ** Check if the specified UnicodeCategory belongs to the separator categories.
        ==============================================================================*/

        internal static bool CheckSeparator(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.SpaceSeparator, UnicodeCategory.ParagraphSeparator);
        }

        private static bool IsSeparatorLatin1(char c)
        {
            // U+00a0 = NO-BREAK SPACE
            // There is no LineSeparator or ParagraphSeparator in Latin 1 range.
            return c == '\x0020' || c == '\x00a0';
        }

        public static bool IsSeparator(char c)
        {
            if (IsLatin1(c))
            {
                return IsSeparatorLatin1(c);
            }
            return CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        public static bool IsSeparator(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return IsSeparatorLatin1(c);
            }

            return CheckSeparator(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        public static bool IsSurrogate(char c)
        {
            return IsInRange(c, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END);
        }

        public static bool IsSurrogate(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return IsSurrogate(s[index]);
        }

        /*================================= CheckSymbol ============================
         ** Check if the specified UnicodeCategory belongs to the symbol categories.
         ==============================================================================*/

        internal static bool CheckSymbol(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.MathSymbol, UnicodeCategory.OtherSymbol);
        }

        public static bool IsSymbol(char c)
        {
            if (IsLatin1(c))
            {
                return CheckSymbol(GetLatin1UnicodeCategory(c));
            }
            return CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(c));
        }

        public static bool IsSymbol(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return CheckSymbol(GetLatin1UnicodeCategory(c));
            }

            return CheckSymbol(CharUnicodeInfo.GetUnicodeCategoryInternal(s, index));
        }

        public static bool IsUpper(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            char c = s[index];
            if (IsLatin1(c))
            {
                return (Latin1CharInfo[c] & IsUpperCaseLetterFlag) != 0;
            }

            return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index) == UnicodeCategory.UppercaseLetter;
        }

        public static bool IsWhiteSpace(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            // All white space code points are within the BMP,
            // so we don't need to handle surrogate pairs here.

            return IsWhiteSpace(s[index]);
        }

        public static UnicodeCategory GetUnicodeCategory(char c)
        {
            if (IsLatin1(c))
            {
                return GetLatin1UnicodeCategory(c);
            }
            return CharUnicodeInfo.GetUnicodeCategory((int)c);
        }

        public static UnicodeCategory GetUnicodeCategory(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            if (IsLatin1(s[index]))
            {
                return GetLatin1UnicodeCategory(s[index]);
            }

            return CharUnicodeInfo.GetUnicodeCategoryInternal(s, index);
        }

        public static double GetNumericValue(char c)
        {
            return CharUnicodeInfo.GetNumericValue(c);
        }

        public static double GetNumericValue(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return CharUnicodeInfo.GetNumericValueInternal(s, index);
        }

        /*================================= IsHighSurrogate ============================
         ** Check if a char is a high surrogate.
         ==============================================================================*/
        public static bool IsHighSurrogate(char c)
        {
            return IsInRange(c, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.HIGH_SURROGATE_END);
        }

        public static bool IsHighSurrogate(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return IsHighSurrogate(s[index]);
        }

        /*================================= IsLowSurrogate ============================
         ** Check if a char is a low surrogate.
         ==============================================================================*/
        public static bool IsLowSurrogate(char c)
        {
            return IsInRange(c, CharUnicodeInfo.LOW_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END);
        }

        public static bool IsLowSurrogate(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return IsLowSurrogate(s[index]);
        }

        /*================================= IsSurrogatePair ============================
         ** Check if the string specified by the index starts with a surrogate pair.
         ==============================================================================*/
        public static bool IsSurrogatePair(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            if (index + 1 < s.Length)
            {
                return IsSurrogatePair(s[index], s[index + 1]);
            }

            return false;
        }

        public static bool IsSurrogatePair(char highSurrogate, char lowSurrogate)
        {
            // Since both the high and low surrogate ranges are exactly 0x400 elements
            // wide, and since this is a power of two, we can perform a single comparison
            // by baselining each value to the start of its respective range and taking
            // the logical OR of them.

            uint highSurrogateOffset = (uint)highSurrogate - CharUnicodeInfo.HIGH_SURROGATE_START;
            uint lowSurrogateOffset = (uint)lowSurrogate - CharUnicodeInfo.LOW_SURROGATE_START;
            return (highSurrogateOffset | lowSurrogateOffset) <= CharUnicodeInfo.HIGH_SURROGATE_RANGE;
        }

        internal const int UNICODE_PLANE00_END = 0x00ffff;
        // The starting codepoint for Unicode plane 1.  Plane 1 contains 0x010000 ~ 0x01ffff.
        internal const int UNICODE_PLANE01_START = 0x10000;
        // The end codepoint for Unicode plane 16.  This is the maximum code point value allowed for Unicode.
        // Plane 16 contains 0x100000 ~ 0x10ffff.
        internal const int UNICODE_PLANE16_END = 0x10ffff;

        /*================================= ConvertFromUtf32 ============================
         ** Convert an UTF32 value into a surrogate pair.
         ==============================================================================*/

        public static string ConvertFromUtf32(int utf32)
        {
            if (!UnicodeUtility.IsValidUnicodeScalar((uint)utf32))
            {
                throw new ArgumentOutOfRangeException(nameof(utf32), SR.ArgumentOutOfRange_InvalidUTF32);
            }

            return Rune.UnsafeCreate((uint)utf32).ToString();
        }

        /*=============================ConvertToUtf32===================================
        ** Convert a surrogate pair to UTF32 value
        ==============================================================================*/

        public static int ConvertToUtf32(char highSurrogate, char lowSurrogate)
        {
            // First, extend both to 32 bits, then calculate the offset of
            // each candidate surrogate char from the start of its range.

            uint highSurrogateOffset = (uint)highSurrogate - CharUnicodeInfo.HIGH_SURROGATE_START;
            uint lowSurrogateOffset = (uint)lowSurrogate - CharUnicodeInfo.LOW_SURROGATE_START;

            // This is a single comparison which allows us to check both for validity at once since
            // both the high surrogate range and the low surrogate range are the same length.
            // If the comparison fails, we call to a helper method to throw the correct exception message.

            if ((highSurrogateOffset | lowSurrogateOffset) > CharUnicodeInfo.HIGH_SURROGATE_RANGE)
            {
                ConvertToUtf32_ThrowInvalidArgs(highSurrogateOffset);
            }

            // The 0x40u << 10 below is to account for uuuuu = wwww + 1 in the surrogate encoding.
            return ((int)highSurrogateOffset << 10) + (lowSurrogate - CharUnicodeInfo.LOW_SURROGATE_START) + (0x40 << 10);
        }

        [StackTraceHidden]
        private static void ConvertToUtf32_ThrowInvalidArgs(uint highSurrogateOffset)
        {
            // If the high surrogate is not within its expected range, throw an exception
            // whose message fingers it as invalid. If it's within the expected range,
            // change the message to read that the low surrogate was the problem.

            if (highSurrogateOffset > CharUnicodeInfo.HIGH_SURROGATE_RANGE)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: "highSurrogate",
                    message: SR.ArgumentOutOfRange_InvalidHighSurrogate);
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    paramName: "lowSurrogate",
                    message: SR.ArgumentOutOfRange_InvalidLowSurrogate);
            }
        }

        /*=============================ConvertToUtf32===================================
        ** Convert a character or a surrogate pair starting at index of the specified string
        ** to UTF32 value.
        ** The char pointed by index should be a surrogate pair or a BMP character.
        ** This method throws if a high-surrogate is not followed by a low surrogate.
        ** This method throws if a low surrogate is seen without preceding a high-surrogate.
        ==============================================================================*/

        public static int ConvertToUtf32(string s, int index)
        {
            if (s == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexMustBeLess);
            }
            // Check if the character at index is a high surrogate.
            int temp1 = (int)s[index] - CharUnicodeInfo.HIGH_SURROGATE_START;
            if (temp1 >= 0 && temp1 <= 0x7ff)
            {
                // Found a surrogate char.
                if (temp1 <= 0x3ff)
                {
                    // Found a high surrogate.
                    if (index < s.Length - 1)
                    {
                        int temp2 = (int)s[index + 1] - CharUnicodeInfo.LOW_SURROGATE_START;
                        if (temp2 >= 0 && temp2 <= 0x3ff)
                        {
                            // Found a low surrogate.
                            return (temp1 * 0x400) + temp2 + UNICODE_PLANE01_START;
                        }
                        else
                        {
                            throw new ArgumentException(SR.Format(SR.Argument_InvalidHighSurrogate, index), nameof(s));
                        }
                    }
                    else
                    {
                        // Found a high surrogate at the end of the string.
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidHighSurrogate, index), nameof(s));
                    }
                }
                else
                {
                    // Find a low surrogate at the character pointed by index.
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidLowSurrogate, index), nameof(s));
                }
            }
            // Not a high-surrogate or low-surrogate. Genereate the UTF32 value for the BMP characters.
            return (int)s[index];
        }

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static char IAdditionOperators<char, char, char>.operator +(char left, char right) => (char) (left + right);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static char IAdditionOperators<char, char, char>.operator checked +(char left, char right) => checked((char)(left + right));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static char IAdditiveIdentity<char, char>.AdditiveIdentity => (char)0;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (char Quotient, char Remainder) DivRem(char left, char right) => ((char, char))Math.DivRem(left, right);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static char LeadingZeroCount(char value) => (char)(BitOperations.LeadingZeroCount(value) - 16);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static char PopCount(char value) => (char)BitOperations.PopCount(value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static char RotateLeft(char value, int rotateAmount) => (char)((value << (rotateAmount & 15)) | (value >> ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static char RotateRight(char value, int rotateAmount) => (char)((value >> (rotateAmount & 15)) | (value << ((16 - rotateAmount) & 15)));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static char TrailingZeroCount(char value) => (char)(BitOperations.TrailingZeroCount(value << 16) - 16);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        long IBinaryInteger<char>.GetShortestBitLength() => (sizeof(char) * 8) - LeadingZeroCount(m_value);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<char>.GetByteCount() => sizeof(char);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<char>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(char))
            {
                ushort value = BitConverter.IsLittleEndian ? m_value : BinaryPrimitives.ReverseEndianness(m_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(char);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(char value) => BitOperations.IsPow2((uint)value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static char Log2(char value) => (char)BitOperations.Log2(value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static char IBitwiseOperators<char, char, char>.operator &(char left, char right) => (char)(left & right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static char IBitwiseOperators<char, char, char>.operator |(char left, char right) => (char)(left | right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static char IBitwiseOperators<char, char, char>.operator ^(char left, char right) => (char)(left ^ right);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static char IBitwiseOperators<char, char, char>.operator ~(char value) => (char)(~value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        static bool IComparisonOperators<char, char>.operator <(char left, char right) => left < right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<char, char>.operator <=(char left, char right) => left <= right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        static bool IComparisonOperators<char, char>.operator >(char left, char right) => left > right;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        static bool IComparisonOperators<char, char>.operator >=(char left, char right) => left >= right;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static char IDecrementOperators<char>.operator --(char value) => --value;

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_CheckedDecrement(TSelf)" />
        static char IDecrementOperators<char>.operator checked --(char value) => checked(--value);

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        static char IDivisionOperators<char, char, char>.operator /(char left, char right) => (char)(left / right);

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static char IDivisionOperators<char, char, char>.operator checked /(char left, char right) => (char)(left / right);

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        static bool IEqualityOperators<char, char>.operator ==(char left, char right) => left == right;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        static bool IEqualityOperators<char, char>.operator !=(char left, char right) => left != right;

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        static char IIncrementOperators<char>.operator ++(char value) => ++value;

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static char IIncrementOperators<char>.operator checked ++(char value) => checked(++value);

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        static char IMinMaxValue<char>.MinValue => MinValue;

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        static char IMinMaxValue<char>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        static char IModulusOperators<char, char, char>.operator %(char left, char right) => (char)(left % right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static char IMultiplicativeIdentity<char, char>.MultiplicativeIdentity => (char)1;

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        static char IMultiplyOperators<char, char, char>.operator *(char left, char right) => (char)(left * right);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static char IMultiplyOperators<char, char, char>.operator checked *(char left, char right) => checked((char)(left * right));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        static char INumber<char>.Abs(char value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static char Clamp(char value, char min, char max) => (char)Math.Clamp(value, min, max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        static char INumber<char>.CopySign(char value, char sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (char)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return checked((char)(decimal)(object)value);
            }
            else if (typeof(TOther) == typeof(double))
            {
                return checked((char)(double)(object)value);
            }
            else if (typeof(TOther) == typeof(short))
            {
                return checked((char)(short)(object)value);
            }
            else if (typeof(TOther) == typeof(int))
            {
                return checked((char)(int)(object)value);
            }
            else if (typeof(TOther) == typeof(long))
            {
                return checked((char)(long)(object)value);
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return checked((char)(nint)(object)value);
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return checked((char)(sbyte)(object)value);
            }
            else if (typeof(TOther) == typeof(float))
            {
                return checked((char)(float)(object)value);
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (char)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return checked((char)(uint)(object)value);
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return checked((char)(ulong)(object)value);
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return checked((char)(nuint)(object)value);
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (char)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;
                return (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;
                return (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;
                return (actualValue > MaxValue) ? MaxValue :
                       (actualValue < MinValue) ? MinValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (char)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (char)actualValue;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;
                return (actualValue > MaxValue) ? MaxValue : (char)actualValue;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (char)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (char)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (char)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (char)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (char)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (char)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (char)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (char)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (char)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (char)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (char)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (char)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (char)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        static bool INumber<char>.IsNegative(char value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static char Max(char x, char y) => (char)Math.Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        static char INumber<char>.MaxMagnitude(char x, char y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static char Min(char x, char y) => (char)Math.Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        static char INumber<char>.MinMagnitude(char x, char y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(char value) => (value == 0) ? 0 : 1;

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out char result)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (char)(byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                var actualValue = (decimal)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                var actualValue = (double)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                var actualValue = (short)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                var actualValue = (int)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                var actualValue = (long)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                var actualValue = (nint)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                var actualValue = (sbyte)(object)value;

                if (actualValue < 0)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                var actualValue = (float)(object)value;

                if ((actualValue < 0) || (actualValue > MaxValue))
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                var actualValue = (ushort)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                var actualValue = (uint)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                var actualValue = (ulong)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                var actualValue = (nuint)(object)value;

                if (actualValue > MaxValue)
                {
                    result = default;
                    return false;
                }

                result = (char)actualValue;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        static char INumber<char>.Parse(string s, NumberStyles style, IFormatProvider? provider) => Parse(s);

        static char INumber<char>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
        {
            if (s.Length != 1)
            {
                throw new FormatException(SR.Format_NeedSingleChar);
            }
            return s[0];
        }

        static bool INumber<char>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out char result) => TryParse(s, out result);

        static bool INumber<char>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out char result)
        {
            if (s.Length != 1)
            {
                result = default;
                return false;
            }
            result = s[0];
            return true;
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static char INumberBase<char>.One => (char)1;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static char INumberBase<char>.Zero => (char)0;

        //
        // IParsable
        //

        static char IParsable<char>.Parse(string s, IFormatProvider? provider) => Parse(s);

        static bool IParsable<char>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out char result) => TryParse(s, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        static char IShiftOperators<char, char>.operator <<(char value, int shiftAmount) => (char)(value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        static char IShiftOperators<char, char>.operator >>(char value, int shiftAmount) => (char)(value >> shiftAmount);

        // /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        // static char IShiftOperators<char, char>.operator >>>(char value, int shiftAmount) => (char)(value >> shiftAmount);

        //
        // ISpanParsable
        //

        static char ISpanParsable<char>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (s.Length != 1)
            {
                throw new FormatException(SR.Format_NeedSingleChar);
            }
            return s[0];
        }

        static bool ISpanParsable<char>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out char result)
        {
            if (s.Length != 1)
            {
                result = default;
                return false;
            }
            result = s[0];
            return true;
        }

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        static char ISubtractionOperators<char, char, char>.operator -(char left, char right) => (char)(left - right);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static char ISubtractionOperators<char, char, char>.operator checked -(char left, char right) => checked((char)(left - right));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static char IUnaryNegationOperators<char, char>.operator -(char value) => (char)(-value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static char IUnaryNegationOperators<char, char>.operator checked -(char value) => checked((char)(-value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        static char IUnaryPlusOperators<char, char>.operator +(char value) => (char)(+value);
    }
}
