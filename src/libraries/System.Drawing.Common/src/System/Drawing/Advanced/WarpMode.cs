// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /**
     * Various wrap modes for brushes
     */
    /// <include file='doc\WarpMode.uex' path='docs/doc[@for="WarpMode"]/*' />
    /// <devdoc>
    ///    Specifies the warp style.
    /// </devdoc>
    public enum WarpMode
    {
        /// <include file='doc\WarpMode.uex' path='docs/doc[@for="WarpMode.Perspective"]/*' />
        /// <devdoc>
        ///    Specifies a perspective warp.
        /// </devdoc>
        Perspective = 0,
        /// <include file='doc\WarpMode.uex' path='docs/doc[@for="WarpMode.Bilinear"]/*' />
        /// <devdoc>
        ///    Specifies a bilinear warp.
        /// </devdoc>
        Bilinear = 1
    }
}
