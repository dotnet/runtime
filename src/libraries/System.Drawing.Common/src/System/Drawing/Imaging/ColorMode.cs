// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Imaging
{
    /**
     * Color mode constants
     */
    /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies two modes for color component
    ///       values.
    ///    </para>
    /// </devdoc>
    public enum ColorMode
    {
        /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode.Argb32Mode"]/*' />
        /// <devdoc>
        ///    Specifies that integer values supplied are
        ///    32-bit values.
        /// </devdoc>
        Argb32Mode = 0,
        /// <include file='doc\ColorMode.uex' path='docs/doc[@for="ColorMode.Argb64Mode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies that integer values supplied are
        ///       64-bit values.
        ///    </para>
        /// </devdoc>
        Argb64Mode = 1
    }
}
