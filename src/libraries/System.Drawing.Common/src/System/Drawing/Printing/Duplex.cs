// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\Duplex.uex' path='docs/doc[@for="Duplex"]/*' />
    /// <devdoc>
    ///    <para>Specifies the printer's duplex setting.</para>
    /// </devdoc>
    public enum Duplex
    {
        /// <include file='doc\Duplex.uex' path='docs/doc[@for="Duplex.Default"]/*' />
        /// <devdoc>
        ///    <para>
        ///       The printer's default duplex setting.
        ///    </para>
        /// </devdoc>
        Default = -1,

        /// <include file='doc\Duplex.uex' path='docs/doc[@for="Duplex.Simplex"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Single-sided printing.
        ///    </para>
        /// </devdoc>
        Simplex = SafeNativeMethods.DMDUP_SIMPLEX,

        /// <include file='doc\Duplex.uex' path='docs/doc[@for="Duplex.Horizontal"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Double-sided, horizontal printing.
        ///       
        ///    </para>
        /// </devdoc>
        Horizontal = SafeNativeMethods.DMDUP_HORIZONTAL,

        /// <include file='doc\Duplex.uex' path='docs/doc[@for="Duplex.Vertical"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Double-sided, vertical printing.
        ///       
        ///    </para>
        /// </devdoc>
        Vertical = SafeNativeMethods.DMDUP_VERTICAL,
    }
}
