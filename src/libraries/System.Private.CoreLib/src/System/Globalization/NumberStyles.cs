// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    /// <summary>
    /// Contains valid formats for Numbers recognized by the Number
    /// class' parsing code.
    /// </summary>
    [Flags]
    public enum NumberStyles
    {
        None = 0x00000000,

        /// <summary>
        /// Bit flag indicating that leading whitespace is allowed. Character values
        /// 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, and 0x0020 are considered to be
        /// whitespace.
        /// </summary>
        AllowLeadingWhite = 0x00000001,

        /// <summary>
        /// Bitflag indicating trailing whitespace is allowed.
        /// </summary>
        AllowTrailingWhite = 0x00000002,

        /// <summary>
        /// Can the number start with a sign char specified by
        /// NumberFormatInfo.PositiveSign and NumberFormatInfo.NegativeSign
        /// </summary>
        AllowLeadingSign = 0x00000004,

        /// <summary>
        /// Allow the number to end with a sign char
        /// </summary>
        AllowTrailingSign = 0x00000008,

        /// <summary>
        /// Allow the number to be enclosed in parens
        /// </summary>
        AllowParentheses = 0x00000010,

        AllowDecimalPoint = 0x00000020,

        AllowThousands = 0x00000040,

        AllowExponent = 0x00000080,

        AllowCurrencySymbol = 0x00000100,

        AllowHexSpecifier = 0x00000200,

        /// <summary>
        /// Indicates that the numeric string represents a binary value. Valid binary values include the numeric digits 0 and 1.
        /// Strings that are parsed using this style do not employ a prefix; "0b" cannot be used. A string that is parsed with
        /// the <see cref="AllowBinarySpecifier"/> style will always be interpreted as a binary value. The only flags that can
        /// be combined with <see cref="AllowBinarySpecifier"/> are <see cref="AllowLeadingWhite"/> and <see cref="AllowTrailingWhite"/>.
        /// The <see cref="NumberStyles"/> enumeration includes a composite style, <see cref="BinaryNumber"/>, that consists of
        /// these three flags.
        /// </summary>
        AllowBinarySpecifier = 0x00000400,

        Integer = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign,

        HexNumber = AllowLeadingWhite | AllowTrailingWhite | AllowHexSpecifier,

        /// <summary>Indicates that the <see cref="AllowLeadingWhite"/>, <see cref="AllowTrailingWhite"/>, and <see cref="AllowBinarySpecifier"/> styles are used. This is a composite number style.</summary>
        BinaryNumber = AllowLeadingWhite | AllowTrailingWhite | AllowBinarySpecifier,

        Number = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowDecimalPoint | AllowThousands,

        Float = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign |
                   AllowDecimalPoint | AllowExponent,

        /// <summary>
        /// Indicates that the <see cref="AllowLeadingWhite"/>, <see cref="AllowTrailingWhite"/>,
        /// <see cref="AllowLeadingSign"/>, <see cref="AllowHexSpecifier"/>, and <see cref="AllowDecimalPoint"/>
        /// styles are used. This is a composite number style used for parsing hexadecimal floating-point values
        /// based on the syntax defined in IEEE 754:2008 §5.12.3. The parsed string must include a "0x" or "0X"
        /// prefix, followed by a hexadecimal significand with an optional decimal point, and a binary exponent
        /// introduced by 'p' or 'P'. Integer-only hexadecimal values must also include the 'p'/'P' exponent.
        /// </summary>
        /// <remarks>
        /// Note that unlike <see cref="HexNumber"/> for integer types (which rejects a "0x"/"0X" prefix),
        /// <see cref="HexFloat"/> requires the prefix. This difference exists because the
        /// IEEE 754 hex float grammar (e.g., <c>0x1.921fb54442d18p+1</c>) naturally includes the prefix.
        /// </remarks>
        HexFloat = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowHexSpecifier | AllowDecimalPoint,

        Currency = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowParentheses | AllowDecimalPoint | AllowThousands | AllowCurrencySymbol,

        Any = AllowLeadingWhite | AllowTrailingWhite | AllowLeadingSign | AllowTrailingSign |
                   AllowParentheses | AllowDecimalPoint | AllowThousands | AllowCurrencySymbol | AllowExponent,
    }
}
