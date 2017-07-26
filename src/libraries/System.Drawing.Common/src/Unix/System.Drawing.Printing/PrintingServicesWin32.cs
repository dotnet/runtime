// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2005 Novell, Inc. http://www.novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Author:
//
//    Jordi Mas i Hernandez, jordimash@gmail.com
//

using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing.Printing;
using System.ComponentModel;
using System.Text;

namespace System.Drawing.Printing
{
    internal class PrintingServicesWin32 : PrintingServices
    {
        //        private string printer_name;
        private bool is_printer_valid;

        internal PrintingServicesWin32()
        {

        }

        internal override bool IsPrinterValid(string printer)
        {
            if (printer == null | printer == String.Empty)
                return false;

            int ret = Win32DocumentProperties(IntPtr.Zero, IntPtr.Zero, printer, IntPtr.Zero, IntPtr.Zero, 0);
            is_printer_valid = (ret > 0);
            //            this.printer_name = printer; 
            return is_printer_valid;
        }

        internal override void LoadPrinterSettings(string printer, PrinterSettings settings)
        {
            int ret;
            DEVMODE devmode;
            IntPtr hPrn = IntPtr.Zero;
            IntPtr ptr_dev = IntPtr.Zero;

            settings.maximum_copies = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_COPIES, IntPtr.Zero, IntPtr.Zero);

            ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_DUPLEX, IntPtr.Zero, IntPtr.Zero);
            settings.can_duplex = (ret == 1) ? true : false;

            ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_COLORDEVICE, IntPtr.Zero, IntPtr.Zero);
            settings.supports_color = (ret == 1) ? true : false;

            ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_ORIENTATION, IntPtr.Zero, IntPtr.Zero);
            if (ret != -1)
                settings.landscape_angle = ret;

            IntPtr dc = IntPtr.Zero;
            dc = Win32CreateIC(null, printer, null, IntPtr.Zero /* DEVMODE */);
            ret = Win32GetDeviceCaps(dc, (int)DevCapabilities.TECHNOLOGY);
            settings.is_plotter = ret == (int)PrinterType.DT_PLOTTER;
            Win32DeleteDC(dc);

            try
            {
                Win32OpenPrinter(printer, out hPrn, IntPtr.Zero);
                ret = Win32DocumentProperties(IntPtr.Zero, hPrn, null, IntPtr.Zero, IntPtr.Zero, 0);

                if (ret < 0)
                    return;

                ptr_dev = Marshal.AllocHGlobal(ret);
                ret = Win32DocumentProperties(IntPtr.Zero, hPrn, null, ptr_dev, IntPtr.Zero, 2);

                devmode = (DEVMODE)Marshal.PtrToStructure(ptr_dev, typeof(DEVMODE));

                LoadPrinterPaperSizes(printer, settings);
                foreach (PaperSize paper_size in settings.PaperSizes)
                {
                    if ((int)paper_size.Kind == devmode.dmPaperSize)
                    {
                        settings.DefaultPageSettings.PaperSize = paper_size;
                        break;
                    }
                }

                LoadPrinterPaperSources(printer, settings);
                foreach (PaperSource paper_source in settings.PaperSources)
                {
                    if ((int)paper_source.Kind == devmode.dmDefaultSource)
                    {
                        settings.DefaultPageSettings.PaperSource = paper_source;
                        break;
                    }
                }
            }
            finally
            {
                Win32ClosePrinter(hPrn);

                if (ptr_dev != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr_dev);
            }


        }

        internal override void LoadPrinterResolutions(string printer, PrinterSettings settings)
        {
            int ret;
            IntPtr ptr, buff = IntPtr.Zero;

            settings.PrinterResolutions.Clear();
            LoadDefaultResolutions(settings.PrinterResolutions);
            ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_ENUMRESOLUTIONS, IntPtr.Zero, IntPtr.Zero);

            if (ret == -1)
                return;

            ptr = buff = Marshal.AllocHGlobal(ret * 2 * Marshal.SizeOf(buff));
            ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_ENUMRESOLUTIONS, buff, IntPtr.Zero);
            int x, y;
            if (ret != -1)
            {
                for (int i = 0; i < ret; i++)
                {
                    x = Marshal.ReadInt32(ptr);
                    ptr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf(x));
                    y = Marshal.ReadInt32(ptr);
                    ptr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf(y));
                    settings.PrinterResolutions.Add(new PrinterResolution
                        (PrinterResolutionKind.Custom, x, y));
                }
            }
            Marshal.FreeHGlobal(buff);
        }

        void LoadPrinterPaperSizes(string printer, PrinterSettings settings)
        {
            int items, ret;
            IntPtr ptr_names, buff_names = IntPtr.Zero;
            IntPtr ptr_sizes, buff_sizes = IntPtr.Zero;
            IntPtr ptr_sizes_enum, buff_sizes_enum = IntPtr.Zero;
            string name;

            if (settings.PaperSizes == null)
                settings.paper_sizes = new PrinterSettings.PaperSizeCollection(new PaperSize[0]);
            else
                settings.PaperSizes.Clear();

            items = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_PAPERSIZE, IntPtr.Zero, IntPtr.Zero);

            if (items == -1)
                return;

            try
            {
                ptr_sizes = buff_sizes = Marshal.AllocHGlobal(items * 2 * 4);
                ptr_names = buff_names = Marshal.AllocHGlobal(items * 64 * 2);
                ptr_sizes_enum = buff_sizes_enum = Marshal.AllocHGlobal(items * 2);
                ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_PAPERSIZE, buff_sizes, IntPtr.Zero);

                if (ret == -1)
                {
                    // the finally clause will free the unmanaged memory before returning
                    return;
                }

                ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_PAPERS, buff_sizes_enum, IntPtr.Zero);
                ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_PAPERNAMES, buff_names, IntPtr.Zero);

                int x, y;
                PaperSize ps;
                PaperKind kind;
                for (int i = 0; i < ret; i++)
                {
                    x = Marshal.ReadInt32(ptr_sizes, i * 8);
                    y = Marshal.ReadInt32(ptr_sizes, (i * 8) + 4);

                    x = PrinterUnitConvert.Convert(x, PrinterUnit.TenthsOfAMillimeter,
                          PrinterUnit.Display);

                    y = PrinterUnitConvert.Convert(y, PrinterUnit.TenthsOfAMillimeter,
                          PrinterUnit.Display);

                    name = Marshal.PtrToStringUni(ptr_names);
                    ptr_names = new IntPtr(ptr_names.ToInt64() + 64 * 2);

                    kind = (PaperKind)Marshal.ReadInt16(ptr_sizes_enum);
                    ptr_sizes_enum = new IntPtr(ptr_sizes_enum.ToInt64() + 2);

                    ps = new PaperSize(name, x, y);
                    ps.RawKind = (int)kind;
                    settings.PaperSizes.Add(ps);
                }
            }
            finally
            {
                if (buff_names != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff_names);
                if (buff_sizes != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff_sizes);
                if (buff_sizes_enum != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff_sizes_enum);
            }
        }

        internal static bool StartDoc(GraphicsPrinter gr, string doc_name, string output_file)
        {
            DOCINFO di = new DOCINFO();
            int ret;

            di.cbSize = Marshal.SizeOf(di);
            di.lpszDocName = Marshal.StringToHGlobalUni(doc_name);
            di.lpszOutput = IntPtr.Zero;
            di.lpszDatatype = IntPtr.Zero;
            di.fwType = 0;

            ret = Win32StartDoc(gr.Hdc, ref di);
            Marshal.FreeHGlobal(di.lpszDocName);
            return (ret > 0) ? true : false;
        }

        void LoadPrinterPaperSources(string printer, PrinterSettings settings)
        {
            int items, ret;
            IntPtr ptr_names, buff_names = IntPtr.Zero;
            IntPtr ptr_bins, buff_bins = IntPtr.Zero;
            PaperSourceKind kind;
            string name;

            if (settings.PaperSources == null)
                settings.paper_sources = new PrinterSettings.PaperSourceCollection(new PaperSource[0]);
            else
                settings.PaperSources.Clear();

            items = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_BINNAMES, IntPtr.Zero, IntPtr.Zero);

            if (items == -1)
                return;

            try
            {
                ptr_names = buff_names = Marshal.AllocHGlobal(items * 2 * 24);
                ptr_bins = buff_bins = Marshal.AllocHGlobal(items * 2);

                ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_BINNAMES, buff_names, IntPtr.Zero);

                if (ret == -1)
                {
                    // the finally clause will free the unmanaged memory before returning
                    return;
                }

                ret = Win32DeviceCapabilities(printer, null, DCCapabilities.DC_BINS, buff_bins, IntPtr.Zero);

                for (int i = 0; i < ret; i++)
                {
                    name = Marshal.PtrToStringUni(ptr_names);
                    kind = (PaperSourceKind)Marshal.ReadInt16(ptr_bins);
                    settings.PaperSources.Add(new PaperSource(kind, name));

                    ptr_names = new IntPtr(ptr_names.ToInt64() + 24 * 2);
                    ptr_bins = new IntPtr(ptr_bins.ToInt64() + 2);
                }

            }
            finally
            {
                if (buff_names != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff_names);

                if (buff_bins != IntPtr.Zero)
                    Marshal.FreeHGlobal(buff_bins);

            }

        }

        internal static bool StartPage(GraphicsPrinter gr)
        {
            int ret = Win32StartPage(gr.Hdc);
            return (ret > 0) ? true : false;
        }

        internal static bool EndPage(GraphicsPrinter gr)
        {
            int ret = Win32EndPage(gr.Hdc);
            return (ret > 0) ? true : false;
        }

        internal static bool EndDoc(GraphicsPrinter gr)
        {
            int ret = Win32EndDoc(gr.Hdc);
            Win32DeleteDC(gr.Hdc);
            gr.Graphics.Dispose();
            return (ret > 0) ? true : false;
        }

        internal static IntPtr CreateGraphicsContext(PrinterSettings settings, PageSettings default_page_settings)
        {
            IntPtr dc = IntPtr.Zero;
            dc = Win32CreateDC(null, settings.PrinterName, null, IntPtr.Zero /* DEVMODE */);
            return dc;
        }

        // Properties
        internal override string DefaultPrinter
        {
            get
            {
                StringBuilder name = new StringBuilder(1024);
                int length = name.Capacity;

                if (Win32GetDefaultPrinter(name, ref length) > 0)
                    if (IsPrinterValid(name.ToString()))
                        return name.ToString();
                return String.Empty;
            }
        }

        internal static PrinterSettings.StringCollection InstalledPrinters
        {
            get
            {
                PrinterSettings.StringCollection col = new PrinterSettings.StringCollection(new string[] { });
                PRINTER_INFO printer_info;
                uint cbNeeded = 0, printers = 0;
                IntPtr ptr, buff;
                string s;

                // Determine space needed
                Win32EnumPrinters((int)EnumPrinters.PRINTER_ENUM_CONNECTIONS | (int)EnumPrinters.PRINTER_ENUM_LOCAL,
                        null, 2, IntPtr.Zero, 0, ref cbNeeded, ref printers);

                if (cbNeeded <= 0)
                    return col;

                ptr = buff = Marshal.AllocHGlobal((int)cbNeeded);

                try
                {
                    // Give us the printer list
                    Win32EnumPrinters((int)EnumPrinters.PRINTER_ENUM_CONNECTIONS | (int)EnumPrinters.PRINTER_ENUM_LOCAL,
                        null, 2, buff, (uint)cbNeeded, ref cbNeeded, ref printers);

                    for (int i = 0; i < printers; i++)
                    {
                        printer_info = (PRINTER_INFO)Marshal.PtrToStructure(ptr, typeof(PRINTER_INFO));
                        s = Marshal.PtrToStringUni(printer_info.pPrinterName);
                        col.Add(s);
                        ptr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf(printer_info));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buff);
                }
                return col;
            }
        }

        internal override void GetPrintDialogInfo(string printer, ref string port, ref string type, ref string status, ref string comment)
        {
            IntPtr hPrn;
            PRINTER_INFO printer_info = new PRINTER_INFO();
            int needed = 0;
            IntPtr ptr;

            Win32OpenPrinter(printer, out hPrn, IntPtr.Zero);

            if (hPrn == IntPtr.Zero)
                return;

            Win32GetPrinter(hPrn, 2, IntPtr.Zero, 0, ref needed);
            ptr = Marshal.AllocHGlobal(needed);

            Win32GetPrinter(hPrn, 2, ptr, needed, ref needed);
            printer_info = (PRINTER_INFO)Marshal.PtrToStructure(ptr, typeof(PRINTER_INFO));
            Marshal.FreeHGlobal(ptr);

            port = Marshal.PtrToStringUni(printer_info.pPortName);
            comment = Marshal.PtrToStringUni(printer_info.pComment);
            type = Marshal.PtrToStringUni(printer_info.pDriverName);
            status = GetPrinterStatusMsg(printer_info.Status);

            Win32ClosePrinter(hPrn);
        }

        private string GetPrinterStatusMsg(uint status)
        {
            string rslt = string.Empty;

            if (status == 0)
                return "Ready";

            if ((status & (uint)PrinterStatus.PS_PAUSED) != 0)
                rslt += "Paused; ";
            if ((status & (uint)PrinterStatus.PS_ERROR) != 0)
                rslt += "Error; ";
            if ((status & (uint)PrinterStatus.PS_PENDING_DELETION) != 0)
                rslt += "Pending deletion; ";
            if ((status & (uint)PrinterStatus.PS_PAPER_JAM) != 0)
                rslt += "Paper jam; ";
            if ((status & (uint)PrinterStatus.PS_PAPER_OUT) != 0)
                rslt += "Paper out; ";
            if ((status & (uint)PrinterStatus.PS_MANUAL_FEED) != 0)
                rslt += "Manual feed; ";
            if ((status & (uint)PrinterStatus.PS_PAPER_PROBLEM) != 0)
                rslt += "Paper problem; ";
            if ((status & (uint)PrinterStatus.PS_OFFLINE) != 0)
                rslt += "Offline; ";
            if ((status & (uint)PrinterStatus.PS_IO_ACTIVE) != 0)
                rslt += "I/O active; ";
            if ((status & (uint)PrinterStatus.PS_BUSY) != 0)
                rslt += "Busy; ";
            if ((status & (uint)PrinterStatus.PS_PRINTING) != 0)
                rslt += "Printing; ";
            if ((status & (uint)PrinterStatus.PS_OUTPUT_BIN_FULL) != 0)
                rslt += "Output bin full; ";
            if ((status & (uint)PrinterStatus.PS_NOT_AVAILABLE) != 0)
                rslt += "Not available; ";
            if ((status & (uint)PrinterStatus.PS_WAITING) != 0)
                rslt += "Waiting; ";
            if ((status & (uint)PrinterStatus.PS_PROCESSING) != 0)
                rslt += "Processing; ";
            if ((status & (uint)PrinterStatus.PS_INITIALIZING) != 0)
                rslt += "Initializing; ";
            if ((status & (uint)PrinterStatus.PS_WARMING_UP) != 0)
                rslt += "Warming up; ";
            if ((status & (uint)PrinterStatus.PS_TONER_LOW) != 0)
                rslt += "Toner low; ";
            if ((status & (uint)PrinterStatus.PS_NO_TONER) != 0)
                rslt += "No toner; ";
            if ((status & (uint)PrinterStatus.PS_PAGE_PUNT) != 0)
                rslt += "Page punt; ";
            if ((status & (uint)PrinterStatus.PS_USER_INTERVENTION) != 0)
                rslt += "User intervention; ";
            if ((status & (uint)PrinterStatus.PS_OUT_OF_MEMORY) != 0)
                rslt += "Out of memory; ";
            if ((status & (uint)PrinterStatus.PS_DOOR_OPEN) != 0)
                rslt += "Door open; ";
            if ((status & (uint)PrinterStatus.PS_SERVER_UNKNOWN) != 0)
                rslt += "Server unkown; ";
            if ((status & (uint)PrinterStatus.PS_POWER_SAVE) != 0)
                rslt += "Power save; ";

            return rslt;
        }

        //
        // DllImports
        //

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "OpenPrinter", SetLastError = true)]
        static extern int Win32OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "GetPrinter", SetLastError = true)]
        static extern int Win32GetPrinter(IntPtr hPrinter, int level, IntPtr dwBuf, int size, ref int dwNeeded);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "ClosePrinter", SetLastError = true)]
        static extern int Win32ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "DeviceCapabilities", SetLastError = true)]
        static extern int Win32DeviceCapabilities(string device, string port, DCCapabilities cap, IntPtr outputBuffer, IntPtr deviceMode);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, EntryPoint = "EnumPrinters", SetLastError = true)]
        static extern int Win32EnumPrinters(int Flags, string Name, uint Level, IntPtr pPrinterEnum, uint cbBuf,
                ref uint pcbNeeded, ref uint pcReturned);

        [DllImport("winspool.drv", EntryPoint = "GetDefaultPrinter", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int Win32GetDefaultPrinter(StringBuilder buffer, ref int bufferSize);

        [DllImport("winspool.drv", EntryPoint = "DocumentProperties", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int Win32DocumentProperties(IntPtr hwnd, IntPtr hPrinter, string pDeviceName,
            IntPtr pDevModeOutput, IntPtr pDevModeInput, int fMode);

        [DllImport("gdi32.dll", EntryPoint = "CreateDC")]
        static extern IntPtr Win32CreateDC(string lpszDriver, string lpszDevice,
               string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", EntryPoint = "CreateIC")]
        static extern IntPtr Win32CreateIC(string lpszDriver, string lpszDevice,
               string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, EntryPoint = "StartDoc")]
        static extern int Win32StartDoc(IntPtr hdc, [In] ref DOCINFO lpdi);

        [DllImport("gdi32.dll", EntryPoint = "StartPage")]
        static extern int Win32StartPage(IntPtr hDC);

        [DllImport("gdi32.dll", EntryPoint = "EndPage")]
        static extern int Win32EndPage(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "EndDoc")]
        static extern int Win32EndDoc(IntPtr hdc);

        [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
        public static extern IntPtr Win32DeleteDC(IntPtr hDc);

        [DllImport("gdi32.dll", EntryPoint = "GetDeviceCaps")]
        public static extern int Win32GetDeviceCaps(IntPtr hDc, int index);

        //
        // Structs
        //
        [StructLayout(LayoutKind.Sequential)]
        internal struct PRINTER_INFO
        {
            public IntPtr pServerName;
            public IntPtr pPrinterName;
            public IntPtr pShareName;
            public IntPtr pPortName;
            public IntPtr pDriverName;
            public IntPtr pComment;
            public IntPtr pLocation;
            public IntPtr pDevMode;
            public IntPtr pSepFile;
            public IntPtr pPrintProcessor;
            public IntPtr pDatatype;
            public IntPtr pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct DOCINFO
        {
            public int cbSize;
            public IntPtr lpszDocName;
            public IntPtr lpszOutput;
            public IntPtr lpszDatatype;
            public int fwType;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;

            public short dmOrientation;
            public short dmPaperSize;
            public short dmPaperLength;
            public short dmPaperWidth;

            public short dmScale;
            public short dmCopies;
            public short dmDefaultSource;
            public short dmPrintQuality;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public short dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        // Enums
        internal enum DCCapabilities : short
        {
            DC_FIELDS = 1,
            DC_PAPERS = 2,
            DC_PAPERSIZE = 3,
            DC_MINEXTENT = 4,
            DC_MAXEXTENT = 5,
            DC_BINS = 6,
            DC_DUPLEX = 7,
            DC_SIZE = 8,
            DC_EXTRA = 9,
            DC_VERSION = 10,
            DC_DRIVER = 11,
            DC_BINNAMES = 12,
            DC_ENUMRESOLUTIONS = 13,
            DC_FILEDEPENDENCIES = 14,
            DC_TRUETYPE = 15,
            DC_PAPERNAMES = 16,
            DC_ORIENTATION = 17,
            DC_COPIES = 18,
            DC_BINADJUST = 19,
            DC_EMF_COMPLIANT = 20,
            DC_DATATYPE_PRODUCED = 21,
            DC_COLLATE = 22,
            DC_MANUFACTURER = 23,
            DC_MODEL = 24,
            DC_PERSONALITY = 25,
            DC_PRINTRATE = 26,
            DC_PRINTRATEUNIT = 27,
            DC_PRINTERMEM = 28,
            DC_MEDIAREADY = 29,
            DC_STAPLE = 30,
            DC_PRINTRATEPPM = 31,
            DC_COLORDEVICE = 32,
            DC_NUP = 33
        }

        [Flags]
        internal enum PrinterStatus : uint
        {
            PS_PAUSED = 0x00000001,
            PS_ERROR = 0x00000002,
            PS_PENDING_DELETION = 0x00000004,
            PS_PAPER_JAM = 0x00000008,
            PS_PAPER_OUT = 0x00000010,
            PS_MANUAL_FEED = 0x00000020,
            PS_PAPER_PROBLEM = 0x00000040,
            PS_OFFLINE = 0x00000080,
            PS_IO_ACTIVE = 0x00000100,
            PS_BUSY = 0x00000200,
            PS_PRINTING = 0x00000400,
            PS_OUTPUT_BIN_FULL = 0x00000800,
            PS_NOT_AVAILABLE = 0x00001000,
            PS_WAITING = 0x00002000,
            PS_PROCESSING = 0x00004000,
            PS_INITIALIZING = 0x00008000,
            PS_WARMING_UP = 0x00010000,
            PS_TONER_LOW = 0x00020000,
            PS_NO_TONER = 0x00040000,
            PS_PAGE_PUNT = 0x00080000,
            PS_USER_INTERVENTION = 0x00100000,
            PS_OUT_OF_MEMORY = 0x00200000,
            PS_DOOR_OPEN = 0x00400000,
            PS_SERVER_UNKNOWN = 0x00800000,
            PS_POWER_SAVE = 0x01000000
        }

        // for use in GetDeviceCaps
        internal enum DevCapabilities
        {
            TECHNOLOGY = 2,
        }

        internal enum PrinterType
        {
            DT_PLOTTER = 0, // Vector Plotter
            DT_RASDIPLAY = 1, // Raster Display
            DT_RASPRINTER = 2, // Raster printer
            DT_RASCAMERA = 3, // Raster camera
            DT_CHARSTREAM = 4, // Character-stream, PLP
            DT_METAFILE = 5, // Metafile, VDM
            DT_DISPFILE = 6, // Display-file
        }

        [Flags]
        internal enum EnumPrinters : uint
        {
            PRINTER_ENUM_DEFAULT = 0x1,
            PRINTER_ENUM_LOCAL = 0x2,
            PRINTER_ENUM_CONNECTIONS = 0x4,
            PRINTER_ENUM_FAVORITE = 0x4,
            PRINTER_ENUM_NAME = 0x8,
            PRINTER_ENUM_REMOTE = 0x10,
            PRINTER_ENUM_SHARED = 0x20,
            PRINTER_ENUM_NETWORK = 0x40,
        }
    }

    class GlobalPrintingServicesWin32 : GlobalPrintingServices
    {
        internal override PrinterSettings.StringCollection InstalledPrinters
        {
            get
            {
                return PrintingServicesWin32.InstalledPrinters;
            }
        }

        internal override IntPtr CreateGraphicsContext(PrinterSettings settings, PageSettings default_page_settings)
        {
            return PrintingServicesWin32.CreateGraphicsContext(settings, default_page_settings);
        }

        internal override bool StartDoc(GraphicsPrinter gr, string doc_name, string output_file)
        {
            return PrintingServicesWin32.StartDoc(gr, doc_name, output_file);
        }

        internal override bool EndDoc(GraphicsPrinter gr)
        {
            return PrintingServicesWin32.EndDoc(gr);
        }

        internal override bool StartPage(GraphicsPrinter gr)
        {
            return PrintingServicesWin32.StartPage(gr);
        }

        internal override bool EndPage(GraphicsPrinter gr)
        {
            return PrintingServicesWin32.EndPage(gr);
        }
    }
}



