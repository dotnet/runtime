// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Internal;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Retrieves the printer graphics during preview.
    /// </summary>
    internal class PrintPreviewGraphics
    {
        private PrintPageEventArgs _printPageEventArgs;
        private PrintDocument _printDocument;

        public PrintPreviewGraphics(PrintDocument document, PrintPageEventArgs e)
        {
            _printPageEventArgs = e;
            _printDocument = document;
        }

        /// <summary>
        /// Gets the Visible bounds of this graphics object. Used during print preview.
        /// </summary>
        public RectangleF VisibleClipBounds
        {
            get
            {
                IntPtr hdevMode = _printPageEventArgs.PageSettings.PrinterSettings.GetHdevmodeInternal();

                using (DeviceContext dc = _printPageEventArgs.PageSettings.PrinterSettings.CreateDeviceContext(hdevMode))
                {
                    using (Graphics graphics = Graphics.FromHdcInternal(dc.Hdc))
                    {
                        if (_printDocument.OriginAtMargins)
                        {
                            // Adjust the origin of the graphics object to be at the user-specified margin location
                            // Note: Graphics.FromHdc internally calls SaveDC(hdc), we can still use the saved hdc to get the resolution.
                            int dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSX);
                            int dpiY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSY);
                            int hardMarginX_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETX);
                            int hardMarginY_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETY);
                            float hardMarginX = hardMarginX_DU * 100 / dpiX;
                            float hardMarginY = hardMarginY_DU * 100 / dpiY;

                            graphics.TranslateTransform(-hardMarginX, -hardMarginY);
                            graphics.TranslateTransform(_printDocument.DefaultPageSettings.Margins.Left, _printDocument.DefaultPageSettings.Margins.Top);
                        }

                        return graphics.VisibleClipBounds;
                    }
                }
            }
        }
    }
}
