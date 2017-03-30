// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Specifies identifiers that can be
    ///       used to indicate the type of a help keyword.
    ///    </para>
    /// </summary>
    public enum HelpKeywordType
    {
        /// <summary>
        ///    <para>
        ///       Indicates the keyword is a word F1 was pressed to request help regarding.
        ///    </para>
        /// </summary>
        F1Keyword,
        /// <summary>
        ///    <para>
        ///       Indicates the keyword is a general keyword.
        ///    </para>
        /// </summary>
        GeneralKeyword,
        /// <summary>
        ///    <para>
        ///       Indicates the keyword is a filter keyword.
        ///    </para>
        /// </summary>
        FilterKeyword
    }
}
