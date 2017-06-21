// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind"]/*' />
    /// <devdoc>
    ///    <para>Specifies a printer resolution.</para>
    /// </devdoc>
    public enum PrinterResolutionKind
    {
        /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind.High"]/*' />
        /// <devdoc>
        ///    <para>
        ///       High resolution.
        ///       
        ///    </para>
        /// </devdoc>
        High = SafeNativeMethods.DMRES_HIGH,
        /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind.Medium"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Medium resolution.
        ///       
        ///    </para>
        /// </devdoc>
        Medium = SafeNativeMethods.DMRES_MEDIUM,
        /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind.Low"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Low resolution.
        ///       
        ///    </para>
        /// </devdoc>
        Low = SafeNativeMethods.DMRES_LOW,
        /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind.Draft"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Draft-quality resolution.
        ///       
        ///    </para>
        /// </devdoc>
        Draft = SafeNativeMethods.DMRES_DRAFT,
        /// <include file='doc\PrinterResolutionKind.uex' path='docs/doc[@for="PrinterResolutionKind.Custom"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Custom resolution.
        ///       
        ///    </para>
        /// </devdoc>
        Custom = 0,
    }
}
