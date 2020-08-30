// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Drawing.Printing
{
    public abstract partial class PrintController
    {
        protected PrintController()
        {
        }

        public virtual bool IsPreview => false;

        /// <summary>
        /// When overridden in a derived class, begins the control sequence of when and how to print a page in a document.
        /// </summary>
        public virtual Graphics? OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            return null;
        }

        /// <summary>
        /// When overridden in a derived class, completes the control sequence of when and how to print a page in a document.
        /// </summary>
        public virtual void OnEndPage(PrintDocument document, PrintPageEventArgs e)
        {
        }
    }
}
