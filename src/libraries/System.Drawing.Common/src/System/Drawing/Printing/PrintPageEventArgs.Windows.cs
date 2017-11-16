// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Printing
{
    /// <summary>
    /// Provides data for the <see cref='PrintDocument.PrintPage'/> event.
    /// </summary>
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


        /// <summary>
        /// Initializes a new instance of the <see cref='PrintPageEventArgs'/> class.
        /// </summary>
        public PrintPageEventArgs(Graphics graphics, Rectangle marginBounds, Rectangle pageBounds, PageSettings pageSettings)
        {
            _graphics = graphics; // may be null, see PrintController
            _marginBounds = marginBounds;
            _pageBounds = pageBounds;
            _pageSettings = pageSettings;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the print job should be canceled.
        /// </summary>
        public bool Cancel
        {
            get { return _cancel; }
            set { _cancel = value; }
        }

        /// <summary>
        /// Gets the <see cref='System.Drawing.Graphics'/> used to paint the item.
        /// </summary>
        public Graphics Graphics
        {
            get
            {
                return _graphics;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an additional page should be printed.
        /// </summary>
        public bool HasMorePages
        {
            get { return _hasMorePages; }
            set { _hasMorePages = value; }
        }

        /// <summary>
        /// Gets the rectangular area that represents the portion of the page between the margins.
        /// </summary>
        public Rectangle MarginBounds
        {
            get
            {
                return _marginBounds;
            }
        }

        /// <summary>
        /// Gets the rectangular area that represents the total area of the page.
        /// </summary>
        public Rectangle PageBounds
        {
            get
            {
                return _pageBounds;
            }
        }

        /// <summary>
        /// Gets the page settings for the current page.
        /// </summary>
        public PageSettings PageSettings
        {
            get
            {
                return _pageSettings;
            }
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the <see cref='PrintPageEventArgs'/>.
        /// </summary>
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

