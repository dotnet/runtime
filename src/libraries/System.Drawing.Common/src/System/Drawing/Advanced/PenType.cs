// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * PenType Type
     */
    /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the type of fill a <see cref='System.Drawing.Pen'/> uses to
    ///       fill lines.
    ///    </para>
    /// </devdoc>
    public enum PenType
    {
        /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType.SolidColor"]/*' />
        /// <devdoc>
        ///    Specifies a solid fill.
        /// </devdoc>
        SolidColor = BrushType.SolidColor,
        /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType.HatchFill"]/*' />
        /// <devdoc>
        ///    Specifies a hatch fill.
        /// </devdoc>
        HatchFill = BrushType.HatchFill,
        /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType.TextureFill"]/*' />
        /// <devdoc>
        ///    Specifies a bitmap texture fill.
        /// </devdoc>
        TextureFill = BrushType.TextureFill,
        /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType.PathGradient"]/*' />
        /// <devdoc>
        ///    Specifies a path gradient fill.
        /// </devdoc>
        PathGradient = BrushType.PathGradient,
        /// <include file='doc\PenType.uex' path='docs/doc[@for="PenType.LinearGradient"]/*' />
        /// <devdoc>
        ///    Specifies a linear gradient fill.
        /// </devdoc>
        LinearGradient = BrushType.LinearGradient,
    }
}
