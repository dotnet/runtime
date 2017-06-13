// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Path Point Type
     */
    /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the type of graphical point
    ///       contained at a specific point in a <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
    ///    </para>
    /// </devdoc>
    public enum PathPointType
    {
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.Start"]/*' />
        /// <devdoc>
        ///    Specifies the starting point of a <see cref='System.Drawing.Drawing2D.GraphicsPath'/>.
        /// </devdoc>
        Start = 0,    // move
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.Line"]/*' />
        /// <devdoc>
        ///    Specifies a line segment.
        /// </devdoc>
        Line = 1,    // line
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.Bezier"]/*' />
        /// <devdoc>
        ///    Specifies a default Bezier curve.
        /// </devdoc>
        Bezier = 3,    // default Beizer (= cubic Bezier)
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.PathTypeMask"]/*' />
        /// <devdoc>
        ///    Specifies a mask point.
        /// </devdoc>
        PathTypeMask = 0x07, // type mask (lowest 3 bits).
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.DashMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that the corresponding segment is dashed.
        ///    </para>
        /// </devdoc>
        DashMode = 0x10, // currently in dash mode.
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.PathMarker"]/*' />
        /// <devdoc>
        ///    Specifies a path marker.
        /// </devdoc>
        PathMarker = 0x20, // a marker for the path.
        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.CloseSubpath"]/*' />
        /// <devdoc>
        ///    Specifies the ending point of a subpath.
        /// </devdoc>
        CloseSubpath = 0x80, // closed flag

        // Path types used for advanced path.

        /// <include file='doc\PathPointType.uex' path='docs/doc[@for="PathPointType.Bezier3"]/*' />
        /// <devdoc>
        ///    Specifies a cubic Bezier curve.
        /// </devdoc>
        Bezier3 = 3,    // cubic Bezier
    }
}
