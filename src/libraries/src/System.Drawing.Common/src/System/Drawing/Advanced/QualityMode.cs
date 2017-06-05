// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    /// <include file='doc\QualityMode.uex' path='docs/doc[@for="QualityMode"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the overall quality of rendering
    ///       of graphics shapes.
    ///    </para>
    /// </devdoc>
    public enum QualityMode
    {
        /// <include file='doc\QualityMode.uex' path='docs/doc[@for="QualityMode.Invalid"]/*' />
        /// <devdoc>
        ///    Specifies an invalid mode.
        /// </devdoc>
        Invalid = -1,
        /// <include file='doc\QualityMode.uex' path='docs/doc[@for="QualityMode.Default"]/*' />
        /// <devdoc>
        ///    Specifies the default mode.
        /// </devdoc>
        Default = 0,
        /// <include file='doc\QualityMode.uex' path='docs/doc[@for="QualityMode.Low"]/*' />
        /// <devdoc>
        ///    Specifies low quality, high performance
        ///    rendering.
        /// </devdoc>
        Low = 1,             // for apps that need the best performance
        /// <include file='doc\QualityMode.uex' path='docs/doc[@for="QualityMode.High"]/*' />
        /// <devdoc>
        ///    Specifies high quality, lower performance
        ///    rendering.
        /// </devdoc>
        High = 2             // for apps that need the best rendering quality                                          
    }
}
