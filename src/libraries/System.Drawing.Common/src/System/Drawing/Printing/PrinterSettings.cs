// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Drawing.Printing
{
    /// <summary>
    /// Information about how a document should be printed, including which printer to print it on.
    /// </summary>
    public class PrinterSettings : ICloneable
    {
        // All read/write data is stored in managed code, and whenever we need to call Win32,
        // we create new DEVMODE and DEVNAMES structures.  We don't store device capabilities,
        // though.
        //
        // Also, all properties have hidden tri-state logic -- yes/no/default
        private const int Padding64Bit = 4;

        private string? _printerName; // default printer.
        private string _driverName = "";
        private string _outputPort = "";
        private bool _printToFile;

        // Whether the PrintDialog has been shown (not whether it's currently shown).  This is how we enforce SafePrinting.
        private bool _printDialogDisplayed;

        private short _extrabytes;
        private byte[]? _extrainfo;

        private short _copies = -1;
        private Duplex _duplex = System.Drawing.Printing.Duplex.Default;
        private TriState _collate = TriState.Default;
        private readonly PageSettings _defaultPageSettings;
        private int _fromPage;
        private int _toPage;
        private int _maxPage = 9999;
        private int _minPage;
        private PrintRange _printRange;

        private short _devmodebytes;
        private byte[]? _cachedDevmode;

        /// <summary>
        /// Initializes a new instance of the <see cref='PrinterSettings'/> class.
        /// </summary>
        public PrinterSettings()
        {
            _defaultPageSettings = new PageSettings(this);
        }

        /// <summary>
        /// Gets a value indicating whether the printer supports duplex (double-sided) printing.
        /// </summary>
        public bool CanDuplex
        {
            get { return DeviceCapabilities(SafeNativeMethods.DC_DUPLEX, IntPtr.Zero, 0) == 1; }
        }

        /// <summary>
        /// Gets or sets the number of copies to print.
        /// </summary>
        public short Copies
        {
            get
            {
                if (_copies != -1)
                    return _copies;
                else
                    return GetModeField(ModeField.Copies, 1);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx,
                                                             nameof(value), value.ToString(CultureInfo.CurrentCulture),
                                                             (0).ToString(CultureInfo.CurrentCulture)));
                /*
                    We shouldnt allow copies to be set since the copies can be a large number
                    and can be reflected in PrintDialog. So for the Copies property,
                    we prefer that for SafePrinting, copied cannot be set programmatically
                    but through the print dialog.
                    Any lower security could set copies to anything.
                */
                _copies = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the print out is collated.
        /// </summary>
        public bool Collate
        {
            get
            {
                if (!_collate.IsDefault)
                    return (bool)_collate;
                else
                    return GetModeField(ModeField.Collate, SafeNativeMethods.DMCOLLATE_FALSE) == SafeNativeMethods.DMCOLLATE_TRUE;
            }
            set { _collate = value; }
        }

        /// <summary>
        /// Gets the default page settings for this printer.
        /// </summary>
        public PageSettings DefaultPageSettings
        {
            get { return _defaultPageSettings; }
        }

        // As far as I can tell, Windows no longer pays attention to driver names and output ports.
        // But I'm leaving this code in place in case I'm wrong.
        internal string DriverName
        {
            get { return _driverName; }
        }

        /// <summary>
        /// Gets or sets the printer's duplex setting.
        /// </summary>
        public Duplex Duplex
        {
            get
            {
                if (_duplex != Duplex.Default)
                {
                    return _duplex;
                }

                return (Duplex)GetModeField(ModeField.Duplex, SafeNativeMethods.DMDUP_SIMPLEX);
            }
            set
            {
                if (value < Duplex.Default || value > Duplex.Horizontal)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(Duplex));
                }

                _duplex = value;
            }
        }

        /// <summary>
        /// Gets or sets the first page to print.
        /// </summary>
        public int FromPage
        {
            get { return _fromPage; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx,
                                                             nameof(value), value.ToString(CultureInfo.CurrentCulture),
                                                             (0).ToString(CultureInfo.CurrentCulture)));
                _fromPage = value;
            }
        }



        /// <summary>
        /// Gets the names of all printers installed on the machine.
        /// </summary>
        public static StringCollection InstalledPrinters
        {
            get
            {
                int sizeofstruct;
                // Note: The call to get the size of the buffer required for level 5 does not work properly on NT platforms.
                const int Level = 4;
                // PRINTER_INFO_4 is 12 or 24 bytes in size depending on the architecture.
                if (IntPtr.Size == 8)
                {
                    sizeofstruct = (IntPtr.Size * 2) + (sizeof(int) * 1) + Padding64Bit;
                }
                else
                {
                    sizeofstruct = (IntPtr.Size * 2) + (sizeof(int) * 1);
                }

                int bufferSize;
                int count;
                Interop.Winspool.EnumPrinters(SafeNativeMethods.PRINTER_ENUM_LOCAL | SafeNativeMethods.PRINTER_ENUM_CONNECTIONS, null, Level, IntPtr.Zero, 0, out bufferSize, out _);

                IntPtr buffer = Marshal.AllocCoTaskMem(bufferSize);
                int returnCode = Interop.Winspool.EnumPrinters(SafeNativeMethods.PRINTER_ENUM_LOCAL | SafeNativeMethods.PRINTER_ENUM_CONNECTIONS,
                                                        null, Level, buffer,
                                                        bufferSize, out _, out count);
                var array = new string[count];

                if (returnCode == 0)
                {
                    Marshal.FreeCoTaskMem(buffer);
                    throw new Win32Exception();
                }

                for (int i = 0; i < count; i++)
                {
                    // The printer name is at offset 0
                    //
                    IntPtr namePointer = (IntPtr)Marshal.ReadIntPtr((IntPtr)(checked((long)buffer + i * sizeofstruct)));
                    array[i] = Marshal.PtrToStringAuto(namePointer)!;
                }

                Marshal.FreeCoTaskMem(buffer);

                return new StringCollection(array);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref='PrinterName'/> property designates the default printer.
        /// </summary>
        public bool IsDefaultPrinter
        {
            get
            {
                return (_printerName == null || _printerName == GetDefaultPrinterName());
            }
        }

        /// <summary>
        /// Gets a value indicating whether the printer is a plotter, as opposed to a raster printer.
        /// </summary>
        public bool IsPlotter
        {
            get
            {
                return GetDeviceCaps(Interop.Gdi32.DeviceCapability.TECHNOLOGY, Interop.Gdi32.DeviceTechnology.DT_RASPRINTER) == Interop.Gdi32.DeviceTechnology.DT_PLOTTER;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref='PrinterName'/> property designates a valid printer.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return DeviceCapabilities(SafeNativeMethods.DC_COPIES, IntPtr.Zero, -1) != -1;
            }
        }

        /// <summary>
        /// Gets the angle, in degrees, which the portrait orientation is rotated to produce the landscape orientation.
        /// </summary>
        public int LandscapeAngle
        {
            get { return DeviceCapabilities(SafeNativeMethods.DC_ORIENTATION, IntPtr.Zero, 0); }
        }

        /// <summary>
        /// Gets the maximum number of copies allowed by the printer.
        /// </summary>
        public int MaximumCopies
        {
            get { return DeviceCapabilities(SafeNativeMethods.DC_COPIES, IntPtr.Zero, 1); }
        }

        /// <summary>
        /// Gets or sets the highest <see cref='FromPage'/> or <see cref='ToPage'/> which may be selected in a print dialog box.
        /// </summary>
        public int MaximumPage
        {
            get { return _maxPage; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx,
                                                             nameof(value), value.ToString(CultureInfo.CurrentCulture),
                                                             (0).ToString(CultureInfo.CurrentCulture)));
                _maxPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the lowest <see cref='FromPage'/> or <see cref='ToPage'/> which may be selected in a print dialog box.
        /// </summary>
        public int MinimumPage
        {
            get { return _minPage; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx,
                                                             nameof(value), value.ToString(CultureInfo.CurrentCulture),
                                                             (0).ToString(CultureInfo.CurrentCulture)));
                _minPage = value;
            }
        }

        internal string OutputPort
        {
            get
            {
                return _outputPort;
            }
            set
            {
                _outputPort = value;
            }
        }

        /// <summary>
        /// Indicates the name of the printerfile.
        /// </summary>
        public string PrintFileName
        {
            get
            {
                string printFileName = OutputPort;
                return printFileName;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException(value);
                }
                OutputPort = value;
            }
        }

        /// <summary>
        /// Gets the paper sizes supported by this printer.
        /// </summary>
        public PaperSizeCollection PaperSizes
        {
            get { return new PaperSizeCollection(Get_PaperSizes()); }
        }

        /// <summary>
        /// Gets the paper sources available on this printer.
        /// </summary>
        public PaperSourceCollection PaperSources
        {
            get { return new PaperSourceCollection(Get_PaperSources()); }
        }

        /// <summary>
        /// Whether the print dialog has been displayed.  In SafePrinting mode, a print dialog is required to print.
        /// After printing, this property is set to false if the program does not have AllPrinting; this guarantees
        /// a document is only printed once each time the print dialog is shown.
        /// </summary>
        internal bool PrintDialogDisplayed
        {
            get
            {
                return _printDialogDisplayed;
            }

            set
            {
                _printDialogDisplayed = value;
            }
        }

        /// <summary>
        /// Gets or sets the pages the user has asked to print.
        /// </summary>
        public PrintRange PrintRange
        {
            get { return _printRange; }
            set
            {
                if (!Enum.IsDefined(typeof(PrintRange), value))
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(PrintRange));

                _printRange = value;
            }
        }

        /// <summary>
        /// Indicates whether to print to a file instead of a port.
        /// </summary>
        public bool PrintToFile
        {
            get
            {
                return _printToFile;
            }
            set
            {
                _printToFile = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the printer.
        /// </summary>
        public string PrinterName
        {
            get
            {
                return PrinterNameInternal;
            }

            set
            {
                PrinterNameInternal = value;
            }
        }

        private string PrinterNameInternal
        {
            get
            {
                if (_printerName == null)
                    return GetDefaultPrinterName();
                else
                    return _printerName;
            }
            set
            {
                // Reset the DevMode and Extrabytes...
                _cachedDevmode = null;
                _extrainfo = null;
                _printerName = value;
                // PrinterName can be set through a fulltrusted assembly without using  the PrintDialog.
                // So dont set this variable here.
                //PrintDialogDisplayed = true;
            }
        }

        /// <summary>
        /// Gets the resolutions supported by this printer.
        /// </summary>
        public PrinterResolutionCollection PrinterResolutions
        {
            get { return new PrinterResolutionCollection(Get_PrinterResolutions()); }
        }

        /// <summary>
        /// If the image is a JPEG or a PNG (Image.RawFormat) and the printer returns true from
        /// ExtEscape(CHECKJPEGFORMAT) or ExtEscape(CHECKPNGFORMAT) then this function returns true.
        /// </summary>
        public bool IsDirectPrintingSupported(ImageFormat imageFormat)
        {
            bool isDirectPrintingSupported = false;
            if (imageFormat.Equals(ImageFormat.Jpeg) || imageFormat.Equals(ImageFormat.Png))
            {
                int nEscape = imageFormat.Equals(ImageFormat.Jpeg) ? Interop.Gdi32.CHECKJPEGFORMAT : Interop.Gdi32.CHECKPNGFORMAT;
                int outData;
                DeviceContext dc = CreateInformationContext(DefaultPageSettings);
                HandleRef hdc = new HandleRef(dc, dc.Hdc);
                try
                {
                    isDirectPrintingSupported = Interop.Gdi32.ExtEscape(hdc, Interop.Gdi32.QUERYESCSUPPORT, sizeof(int), ref nEscape, 0, out outData) > 0;
                }
                finally
                {
                    dc.Dispose();
                }
            }
            return isDirectPrintingSupported;
        }

        /// <summary>
        /// This method utilizes the CHECKJPEGFORMAT/CHECKPNGFORMAT printer escape functions
        /// to determine whether the printer can handle a JPEG image.
        ///
        /// If the image is a JPEG or a PNG (Image.RawFormat) and the printer returns true
        /// from ExtEscape(CHECKJPEGFORMAT) or ExtEscape(CHECKPNGFORMAT) then this function returns true.
        /// </summary>
        public bool IsDirectPrintingSupported(Image image)
        {
            bool isDirectPrintingSupported = false;
            if (image.RawFormat.Equals(ImageFormat.Jpeg) || image.RawFormat.Equals(ImageFormat.Png))
            {
                MemoryStream stream = new MemoryStream();
                try
                {
                    image.Save(stream, image.RawFormat);

                    byte[] pvImage = stream.ToArray();

                    int nEscape = image.RawFormat.Equals(ImageFormat.Jpeg) ? Interop.Gdi32.CHECKJPEGFORMAT : Interop.Gdi32.CHECKPNGFORMAT;
                    int outData = 0;

                    DeviceContext dc = CreateInformationContext(DefaultPageSettings);
                    HandleRef hdc = new HandleRef(dc, dc.Hdc);
                    try
                    {
                        bool querySupported = Interop.Gdi32.ExtEscape(hdc, Interop.Gdi32.QUERYESCSUPPORT, sizeof(int), ref nEscape, 0, out outData) > 0;
                        if (querySupported)
                        {
                            isDirectPrintingSupported = (Interop.Gdi32.ExtEscape(hdc, nEscape, pvImage.Length, pvImage, sizeof(int), out outData) > 0)
                                                        && (outData == 1);
                        }
                    }
                    finally
                    {
                        dc.Dispose();
                    }
                }
                finally
                {
                    stream.Close();
                }
            }
            return isDirectPrintingSupported;
        }

        /// <summary>
        /// Gets a value indicating whether the printer supports color printing.
        /// </summary>
        public bool SupportsColor
        {
            get
            {
                // If the printer supports color printing, the return value is 1; otherwise, the return value is zero.
                // The pointerToBuffer parameter is not used.
                return DeviceCapabilities(
                    capability: SafeNativeMethods.DC_COLORDEVICE,
                    pointerToBuffer: IntPtr.Zero,
                    defaultValue: 0) == 1;
            }
        }

        /// <summary>
        /// Gets or sets the last page to print.
        /// </summary>
        public int ToPage
        {
            get { return _toPage; }
            set
            {
                if (value < 0)
                    throw new ArgumentException(SR.Format(SR.InvalidLowBoundArgumentEx,
                                                             nameof(value), value.ToString(CultureInfo.CurrentCulture),
                                                             (0).ToString(CultureInfo.CurrentCulture)));
                _toPage = value;
            }
        }

        /// <summary>
        /// Creates an identical copy of this object.
        /// </summary>
        public object Clone()
        {
            PrinterSettings clone = (PrinterSettings)MemberwiseClone();
            clone._printDialogDisplayed = false;
            return clone;
        }
        // what is done in copytohdevmode cannot give unwanted access AllPrinting permission
        internal DeviceContext CreateDeviceContext(PageSettings pageSettings)
        {
            IntPtr modeHandle = GetHdevmodeInternal();
            DeviceContext? dc = null;

            try
            {
                //Copy the PageSettings to the DEVMODE...
                pageSettings.CopyToHdevmode(modeHandle);
                dc = CreateDeviceContext(modeHandle);
            }
            finally
            {
                Interop.Kernel32.GlobalFree(modeHandle);
            }
            return dc;
        }

        internal DeviceContext CreateDeviceContext(IntPtr hdevmode)
        {
            IntPtr modePointer = Interop.Kernel32.GlobalLock(hdevmode);
            DeviceContext dc = DeviceContext.CreateDC(DriverName, PrinterNameInternal, fileName:null, modePointer);
            Interop.Kernel32.GlobalUnlock(hdevmode);
            return dc;
        }

        // A read-only DC, which is faster than CreateHdc
        // what is done in copytohdevmode cannot give unwanted access AllPrinting permission
        internal DeviceContext CreateInformationContext(PageSettings pageSettings)
        {
            IntPtr modeHandle = GetHdevmodeInternal();
            DeviceContext dc;

            try
            {
                //Copy the PageSettings to the DEVMODE...
                pageSettings.CopyToHdevmode(modeHandle);
                dc = CreateInformationContext(modeHandle);
            }
            finally
            {
                Interop.Kernel32.GlobalFree(modeHandle);
            }
            return dc;
        }

        // A read-only DC, which is faster than CreateHdc
        internal DeviceContext CreateInformationContext(IntPtr hdevmode)
        {
            IntPtr modePointer = Interop.Kernel32.GlobalLock(hdevmode);
            DeviceContext dc = DeviceContext.CreateIC(DriverName, PrinterNameInternal, fileName:null, modePointer);
            Interop.Kernel32.GlobalUnlock(hdevmode);
            return dc;
        }

        public Graphics CreateMeasurementGraphics()
        {
            return CreateMeasurementGraphics(DefaultPageSettings);
        }

        //whatever the call stack calling HardMarginX and HardMarginY here is safe
        public Graphics CreateMeasurementGraphics(bool honorOriginAtMargins)
        {
            Graphics g = CreateMeasurementGraphics();
            if (honorOriginAtMargins)
            {
                g.TranslateTransform(-_defaultPageSettings.HardMarginX, -_defaultPageSettings.HardMarginY);
                g.TranslateTransform(_defaultPageSettings.Margins.Left, _defaultPageSettings.Margins.Top);
            }
            return g;
        }

        public Graphics CreateMeasurementGraphics(PageSettings pageSettings)
        {
            // returns the Graphics object for the printer
            DeviceContext dc = CreateDeviceContext(pageSettings);
            Graphics g = Graphics.FromHdcInternal(dc.Hdc);
            g.PrintingHelper = dc; // Graphics will dispose of the DeviceContext.
            return g;
        }

        //whatever the call stack calling HardMarginX and HardMarginY here is safe
        public Graphics CreateMeasurementGraphics(PageSettings pageSettings, bool honorOriginAtMargins)
        {
            Graphics g = CreateMeasurementGraphics();
            if (honorOriginAtMargins)
            {
                g.TranslateTransform(-pageSettings.HardMarginX, -pageSettings.HardMarginY);
                g.TranslateTransform(pageSettings.Margins.Left, pageSettings.Margins.Top);
            }
            return g;
        }

        // Create a PRINTDLG with a few useful defaults.
        // Try to keep this consistent with PrintDialog.CreatePRINTDLG.
        private static unsafe void CreatePRINTDLGX86(out Interop.Comdlg32.PRINTDLGX86 data)
        {
            data = default;
            data.lStructSize = sizeof(Interop.Comdlg32.PRINTDLGX86);
            data.nFromPage = 1;
            data.nToPage = 1;
            data.nMinPage = 0;
            data.nMaxPage = 9999;
            data.nCopies = 1;
        }

        // Create a PRINTDLG with a few useful defaults.
        // Try to keep this consistent with PrintDialog.CreatePRINTDLG.
        private static unsafe void CreatePRINTDLG(out Interop.Comdlg32.PRINTDLG data)
        {
            data = default;
            data.lStructSize = sizeof(Interop.Comdlg32.PRINTDLG);
            data.nFromPage = 1;
            data.nToPage = 1;
            data.nMinPage = 0;
            data.nMaxPage = 9999;
            data.nCopies = 1;
        }

        //  Use FastDeviceCapabilities where possible -- computing PrinterName is quite slow
        private int DeviceCapabilities(short capability, IntPtr pointerToBuffer, int defaultValue)
        {
            string printerName = PrinterName;
            return FastDeviceCapabilities(capability, pointerToBuffer, defaultValue, printerName);
        }

        // We pass PrinterName in as a parameter rather than computing it ourselves because it's expensive to compute.
        // We need to pass IntPtr.Zero since passing HDevMode is non-performant.
        private static int FastDeviceCapabilities(short capability, IntPtr pointerToBuffer, int defaultValue, string printerName)
        {
            int result = Interop.Winspool.DeviceCapabilities(printerName, GetOutputPort(),
                                                          capability, pointerToBuffer, IntPtr.Zero);
            if (result == -1)
                return defaultValue;
            return result;
        }

        // Called by get_PrinterName
        private static string GetDefaultPrinterName()
        {
            if (IntPtr.Size == 8)
            {
                CreatePRINTDLG(out Interop.Comdlg32.PRINTDLG data);
                data.Flags = SafeNativeMethods.PD_RETURNDEFAULT;
                bool status = Interop.Comdlg32.PrintDlg(ref data);

                if (!status)
                    return SR.NoDefaultPrinter;

                IntPtr handle = data.hDevNames;
                IntPtr names = Interop.Kernel32.GlobalLock(handle);
                if (names == IntPtr.Zero)
                    throw new Win32Exception();

                string name = ReadOneDEVNAME(names, 1);
                Interop.Kernel32.GlobalUnlock(handle);

                // Windows allocates them, but we have to free them
                Interop.Kernel32.GlobalFree(data.hDevNames);
                Interop.Kernel32.GlobalFree(data.hDevMode);

                return name;
            }
            else
            {
                CreatePRINTDLGX86(out Interop.Comdlg32.PRINTDLGX86 data);
                data.Flags = SafeNativeMethods.PD_RETURNDEFAULT;
                bool status = Interop.Comdlg32.PrintDlg(ref data);

                if (!status)
                    return SR.NoDefaultPrinter;

                IntPtr handle = data.hDevNames;
                IntPtr names = Interop.Kernel32.GlobalLock(handle);
                if (names == IntPtr.Zero)
                    throw new Win32Exception();

                string name = ReadOneDEVNAME(names, 1);
                Interop.Kernel32.GlobalUnlock(handle);

                // Windows allocates them, but we have to free them
                Interop.Kernel32.GlobalFree(data.hDevNames);
                Interop.Kernel32.GlobalFree(data.hDevMode);

                return name;
            }
        }


        // Called by get_OutputPort
        private static string GetOutputPort()
        {
            if (IntPtr.Size == 8)
            {
                CreatePRINTDLG(out Interop.Comdlg32.PRINTDLG data);
                data.Flags = SafeNativeMethods.PD_RETURNDEFAULT;
                bool status = Interop.Comdlg32.PrintDlg(ref data);
                if (!status)
                    return SR.NoDefaultPrinter;

                IntPtr handle = data.hDevNames;
                IntPtr names = Interop.Kernel32.GlobalLock(handle);
                if (names == IntPtr.Zero)
                    throw new Win32Exception();

                string name = ReadOneDEVNAME(names, 2);

                Interop.Kernel32.GlobalUnlock(handle);

                // Windows allocates them, but we have to free them
                Interop.Kernel32.GlobalFree(data.hDevNames);
                Interop.Kernel32.GlobalFree(data.hDevMode);

                return name;
            }
            else
            {
                CreatePRINTDLGX86(out Interop.Comdlg32.PRINTDLGX86 data);
                data.Flags = SafeNativeMethods.PD_RETURNDEFAULT;
                bool status = Interop.Comdlg32.PrintDlg(ref data);

                if (!status)
                    return SR.NoDefaultPrinter;

                IntPtr handle = data.hDevNames;
                IntPtr names = Interop.Kernel32.GlobalLock(handle);
                if (names == IntPtr.Zero)
                    throw new Win32Exception();

                string name = ReadOneDEVNAME(names, 2);

                Interop.Kernel32.GlobalUnlock(handle);

                // Windows allocates them, but we have to free them
                Interop.Kernel32.GlobalFree(data.hDevNames);
                Interop.Kernel32.GlobalFree(data.hDevMode);

                return name;
            }
        }

        private int GetDeviceCaps(Interop.Gdi32.DeviceCapability capability, int defaultValue)
        {
            using (DeviceContext dc = CreateInformationContext(DefaultPageSettings))
            {
                return Interop.Gdi32.GetDeviceCaps(new HandleRef(dc, dc.Hdc), capability);
            }
        }

        /// <summary>
        /// Creates a handle to a DEVMODE structure which correspond too the printer settings.When you are done with the
        /// handle, you must deallocate it yourself:
        ///   Interop.Kernel32.GlobalFree(handle);
        ///   Where "handle" is the return value from this method.
        /// </summary>
        public IntPtr GetHdevmode()
        {
            IntPtr modeHandle = GetHdevmodeInternal();
            _defaultPageSettings.CopyToHdevmode(modeHandle);
            return modeHandle;
        }

        internal IntPtr GetHdevmodeInternal()
        {
            // getting the printer name is quite expensive if PrinterName is left default,
            // because it needs to figure out what the default printer is
            return GetHdevmodeInternal(PrinterNameInternal);
        }

        private IntPtr GetHdevmodeInternal(string printer)
        {
            // Create DEVMODE
            int modeSize = Interop.Winspool.DocumentProperties(NativeMethods.NullHandleRef, NativeMethods.NullHandleRef, printer, IntPtr.Zero, NativeMethods.NullHandleRef, 0);
            if (modeSize < 1)
            {
                throw new InvalidPrinterException(this);
            }
            IntPtr handle = Interop.Kernel32.GlobalAlloc(SafeNativeMethods.GMEM_MOVEABLE, (uint)modeSize); // cannot be <0 anyway
            IntPtr pointer = Interop.Kernel32.GlobalLock(handle);

            //Get the DevMode only if its not cached....
            if (_cachedDevmode != null)
            {
                Marshal.Copy(_cachedDevmode, 0, pointer, _devmodebytes);
            }
            else
            {
                int returnCode = Interop.Winspool.DocumentProperties(NativeMethods.NullHandleRef, NativeMethods.NullHandleRef, printer, pointer, NativeMethods.NullHandleRef, SafeNativeMethods.DM_OUT_BUFFER);
                if (returnCode < 0)
                {
                    throw new Win32Exception();
                }
            }

            Interop.Gdi32.DEVMODE mode = Marshal.PtrToStructure<Interop.Gdi32.DEVMODE>(pointer)!;

            if (_extrainfo != null)
            {
                // guard against buffer overrun attacks (since design allows client to set a new printer name without updating the devmode)
                // by checking for a large enough buffer size before copying the extrainfo buffer
                if (_extrabytes <= mode.dmDriverExtra)
                {
                    IntPtr pointeroffset = (IntPtr)(checked((long)pointer + (long)mode.dmSize));
                    Marshal.Copy(_extrainfo, 0, pointeroffset, _extrabytes);
                }
            }
            if ((mode.dmFields & SafeNativeMethods.DM_COPIES) == SafeNativeMethods.DM_COPIES)
            {
                if (_copies != -1)
                    mode.dmCopies = _copies;
            }

            if ((mode.dmFields & SafeNativeMethods.DM_DUPLEX) == SafeNativeMethods.DM_DUPLEX)
            {
                if (unchecked((int)_duplex) != -1)
                    mode.dmDuplex = unchecked((short)_duplex);
            }

            if ((mode.dmFields & SafeNativeMethods.DM_COLLATE) == SafeNativeMethods.DM_COLLATE)
            {
                if (_collate.IsNotDefault)
                    mode.dmCollate = (short)(((bool)_collate) ? SafeNativeMethods.DMCOLLATE_TRUE : SafeNativeMethods.DMCOLLATE_FALSE);
            }

            Marshal.StructureToPtr(mode, pointer, false);

            int retCode = Interop.Winspool.DocumentProperties(NativeMethods.NullHandleRef, NativeMethods.NullHandleRef, printer, pointer, pointer, SafeNativeMethods.DM_IN_BUFFER | SafeNativeMethods.DM_OUT_BUFFER);
            if (retCode < 0)
            {
                Interop.Kernel32.GlobalFree(handle);
                Interop.Kernel32.GlobalUnlock(handle);
                return IntPtr.Zero;
            }


            Interop.Kernel32.GlobalUnlock(handle);
            return handle;
        }

        /// <summary>
        /// Creates a handle to a DEVMODE structure which correspond to the printer and page settings.
        /// When you are done with the handle, you must deallocate it yourself:
        ///   Interop.Kernel32.GlobalFree(handle);
        ///   Where "handle" is the return value from this method.
        /// </summary>
        public IntPtr GetHdevmode(PageSettings pageSettings)
        {
            IntPtr modeHandle = GetHdevmodeInternal();
            pageSettings.CopyToHdevmode(modeHandle);

            return modeHandle;
        }

        /// <summary>
        /// Creates a handle to a DEVNAMES structure which correspond to the printer settings.
        /// When you are done with the handle, you must deallocate it yourself:
        ///   Interop.Kernel32.GlobalFree(handle);
        ///   Where "handle" is the return value from this method.
        /// </summary>
        public IntPtr GetHdevnames()
        {
            string printerName = PrinterName; // the PrinterName property is slow when using the default printer
            string driver = DriverName;  // make sure we are writing out exactly the same string as we got the length of
            string outPort = OutputPort;

            // Create DEVNAMES structure
            // +4 for null terminator
            int namesCharacters = checked(4 + printerName.Length + driver.Length + outPort.Length);

            // 8 = size of fixed portion of DEVNAMES
            short offset = (short)(8 / Marshal.SystemDefaultCharSize); // Offsets are in characters, not bytes
            uint namesSize = (uint)checked(Marshal.SystemDefaultCharSize * (offset + namesCharacters)); // always >0
            IntPtr handle = Interop.Kernel32.GlobalAlloc(SafeNativeMethods.GMEM_MOVEABLE | SafeNativeMethods.GMEM_ZEROINIT, namesSize);
            IntPtr namesPointer = Interop.Kernel32.GlobalLock(handle);

            Marshal.WriteInt16(namesPointer, offset); // wDriverOffset
            offset += WriteOneDEVNAME(driver, namesPointer, offset);
            Marshal.WriteInt16((IntPtr)(checked((long)namesPointer + 2)), offset); // wDeviceOffset
            offset += WriteOneDEVNAME(printerName, namesPointer, offset);
            Marshal.WriteInt16((IntPtr)(checked((long)namesPointer + 4)), offset); // wOutputOffset
            offset += WriteOneDEVNAME(outPort, namesPointer, offset);
            Marshal.WriteInt16((IntPtr)(checked((long)namesPointer + 6)), offset); // wDefault

            Interop.Kernel32.GlobalUnlock(handle);
            return handle;
        }

        // Handles creating then disposing a default DEVMODE
        internal short GetModeField(ModeField field, short defaultValue)
        {
            return GetModeField(field, defaultValue, IntPtr.Zero);
        }

        internal short GetModeField(ModeField field, short defaultValue, IntPtr modeHandle)
        {
            bool ownHandle = false;
            short result;
            try
            {
                if (modeHandle == IntPtr.Zero)
                {
                    try
                    {
                        modeHandle = GetHdevmodeInternal();
                        ownHandle = true;
                    }
                    catch (InvalidPrinterException)
                    {
                        return defaultValue;
                    }
                }

                IntPtr modePointer = Interop.Kernel32.GlobalLock(new HandleRef(this, modeHandle));
                Interop.Gdi32.DEVMODE mode = Marshal.PtrToStructure<Interop.Gdi32.DEVMODE>(modePointer)!;
                switch (field)
                {
                    case ModeField.Orientation:
                        result = mode.dmOrientation;
                        break;
                    case ModeField.PaperSize:
                        result = mode.dmPaperSize;
                        break;
                    case ModeField.PaperLength:
                        result = mode.dmPaperLength;
                        break;
                    case ModeField.PaperWidth:
                        result = mode.dmPaperWidth;
                        break;
                    case ModeField.Copies:
                        result = mode.dmCopies;
                        break;
                    case ModeField.DefaultSource:
                        result = mode.dmDefaultSource;
                        break;
                    case ModeField.PrintQuality:
                        result = mode.dmPrintQuality;
                        break;
                    case ModeField.Color:
                        result = mode.dmColor;
                        break;
                    case ModeField.Duplex:
                        result = mode.dmDuplex;
                        break;
                    case ModeField.YResolution:
                        result = mode.dmYResolution;
                        break;
                    case ModeField.TTOption:
                        result = mode.dmTTOption;
                        break;
                    case ModeField.Collate:
                        result = mode.dmCollate;
                        break;
                    default:
                        Debug.Fail("Invalid field in GetModeField");
                        result = defaultValue;
                        break;
                }
                Interop.Kernel32.GlobalUnlock(new HandleRef(this, modeHandle));
            }
            finally
            {
                if (ownHandle)
                {
                    Interop.Kernel32.GlobalFree(new HandleRef(this, modeHandle));
                }
            }
            return result;
        }

        internal PaperSize[] Get_PaperSizes()
        {
            string printerName = PrinterName; //  this is quite expensive if PrinterName is left default

            int count = FastDeviceCapabilities(SafeNativeMethods.DC_PAPERNAMES, IntPtr.Zero, -1, printerName);
            if (count == -1)
                return Array.Empty<PaperSize>();
            int stringSize = Marshal.SystemDefaultCharSize * 64;
            IntPtr namesBuffer = Marshal.AllocCoTaskMem(checked(stringSize * count));
            FastDeviceCapabilities(SafeNativeMethods.DC_PAPERNAMES, namesBuffer, -1, printerName);

            Debug.Assert(FastDeviceCapabilities(SafeNativeMethods.DC_PAPERS, IntPtr.Zero, -1, printerName) == count,
                         "Not the same number of paper kinds as paper names?");
            IntPtr kindsBuffer = Marshal.AllocCoTaskMem(2 * count);
            FastDeviceCapabilities(SafeNativeMethods.DC_PAPERS, kindsBuffer, -1, printerName);

            Debug.Assert(FastDeviceCapabilities(SafeNativeMethods.DC_PAPERSIZE, IntPtr.Zero, -1, printerName) == count,
                         "Not the same number of paper kinds as paper names?");
            IntPtr dimensionsBuffer = Marshal.AllocCoTaskMem(8 * count);
            FastDeviceCapabilities(SafeNativeMethods.DC_PAPERSIZE, dimensionsBuffer, -1, printerName);

            PaperSize[] result = new PaperSize[count];
            for (int i = 0; i < count; i++)
            {
                string name = Marshal.PtrToStringAuto((IntPtr)(checked((long)namesBuffer + stringSize * i)), 64)!;
                int index = name.IndexOf('\0');
                if (index > -1)
                {
                    name = name.Substring(0, index);
                }
                short kind = Marshal.ReadInt16((IntPtr)(checked((long)kindsBuffer + i * 2)));
                int width = Marshal.ReadInt32((IntPtr)(checked((long)dimensionsBuffer + i * 8)));
                int height = Marshal.ReadInt32((IntPtr)(checked((long)dimensionsBuffer + i * 8 + 4)));
                result[i] = new PaperSize((PaperKind)kind, name,
                                          PrinterUnitConvert.Convert(width, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display),
                                          PrinterUnitConvert.Convert(height, PrinterUnit.TenthsOfAMillimeter, PrinterUnit.Display));
            }

            Marshal.FreeCoTaskMem(namesBuffer);
            Marshal.FreeCoTaskMem(kindsBuffer);
            Marshal.FreeCoTaskMem(dimensionsBuffer);
            return result;
        }

        internal PaperSource[] Get_PaperSources()
        {
            string printerName = PrinterName; //  this is quite expensive if PrinterName is left default

            int count = FastDeviceCapabilities(SafeNativeMethods.DC_BINNAMES, IntPtr.Zero, -1, printerName);
            if (count == -1)
                return Array.Empty<PaperSource>();

            // Contrary to documentation, DeviceCapabilities returns char[count, 24],
            // not char[count][24]
            int stringSize = Marshal.SystemDefaultCharSize * 24;
            IntPtr namesBuffer = Marshal.AllocCoTaskMem(checked(stringSize * count));
            FastDeviceCapabilities(SafeNativeMethods.DC_BINNAMES, namesBuffer, -1, printerName);

            Debug.Assert(FastDeviceCapabilities(SafeNativeMethods.DC_BINS, IntPtr.Zero, -1, printerName) == count,
                         "Not the same number of bin kinds as bin names?");
            IntPtr kindsBuffer = Marshal.AllocCoTaskMem(2 * count);
            FastDeviceCapabilities(SafeNativeMethods.DC_BINS, kindsBuffer, -1, printerName);

            PaperSource[] result = new PaperSource[count];
            for (int i = 0; i < count; i++)
            {
                string name = Marshal.PtrToStringAuto((IntPtr)(checked((long)namesBuffer + stringSize * i)), 24)!;
                int index = name.IndexOf('\0');
                if (index > -1)
                {
                    name = name.Substring(0, index);
                }

                short kind = Marshal.ReadInt16((IntPtr)(checked((long)kindsBuffer + 2 * i)));
                result[i] = new PaperSource((PaperSourceKind)kind, name);
            }

            Marshal.FreeCoTaskMem(namesBuffer);
            Marshal.FreeCoTaskMem(kindsBuffer);
            return result;
        }

        internal PrinterResolution[] Get_PrinterResolutions()
        {
            string printerName = PrinterName; //  this is quite expensive if PrinterName is left default
            PrinterResolution[] result;

            int count = FastDeviceCapabilities(SafeNativeMethods.DC_ENUMRESOLUTIONS, IntPtr.Zero, -1, printerName);
            if (count == -1)
            {
                //Just return the standard values if custom resolutions are absent ....
                result = new PrinterResolution[4];
                result[0] = new PrinterResolution(PrinterResolutionKind.High, -4, -1);
                result[1] = new PrinterResolution(PrinterResolutionKind.Medium, -3, -1);
                result[2] = new PrinterResolution(PrinterResolutionKind.Low, -2, -1);
                result[3] = new PrinterResolution(PrinterResolutionKind.Draft, -1, -1);

                return result;
            }

            result = new PrinterResolution[count + 4];
            result[0] = new PrinterResolution(PrinterResolutionKind.High, -4, -1);
            result[1] = new PrinterResolution(PrinterResolutionKind.Medium, -3, -1);
            result[2] = new PrinterResolution(PrinterResolutionKind.Low, -2, -1);
            result[3] = new PrinterResolution(PrinterResolutionKind.Draft, -1, -1);

            IntPtr buffer = Marshal.AllocCoTaskMem(checked(8 * count));
            FastDeviceCapabilities(SafeNativeMethods.DC_ENUMRESOLUTIONS, buffer, -1, printerName);

            for (int i = 0; i < count; i++)
            {
                int x = Marshal.ReadInt32((IntPtr)(checked((long)buffer + i * 8)));
                int y = Marshal.ReadInt32((IntPtr)(checked((long)buffer + i * 8 + 4)));
                result[i + 4] = new PrinterResolution(PrinterResolutionKind.Custom, x, y);
            }

            Marshal.FreeCoTaskMem(buffer);
            return result;
        }

        // names is pointer to DEVNAMES
        private static string ReadOneDEVNAME(IntPtr pDevnames, int slot)
        {
            int offset = checked(Marshal.SystemDefaultCharSize * Marshal.ReadInt16((IntPtr)(checked((long)pDevnames + slot * 2))));
            string result = Marshal.PtrToStringAuto((IntPtr)(checked((long)pDevnames + offset)))!;
            return result;
        }

        /// <summary>
        /// Copies the relevant information out of the handle and into the PrinterSettings.
        /// </summary>
        public void SetHdevmode(IntPtr hdevmode)
        {
            if (hdevmode == IntPtr.Zero)
                throw new ArgumentException(SR.Format(SR.InvalidPrinterHandle, hdevmode));

            IntPtr pointer = Interop.Kernel32.GlobalLock(hdevmode);
            Interop.Gdi32.DEVMODE mode = Marshal.PtrToStructure<Interop.Gdi32.DEVMODE>(pointer)!;

            //Copy entire public devmode as a byte array...
            _devmodebytes = mode.dmSize;
            if (_devmodebytes > 0)
            {
                _cachedDevmode = new byte[_devmodebytes];
                Marshal.Copy(pointer, _cachedDevmode, 0, _devmodebytes);
            }

            //Copy private devmode as a byte array..
            _extrabytes = mode.dmDriverExtra;
            if (_extrabytes > 0)
            {
                _extrainfo = new byte[_extrabytes];
                Marshal.Copy((IntPtr)(checked((long)pointer + (long)mode.dmSize)), _extrainfo, 0, _extrabytes);
            }

            if ((mode.dmFields & SafeNativeMethods.DM_COPIES) == SafeNativeMethods.DM_COPIES)
            {
                _copies = mode.dmCopies;
            }

            if ((mode.dmFields & SafeNativeMethods.DM_DUPLEX) == SafeNativeMethods.DM_DUPLEX)
            {
                _duplex = (Duplex)mode.dmDuplex;
            }

            if ((mode.dmFields & SafeNativeMethods.DM_COLLATE) == SafeNativeMethods.DM_COLLATE)
            {
                _collate = (mode.dmCollate == SafeNativeMethods.DMCOLLATE_TRUE);
            }

            Interop.Kernel32.GlobalUnlock(hdevmode);
        }

        /// <summary>
        /// Copies the relevant information out of the handle and into the PrinterSettings.
        /// </summary>
        public void SetHdevnames(IntPtr hdevnames)
        {
            if (hdevnames == IntPtr.Zero)
            {
                throw new ArgumentException(SR.Format(SR.InvalidPrinterHandle, hdevnames));
            }

            IntPtr namesPointer = Interop.Kernel32.GlobalLock(hdevnames);

            _driverName = ReadOneDEVNAME(namesPointer, 0);
            _printerName = ReadOneDEVNAME(namesPointer, 1);
            _outputPort = ReadOneDEVNAME(namesPointer, 2);

            PrintDialogDisplayed = true;

            Interop.Kernel32.GlobalUnlock(hdevnames);
        }

        /// <summary>
        /// Provides some interesting information about the PrinterSettings in String form.
        /// </summary>
        public override string ToString()
        {
            string printerName = PrinterName;
            return "[PrinterSettings "
                + printerName
                + " Copies=" + Copies.ToString(CultureInfo.InvariantCulture)
                + " Collate=" + Collate.ToString(CultureInfo.InvariantCulture)
                + " Duplex=" + Duplex.ToString()
                + " FromPage=" + FromPage.ToString(CultureInfo.InvariantCulture)
                + " LandscapeAngle=" + LandscapeAngle.ToString(CultureInfo.InvariantCulture)
                + " MaximumCopies=" + MaximumCopies.ToString(CultureInfo.InvariantCulture)
                + " OutputPort=" + OutputPort.ToString(CultureInfo.InvariantCulture)
                + " ToPage=" + ToPage.ToString(CultureInfo.InvariantCulture)
                + "]";
        }

        // Write null terminated string, return length of string in characters (including null)
        private static short WriteOneDEVNAME(string str, IntPtr bufferStart, int index)
        {
            if (str == null)
                str = "";
            IntPtr address = (IntPtr)(checked((long)bufferStart + index * Marshal.SystemDefaultCharSize));

            char[] data = str.ToCharArray();
            Marshal.Copy(data, 0, address, data.Length);
            Marshal.WriteInt16((IntPtr)(checked((long)address + data.Length * 2)), 0);

            return checked((short)(str.Length + 1));
        }

        /// <summary>
        /// Collection of PaperSize's...
        /// </summary>
        public class PaperSizeCollection : ICollection
        {
            private PaperSize[] _array;

            /// <summary>
            /// Initializes a new instance of the <see cref='System.Drawing.Printing.PrinterSettings.PaperSizeCollection'/> class.
            /// </summary>
            public PaperSizeCollection(PaperSize[] array)
            {
                _array = array;
            }

            /// <summary>
            /// Gets a value indicating the number of paper sizes.
            /// </summary>
            public int Count
            {
                get
                {
                    return _array.Length;
                }
            }

            /// <summary>
            /// Retrieves the PaperSize with the specified index.
            /// </summary>
            public virtual PaperSize this[int index]
            {
                get
                {
                    return _array[index];
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new ArrayEnumerator(_array, Count);
            }

            int ICollection.Count
            {
                get
                {
                    return Count;
                }
            }


            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                Array.Copy(_array, index, array, 0, _array.Length);
            }

            public void CopyTo(PaperSize[] paperSizes, int index)
            {
                Array.Copy(_array, index, paperSizes, 0, _array.Length);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [
                EditorBrowsable(EditorBrowsableState.Never)
            ]
            public int Add(PaperSize paperSize)
            {
                PaperSize[] newArray = new PaperSize[Count + 1];
                ((ICollection)this).CopyTo(newArray, 0);
                newArray[Count] = paperSize;
                _array = newArray;
                return Count;
            }
        }

        public class PaperSourceCollection : ICollection
        {
            private PaperSource[] _array;

            /// <summary>
            /// Initializes a new instance of the <see cref='PaperSourceCollection'/> class.
            /// </summary>
            public PaperSourceCollection(PaperSource[] array)
            {
                _array = array;
            }

            /// <summary>
            /// Gets a value indicating the number of paper sources.
            /// </summary>
            public int Count
            {
                get
                {
                    return _array.Length;
                }
            }

            /// <summary>
            /// Gets the PaperSource with the specified index.
            /// </summary>
            public virtual PaperSource this[int index]
            {
                get
                {
                    return _array[index];
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new ArrayEnumerator(_array, Count);
            }

            int ICollection.Count
            {
                get
                {
                    return Count;
                }
            }


            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                Array.Copy(_array, index, array, 0, _array.Length);
            }

            public void CopyTo(PaperSource[] paperSources, int index)
            {
                Array.Copy(_array, index, paperSources, 0, _array.Length);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public int Add(PaperSource paperSource)
            {
                PaperSource[] newArray = new PaperSource[Count + 1];
                ((ICollection)this).CopyTo(newArray, 0);
                newArray[Count] = paperSource;
                _array = newArray;
                return Count;
            }
        }

        public class PrinterResolutionCollection : ICollection
        {
            private PrinterResolution[] _array;

            /// <summary>
            /// Initializes a new instance of the <see cref='PrinterResolutionCollection'/> class.
            /// </summary>
            public PrinterResolutionCollection(PrinterResolution[] array)
            {
                _array = array;
            }

            /// <summary>
            /// Gets a value indicating the number of available printer resolutions.
            /// </summary>
            public int Count
            {
                get
                {
                    return _array.Length;
                }
            }

            /// <summary>
            /// Retrieves the PrinterResolution with the specified index.
            /// </summary>
            public virtual PrinterResolution this[int index]
            {
                get
                {
                    return _array[index];
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new ArrayEnumerator(_array, Count);
            }

            int ICollection.Count
            {
                get
                {
                    return Count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                Array.Copy(_array, index, array, 0, _array.Length);
            }

            public void CopyTo(PrinterResolution[] printerResolutions, int index)
            {
                Array.Copy(_array, index, printerResolutions, 0, _array.Length);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [EditorBrowsable(EditorBrowsableState.Never)]
            public int Add(PrinterResolution printerResolution)
            {
                PrinterResolution[] newArray = new PrinterResolution[Count + 1];
                ((ICollection)this).CopyTo(newArray, 0);
                newArray[Count] = printerResolution;
                _array = newArray;
                return Count;
            }
        }

        public class StringCollection : ICollection
        {
            private string[] _array;

            /// <summary>
            /// Initializes a new instance of the <see cref='StringCollection'/> class.
            /// </summary>
            public StringCollection(string[] array)
            {
                _array = array;
            }

            /// <summary>
            /// Gets a value indicating the number of strings.
            /// </summary>
            public int Count
            {
                get
                {
                    return _array.Length;
                }
            }

            /// <summary>
            /// Gets the string with the specified index.
            /// </summary>
            public virtual string this[int index]
            {
                get
                {
                    return _array[index];
                }
            }

            public IEnumerator GetEnumerator()
            {
                return new ArrayEnumerator(_array, Count);
            }

            int ICollection.Count
            {
                get
                {
                    return Count;
                }
            }

            bool ICollection.IsSynchronized
            {
                get
                {
                    return false;
                }
            }

            object ICollection.SyncRoot
            {
                get
                {
                    return this;
                }
            }

            void ICollection.CopyTo(Array array, int index)
            {
                Array.Copy(_array, index, array, 0, _array.Length);
            }


            public void CopyTo(string[] strings, int index)
            {
                Array.Copy(_array, index, strings, 0, _array.Length);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            [
                EditorBrowsable(EditorBrowsableState.Never)
            ]
            public int Add(string value)
            {
                string[] newArray = new string[Count + 1];
                ((ICollection)this).CopyTo(newArray, 0);
                newArray[Count] = value;
                _array = newArray;
                return Count;
            }
        }

        private sealed class ArrayEnumerator : IEnumerator
        {
            private readonly object[] _array;
            private readonly int _endIndex;
            private int _index;
            private object? _item;

            public ArrayEnumerator(object[] array, int count)
            {
                _array = array;
                _endIndex = count;
            }

            public object? Current => _item;

            public bool MoveNext()
            {
                if (_index >= _endIndex)
                    return false;
                _item = _array[_index++];
                return true;
            }

            public void Reset()
            {
                // Position enumerator before first item
                _index = 0;
                _item = null;
            }
        }
    }
}
