// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.OleDb
{
    public enum OleDbLiteral : int
    {
        Invalid = 0,
        Binary_Literal = 1,
        Catalog_Name = 2,
        Catalog_Separator = 3,
        Char_Literal = 4,
        Column_Alias = 5,
        Column_Name = 6,
        Correlation_Name = 7,
        Cursor_Name = 8,
        Escape_Percent_Prefix = 9,
        Escape_Underscore_Prefix = 10,
        Index_Name = 11,
        Like_Percent = 12,
        Like_Underscore = 13,
        Procedure_Name = 14,
        Quote_Prefix = 15,
        Schema_Name = 16,
        Table_Name = 17,
        Text_Command = 18,
        User_Name = 19,
        View_Name = 20,

        // MDAC 2.0
        Cube_Name = 21,
        Dimension_Name = 22,
        Hierarchy_Name = 23,
        Level_Name = 24,
        Member_Name = 25,
        Property_Name = 26,
        Schema_Separator = 27,
        Quote_Suffix = 28,

        // MDAC 2.1
        Escape_Percent_Suffix = 29,
        Escape_Underscore_Suffix = 30,
    }
}
