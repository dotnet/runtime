// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode"]/*' />
    /// <devdoc>
    ///    Specifies how pixels are offset during
    ///    rendering.
    /// </devdoc>
    public enum PixelOffsetMode
    {
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Invalid"]/*' />
        /// <devdoc>
        ///    Specifies an invalid mode.
        /// </devdoc>
        Invalid = QualityMode.Invalid,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Default"]/*' />
        /// <devdoc>
        ///    Specifies the default mode.
        /// </devdoc>
        Default = QualityMode.Default,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.HighSpeed"]/*' />
        /// <devdoc>
        ///    Specifies high low quality (high
        ///    performance) mode.
        /// </devdoc>
        HighSpeed = QualityMode.Low,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.HighQuality"]/*' />
        /// <devdoc>
        ///    Specifies high quality (lower performance)
        ///    mode.
        /// </devdoc>
        HighQuality = QualityMode.High,
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.None"]/*' />
        /// <devdoc>
        ///    Specifies no pixel offset.
        /// </devdoc>
        None,                   // no pixel offset
        /// <include file='doc\PixelOffsetMode.uex' path='docs/doc[@for="PixelOffsetMode.Half"]/*' />
        /// <devdoc>
        ///    Specifies that pixels are offset by -.5
        ///    units both horizontally and vertically for high performance anti-aliasing.
        /// </devdoc>
        Half                    // offset by -0.5, -0.5 for fast anti-alias perf
    }
}
