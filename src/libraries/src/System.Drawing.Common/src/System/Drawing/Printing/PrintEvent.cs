// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    using System.ComponentModel;

    /// <include file='doc\PrintEvent.uex' path='docs/doc[@for="PrintEventArgs"]/*' />    
    /// <devdoc>
    /// <para>Provides data for the <see cref='E:System.Drawing.Printing.PrintDocument.BeginPrint'/> and
    /// <see cref='E:System.Drawing.Printing.PrintDocument.EndPrint'/> events.</para>
    /// </devdoc>
    public class PrintEventArgs : CancelEventArgs
    {
        private PrintAction _printAction;

        /// <include file='doc\PrintEvent.uex' path='docs/doc[@for="PrintEventArgs.PrintEventArgs"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PrintEventArgs'/> class.
        ///    </para>
        /// </devdoc>
        public PrintEventArgs()
        {
        }

        /// <include file='doc\PrintEvent.uex' path='docs/doc[@for="PrintEventArgs.PrintEventArgs1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Printing.PrintEventArgs'/> class.
        ///    </para>
        /// </devdoc>
        internal PrintEventArgs(PrintAction action)
        {
            _printAction = action;
        }

        /// <include file='doc\PrintEvent.uex' path='docs/doc[@for="PrintEventArgs.PrintAction"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Specifies which <see cref='System.Drawing.Printing.PrintAction'/> is causing this event.
        ///    </para>
        /// </devdoc>
        public PrintAction PrintAction
        {
            get
            {
                return _printAction;
            }
        }
    }
}

