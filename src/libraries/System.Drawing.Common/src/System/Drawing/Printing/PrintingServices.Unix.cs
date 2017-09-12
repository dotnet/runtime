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
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace System.Drawing.Printing
{
    /// <summary>
    /// This class is designed to cache the values retrieved by the 
    /// native printing services, as opposed to GlobalPrintingServices, which
    /// doesn't cache any values.
    /// </summary>
    internal static class PrintingServices
    {
        #region Private Fields

        private static Hashtable doc_info = new Hashtable();
        private static bool cups_installed;

        private static Hashtable installed_printers;
        private static string default_printer = String.Empty;

        #endregion

        #region Constructor

        static PrintingServices()
        {
            installed_printers = new Hashtable();
            CheckCupsInstalled();
        }

        #endregion

        #region Properties

        internal static PrinterSettings.StringCollection InstalledPrinters
        {
            get
            {
                LoadPrinters();
                PrinterSettings.StringCollection list = new PrinterSettings.StringCollection(new string[] { });
                foreach (object key in installed_printers.Keys)
                {
                    list.Add(key.ToString());
                }
                return list;
            }
        }

        internal static string DefaultPrinter
        {
            get
            {
                if (installed_printers.Count == 0)
                    LoadPrinters();
                return default_printer;
            }
        }

        #endregion


        #region Methods

        /// <summary>
        /// Do a cups call to check if it is installed
        /// </summary>
        private static void CheckCupsInstalled()
        {
            try
            {
                LibcupsNative.cupsGetDefault();
            }
            catch (DllNotFoundException)
            {
#if NETCORE
                System.Diagnostics.Debug.WriteLine("libcups not found. To have printing support, you need cups installed");
#else
                Console.WriteLine("libcups not found. To have printing support, you need cups installed");
#endif
                cups_installed = false;
                return;
            }

            cups_installed = true;
        }

        /// <summary>
        /// Open the printer's PPD file
        /// </summary>
        /// <param name="printer">Printer name, returned from cupsGetDests</param>
        private static IntPtr OpenPrinter(string printer)
        {
            try
            {
                IntPtr ptr = LibcupsNative.cupsGetPPD(printer);
                string ppd_filename = Marshal.PtrToStringAnsi(ptr);
                IntPtr ppd_handle = LibcupsNative.ppdOpenFile(ppd_filename);
                return ppd_handle;
            }
            catch (Exception)
            {
#if NETCORE
                System.Diagnostics.Debug.WriteLine("There was an error opening the printer {0}. Please check your cups installation.");
#else
                Console.WriteLine("There was an error opening the printer {0}. Please check your cups installation.");
#endif
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Close the printer file
        /// </summary>
        /// <param name="handle">PPD handle</param>
        private static void ClosePrinter(ref IntPtr handle)
        {
            try
            {
                if (handle != IntPtr.Zero)
                    LibcupsNative.ppdClose(handle);
            }
            finally
            {
                handle = IntPtr.Zero;
            }
        }

        private static int OpenDests(ref IntPtr ptr)
        {
            try
            {
                return LibcupsNative.cupsGetDests(ref ptr);
            }
            catch
            {
                ptr = IntPtr.Zero;
            }
            return 0;
        }

        private static void CloseDests(ref IntPtr ptr, int count)
        {
            try
            {
                if (ptr != IntPtr.Zero)
                    LibcupsNative.cupsFreeDests(count, ptr);
            }
            finally
            {
                ptr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Checks if a printer has a valid PPD file. Caches the result unless force is true
        /// </summary>
        /// <param name="force">Does the check disregarding the last cached value if true</param>
        internal static bool IsPrinterValid(string printer)
        {
            if (!cups_installed || printer == null | printer == String.Empty)
                return false;

            return installed_printers.Contains(printer);
        }

        /// <summary>
        /// Loads the printer settings and initializes the PrinterSettings and PageSettings fields
        /// </summary>
        /// <param name="printer">Printer name</param>
        /// <param name="settings">PrinterSettings object to initialize</param>
        internal static void LoadPrinterSettings(string printer, PrinterSettings settings)
        {
            if (cups_installed == false || (printer == null) || (printer == String.Empty))
                return;

            if (installed_printers.Count == 0)
                LoadPrinters();

            if (((SysPrn.Printer)installed_printers[printer]).Settings != null)
            {
                SysPrn.Printer p = (SysPrn.Printer)installed_printers[printer];
                settings.can_duplex = p.Settings.can_duplex;
                settings.is_plotter = p.Settings.is_plotter;
                settings.landscape_angle = p.Settings.landscape_angle;
                settings.maximum_copies = p.Settings.maximum_copies;
                settings.paper_sizes = p.Settings.paper_sizes;
                settings.paper_sources = p.Settings.paper_sources;
                settings.printer_capabilities = p.Settings.printer_capabilities;
                settings.printer_resolutions = p.Settings.printer_resolutions;
                settings.supports_color = p.Settings.supports_color;
                return;
            }

            settings.PrinterCapabilities.Clear();

            IntPtr dests = IntPtr.Zero, ptr = IntPtr.Zero, ptr_printer, ppd_handle = IntPtr.Zero;
            string name = String.Empty;
            CUPS_DESTS printer_dest;
            PPD_FILE ppd;
            int ret = 0, cups_dests_size;
            NameValueCollection options, paper_names, paper_sources;

            try
            {
                ret = OpenDests(ref dests);
                if (ret == 0)
                    return;

                cups_dests_size = Marshal.SizeOf(typeof(CUPS_DESTS));
                ptr = dests;
                for (int i = 0; i < ret; i++)
                {
                    ptr_printer = (IntPtr)Marshal.ReadIntPtr(ptr);
                    if (Marshal.PtrToStringAnsi(ptr_printer).Equals(printer))
                    {
                        name = printer;
                        break;
                    }
                    ptr = (IntPtr)((long)ptr + cups_dests_size);
                }

                if (!name.Equals(printer))
                {
                    return;
                }

                ppd_handle = OpenPrinter(printer);
                if (ppd_handle == IntPtr.Zero)
                    return;

                printer_dest = (CUPS_DESTS)Marshal.PtrToStructure(ptr, typeof(CUPS_DESTS));
                options = new NameValueCollection();
                paper_names = new NameValueCollection();
                paper_sources = new NameValueCollection();
                string defsize;
                string defsource;
                LoadPrinterOptions(printer_dest.options, printer_dest.num_options, ppd_handle, options,
                    paper_names, out defsize,
                    paper_sources, out defsource);

                if (settings.paper_sizes == null)
                    settings.paper_sizes = new PrinterSettings.PaperSizeCollection(new PaperSize[] { });
                else
                    settings.paper_sizes.Clear();

                if (settings.paper_sources == null)
                    settings.paper_sources = new PrinterSettings.PaperSourceCollection(new PaperSource[] { });
                else
                    settings.paper_sources.Clear();

                settings.DefaultPageSettings.PaperSource = LoadPrinterPaperSources(settings, defsource, paper_sources);
                settings.DefaultPageSettings.PaperSize = LoadPrinterPaperSizes(ppd_handle, settings, defsize, paper_names);
                LoadPrinterResolutionsAndDefault(printer, settings, ppd_handle);

                ppd = (PPD_FILE)Marshal.PtrToStructure(ppd_handle, typeof(PPD_FILE));
                settings.landscape_angle = ppd.landscape;
                settings.supports_color = (ppd.color_device == 0) ? false : true;
                settings.can_duplex = options["Duplex"] != null;

                ClosePrinter(ref ppd_handle);

                ((SysPrn.Printer)installed_printers[printer]).Settings = settings;
            }
            finally
            {
                CloseDests(ref dests, ret);
            }
        }

        /// <summary>
        /// Loads the global options of a printer plus the paper types and trays supported,
        /// and sets the default paper size and source tray.
        /// </summary>
        /// <param name="options">The options field of a printer's CUPS_DESTS structure</param>
        /// <param name="numOptions">The number of options of the printer</param>
        /// <param name="ppd">A ppd handle for the printer, returned by ppdOpen</param>
        /// <param name="list">The list of options</param>
        /// <param name="paper_names">A list of types of paper (PageSize)</param>
        /// <param name="defsize">The default paper size, set by LoadOptionList</param>
        /// <param name="paper_sources">A list of trays(InputSlot) </param>
        /// <param name="defsource">The default source tray, set by LoadOptionList</param>
        private static void LoadPrinterOptions(IntPtr options, int numOptions, IntPtr ppd,
                                         NameValueCollection list,
                                         NameValueCollection paper_names, out string defsize,
                                         NameValueCollection paper_sources, out string defsource)
        {
            CUPS_OPTIONS cups_options;
            string option_name, option_value;
            int cups_size = Marshal.SizeOf(typeof(CUPS_OPTIONS));

            LoadOptionList(ppd, "PageSize", paper_names, out defsize);
            LoadOptionList(ppd, "InputSlot", paper_sources, out defsource);

            for (int j = 0; j < numOptions; j++)
            {
                cups_options = (CUPS_OPTIONS)Marshal.PtrToStructure(options, typeof(CUPS_OPTIONS));
                option_name = Marshal.PtrToStringAnsi(cups_options.name);
                option_value = Marshal.PtrToStringAnsi(cups_options.val);

                if (option_name == "PageSize")
                    defsize = option_value;
                else if (option_name == "InputSlot")
                    defsource = option_value;
#if PrintDebug
                Console.WriteLine("{0} = {1}", option_name, option_value);
#endif

                list.Add(option_name, option_value);

                options = (IntPtr)((long)options + cups_size);
            }
        }

        /// <summary>
        /// Loads the global options of a printer. 
        /// </summary>
        /// <param name="options">The options field of a printer's CUPS_DESTS structure</param>
        /// <param name="numOptions">The number of options of the printer</param>
        private static NameValueCollection LoadPrinterOptions(IntPtr options, int numOptions)
        {
            CUPS_OPTIONS cups_options;
            string option_name, option_value;
            int cups_size = Marshal.SizeOf(typeof(CUPS_OPTIONS));
            NameValueCollection list = new NameValueCollection();
            for (int j = 0; j < numOptions; j++)
            {
                cups_options = (CUPS_OPTIONS)Marshal.PtrToStructure(options, typeof(CUPS_OPTIONS));
                option_name = Marshal.PtrToStringAnsi(cups_options.name);
                option_value = Marshal.PtrToStringAnsi(cups_options.val);

#if PrintDebug
                Console.WriteLine("{0} = {1}", option_name, option_value);
#endif

                list.Add(option_name, option_value);

                options = (IntPtr)((long)options + cups_size);
            }
            return list;
        }

        /// <summary>
        /// Loads a printer's options (selection of paper sizes, paper sources, etc)
        /// and sets the default option from the selected list.
        /// </summary>
        /// <param name="ppd">Printer ppd file handle</param>
        /// <param name="option_name">Name of the option group to load</param>
        /// <param name="list">List of loaded options</param>
        /// <param name="defoption">The default option from the loaded options list</param>
        private static void LoadOptionList(IntPtr ppd, string option_name, NameValueCollection list, out string defoption)
        {

            IntPtr ptr = IntPtr.Zero;
            PPD_OPTION ppd_option;
            PPD_CHOICE choice;
            int choice_size = Marshal.SizeOf(typeof(PPD_CHOICE));
            defoption = null;

            ptr = LibcupsNative.ppdFindOption(ppd, option_name);
            if (ptr != IntPtr.Zero)
            {
                ppd_option = (PPD_OPTION)Marshal.PtrToStructure(ptr, typeof(PPD_OPTION));
#if PrintDebug
                Console.WriteLine (" OPTION  key:{0} def:{1} text: {2}", ppd_option.keyword, ppd_option.defchoice, ppd_option.text);
#endif
                defoption = ppd_option.defchoice;
                ptr = ppd_option.choices;
                for (int c = 0; c < ppd_option.num_choices; c++)
                {
                    choice = (PPD_CHOICE)Marshal.PtrToStructure(ptr, typeof(PPD_CHOICE));
                    list.Add(choice.choice, choice.text);
#if PrintDebug
                    Console.WriteLine ("       choice:{0} - text: {1}", choice.choice, choice.text);
#endif

                    ptr = (IntPtr)((long)ptr + choice_size);
                }
            }
        }

        /// <summary>
        /// Loads a printer's available resolutions
        /// </summary>
        /// <param name="printer">Printer name</param>
        /// <param name="settings">PrinterSettings object to fill</param>
        internal static void LoadPrinterResolutions(string printer, PrinterSettings settings)
        {
            IntPtr ppd_handle = OpenPrinter(printer);
            if (ppd_handle == IntPtr.Zero)
                return;

            LoadPrinterResolutionsAndDefault(printer, settings, ppd_handle);

            ClosePrinter(ref ppd_handle);
        }

        /// <summary>
        /// Create a PrinterResolution from a string Resolution that is set in the PPD option.
        /// An example of Resolution is "600x600dpi" or "600dpi". Returns null if malformed or "Unknown".
        /// </summary>
        private static PrinterResolution ParseResolution(string resolution)
        {
            if (String.IsNullOrEmpty(resolution))
                return null;

            int dpiIndex = resolution.IndexOf("dpi");
            if (dpiIndex == -1)
            {
                // Resolution is "Unknown" or unparsable
                return null;
            }
            resolution = resolution.Substring(0, dpiIndex);

            int x_resolution, y_resolution;
            try
            {
                if (resolution.Contains("x"))
                {
                    string[] resolutions = resolution.Split(new[] { 'x' });
                    x_resolution = Convert.ToInt32(resolutions[0]);
                    y_resolution = Convert.ToInt32(resolutions[1]);
                }
                else
                {
                    x_resolution = Convert.ToInt32(resolution);
                    y_resolution = x_resolution;
                }
            }
            catch (Exception)
            {
                return null;
            }

            return new PrinterResolution(PrinterResolutionKind.Custom, x_resolution, y_resolution);
        }

        /// <summary>
        /// Loads a printer's paper sizes. Returns the default PaperSize, and fills a list of paper_names for use in dialogues
        /// </summary>
        /// <param name="ppd_handle">PPD printer file handle</param>
        /// <param name="settings">PrinterSettings object to fill</param>
        /// <param name="def_size">Default paper size, from the global options of the printer</param>
        /// <param name="paper_names">List of available paper sizes that gets filled</param>
        private static PaperSize LoadPrinterPaperSizes(IntPtr ppd_handle, PrinterSettings settings,
                                                string def_size, NameValueCollection paper_names)
        {
            IntPtr ptr;
            string real_name;
            PPD_FILE ppd;
            PPD_SIZE size;
            PaperSize ps;

            PaperSize defsize = new PaperSize(GetPaperKind(827, 1169), "A4", 827, 1169);
            ppd = (PPD_FILE)Marshal.PtrToStructure(ppd_handle, typeof(PPD_FILE));
            ptr = ppd.sizes;
            float w, h;
            for (int i = 0; i < ppd.num_sizes; i++)
            {
                size = (PPD_SIZE)Marshal.PtrToStructure(ptr, typeof(PPD_SIZE));
                real_name = paper_names[size.name];
                w = size.width * 100 / 72;
                h = size.length * 100 / 72;
                PaperKind kind = GetPaperKind((int)w, (int)h);
                ps = new PaperSize(kind, real_name, (int)w, (int)h);
                ps.RawKind = (int)kind;
                if (def_size == ps.Kind.ToString())
                    defsize = ps;
                settings.paper_sizes.Add(ps);
                ptr = (IntPtr)((long)ptr + Marshal.SizeOf(size));
            }

            return defsize;

        }

        /// <summary>
        /// Loads a printer's paper sources (trays). Returns the default PaperSource, and fills a list of paper_sources for use in dialogues
        /// </summary>
        /// <param name="settings">PrinterSettings object to fill</param>
        /// <param name="def_source">Default paper source, from the global options of the printer</param>
        /// <param name="paper_sources">List of available paper sizes that gets filled</param>
        private static PaperSource LoadPrinterPaperSources(PrinterSettings settings, string def_source,
                                                    NameValueCollection paper_sources)
        {
            PaperSourceKind kind;
            PaperSource defsource = null;
            foreach (string source in paper_sources)
            {
                switch (source)
                {
                    case "Auto":
                        kind = PaperSourceKind.AutomaticFeed;
                        break;
                    case "Standard":
                        kind = PaperSourceKind.AutomaticFeed;
                        break;
                    case "Tray":
                        kind = PaperSourceKind.AutomaticFeed;
                        break;
                    case "Envelope":
                        kind = PaperSourceKind.Envelope;
                        break;
                    case "Manual":
                        kind = PaperSourceKind.Manual;
                        break;
                    default:
                        kind = PaperSourceKind.Custom;
                        break;
                }
                settings.paper_sources.Add(new PaperSource(kind, paper_sources[source]));
                if (def_source == source)
                    defsource = settings.paper_sources[settings.paper_sources.Count - 1];
            }

            if (defsource == null && settings.paper_sources.Count > 0)
                return settings.paper_sources[0];
            return defsource;
        }

        /// <summary>
        /// Sets the available resolutions and default resolution from a
        /// printer's PPD file into settings.
        /// </summary>
        private static void LoadPrinterResolutionsAndDefault(string printer,
            PrinterSettings settings, IntPtr ppd_handle)
        {
            if (settings.printer_resolutions == null)
                settings.printer_resolutions = new PrinterSettings.PrinterResolutionCollection(new PrinterResolution[] { });
            else
                settings.printer_resolutions.Clear();

            var printer_resolutions = new NameValueCollection();
            string defresolution;
            LoadOptionList(ppd_handle, "Resolution", printer_resolutions, out defresolution);
            foreach (var resolution in printer_resolutions.Keys)
            {
                var new_resolution = ParseResolution(resolution.ToString());
                settings.PrinterResolutions.Add(new_resolution);
            }

            var default_resolution = ParseResolution(defresolution);

            if (default_resolution == null)
                default_resolution = ParseResolution("300dpi");
            if (printer_resolutions.Count == 0)
                settings.PrinterResolutions.Add(default_resolution);

            settings.DefaultPageSettings.PrinterResolution = default_resolution;
        }

        /// <summary>
        /// </summary>
        /// <param name="load"></param>
        /// <param name="def_printer"></param>
        private static void LoadPrinters()
        {
            installed_printers.Clear();
            if (cups_installed == false)
                return;

            IntPtr dests = IntPtr.Zero, ptr_printers;
            CUPS_DESTS printer;
            int n_printers = 0;
            int cups_dests_size = Marshal.SizeOf(typeof(CUPS_DESTS));
            string name, first, type, status, comment;
            first = type = status = comment = String.Empty;
            int state = 0;

            try
            {
                n_printers = OpenDests(ref dests);

                ptr_printers = dests;
                for (int i = 0; i < n_printers; i++)
                {
                    printer = (CUPS_DESTS)Marshal.PtrToStructure(ptr_printers, typeof(CUPS_DESTS));
                    name = Marshal.PtrToStringAnsi(printer.name);

                    if (printer.is_default == 1)
                        default_printer = name;

                    if (first.Equals(String.Empty))
                        first = name;

                    NameValueCollection options = LoadPrinterOptions(printer.options, printer.num_options);

                    if (options["printer-state"] != null)
                        state = Int32.Parse(options["printer-state"]);

                    if (options["printer-comment"] != null)
                        comment = options["printer-state"];

                    switch (state)
                    {
                        case 4:
                            status = "Printing";
                            break;
                        case 5:
                            status = "Stopped";
                            break;
                        default:
                            status = "Ready";
                            break;
                    }

                    installed_printers.Add(name, new SysPrn.Printer(String.Empty, type, status, comment));

                    ptr_printers = (IntPtr)((long)ptr_printers + cups_dests_size);
                }

            }
            finally
            {
                CloseDests(ref dests, n_printers);
            }

            if (default_printer.Equals(String.Empty))
                default_printer = first;
        }

        /// <summary>
        /// Gets a printer's settings for use in the print dialogue
        /// </summary>
        /// <param name="printer"></param>
        /// <param name="port"></param>
        /// <param name="type"></param>
        /// <param name="status"></param>
        /// <param name="comment"></param>
        internal static void GetPrintDialogInfo(string printer, ref string port, ref string type, ref string status, ref string comment)
        {
            int count = 0, state = -1;
            bool found = false;
            CUPS_DESTS cups_dests;
            IntPtr dests = IntPtr.Zero, ptr_printers, ptr_printer;
            int cups_dests_size = Marshal.SizeOf(typeof(CUPS_DESTS));

            if (cups_installed == false)
                return;

            try
            {
                count = OpenDests(ref dests);

                if (count == 0)
                    return;

                ptr_printers = dests;

                for (int i = 0; i < count; i++)
                {
                    ptr_printer = (IntPtr)Marshal.ReadIntPtr(ptr_printers);
                    if (Marshal.PtrToStringAnsi(ptr_printer).Equals(printer))
                    {
                        found = true;
                        break;
                    }
                    ptr_printers = (IntPtr)((long)ptr_printers + cups_dests_size);
                }

                if (!found)
                    return;

                cups_dests = (CUPS_DESTS)Marshal.PtrToStructure(ptr_printers, typeof(CUPS_DESTS));

                NameValueCollection options = LoadPrinterOptions(cups_dests.options, cups_dests.num_options);

                if (options["printer-state"] != null)
                    state = Int32.Parse(options["printer-state"]);

                if (options["printer-comment"] != null)
                    comment = options["printer-state"];

                switch (state)
                {
                    case 4:
                        status = "Printing";
                        break;
                    case 5:
                        status = "Stopped";
                        break;
                    default:
                        status = "Ready";
                        break;
                }
            }
            finally
            {
                CloseDests(ref dests, count);
            }
        }

        /// <summary>
        /// Returns the appropriate PaperKind for the width and height
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static PaperKind GetPaperKind(int width, int height)
        {
            if (width == 827 && height == 1169)
                return PaperKind.A4;
            if (width == 583 && height == 827)
                return PaperKind.A5;
            if (width == 717 && height == 1012)
                return PaperKind.B5;
            if (width == 693 && height == 984)
                return PaperKind.B5Envelope;
            if (width == 638 && height == 902)
                return PaperKind.C5Envelope;
            if (width == 449 && height == 638)
                return PaperKind.C6Envelope;
            if (width == 1700 && height == 2200)
                return PaperKind.CSheet;
            if (width == 433 && height == 866)
                return PaperKind.DLEnvelope;
            if (width == 2200 && height == 3400)
                return PaperKind.DSheet;
            if (width == 3400 && height == 4400)
                return PaperKind.ESheet;
            if (width == 725 && height == 1050)
                return PaperKind.Executive;
            if (width == 850 && height == 1300)
                return PaperKind.Folio;
            if (width == 850 && height == 1200)
                return PaperKind.GermanStandardFanfold;
            if (width == 1700 && height == 1100)
                return PaperKind.Ledger;
            if (width == 850 && height == 1400)
                return PaperKind.Legal;
            if (width == 927 && height == 1500)
                return PaperKind.LegalExtra;
            if (width == 850 && height == 1100)
                return PaperKind.Letter;
            if (width == 927 && height == 1200)
                return PaperKind.LetterExtra;
            if (width == 850 && height == 1269)
                return PaperKind.LetterPlus;
            if (width == 387 && height == 750)
                return PaperKind.MonarchEnvelope;
            if (width == 387 && height == 887)
                return PaperKind.Number9Envelope;
            if (width == 413 && height == 950)
                return PaperKind.Number10Envelope;
            if (width == 450 && height == 1037)
                return PaperKind.Number11Envelope;
            if (width == 475 && height == 1100)
                return PaperKind.Number12Envelope;
            if (width == 500 && height == 1150)
                return PaperKind.Number14Envelope;
            if (width == 363 && height == 650)
                return PaperKind.PersonalEnvelope;
            if (width == 1000 && height == 1100)
                return PaperKind.Standard10x11;
            if (width == 1000 && height == 1400)
                return PaperKind.Standard10x14;
            if (width == 1100 && height == 1700)
                return PaperKind.Standard11x17;
            if (width == 1200 && height == 1100)
                return PaperKind.Standard12x11;
            if (width == 1500 && height == 1100)
                return PaperKind.Standard15x11;
            if (width == 900 && height == 1100)
                return PaperKind.Standard9x11;
            if (width == 550 && height == 850)
                return PaperKind.Statement;
            if (width == 1100 && height == 1700)
                return PaperKind.Tabloid;
            if (width == 1487 && height == 1100)
                return PaperKind.USStandardFanfold;

            return PaperKind.Custom;
        }

        #endregion

        #region Print job methods

        static string tmpfile;

        /// <summary>
        /// Gets a pointer to an options list parsed from the printer's current settings, to use when setting up the printing job
        /// </summary>
        /// <param name="printer_settings"></param>
        /// <param name="page_settings"></param>
        /// <param name="options"></param>
        internal static int GetCupsOptions(PrinterSettings printer_settings, PageSettings page_settings, out IntPtr options)
        {
            options = IntPtr.Zero;

            PaperSize size = page_settings.PaperSize;
            int width = size.Width * 72 / 100;
            int height = size.Height * 72 / 100;

            StringBuilder sb = new StringBuilder();
            sb.Append(
                "copies=" + printer_settings.Copies + " " +
                "Collate=" + printer_settings.Collate + " " +
                "ColorModel=" + (page_settings.Color ? "Color" : "Black") + " " +
                "PageSize=" + String.Format("Custom.{0}x{1}", width, height) + " " +
                "landscape=" + page_settings.Landscape
            );

            if (printer_settings.CanDuplex)
            {
                if (printer_settings.Duplex == Duplex.Simplex)
                    sb.Append(" Duplex=None");
                else
                    sb.Append(" Duplex=DuplexNoTumble");
            }

            return LibcupsNative.cupsParseOptions(sb.ToString(), 0, ref options);
        }

        internal static bool StartDoc(GraphicsPrinter gr, string doc_name, string output_file)
        {
            DOCINFO doc = (DOCINFO)doc_info[gr.Hdc];
            doc.title = doc_name;
            return true;
        }

        internal static bool EndDoc(GraphicsPrinter gr)
        {
            DOCINFO doc = (DOCINFO)doc_info[gr.Hdc];

            gr.Graphics.Dispose(); // Dispose object to force surface finish

            IntPtr options;
            int options_count = GetCupsOptions(doc.settings, doc.default_page_settings, out options);

            LibcupsNative.cupsPrintFile(doc.settings.PrinterName, doc.filename, doc.title, options_count, options);
            LibcupsNative.cupsFreeOptions(options_count, options);
            doc_info.Remove(gr.Hdc);
            if (tmpfile != null)
            {
                try
                { File.Delete(tmpfile); }
                catch { }
            }
            return true;
        }

        internal static bool StartPage(GraphicsPrinter gr)
        {
            return true;
        }

        internal static bool EndPage(GraphicsPrinter gr)
        {
            SafeNativeMethods.Gdip.GdipGetPostScriptSavePage(gr.Hdc);
            return true;
        }

        // Unfortunately, PrinterSettings and PageSettings couldn't be referencing each other,
        // thus we need to pass them separately
        internal static IntPtr CreateGraphicsContext(PrinterSettings settings, PageSettings default_page_settings)
        {
            IntPtr graphics = IntPtr.Zero;
            string name;
            if (!settings.PrintToFile)
            {
                StringBuilder sb = new StringBuilder(1024);
                int length = sb.Capacity;
                LibcupsNative.cupsTempFd(sb, length);
                name = sb.ToString();
                tmpfile = name;
            }
            else
                name = settings.PrintFileName;

            PaperSize psize = default_page_settings.PaperSize;
            int width, height;
            if (default_page_settings.Landscape)
            { // Swap in case of landscape
                width = psize.Height;
                height = psize.Width;
            }
            else
            {
                width = psize.Width;
                height = psize.Height;
            }

            SafeNativeMethods.Gdip.GdipGetPostScriptGraphicsContext(name,
                width * 72 / 100,
                height * 72 / 100,
                default_page_settings.PrinterResolution.X,
                default_page_settings.PrinterResolution.Y, ref graphics);

            DOCINFO doc = new DOCINFO();
            doc.filename = name;
            doc.settings = settings;
            doc.default_page_settings = default_page_settings;
            doc_info.Add(graphics, doc);

            return graphics;
        }

        #endregion

#pragma warning disable 649
        #region Struct
        public struct DOCINFO
        {
            public PrinterSettings settings;
            public PageSettings default_page_settings;
            public string title;
            public string filename;
        }

        public struct PPD_SIZE
        {
            public int marked;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 42)]
            public string name;
            public float width;
            public float length;
            public float left;
            public float bottom;
            public float right;
            public float top;
        }

        public struct PPD_GROUP
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string text;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 42)]
            public string name;
            public int num_options;
            public IntPtr options;
            public int num_subgroups;
            public IntPtr subgrups;
        }

        public struct PPD_OPTION
        {
            public byte conflicted;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string keyword;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string defchoice;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 81)]
            public string text;
            public int ui;
            public int section;
            public float order;
            public int num_choices;
            public IntPtr choices;
        }

        public struct PPD_CHOICE
        {
            public byte marked;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 41)]
            public string choice;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 81)]
            public string text;
            public IntPtr code;
            public IntPtr option;
        }

        public struct PPD_FILE
        {
            public int language_level;
            public int color_device;
            public int variable_sizes;
            public int accurate_screens;
            public int contone_only;
            public int landscape;
            public int model_number;
            public int manual_copies;
            public int throughput;
            public int colorspace;
            public IntPtr patches;
            public int num_emulations;
            public IntPtr emulations;
            public IntPtr jcl_begin;
            public IntPtr jcl_ps;
            public IntPtr jcl_end;
            public IntPtr lang_encoding;
            public IntPtr lang_version;
            public IntPtr modelname;
            public IntPtr ttrasterizer;
            public IntPtr manufacturer;
            public IntPtr product;
            public IntPtr nickname;
            public IntPtr shortnickname;
            public int num_groups;
            public IntPtr groups;
            public int num_sizes;
            public IntPtr sizes;

            /* There is more data after this that we are not using*/
        }


        public struct CUPS_OPTIONS
        {
            public IntPtr name;
            public IntPtr val;
        }

        public struct CUPS_DESTS
        {
            public IntPtr name;
            public IntPtr instance;
            public int is_default;
            public int num_options;
            public IntPtr options;
        }

        #endregion
