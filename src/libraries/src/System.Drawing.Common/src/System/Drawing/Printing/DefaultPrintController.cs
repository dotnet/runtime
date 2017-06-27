// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies a print controller that sends information to a printer.
    /// </summary>
    public class StandardPrintController : PrintController
    {
        private DeviceContext _dc;
        private Graphics _graphics;

        /// <summary>
        /// Implements StartPrint for printing to a physical printer.
        /// </summary>
        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert(_dc == null && _graphics == null, "PrintController methods called in the wrong order?");

            base.OnStartPrint(document, e);
            // the win32 methods below SuppressUnmanagedCodeAttributes so assertin on UnmanagedCodePermission is redundant
            if (!document.PrinterSettings.IsValid)
                throw new InvalidPrinterException(document.PrinterSettings);

            _dc = document.PrinterSettings.CreateDeviceContext(modeHandle);
            SafeNativeMethods.DOCINFO info = new SafeNativeMethods.DOCINFO();
            info.lpszDocName = document.DocumentName;
            if (document.PrinterSettings.PrintToFile)
                info.lpszOutput = document.PrinterSettings.OutputPort; //This will be "FILE:"
            else
                info.lpszOutput = null;
            info.lpszDatatype = null;
            info.fwType = 0;

            int result = SafeNativeMethods.StartDoc(new HandleRef(_dc, _dc.Hdc), info);
            if (result <= 0)
            {
                int error = Marshal.GetLastWin32Error();
                if (error == SafeNativeMethods.ERROR_CANCELLED)
                {
                    e.Cancel = true;
                }
                else
                {
                    throw new Win32Exception(error);
                }
            }
        }

        /// <summary>
        /// Implements StartPage for printing to a physical printer.
        /// </summary>
        public override Graphics OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            Debug.Assert(_dc != null && _graphics == null, "PrintController methods called in the wrong order?");

            base.OnStartPage(document, e);
            e.PageSettings.CopyToHdevmode(modeHandle);
            IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
            try
            {
                IntPtr result = SafeNativeMethods.ResetDC(new HandleRef(_dc, _dc.Hdc), new HandleRef(null, modePointer));
                Debug.Assert(result == _dc.Hdc, "ResetDC didn't return the same handle I gave it");
            }
            finally
            {
                SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
            }

            // int horizontalResolution = Windows.GetDeviceCaps(dc.Hdc, SafeNativeMethods.HORZRES);
            // int verticalResolution = Windows.GetDeviceCaps(dc.Hdc, SafeNativeMethods.VERTRES);

            _graphics = Graphics.FromHdcInternal(_dc.Hdc);

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


            int result2 = SafeNativeMethods.StartPage(new HandleRef(_dc, _dc.Hdc));
            if (result2 <= 0)
                throw new Win32Exception();
            return _graphics;
        }

        /// <summary>
        /// Implements EndPage for printing to a physical printer.
        /// </summary>
        public override void OnEndPage(PrintDocument document, PrintPageEventArgs e)
        {
            Debug.Assert(_dc != null && _graphics != null, "PrintController methods called in the wrong order?");

            try
            {
                int result = SafeNativeMethods.EndPage(new HandleRef(_dc, _dc.Hdc));
                if (result <= 0)
                    throw new Win32Exception();
            }
            finally
            {
                _graphics.Dispose(); // Dispose of GDI+ Graphics; keep the DC
                _graphics = null;
            }
            base.OnEndPage(document, e);
        }

        /// <summary>
        /// Implements EndPrint for printing to a physical printer.
        /// </summary>
        public override void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert(_dc != null && _graphics == null, "PrintController methods called in the wrong order?");

            if (_dc != null)
            {
                try
                {
                    int result = (e.Cancel) ? SafeNativeMethods.AbortDoc(new HandleRef(_dc, _dc.Hdc)) : SafeNativeMethods.EndDoc(new HandleRef(_dc, _dc.Hdc));
                    if (result <= 0)
                        throw new Win32Exception();
                }
                finally
                {
                    _dc.Dispose();
                    _dc = null;
                }
            }

            base.OnEndPrint(document, e);
        }
    }
}

