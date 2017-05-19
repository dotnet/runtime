// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PrintAction.uex' path='docs/doc[@for="PrintAction"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Specifies the type of action for the <see cref='System.Drawing.Printing.PrintEventArgs'/>.
    ///    </para>
    /// </devdoc>
    public enum PrintAction
    {
        /// <include file='doc\PrintAction.uex' path='docs/doc[@for="PrintAction.PrintToFile"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Printing to a file.
        ///    </para>
        /// </devdoc>
        PrintToFile,
        /// <include file='doc\PrintAction.uex' path='docs/doc[@for="PrintAction.PrintToPreview"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Printing to a preview.
        ///    </para>
        /// </devdoc>
        PrintToPreview,
        /// <include file='doc\PrintAction.uex' path='docs/doc[@for="PrintAction.PrintToPrinter"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Printing to a printer.
        ///    </para>
        /// </devdoc>
        PrintToPrinter
    }
}
