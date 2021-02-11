// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    /// <summary>
    /// Grapheme cluster break property values from UAX#29.
    /// </summary>
    /// <remarks>
    /// See https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values
    /// and https://www.unicode.org/Public/emoji/12.1/emoji-data.txt.
    /// </remarks>
    public enum GraphemeClusterBreakProperty
    {
        Other,
        CR,
        LF,
        Control,
        Extend,
        ZWJ,
        Regional_Indicator,
        Prepend,
        SpacingMark,
        L,
        V,
        T,
        LV,
        LVT,
        Extended_Pictographic,
    }
}
