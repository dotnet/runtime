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
//	Jordi Mas i Hernandez, jordimash@gmail.com
//

using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing.Printing;
using System.ComponentModel;
using System.Drawing.Imaging;

namespace System.Drawing.Printing
{
    /// <summary>
    /// This class is designed to cache the values retrieved by the 
    /// native printing services, as opposed to GlobalPrintingServices, which
    /// doesn't cache any values.
    /// </summary>
    internal abstract class PrintingServices
    {
        #region Properties
        internal abstract string DefaultPrinter { get; }
        #endregion

        #region Methods
        internal abstract bool IsPrinterValid(string printer);
        internal abstract void LoadPrinterSettings(string printer, PrinterSettings settings);
        internal abstract void LoadPrinterResolutions(string printer, PrinterSettings settings);

        // Used from SWF
        internal abstract void GetPrintDialogInfo(string printer, ref string port, ref string type, ref string status, ref string comment);

        internal void LoadDefaultResolutions(PrinterSettings.PrinterResolutionCollection col)
        {
            col.Add(new PrinterResolution(PrinterResolutionKind.High, (int)PrinterResolutionKind.High, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Medium, (int)PrinterResolutionKind.Medium, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Low, (int)PrinterResolutionKind.Low, -1));
            col.Add(new PrinterResolution(PrinterResolutionKind.Draft, (int)PrinterResolutionKind.Draft, -1));
        }
        #endregion
    }

    internal abstract class GlobalPrintingServices
    {
        #region Properties
        internal abstract PrinterSettings.StringCollection InstalledPrinters { get; }
        #endregion

        #region Methods
        internal abstract IntPtr CreateGraphicsContext(PrinterSettings settings, PageSettings page_settings);

        internal abstract bool StartDoc(GraphicsPrinter gr, string doc_name, string output_file);
        internal abstract bool StartPage(GraphicsPrinter gr);
        internal abstract bool EndPage(GraphicsPrinter gr);
        internal abstract bool EndDoc(GraphicsPrinter gr);
        #endregion

    }

    internal class SysPrn
    {
        static GlobalPrintingServices global_printing_services;
        static bool is_unix;

        static SysPrn()
        {
            is_unix = GDIPlus.RunningOnUnix();
        }

        internal static PrintingServices CreatePrintingService()
        {
            if (is_unix)
                return new PrintingServicesUnix();
            return new PrintingServicesWin32();
        }

        internal static GlobalPrintingServices GlobalService
        {
            get
            {
                if (global_printing_services == null)
                {
                    if (is_unix)
                        global_printing_services = new GlobalPrintingServicesUnix();
                    else
                        global_printing_services = new GlobalPrintingServicesWin32();
                }

                return global_printing_services;
            }
        }

        internal static void GetPrintDialogInfo(string printer, ref string port, ref string type, ref string status, ref string comment)
        {
            CreatePrintingService().GetPrintDialogInfo(printer, ref port, ref type, ref status, ref comment);
        }

        internal class Printer
        {
            //public readonly string Name;
            public readonly string Comment;
            public readonly string Port;
            public readonly string Type;
            public readonly string Status;
            public PrinterSettings Settings;
            //public bool IsDefault;

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


