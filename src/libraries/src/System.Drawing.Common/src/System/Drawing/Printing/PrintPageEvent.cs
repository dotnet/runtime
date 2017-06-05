// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs"]/*' />
    /// <devdoc>
    /// <para>Provides data for the <see cref='E:System.Drawing.Printing.PrintDocument.PrintPage'/>
    /// event.</para>
    /// </devdoc>
    // NOTE: Please keep this class consistent with PaintEventArgs.
    public class PrintPageEventArgs : EventArgs
    {
        private bool _hasMorePages;
        private bool _cancel;

        private Graphics _graphics;
        private readonly Rectangle _marginBounds;
        private readonly Rectangle _pageBounds;
        private readonly PageSettings _pageSettings;

        // Apply page settings to the printer.
        internal bool CopySettingsToDevMode = true;


        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.PrintPageEventArgs"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.Drawing.Printing.PrintPageEventArgs'/> class.</para>
        /// </devdoc>
        public PrintPageEventArgs(Graphics graphics, Rectangle marginBounds, Rectangle pageBounds, PageSettings pageSettings)
        {
            _graphics = graphics; // may be null, see PrintController
            _marginBounds = marginBounds;
            _pageBounds = pageBounds;
            _pageSettings = pageSettings;
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.Cancel"]/*' />
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether the print job should be canceled.</para>
        /// </devdoc>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.Graphics"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the <see cref='System.Drawing.Graphics'/>
        ///       used to paint the
        ///       item.
        ///    </para>
        /// </devdoc>
        public Graphics Graphics
        {
            get
            {
                return _graphics;
            }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.HasMorePages"]/*' />
        /// <devdoc>
        ///    <para> Gets or sets a value indicating whether an additional page should
        ///       be printed.</para>
        /// </devdoc>
        public bool HasMorePages
        {
            get { return _hasMorePages; }
            set { _hasMorePages = value; }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.MarginBounds"]/*' />
        /// <devdoc>
        ///    <para>Gets the rectangular area that represents the portion of the page between the margins.</para>
        /// </devdoc>
        public Rectangle MarginBounds
        {
            get
            {
                return _marginBounds;
            }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.PageBounds"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets the rectangular area that represents the total area of the page.
        ///    </para>
        /// </devdoc>
        public Rectangle PageBounds
        {
            get
            {
                return _pageBounds;
            }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.PageSettings"]/*' />
        /// <devdoc>
        ///    <para>Gets
        ///       the page settings for the current page.</para>
        /// </devdoc>
        public PageSettings PageSettings
        {
            get
            {
                return _pageSettings;
            }
        }

        /// <include file='doc\PrintPageEvent.uex' path='docs/doc[@for="PrintPageEventArgs.Dispose"]/*' />
        /// <devdoc>
        ///    <para>Disposes
        ///       of the resources (other than memory) used by
        ///       the <see cref='System.Drawing.Printing.PrintPageEventArgs'/>.</para>
        /// </devdoc>
        // We want a way to dispose the GDI+ Graphics, but we don't want to create one
        // simply to dispose it
        internal void Dispose()
        {
            _graphics.Dispose();
        }

        internal void SetGraphics(Graphics value)
        {
            _graphics = value;
        }
    }
}

