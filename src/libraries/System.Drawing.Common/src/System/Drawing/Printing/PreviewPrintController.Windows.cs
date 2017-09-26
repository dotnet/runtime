// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
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
    public class PreviewPrintController : PrintController
    {
        private IList _list = new ArrayList(); // list of PreviewPageInfo
        private System.Drawing.Graphics _graphics;
        private DeviceContext _dc;
        private bool _antiAlias;

        private void CheckSecurity()
        {
        }

        /// <summary>
        /// This is new public property which notifies if this controller is used for PrintPreview.
        /// </summary>
        public override bool IsPreview
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Implements StartPrint for generating print preview information.
        /// </summary>
        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert(_dc == null && _graphics == null, "PrintController methods called in the wrong order?");

            // For security purposes, don't assume our public methods methods are called in any particular order
            CheckSecurity();

            base.OnStartPrint(document, e);

            if (!document.PrinterSettings.IsValid)
                throw new InvalidPrinterException(document.PrinterSettings);

            // We need a DC as a reference; we don't actually draw on it.
            // We make sure to reuse the same one to improve performance.
            _dc = document.PrinterSettings.CreateInformationContext(modeHandle);
        }

        /// <summary>
        /// Implements StartEnd for generating print preview information.
        /// </summary>
        public override Graphics OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            Debug.Assert(_dc != null && _graphics == null, "PrintController methods called in the wrong order?");

            // For security purposes, don't assume our public methods methods are called in any particular order
            CheckSecurity();

            base.OnStartPage(document, e);

            if (e.CopySettingsToDevMode)
            {
                e.PageSettings.CopyToHdevmode(modeHandle);
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
                //
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


            if (_antiAlias)
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
            Debug.Assert(_dc != null && _graphics != null, "PrintController methods called in the wrong order?");

            // For security purposes, don't assume our public methods methods are called in any particular order
            CheckSecurity();

            _graphics.Dispose();
            _graphics = null;

            base.OnEndPage(document, e);
        }

        /// <summary>
        /// Implements EndPrint for generating print preview information.
        /// </summary>
        public override void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert(_dc != null && _graphics == null, "PrintController methods called in the wrong order?");

            // For security purposes, don't assume our public methods are called in any particular order
            CheckSecurity();

            _dc.Dispose();
            _dc = null;

            base.OnEndPrint(document, e);
        }

        public PreviewPageInfo[] GetPreviewPageInfo()
        {
            // For security purposes, don't assume our public methods methods are called in any particular order
            CheckSecurity();

            PreviewPageInfo[] temp = new PreviewPageInfo[_list.Count];
            _list.CopyTo(temp, 0);
            return temp;
        }

        public virtual bool UseAntiAlias
        {
            get
            {
                return _antiAlias;
            }
            set
            {
                _antiAlias = value;
            }
        }
    }
}
