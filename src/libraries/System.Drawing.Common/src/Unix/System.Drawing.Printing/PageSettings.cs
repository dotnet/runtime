// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.PageSettings.cs
//
// Authors:
//   Dennis Hayes (dennish@Raytek.com)
//   Herve Poussineau (hpoussineau@fr.st)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) 2002 Ximian, Inc
//

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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

using System;
using System.Runtime.InteropServices;

namespace System.Drawing.Printing
{
    [Serializable]
    public class PageSettings : ICloneable
    {
        internal bool color;
        internal bool landscape;
        internal PaperSize paperSize;
        internal PaperSource paperSource;
        internal PrinterResolution printerResolution;

        // create a new default Margins object (is 1 inch for all margins)
        Margins margins = new Margins();
#pragma warning disable 649
        float hardMarginX;
        float hardMarginY;
        RectangleF printableArea;
        PrinterSettings printerSettings;
#pragma warning restore 649

        public PageSettings() : this(new PrinterSettings())
        {
        }

        public PageSettings(PrinterSettings printerSettings)
        {
            PrinterSettings = printerSettings;

            this.color = printerSettings.DefaultPageSettings.color;
            this.landscape = printerSettings.DefaultPageSettings.landscape;
            this.paperSize = printerSettings.DefaultPageSettings.paperSize;
            this.paperSource = printerSettings.DefaultPageSettings.paperSource;
            this.printerResolution = printerSettings.DefaultPageSettings.printerResolution;
        }

        // used by PrinterSettings.DefaultPageSettings
        internal PageSettings(PrinterSettings printerSettings, bool color, bool landscape, PaperSize paperSize, PaperSource paperSource, PrinterResolution printerResolution)
        {
            PrinterSettings = printerSettings;
            this.color = color;
            this.landscape = landscape;
            this.paperSize = paperSize;
            this.paperSource = paperSource;
            this.printerResolution = printerResolution;
        }

        //props
        public Rectangle Bounds
        {
            get
            {
                int width = this.paperSize.Width;
                int height = this.paperSize.Height;

                width -= this.margins.Left + this.margins.Right;
                height -= this.margins.Top + this.margins.Bottom;

                if (this.landscape)
                {
                    // swap width and height
                    int tmp = width;
                    width = height;
                    height = tmp;
                }
                return new Rectangle(this.margins.Left, this.margins.Top, width, height);
            }
        }

        public bool Color
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return color;
            }
            set
            {
                color = value;
            }
        }

        public bool Landscape
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return landscape;
            }
            set
            {
                landscape = value;
            }
        }

        public Margins Margins
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return margins;
            }
            set
            {
                margins = value;
            }
        }

        public PaperSize PaperSize
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return paperSize;
            }
            set
            {
                if (value != null)
                    paperSize = value;
            }
        }

        public PaperSource PaperSource
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return paperSource;
            }
            set
            {
                if (value != null)
                    paperSource = value;
            }
        }

        public PrinterResolution PrinterResolution
        {
            get
            {
                if (!this.printerSettings.IsValid)
                    throw new InvalidPrinterException(this.printerSettings);
                return printerResolution;
            }
            set
            {
                if (value != null)
                    printerResolution = value;
            }
        }

        public PrinterSettings PrinterSettings
        {
            get
            {
                return printerSettings;
            }
            set
            {
                printerSettings = value;
            }
        }
        public float HardMarginX
        {
            get
            {
                return hardMarginX;
            }
        }

        public float HardMarginY
        {
            get
            {
                return hardMarginY;
            }
        }

        public RectangleF PrintableArea
        {
            get
            {
                return printableArea;
            }
        }


        public object Clone()
        {
            // We do a deep copy
            PrinterResolution pres = new PrinterResolution(this.printerResolution.Kind, this.printerResolution.X, this.printerResolution.Y);
            PaperSource psource = new PaperSource(this.paperSource.Kind, this.paperSource.SourceName);
            PaperSize psize = new PaperSize(this.paperSize.PaperName, this.paperSize.Width, this.paperSize.Height);
            psize.RawKind = (int)this.paperSize.Kind;

            PageSettings ps = new PageSettings(this.printerSettings, this.color, this.landscape,
                    psize, psource, pres);
            ps.Margins = (Margins)this.margins.Clone();
            return ps;
        }


        [MonoTODO("PageSettings.CopyToHdevmode")]
        public void CopyToHdevmode(IntPtr hdevmode)
        {
            throw new NotImplementedException();
        }


        [MonoTODO("PageSettings.SetHdevmode")]
        public void SetHdevmode(IntPtr hdevmode)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            string ret = "[PageSettings: Color={0}";
            ret += ", Landscape={1}";
            ret += ", Margins={2}";
            ret += ", PaperSize={3}";
            ret += ", PaperSource={4}";
            ret += ", PrinterResolution={5}";
            ret += "]";

            return String.Format(ret, this.color, this.landscape, this.margins, this.paperSize, this.paperSource, this.printerResolution);
        }
    }
}
