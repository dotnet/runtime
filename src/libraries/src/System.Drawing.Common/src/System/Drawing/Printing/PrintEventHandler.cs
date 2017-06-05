// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PrintEventHandler.uex' path='docs/doc[@for="PrintEventHandler"]/*' />
    /// <devdoc>
    ///    <para>
    ///       Represents the method that will handle the <see cref='E:System.Drawing.Printing.PrintDocument.BeginPrint'/>,
    ///    <see cref='E:System.Drawing.Printing.PrintDocument.EndPrint'/>, or <see cref='E:System.Drawing.Printing.PrintDocument.QueryPageSettings'/> event of a <see cref='System.Drawing.Printing.PrintDocument'/>.
    ///    </para>
    /// </devdoc>
    public delegate void PrintEventHandler(object sender, PrintEventArgs e);
}

