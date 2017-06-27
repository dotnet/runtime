// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies the alignment of a text string relative to its layout rectangle.
    /// </summary>
    public enum StringAlignment
    {
        // left or top in English
        /// <summary>
        /// Specifies the text be aligned near the layout. In a left-to-right layout, the near position is left. In a
        /// right-to-left layout, the near position is right.
        /// </summary>
        Near = 0,

        /// <summary>
        /// Specifies that text is aligned in the center of the layout rectangle.
        /// </summary>
        Center = 1,

        // right or bottom in English
        /// <summary>
        /// Specifies that text is aligned far from the origin position of the layout rectangle. In a left-to-right
        /// layout, the far position is right. In a right-to-left layout, the far position is left.
        /// </summary>
        Far = 2
    }
}
