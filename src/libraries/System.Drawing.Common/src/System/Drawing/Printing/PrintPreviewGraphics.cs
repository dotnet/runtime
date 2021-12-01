// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Internal;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Retrieves the printer graphics during preview.
    /// </summary>
    internal sealed class PrintPreviewGraphics
    {
        private readonly PrintPageEventArgs _printPageEventArgs;
        private readonly PrintDocument _printDocument;

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
                            int dpiX = Interop.Gdi32.GetDeviceCaps(new HandleRef(dc, dc.Hdc), Interop.Gdi32.DeviceCapability.LOGPIXELSX);
                            int dpiY = Interop.Gdi32.GetDeviceCaps(new HandleRef(dc, dc.Hdc), Interop.Gdi32.DeviceCapability.LOGPIXELSY);
                            int hardMarginX_DU = Interop.Gdi32.GetDeviceCaps(new HandleRef(dc, dc.Hdc), Interop.Gdi32.DeviceCapability.PHYSICALOFFSETX);
                            int hardMarginY_DU = Interop.Gdi32.GetDeviceCaps(new HandleRef(dc, dc.Hdc), Interop.Gdi32.DeviceCapability.PHYSICALOFFSETY);
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
