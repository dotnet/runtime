// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    // Grapheme cluster break property values, as specified in
    // https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Boundaries, Sec. 3.1.
    internal enum GraphemeClusterBreakType
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
        Extended_Pictograph,
    }
}
