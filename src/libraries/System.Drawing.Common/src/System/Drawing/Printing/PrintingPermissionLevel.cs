/*
 * PrintingPermission.cool
 *
 * Copyright (c) 2000 Microsoft Corporation.  All Rights Reserved.
 * Microsoft Confidential.
 */

namespace System.Drawing.Printing {
    using System;

    /// <include file='doc\PrintingPermissionLevel.uex' path='docs/doc[@for="PrintingPermissionLevel"]/*' />
    /// <devdoc>
    ///    <para>Specifies the type of printing that code is allowed to do.</para>
    /// </devdoc>
    [Serializable] 
    public enum PrintingPermissionLevel {
        /**
         * No printing use allowed at all.
         */
        /// <include file='doc\PrintingPermissionLevel.uex' path='docs/doc[@for="PrintingPermissionLevel.NoPrinting"]/*' />
        /// <devdoc>
        ///    <para>Users have no ability to use any printers.</para>
        /// </devdoc>
        NoPrinting = 0x0,

        /**
         * Only allow safe printing use.
         */
        /// <include file='doc\PrintingPermissionLevel.uex' path='docs/doc[@for="PrintingPermissionLevel.SafePrinting"]/*' />
        /// <devdoc>
        ///    <para>Users can only use safe printing to print from a restricted dialog box.</para>
        /// </devdoc>
        SafePrinting = 0x01,

        /**
         * Use of the default printer allowed.
         */
        /// <include file='doc\PrintingPermissionLevel.uex' path='docs/doc[@for="PrintingPermissionLevel.DefaultPrinting"]/*' />
        /// <devdoc>
        ///    <para>Users can print programmically to the default printer along with safe printing through
        ///          a less restricted dialog box.</para>
        /// </devdoc>
        DefaultPrinting = 0x02,

        /**
         * All windows and all event may be used.
         */
        /// <include file='doc\PrintingPermissionLevel.uex' path='docs/doc[@for="PrintingPermissionLevel.AllPrinting"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Users have full access to all printers on the network.
        ///    </para>
        /// </devdoc>
        AllPrinting = 0x03,

    }
}

