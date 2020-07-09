// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    /// <summary>
    /// Code point properties from UAX#44.
    /// </summary>
    /// <remarks>
    /// See https://www.unicode.org/reports/tr44/#PropList.txt
    /// and https://www.unicode.org/Public/UCD/latest/ucd/PropList.txt.
    /// </remarks>
    [Flags]
    public enum CodePointFlags : ulong
    {
        ASCII_Hex_Digit = 1ul << 0,
        Bidi_Control = 1ul << 1,
        Dash = 1ul << 2,
        Deprecated = 1ul << 3,
        Diacritic = 1ul << 4,
        Extender = 1ul << 5,
        Hex_Digit = 1ul << 6,
        Hyphen = 1ul << 7,
        Ideographic = 1ul << 8,
        IDS_Binary_Operator = 1ul << 9,
        IDS_Trinary_Operator = 1ul << 10,
        Join_Control = 1ul << 11,
        Logical_Order_Exception = 1ul << 12,
        Noncharacter_Code_Point = 1ul << 13,
        Other_Alphabetic = 1ul << 14,
        Other_Default_Ignorable_Code_Point = 1ul << 15,
        Other_Grapheme_Extend = 1ul << 16,
        Other_ID_Continue = 1ul << 17,
        Other_ID_Start = 1ul << 18,
        Other_Lowercase = 1ul << 19,
        Other_Math = 1ul << 20,
        Other_Uppercase = 1ul << 21,
        Pattern_Syntax = 1ul << 22,
        Pattern_White_Space = 1ul << 23,
        Prepended_Concatenation_Mark = 1ul << 24,
        Quotation_Mark = 1ul << 25,
        Radical = 1ul << 26,
        Regional_Indicator = 1ul << 27,
        Sentence_Terminal = 1ul << 28,
        Soft_Dotted = 1ul << 29,
        Terminal_Punctuation = 1ul << 30,
        Unified_Ideograph = 1ul << 31,
        Variation_Selector = 1ul << 32,
        White_Space = 1ul << 33,
    }
}
