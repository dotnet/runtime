// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization
{
    /// <summary>
    /// Defines the string comparison options to use with <see cref="CompareInfo"/>.
    /// </summary>
    [Flags]
    public enum CompareOptions
    {
        /// <summary>
        /// Indicates the default option settings for string comparisons
        /// </summary>
        None = 0x00000000,

        /// <summary>
        /// Indicates that the string comparison must ignore case.
        /// </summary>
        IgnoreCase = 0x00000001,

        /// <summary>
        /// Indicates that the string comparison must ignore nonspacing combining characters, such as diacritics.
        /// The <see href="https://go.microsoft.com/fwlink/?linkid=37123">Unicode Standard</see> defines combining characters as
        /// characters that are combined with base characters to produce a new character. Nonspacing combining characters do not
        /// occupy a spacing position by themselves when rendered.
        /// </summary>
        IgnoreNonSpace = 0x00000002,

        /// <summary>
        /// Indicates that the string comparison must ignore symbols, such as white-space characters, punctuation, currency symbols,
        /// the percent sign, mathematical symbols, the ampersand, and so on.
        /// </summary>
        IgnoreSymbols = 0x00000004,

        /// <summary>
        /// Indicates that the string comparison must ignore the Kana type. Kana type refers to Japanese hiragana and katakana characters, which represent phonetic sounds in the Japanese language.
        /// Hiragana is used for native Japanese expressions and words, while katakana is used for words borrowed from other languages, such as "computer" or "Internet".
        /// A phonetic sound can be expressed in both hiragana and katakana. If this value is selected, the hiragana character for one sound is considered equal to the katakana character for the same sound.
        /// </summary>
        IgnoreKanaType = 0x00000008,

        /// <summary>
        /// Indicates that the string comparison must ignore the character width. For example, Japanese katakana characters can be written as full-width or half-width.
        /// If this value is selected, the katakana characters written as full-width are considered equal to the same characters written as half-width.
        /// </summary>
        IgnoreWidth = 0x00000010,

        /// <summary>
        /// Indicates that the string comparison must sort sequences of digits (Unicode general category "Nd") based on their numeric value.
        /// For example, "2" comes before "10". Non-digit characters such as decimal points, minus or plus signs, etc.
        /// are not considered as part of the sequence and will terminate it. This flag is not valid for indexing
        /// (such as <see cref="CompareInfo.IndexOf(string, string, CompareOptions)"/>, <see cref="CompareInfo.IsPrefix(string, string, CompareOptions)"/>, etc.).
        /// </summary>
        NumericOrdering = 0x00000020,

        /// <summary>
        /// String comparison must ignore case, then perform an ordinal comparison. This technique is equivalent to
        /// converting the string to uppercase using the invariant culture and then performing an ordinal comparison on the result.
        /// This value cannot be combined with other <see cref="CompareOptions" /> values and must be used alone.
        /// </summary>
        OrdinalIgnoreCase = 0x10000000, // This flag can not be used with other flags.

        /// <summary>
        /// Indicates that the string comparison must use the string sort algorithm. In a string sort, the hyphen and the apostrophe,
        /// as well as other nonalphanumeric symbols, come before alphanumeric characters.
        /// </summary>
        StringSort = 0x20000000,

        /// <summary>
        /// Indicates that the string comparison must use successive Unicode UTF-16 encoded values of the string (code unit by code unit comparison),
        /// leading to a fast comparison but one that is culture-insensitive. A string starting with a code unit XXXX16 comes before a string starting with YYYY16,
        /// if XXXX16 is less than YYYY16. This value cannot be combined with other <see cref="CompareOptions" /> values and must be used alone.
        /// </summary>
        Ordinal = 0x40000000, // This flag can not be used with other flags.
    }
}
