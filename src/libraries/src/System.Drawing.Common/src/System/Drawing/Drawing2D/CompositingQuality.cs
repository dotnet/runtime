// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality"]/*' />
    /// <devdoc>
    ///    Specifies the quality level to use during
    ///    compositing.
    /// </devdoc>
    public enum CompositingQuality
    {
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.Invalid"]/*' />
        /// <devdoc>
        ///    Invalid quality.
        /// </devdoc>
        Invalid = QualityMode.Invalid,
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.Default"]/*' />
        /// <devdoc>
        ///    Default quality.
        /// </devdoc>
        Default = QualityMode.Default,
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.HighSpeed"]/*' />
        /// <devdoc>
        ///    Low quality, high speed.
        /// </devdoc>
        HighSpeed = QualityMode.Low,
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.HighQuality"]/*' />
        /// <devdoc>
        ///    High quality, low speed.
        /// </devdoc>
        HighQuality = QualityMode.High,
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.GammaCorrected"]/*' />
        /// <devdoc>
        ///    Gamma correction is used.
        /// </devdoc>
        GammaCorrected,
        /// <include file='doc\CompositingQuality.uex' path='docs/doc[@for="CompositingQuality.AssumeLinear"]/*' />
        /// <devdoc>
        ///    Assume linear values.
        /// </devdoc>
        AssumeLinear
    }
}
