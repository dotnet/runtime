// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    /// <summary>
    /// Bidi class values from UAX#44.
    /// </summary>
    /// <remarks>
    /// See https://www.unicode.org/reports/tr44/#BC_Values_Table
    /// and https://www.unicode.org/Public/UCD/latest/ucd/PropertyValueAliases.txt (bc).
    /// </remarks>
    public enum BidiClass
    {
        Arabic_Letter,
        Arabic_Number,
        Paragraph_Separator,
        Boundary_Neutral,
        Common_Separator,
        European_Number,
        European_Separator,
        European_Terminator,
        First_Strong_Isolate,
        Left_To_Right,
        Left_To_Right_Embedding,
        Left_To_Right_Isolate,
        Left_To_Right_Override,
        Nonspacing_Mark,
        Other_Neutral,
        Pop_Directional_Format,
        Pop_Directional_Isolate,
        Right_To_Left,
        Right_To_Left_Embedding,
        Right_To_Left_Isolate,
        Right_To_Left_Override,
        Segment_Separator,
        White_Space,
    }
}
