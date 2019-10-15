// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    /// <summary>
    /// Specifies alignment of content on the drawing surface.
    /// </summary>
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public enum ContentAlignment
    {
        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned on the left.
        /// </summary>
        TopLeft = 0x001,
        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned at the center.
        /// </summary>
        TopCenter = 0x002,
        /// <summary>
        /// Content is vertically aligned at the top, and horizontally aligned on the right.
        /// </summary>
        TopRight = 0x004,
        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned on the left.
        /// </summary>
        MiddleLeft = 0x010,
        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned at the center.
        /// </summary>
        MiddleCenter = 0x020,
        /// <summary>
        /// Content is vertically aligned in the middle, and horizontally aligned on the right.
        /// </summary>
        MiddleRight = 0x040,
        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned on the left.
        /// </summary>
        BottomLeft = 0x100,
        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned at the center.
        /// </summary>
        BottomCenter = 0x200,
        /// <summary>
        /// Content is vertically aligned at the bottom, and horizontally aligned on the right.
        /// </summary>
        BottomRight = 0x400,
    }
}
