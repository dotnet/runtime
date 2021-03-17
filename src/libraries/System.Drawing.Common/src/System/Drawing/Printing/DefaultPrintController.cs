// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private DeviceContext? _dc;
        private Graphics? _graphics;

        /// <summary>
        /// Implements StartPrint for printing to a physical printer.
        /// </summary>
        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            Debug.Assert(_dc == null && _graphics == null, "PrintController methods called in the wrong order?");
            Debug.Assert(_modeHandle != null);

            base.OnStartPrint(document, e);
            // the win32 methods below SuppressUnmanagedCodeAttributes so assertin on UnmanagedCodePermission is redundant
            if (!document.PrinterSettings.IsValid)
                throw new InvalidPrinterException(document.PrinterSettings);

            _dc = document.PrinterSettings.CreateDeviceContext(_modeHandle);
            Interop.Gdi32.DOCINFO info = new Interop.Gdi32.DOCINFO();
            info.lpszDocName = document.DocumentName;
            if (document.PrinterSettings.PrintToFile)
                info.lpszOutput = document.PrinterSettings.OutputPort; //This will be "FILE:"
            else
                info.lpszOutput = null;
            info.lpszDatatype = null;
            info.fwType = 0;

            int result = Interop.Gdi32.StartDoc(new HandleRef(_dc, _dc.Hdc), info);
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
            Debug.Assert(_modeHandle != null);

            base.OnStartPage(document, e);
            e.PageSettings.CopyToHdevmode(_modeHandle);
            IntPtr modePointer = Interop.Kernel32.GlobalLock(new HandleRef(this, _modeHandle));
            try
            {
                IntPtr result = Interop.Gdi32.ResetDC(new HandleRef(_dc, _dc.Hdc), new HandleRef(null, modePointer));
                Debug.Assert(result == _dc.Hdc, "ResetDC didn't return the same handle I gave it");
            }
            finally
            {
                Interop.Kernel32.GlobalUnlock(new HandleRef(this, _modeHandle));
            }

            _graphics = Graphics.FromHdcInternal(_dc.Hdc);

            if (document.OriginAtMargins)
            {
                // Adjust the origin of the graphics object to be at the
                // user-specified margin location
                //
                int dpiX = Interop.Gdi32.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), Interop.Gdi32.DeviceCapability.LOGPIXELSX);
                int dpiY = Interop.Gdi32.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), Interop.Gdi32.DeviceCapability.LOGPIXELSY);
                int hardMarginX_DU = Interop.Gdi32.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), Interop.Gdi32.DeviceCapability.PHYSICALOFFSETX);
                int hardMarginY_DU = Interop.Gdi32.GetDeviceCaps(new HandleRef(_dc, _dc.Hdc), Interop.Gdi32.DeviceCapability.PHYSICALOFFSETY);
                float hardMarginX = hardMarginX_DU * 100 / dpiX;
                float hardMarginY = hardMarginY_DU * 100 / dpiY;

                _graphics.TranslateTransform(-hardMarginX, -hardMarginY);
                _graphics.TranslateTransform(document.DefaultPageSettings.Margins.Left, document.DefaultPageSettings.Margins.Top);
            }


            int result2 = Interop.Gdi32.StartPage(new HandleRef(_dc, _dc.Hdc));
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
                int result = Interop.Gdi32.EndPage(new HandleRef(_dc, _dc.Hdc));
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
                    int result = (e.Cancel) ? Interop.Gdi32.AbortDoc(new HandleRef(_dc, _dc.Hdc)) : Interop.Gdi32.EndDoc(new HandleRef(_dc, _dc.Hdc));
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
