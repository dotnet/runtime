// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Color matrix flag constants
     */
    /// <include file='doc\ColorMatrixFlags.uex' path='docs/doc[@for="ColorMatrixFlag"]/*' />
    /// <devdoc>
    ///    Specifies available options for
    ///    color-adjusting. GDI+ can adjust color data only, grayscale data only,
    ///    or both.
    /// </devdoc>
    public enum ColorMatrixFlag
    {
        /// <include file='doc\ColorMatrixFlags.uex' path='docs/doc[@for="ColorMatrixFlag.Default"]/*' />
        /// <devdoc>
        ///    Both colors and grayscale are
        ///    color-adjusted.
        /// </devdoc>
        Default = 0,
        /// <include file='doc\ColorMatrixFlags.uex' path='docs/doc[@for="ColorMatrixFlag.SkipGrays"]/*' />
        /// <devdoc>
        ///    Grascale values are not color-adjusted.
        /// </devdoc>
        SkipGrays = 1,
        /// <include file='doc\ColorMatrixFlags.uex' path='docs/doc[@for="ColorMatrixFlag.AltGrays"]/*' />
        /// <devdoc>
        ///    Only grascale values are color-adjusted.
        /// </devdoc>
        AltGrays = 2
    }
}