#pragma warning restore 649
        internal static void LoadDefaultResolutions(PrinterSettings.PrinterResolutionCollection col)
        {
            col.Add(new PrinterResolution(PrinterResolutionKind.High, (int)PrinterResolutionKind.High, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Medium, (int)PrinterResolutionKind.Medium, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Low, (int)PrinterResolutionKind.Low, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Draft, (int)PrinterResolutionKind.Draft, -1));
        }
    }

    internal class SysPrn
    {
        internal static void GetPrintDialogInfo(string printer, ref string port, ref string type, ref string status, ref string comment)
        {
            PrintingServices.GetPrintDialogInfo(printer, ref port, ref type, ref status, ref comment);
        }

        internal class Printer
        {
            public readonly string Comment;
            public readonly string Port;
            public readonly string Type;
            public readonly string Status;
            public PrinterSettings Settings;

            public Printer(string port, string type, string status, string comment)
            {
                Port = port;
                Type = type;
                Status = status;
                Comment = comment;
            }
        }
    }

    internal class GraphicsPrinter
    {
        private Graphics graphics;
        private IntPtr hDC;

        internal GraphicsPrinter(Graphics gr, IntPtr dc)
        {
            graphics = gr;
            hDC = dc;
        }

        internal Graphics Graphics
        {
            get { return graphics; }
            set { graphics = value; }
        }
        internal IntPtr Hdc { get { return hDC; } }
    }
}
