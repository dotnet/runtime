// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Specifies settings that apply to a single page.
    /// </summary>
    public partial class PageSettings : ICloneable
    {
        internal PrinterSettings printerSettings;

        private TriState _color = TriState.Default;
        private PaperSize _paperSize;
        private PaperSource _paperSource;
        private PrinterResolution _printerResolution;
        private TriState _landscape = TriState.Default;
        private Margins _margins = new Margins();

        /// <summary>
        /// Initializes a new instance of the <see cref='PageSettings'/> class using the default printer.
        /// </summary>
        public PageSettings() : this(new PrinterSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='PageSettings'/> class using the specified printer.
        /// </summary>
        public PageSettings(PrinterSettings printerSettings)
        {
            Debug.Assert(printerSettings != null, "printerSettings == null");
            this.printerSettings = printerSettings;
        }

        /// <summary>
        /// Gets the bounds of the page, taking into account the Landscape property.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                IntPtr modeHandle = printerSettings.GetHdevmode();

                Rectangle pageBounds = GetBounds(modeHandle);

                SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));
                return pageBounds;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the page is printed in color.
        /// </summary>
        public bool Color
        {
            get
            {
                if (_color.IsDefault)
                    return printerSettings.GetModeField(ModeField.Color, SafeNativeMethods.DMCOLOR_MONOCHROME) == SafeNativeMethods.DMCOLOR_COLOR;
                else
                    return (bool)_color;
            }
            set { _color = value; }
        }

        /// <summary>
        /// Returns the x dimension of the hard margin
        /// </summary>
        public float HardMarginX
        {
            get
            {
                float hardMarginX = 0;
                DeviceContext dc = printerSettings.CreateDeviceContext(this);

                try
                {
                    int dpiX = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSX);
                    int hardMarginX_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETX);
                    hardMarginX = hardMarginX_DU * 100 / dpiX;
                }
                finally
                {
                    dc.Dispose();
                }
                return hardMarginX;
            }
        }


        /// <summary>
        /// Returns the y dimension of the hard margin.
        /// </summary>
        public float HardMarginY
        {
            get
            {
                float hardMarginY = 0;
                DeviceContext dc = printerSettings.CreateDeviceContext(this);

                try
                {
                    int dpiY = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.LOGPIXELSY);
                    int hardMarginY_DU = UnsafeNativeMethods.GetDeviceCaps(new HandleRef(dc, dc.Hdc), SafeNativeMethods.PHYSICALOFFSETY);
                    hardMarginY = hardMarginY_DU * 100 / dpiY;
                }
                finally
                {
                    dc.Dispose();
                }
                return hardMarginY;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the page should be printed in landscape or portrait orientation.
        /// </summary>
        public bool Landscape
        {
            get
            {
                if (_landscape.IsDefault)
                    return printerSettings.GetModeField(ModeField.Orientation, SafeNativeMethods.DMORIENT_PORTRAIT) == SafeNativeMethods.DMORIENT_LANDSCAPE;
                else
                    return (bool)_landscape;
            }
            set { _landscape = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating the margins for this page.
        /// </summary>
        public Margins Margins
        {
            get { return _margins; }
            set { _margins = value; }
        }

        /// <summary>
        /// Gets or sets the paper size.
        /// </summary>
        public PaperSize PaperSize
        {
            get
            {
                return GetPaperSize(IntPtr.Zero);
            }
            set { _paperSize = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating the paper source (i.e. upper bin).
        /// </summary>
        public PaperSource PaperSource
        {
            get
            {
                if (_paperSource == null)
                {
                    IntPtr modeHandle = printerSettings.GetHdevmode();
                    IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                    PaperSource result = PaperSourceFromMode(mode);

                    SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                    return result;
                }
                else
                    return _paperSource;
            }
            set { _paperSource = value; }
        }

        /// <summary>
        /// Gets the PrintableArea for the printer. Units = 100ths of an inch.
        /// </summary>
        public RectangleF PrintableArea
        {
            get
            {
                RectangleF printableArea = new RectangleF();
                DeviceContext dc = printerSettings.CreateInformationContext(this);
                HandleRef hdc = new HandleRef(dc, dc.Hdc);

                try
                {
                    int dpiX = UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.LOGPIXELSX);
                    int dpiY = UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.LOGPIXELSY);
                    if (!Landscape)
                    {
                        //
                        // Need to convert the printable area to 100th of an inch from the device units
                        printableArea.X = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETX) * 100 / dpiX;
                        printableArea.Y = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETY) * 100 / dpiY;
                        printableArea.Width = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.HORZRES) * 100 / dpiX;
                        printableArea.Height = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.VERTRES) * 100 / dpiY;
                    }
                    else
                    {
                        //
                        // Need to convert the printable area to 100th of an inch from the device units
                        printableArea.Y = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETX) * 100 / dpiX;
                        printableArea.X = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.PHYSICALOFFSETY) * 100 / dpiY;
                        printableArea.Height = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.HORZRES) * 100 / dpiX;
                        printableArea.Width = (float)UnsafeNativeMethods.GetDeviceCaps(hdc, SafeNativeMethods.VERTRES) * 100 / dpiY;
                    }
                }
                finally
                {
                    dc.Dispose();
                }

                return printableArea;
            }
        }

        /// <summary>
        /// Gets or sets the printer resolution for the page.
        /// </summary>
        public PrinterResolution PrinterResolution
        {
            get
            {
                if (_printerResolution == null)
                {
                    IntPtr modeHandle = printerSettings.GetHdevmode();
                    IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                    PrinterResolution result = PrinterResolutionFromMode(mode);

                    SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                    SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                    return result;
                }
                else
                    return _printerResolution;
            }
            set
            {
                _printerResolution = value;
            }
        }

        /// <summary>
        /// Gets or sets the associated printer settings.
        /// </summary>
        public PrinterSettings PrinterSettings
        {
            get { return printerSettings; }
            set
            {
                if (value == null)
                    value = new PrinterSettings();
                printerSettings = value;
            }
        }

        /// <summary>
        /// Copies the settings and margins.
        /// </summary>
        public object Clone()
        {
            PageSettings result = (PageSettings)MemberwiseClone();
            result._margins = (Margins)_margins.Clone();
            return result;
        }

        /// <summary>
        /// Copies the relevant information out of the PageSettings and into the handle.
        /// </summary>
        public void CopyToHdevmode(IntPtr hdevmode)
        {
            IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(null, hdevmode));
            SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

            if (_color.IsNotDefault && ((mode.dmFields & SafeNativeMethods.DM_COLOR) == SafeNativeMethods.DM_COLOR))
                mode.dmColor = unchecked((short)(((bool)_color) ? SafeNativeMethods.DMCOLOR_COLOR : SafeNativeMethods.DMCOLOR_MONOCHROME));
            if (_landscape.IsNotDefault && ((mode.dmFields & SafeNativeMethods.DM_ORIENTATION) == SafeNativeMethods.DM_ORIENTATION))
                mode.dmOrientation = unchecked((short)(((bool)_landscape) ? SafeNativeMethods.DMORIENT_LANDSCAPE : SafeNativeMethods.DMORIENT_PORTRAIT));

            if (_paperSize != null)
            {
                if ((mode.dmFields & SafeNativeMethods.DM_PAPERSIZE) == SafeNativeMethods.DM_PAPERSIZE)
                {
                    mode.dmPaperSize = unchecked((short)_paperSize.RawKind);
                }

                bool setWidth = false;
                bool setLength = false;

                if ((mode.dmFields & SafeNativeMethods.DM_PAPERLENGTH) == SafeNativeMethods.DM_PAPERLENGTH)
                {
                    // dmPaperLength is always in tenths of millimeter but paperSizes are in hundredth of inch .. 
                    // so we need to convert :: use PrinterUnitConvert.Convert(value, PrinterUnit.TenthsOfAMillimeter /*fromUnit*/, PrinterUnit.Display /*ToUnit*/)
                    int length = PrinterUnitConvert.Convert(_paperSize.Height, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                    mode.dmPaperLength = unchecked((short)length);
                    setLength = true;
                }
                if ((mode.dmFields & SafeNativeMethods.DM_PAPERWIDTH) == SafeNativeMethods.DM_PAPERWIDTH)
                {
                    int width = PrinterUnitConvert.Convert(_paperSize.Width, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                    mode.dmPaperWidth = unchecked((short)width);
                    setWidth = true;
                }

                if (_paperSize.Kind == PaperKind.Custom)
                {
                    if (!setLength)
                    {
                        mode.dmFields |= SafeNativeMethods.DM_PAPERLENGTH;
                        int length = PrinterUnitConvert.Convert(_paperSize.Height, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                        mode.dmPaperLength = unchecked((short)length);
                    }
                    if (!setWidth)
                    {
                        mode.dmFields |= SafeNativeMethods.DM_PAPERWIDTH;
                        int width = PrinterUnitConvert.Convert(_paperSize.Width, PrinterUnit.Display, PrinterUnit.TenthsOfAMillimeter);
                        mode.dmPaperWidth = unchecked((short)width);
                    }
                }
            }

            if (_paperSource != null && ((mode.dmFields & SafeNativeMethods.DM_DEFAULTSOURCE) == SafeNativeMethods.DM_DEFAULTSOURCE))
            {
                mode.dmDefaultSource = unchecked((short)_paperSource.RawKind);
            }

            if (_printerResolution != null)
            {
                if (_printerResolution.Kind == PrinterResolutionKind.Custom)
                {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        mode.dmPrintQuality = unchecked((short)_printerResolution.X);
                    }
                    if ((mode.dmFields & SafeNativeMethods.DM_YRESOLUTION) == SafeNativeMethods.DM_YRESOLUTION)
                    {
                        mode.dmYResolution = unchecked((short)_printerResolution.Y);
                    }
                }
                else
                {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        mode.dmPrintQuality = unchecked((short)_printerResolution.Kind);
                    }
                }
            }

            Marshal.StructureToPtr(mode, modePointer, false);

            // It's possible this page has a DEVMODE for a different printer than the DEVMODE passed in here 
            // (Ex: occurs when Doc.DefaultPageSettings.PrinterSettings.PrinterName != Doc.PrinterSettings.PrinterName)
            // 
            // if the passed in devmode has fewer bytes than our buffer for the extrainfo, we want to skip the merge as it will cause
            // a buffer overrun
            if (mode.dmDriverExtra >= ExtraBytes)
            {
                int retCode = SafeNativeMethods.DocumentProperties(NativeMethods.NullHandleRef, NativeMethods.NullHandleRef, printerSettings.PrinterName, modePointer, modePointer, SafeNativeMethods.DM_IN_BUFFER | SafeNativeMethods.DM_OUT_BUFFER);
                if (retCode < 0)
                {
                    SafeNativeMethods.GlobalFree(new HandleRef(null, modePointer));
                }
            }

            SafeNativeMethods.GlobalUnlock(new HandleRef(null, hdevmode));
        }

        private short ExtraBytes
        {
            get
            {
                IntPtr modeHandle = printerSettings.GetHdevmodeInternal();
                IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(this, modeHandle));
                SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                short result = mode.dmDriverExtra;

                SafeNativeMethods.GlobalUnlock(new HandleRef(this, modeHandle));
                SafeNativeMethods.GlobalFree(new HandleRef(this, modeHandle));

                return result;
            }
        }


        // This function shows up big on profiles, so we need to make it fast
        internal Rectangle GetBounds(IntPtr modeHandle)
        {
            Rectangle pageBounds;
            PaperSize size = GetPaperSize(modeHandle);
            if (GetLandscape(modeHandle))
                pageBounds = new Rectangle(0, 0, size.Height, size.Width);
            else
                pageBounds = new Rectangle(0, 0, size.Width, size.Height);

            return pageBounds;
        }

        private bool GetLandscape(IntPtr modeHandle)
        {
            if (_landscape.IsDefault)
                return printerSettings.GetModeField(ModeField.Orientation, SafeNativeMethods.DMORIENT_PORTRAIT, modeHandle) == SafeNativeMethods.DMORIENT_LANDSCAPE;
            else
                return (bool)_landscape;
        }

        private PaperSize GetPaperSize(IntPtr modeHandle)
        {
            if (_paperSize == null)
            {
                bool ownHandle = false;
                if (modeHandle == IntPtr.Zero)
                {
                    modeHandle = printerSettings.GetHdevmode();
                    ownHandle = true;
                }

                IntPtr modePointer = SafeNativeMethods.GlobalLock(new HandleRef(null, modeHandle));
                SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(modePointer, typeof(SafeNativeMethods.DEVMODE));

                PaperSize result = PaperSizeFromMode(mode);

                SafeNativeMethods.GlobalUnlock(new HandleRef(null, modeHandle));

                if (ownHandle)
                    SafeNativeMethods.GlobalFree(new HandleRef(null, modeHandle));

                return result;
            }
            else
                return _paperSize;
        }

        private PaperSize PaperSizeFromMode(SafeNativeMethods.DEVMODE mode)
        {
            PaperSize[] sizes = printerSettings.Get_PaperSizes();
            if ((mode.dmFields & SafeNativeMethods.DM_PAPERSIZE) == SafeNativeMethods.DM_PAPERSIZE)
            {
                for (int i = 0; i < sizes.Length; i++)
                {
                    if ((int)sizes[i].RawKind == mode.dmPaperSize)
                        return sizes[i];
                }
            }
            return new PaperSize(PaperKind.Custom, "custom",
                                     //mode.dmPaperWidth, mode.dmPaperLength);
                                     PrinterUnitConvert.Convert(mode.dmPaperWidth, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display),
                                     PrinterUnitConvert.Convert(mode.dmPaperLength, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display));
        }

        private PaperSource PaperSourceFromMode(SafeNativeMethods.DEVMODE mode)
        {
            PaperSource[] sources = printerSettings.Get_PaperSources();
            if ((mode.dmFields & SafeNativeMethods.DM_DEFAULTSOURCE) == SafeNativeMethods.DM_DEFAULTSOURCE)
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    // the dmDefaultSource == to the RawKind in the Papersource.. and Not the Kind...
                    // if the PaperSource is populated with CUSTOM values...
                    if (unchecked((short)sources[i].RawKind) == mode.dmDefaultSource)
                    {
                        return sources[i];
                    }
                }
            }
            return new PaperSource((PaperSourceKind)mode.dmDefaultSource, "unknown");
        }

        private PrinterResolution PrinterResolutionFromMode(SafeNativeMethods.DEVMODE mode)
        {
            PrinterResolution[] resolutions = printerSettings.Get_PrinterResolutions();
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (mode.dmPrintQuality >= 0 && ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    && ((mode.dmFields & SafeNativeMethods.DM_YRESOLUTION) == SafeNativeMethods.DM_YRESOLUTION))
                {
                    if (resolutions[i].X == unchecked((int)(PrinterResolutionKind)mode.dmPrintQuality)
                        && resolutions[i].Y == unchecked((int)(PrinterResolutionKind)mode.dmYResolution))
                        return resolutions[i];
                }
                else
                {
                    if ((mode.dmFields & SafeNativeMethods.DM_PRINTQUALITY) == SafeNativeMethods.DM_PRINTQUALITY)
                    {
                        if (resolutions[i].Kind == (PrinterResolutionKind)mode.dmPrintQuality)
                            return resolutions[i];
                    }
                }
            }
            return new PrinterResolution(PrinterResolutionKind.Custom,
                                         mode.dmPrintQuality, mode.dmYResolution);
        }

        /// <summary>
        /// Copies the relevant information out of the handle and into the PageSettings.
        /// </summary>
        public void SetHdevmode(IntPtr hdevmode)
        {
            if (hdevmode == IntPtr.Zero)
                throw new ArgumentException(SR.Format(SR.InvalidPrinterHandle, hdevmode));

            IntPtr pointer = SafeNativeMethods.GlobalLock(new HandleRef(null, hdevmode));
            SafeNativeMethods.DEVMODE mode = (SafeNativeMethods.DEVMODE)Marshal.PtrToStructure(pointer, typeof(SafeNativeMethods.DEVMODE));

            if ((mode.dmFields & SafeNativeMethods.DM_COLOR) == SafeNativeMethods.DM_COLOR)
            {
                _color = (mode.dmColor == SafeNativeMethods.DMCOLOR_COLOR);
            }

            if ((mode.dmFields & SafeNativeMethods.DM_ORIENTATION) == SafeNativeMethods.DM_ORIENTATION)
            {
                _landscape = (mode.dmOrientation == SafeNativeMethods.DMORIENT_LANDSCAPE);
            }

            _paperSize = PaperSizeFromMode(mode);
            _paperSource = PaperSourceFromMode(mode);
            _printerResolution = PrinterResolutionFromMode(mode);

            SafeNativeMethods.GlobalUnlock(new HandleRef(null, hdevmode));
        }

        /// <summary>
        /// Provides some interesting information about the PageSettings in String form.
        /// </summary>
        public override string ToString()
        {
            return "[PageSettings:"
            + " Color=" + Color.ToString()
            + ", Landscape=" + Landscape.ToString()
            + ", Margins=" + Margins.ToString()
            + ", PaperSize=" + PaperSize.ToString()
            + ", PaperSource=" + PaperSource.ToString()
            + ", PrinterResolution=" + PrinterResolution.ToString()
            + "]";
        }
    }
}
