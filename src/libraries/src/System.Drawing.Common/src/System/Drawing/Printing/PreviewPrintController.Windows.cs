// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace System.Drawing.Printing
{
    /// <summary>
    /// A PrintController which "prints" to a series of images.
    /// </summary>
    public partial class PreviewPrintController : PrintController
    {
        private Graphics _graphics;
        private DeviceContext _dc;

        /// <summary>
        /// Implements StartPrint for generating print preview information.
        /// </summary>
        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            base.OnStartPrint(document, e);

            if (!document.PrinterSettings.IsValid)
            {
                throw new InvalidPrinterException(document.PrinterSettings);
            }

            // We need a DC as a reference; we don't actually draw on it.
            // We make sure to reuse the same one to improve performance.
            _dc = document.PrinterSettings.CreateInformationContext(_modeHandle);
        }

        /// <summary>
        /// Implements StartEnd for generating print preview information.
        /// </summary>
        public override Graphics OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            base.OnStartPage(document, e);

            if (e.CopySettingsToDevMode)
            {
                e.PageSettings.CopyToHdevmode(_modeHandle);
            }

            Size size = e.PageBounds.Size;

            // Metafile framing rectangles apparently use hundredths of mm as their unit of measurement,
            // instead of the GDI+ standard hundredth of an inch.
            Size metafileSize = PrinterUnitConvert.Convert(size, PrinterUnit.Display, PrinterUnit.HundredthsOfAMillimeter);

            // Create a Metafile which accepts only GDI+ commands since we are the ones creating
            // and using this ...
            // Framework creates a dual-mode EMF for each page in the preview.
            // When these images are displayed in preview,
            // they are added to the dual-mode EMF. However,
            // GDI+ breaks during this process if the image
            // is sufficiently large and has more than 254 colors.
            // This code path can easily be avoided by requesting
            // an EmfPlusOnly EMF..
            Metafile metafile = new Metafile(_dc.Hdc, new Rectangle(0, 0, metafileSize.Width, metafileSize.Height), MetafileFrameUnit.GdiCompatible, EmfType.EmfPlusOnly);

            PreviewPageInfo info = new PreviewPageInfo(metafile, size);
            _list.Add(info);
            PrintPreviewGraphics printGraphics = new PrintPreviewGraphics(document, e);
            _graphics = Graphics.FromImage(metafile);

            if (_graphics != null && document.OriginAtMargins)
            {
                // Adjust the origin of the graphics object to be at the
                // user-specified margin location
                int dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), SafeNativeMethods.LOGPIXELSX);
                int dpiY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), SafeNativeMethods.LOGPIXELSY);
                int hardMarginX_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), SafeNativeMethods.PHYSICALOFFSETX);
                int hardMarginY_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), SafeNativeMethods.PHYSICALOFFSETY);
                float hardMarginX = hardMarginX_DU * 100 / dpiX;
                float hardMarginY = hardMarginY_DU * 100 / dpiY;

                _graphics.TranslateTransform(-hardMarginX, -hardMarginY);
                _graphics.TranslateTransform(document.DefaultPageSettings.Margins.Left, document.DefaultPageSettings.Margins.Top);
            }

            _graphics.PrintingHelper = printGraphics;

            if (UseAntiAlias)
            {
                _graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }
            return _graphics;
        }

        /// <summary>
        /// Implements EndPage for generating print preview information.
        /// </summary>
        public override void OnEndPage(PrintDocument document, PrintPageEventArgs e)
        {
            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }

            base.OnEndPage(document, e);
        }

        /// <summary>
        /// Implements EndPrint for generating print preview information.
        /// </summary>
        public override void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            if (_dc != null)
            {
                _dc.Dispose();
                _dc = null;
            }

            base.OnEndPrint(document, e);
        }
    }
}
