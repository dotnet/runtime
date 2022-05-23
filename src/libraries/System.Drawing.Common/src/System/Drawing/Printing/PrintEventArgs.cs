// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Provides data for the <see cref='PrintDocument.BeginPrint'/> and <see cref='PrintDocument.EndPrint'/> events.
    /// </summary>
    public class PrintEventArgs : CancelEventArgs
    {
        private readonly PrintAction _printAction;

        /// <summary>
        /// Initializes a new instance of the <see cref='PrintEventArgs'/> class.
        /// </summary>
        public PrintEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='PrintEventArgs'/> class.
        /// </summary>
        internal PrintEventArgs(PrintAction action)
        {
            _printAction = action;
        }

        /// <summary>
        /// Specifies which <see cref='Printing.PrintAction'/> is causing this event.
        /// </summary>
        public PrintAction PrintAction
        {
            get
            {
                return _printAction;
            }
        }
    }
}
