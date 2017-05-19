// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /*
     * Alpha compositing mode constants
     *
     * @notes Should we scrap this for the first version
     *  and support only SrcOver instead?
     */

    /// <include file='doc\CompositingMode.uex' path='docs/doc[@for="CompositingMode"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Defines how the source image is composited with the background image.
    ///    </para>
    /// </devdoc>
    public enum CompositingMode
    {
        /// <include file='doc\CompositingMode.uex' path='docs/doc[@for="CompositingMode.SourceOver"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The source pixels overwrite the background pixels.
        ///    </para>
        /// </devdoc>
        SourceOver = 0,
        /// <include file='doc\CompositingMode.uex' path='docs/doc[@for="CompositingMode.SourceCopy"]/*' />
        /// <devdoc>
        ///    The source pixels are combined with the
        ///    background pixels.
        /// </devdoc>
        SourceCopy = 1
    }
}
