// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: This is the value class representing a Unicode character
** Char methods until we create this functionality.
**
**
===========================================================*/

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    public readonly struct Char : IComparable, IComparable<char>, IEquatable<char>, IConvertible
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

        // Unicode category values from Unicode U+0000 ~ U+00FF. Store them in byte[] array to save space.
        private static ReadOnlySpan<byte> CategoryForLatin1 => new byte[] { // uses C# compiler's optimization for static byte[] data
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0000 - 0007
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0008 - 000F
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0010 - 0017
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0018 - 001F
            (byte)UnicodeCategory.SpaceSeparator, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation,    // 0020 - 0027
            (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.DashPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation,    // 0028 - 002F
            (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber,    // 0030 - 0037
            (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.DecimalDigitNumber, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherPunctuation,    // 0038 - 003F
            (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter,    // 0040 - 0047
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter,    // 0048 - 004F
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter,    // 0050 - 0057
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.ConnectorPunctuation,    // 0058 - 005F
            (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 0060 - 0067
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 0068 - 006F
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 0070 - 0077
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.OpenPunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.ClosePunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.Control,    // 0078 - 007F
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0080 - 0087
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0088 - 008F
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0090 - 0097
            (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control, (byte)UnicodeCategory.Control,    // 0098 - 009F
            (byte)UnicodeCategory.SpaceSeparator, (byte)UnicodeCategory.OtherPunctuation, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.CurrencySymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.OtherSymbol,    // 00A0 - 00A7
            (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.InitialQuotePunctuation, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.DashPunctuation, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.ModifierSymbol,    // 00A8 - 00AF
            (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.MathSymbol, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.OtherSymbol, (byte)UnicodeCategory.OtherPunctuation,    // 00B0 - 00B7
            (byte)UnicodeCategory.ModifierSymbol, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.FinalQuotePunctuation, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherNumber, (byte)UnicodeCategory.OtherPunctuation,    // 00B8 - 00BF
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter,    // 00C0 - 00C7
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter,    // 00C8 - 00CF
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.MathSymbol,    // 00D0 - 00D7
            (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.UppercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 00D8 - 00DF
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 00E0 - 00E7
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 00E8 - 00EF
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.MathSymbol,    // 00F0 - 00F7
            (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter, (byte)UnicodeCategory.LowercaseLetter,    // 00F8 - 00FF
        };

        // Return true for all characters below or equal U+00ff, which is ASCII + Latin-1 Supplement.
        private static bool IsLatin1(char ch)
        {
            return (uint)ch <= '\x00ff';
        }

        // Return true for all characters below or equal U+007f, which is ASCII.
        private static bool IsAscii(char ch)
        {
            return (uint)ch <= '\x007f';
        }

        // Return the Unicode category for Unicode character <= 0x00ff.      
        private static UnicodeCategory GetLatin1UnicodeCategory(char ch)
        {
            Debug.Assert(IsLatin1(ch), "char.GetLatin1UnicodeCategory(): ch should be <= 007f");
            return (UnicodeCategory)CategoryForLatin1[(int)ch];
        }

        //
        // Private Constants
        //

        //
        // Overriden Instance Methods
        //

        // Calculate a hashcode for a 2 byte Unicode character.
        public override int GetHashCode()
        {
            return (int)m_value | ((int)m_value << 16);
        }

        // Used for comparing two boxed Char objects.
        //
        public override bool Equals(object? obj)
        {
            if (!(obj is char))
            {
                return false;
            }
            return (m_value == ((char)obj).m_value);
        }

        [System.Runtime.Versioning.NonVersionable]
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

            return (m_value - ((char)value).m_value);
        }

        public int CompareTo(char value)
        {
            return (m_value - value);
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

        public static char Parse(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (s.Length != 1)
            {
                throw new FormatException(SR.Format_NeedSingleChar);
            }
            return s[0];
        }

        public static bool TryParse(string? s, out char result)
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
        /*=================================ISDIGIT======================================
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
            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber);
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

        /*=================================ISLETTER=====================================
        **A wrapper for char.  Returns a boolean indicating whether    **
        **character c is considered to be a letter.                                   **
        ==============================================================================*/
        // Determines whether a character is a letter.
        public static bool IsLetter(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    c |= (char)0x20;
                    return IsInRange(c, 'a', 'z');
                }
                return (CheckLetter(GetLatin1UnicodeCategory(c)));
            }
            return (CheckLetter(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        private static bool IsWhiteSpaceLatin1(char c)
        {
            // There are characters which belong to UnicodeCategory.Control but are considered as white spaces.
            // We use code point comparisons for these characters here as a temporary fix.

            // U+0009 = <control> HORIZONTAL TAB
            // U+000a = <control> LINE FEED
            // U+000b = <control> VERTICAL TAB
            // U+000c = <contorl> FORM FEED
            // U+000d = <control> CARRIAGE RETURN
            // U+0085 = <control> NEXT LINE
            // U+00a0 = NO-BREAK SPACE
            return
                c == ' ' ||
                (uint)(c - '\x0009') <= ('\x000d' - '\x0009') || // (c >= '\x0009' && c <= '\x000d')
                c == '\x00a0' ||
                c == '\x0085';
        }

        /*===============================ISWHITESPACE===================================
        **A wrapper for char.  Returns a boolean indicating whether    **
        **character c is considered to be a whitespace character.                     **
        ==============================================================================*/
        // Determines whether a character is whitespace.
        public static bool IsWhiteSpace(char c)
        {
            if (IsLatin1(c))
            {
                return (IsWhiteSpaceLatin1(c));
            }
            return CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c));
        }


        /*===================================IsUpper====================================
        **Arguments: c -- the characater to be checked.
        **Returns:  True if c is an uppercase character.
        ==============================================================================*/
        // Determines whether a character is upper-case.
        public static bool IsUpper(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, 'A', 'Z');
                }
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
            }
            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
        }

        /*===================================IsLower====================================
        **Arguments: c -- the characater to be checked.
        **Returns:  True if c is an lowercase character.
        ==============================================================================*/
        // Determines whether a character is lower-case.
        public static bool IsLower(char c)
        {
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, 'a', 'z');
                }
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
            }
            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
        }

        internal static bool CheckPunctuation(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.OtherPunctuation);
        }


        /*================================IsPunctuation=================================
        **Arguments: c -- the characater to be checked.
        **Returns:  True if c is an punctuation mark
        ==============================================================================*/
        // Determines whether a character is a punctuation mark.
        public static bool IsPunctuation(char c)
        {
            if (IsLatin1(c))
            {
                return (CheckPunctuation(GetLatin1UnicodeCategory(c)));
            }
            return (CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(c)));
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
                return (CheckLetterOrDigit(GetLatin1UnicodeCategory(c)));
            }
            return (CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        /*===================================ToUpper====================================
        **
        ==============================================================================*/
        // Converts a character to upper-case for the specified culture.
        // <;<;Not fully implemented>;>;
        public static char ToUpper(char c, CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));
            return culture.TextInfo.ToUpper(c);
        }

        /*=================================TOUPPER======================================
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
        public static char ToUpperInvariant(char c)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToUpper(c);
        }


        /*===================================ToLower====================================
        **
        ==============================================================================*/
        // Converts a character to lower-case for the specified culture.
        // <;<;Not fully implemented>;>;
        public static char ToLower(char c, CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));
            return culture.TextInfo.ToLower(c);
        }

        /*=================================TOLOWER======================================
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
        public static char ToLowerInvariant(char c)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToLower(c);
        }


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
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.Control);
            }
            return (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Control);
        }

        public static bool IsControl(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.Control);
            }
            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.Control);
        }


        public static bool IsDigit(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return IsInRange(c, '0', '9');
            }
            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.DecimalDigitNumber);
        }

        public static bool IsLetter(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    c |= (char)0x20;
                    return IsInRange(c, 'a', 'z');
                }
                return (CheckLetter(GetLatin1UnicodeCategory(c)));
            }
            return (CheckLetter(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsLetterOrDigit(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return CheckLetterOrDigit(GetLatin1UnicodeCategory(c));
            }
            return CheckLetterOrDigit(CharUnicodeInfo.GetUnicodeCategory(s, index));
        }

        public static bool IsLower(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, 'a', 'z');
                }
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.LowercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.LowercaseLetter);
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
                return (CheckNumber(GetLatin1UnicodeCategory(c)));
            }
            return (CheckNumber(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsNumber(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, '0', '9');
                }
                return (CheckNumber(GetLatin1UnicodeCategory(c)));
            }
            return (CheckNumber(CharUnicodeInfo.GetUnicodeCategory(s, index)));
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
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return (CheckPunctuation(GetLatin1UnicodeCategory(c)));
            }
            return (CheckPunctuation(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }


        /*================================= CheckSeparator ============================
        ** Check if the specified UnicodeCategory belongs to the seprator categories.
        ==============================================================================*/

        internal static bool CheckSeparator(UnicodeCategory uc)
        {
            return IsInRange(uc, UnicodeCategory.SpaceSeparator, UnicodeCategory.ParagraphSeparator);
        }

        private static bool IsSeparatorLatin1(char c)
        {
            // U+00a0 = NO-BREAK SPACE
            // There is no LineSeparator or ParagraphSeparator in Latin 1 range.
            return (c == '\x0020' || c == '\x00a0');
        }

        public static bool IsSeparator(char c)
        {
            if (IsLatin1(c))
            {
                return (IsSeparatorLatin1(c));
            }
            return (CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsSeparator(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return (IsSeparatorLatin1(c));
            }
            return (CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }

        public static bool IsSurrogate(char c)
        {
            return IsInRange(c, CharUnicodeInfo.HIGH_SURROGATE_START, CharUnicodeInfo.LOW_SURROGATE_END);
        }

        public static bool IsSurrogate(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return (IsSurrogate(s[index]));
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
                return (CheckSymbol(GetLatin1UnicodeCategory(c)));
            }
            return (CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(c)));
        }

        public static bool IsSymbol(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                return (CheckSymbol(GetLatin1UnicodeCategory(c)));
            }
            return (CheckSymbol(CharUnicodeInfo.GetUnicodeCategory(s, index)));
        }


        public static bool IsUpper(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            char c = s[index];
            if (IsLatin1(c))
            {
                if (IsAscii(c))
                {
                    return IsInRange(c, 'A', 'Z');
                }
                return (GetLatin1UnicodeCategory(c) == UnicodeCategory.UppercaseLetter);
            }

            return (CharUnicodeInfo.GetUnicodeCategory(s, index) == UnicodeCategory.UppercaseLetter);
        }

        public static bool IsWhiteSpace(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (IsLatin1(s[index]))
            {
                return IsWhiteSpaceLatin1(s[index]);
            }

            return CheckSeparator(CharUnicodeInfo.GetUnicodeCategory(s, index));
        }

        public static UnicodeCategory GetUnicodeCategory(char c)
        {
            if (IsLatin1(c))
            {
                return (GetLatin1UnicodeCategory(c));
            }
            return CharUnicodeInfo.GetUnicodeCategory((int)c);
        }

        public static UnicodeCategory GetUnicodeCategory(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (IsLatin1(s[index]))
            {
                return (GetLatin1UnicodeCategory(s[index]));
            }
            return CharUnicodeInfo.InternalGetUnicodeCategory(s, index);
        }

        public static double GetNumericValue(char c)
        {
            return CharUnicodeInfo.GetNumericValue(c);
        }

        public static double GetNumericValue(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s));
            if (((uint)index) >= ((uint)s.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return CharUnicodeInfo.GetNumericValue(s, index);
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
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return (IsHighSurrogate(s[index]));
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
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return (IsLowSurrogate(s[index]));
        }

        /*================================= IsSurrogatePair ============================
         ** Check if the string specified by the index starts with a surrogate pair.
         ==============================================================================*/
        public static bool IsSurrogatePair(string s, int index)
        {
            if (s == null)
            {
                throw new ArgumentNullException(nameof(s));
            }
            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            if (index + 1 < s.Length)
            {
                return (IsSurrogatePair(s[index], s[index + 1]));
            }
            return (false);
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
                throw new ArgumentNullException(nameof(s));
            }

            if (index < 0 || index >= s.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
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
                            return ((temp1 * 0x400) + temp2 + UNICODE_PLANE01_START);
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
            return ((int)s[index]);
        }
    }
}
