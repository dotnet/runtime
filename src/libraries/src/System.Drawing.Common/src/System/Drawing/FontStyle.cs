// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies style information applied to text.
    /// </summary>
    [Flags]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public enum FontStyle
    {
        /// <summary>
        /// Normal text.
        /// </summary>
        Regular = 0,
        /// <summary>
        /// Bold text.
        /// </summary>
        Bold = 1,
        /// <summary>
        /// Italic text.
        /// </summary>
        Italic = 2,
        /// <summary>
        /// Underlined text.
        /// </summary>
        Underline = 4,
        /// <summary>
        /// Text with a line through the middle.
        /// </summary>
        Strikeout = 8,
    }
}
