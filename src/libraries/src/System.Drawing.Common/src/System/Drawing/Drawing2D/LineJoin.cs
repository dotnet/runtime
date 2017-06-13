// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Line join constants
     */
    /// <include file='doc\LineJoin.uex' path='docs/doc[@for="LineJoin"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies how to join two intersecting lines in a
    ///    <see cref='System.Drawing.Drawing2D.GraphicsPath'/> at their intersection.
    ///    </para>
    /// </devdoc>
    public enum LineJoin
    {
        /// <include file='doc\LineJoin.uex' path='docs/doc[@for="LineJoin.Miter"]/*' />
        /// <devdoc>
        ///    Specifies an angled miter join.
        /// </devdoc>
        Miter = 0,
        /// <include file='doc\LineJoin.uex' path='docs/doc[@for="LineJoin.Bevel"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies a beveled join.
        ///    </para>
        /// </devdoc>
        Bevel = 1,
        /// <include file='doc\LineJoin.uex' path='docs/doc[@for="LineJoin.Round"]/*' />
        /// <devdoc>
        ///    Specifies a smooth, rounded join.
        /// </devdoc>
        Round = 2,
        /// <include file='doc\LineJoin.uex' path='docs/doc[@for="LineJoin.MiterClipped"]/*' />
        /// <devdoc>
        ///    Specifies a mitered clipped join
        /// </devdoc>
        MiterClipped = 3
    }
}
