// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.StandardPrintController.cs
//
// Author:
//   Dennis Hayes (dennish@Raytek.com)
//   Herve Poussineau (hpoussineau@fr.st)
//   Jordi Mas i Hernandez (jordimash@gmail.com)
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

namespace System.Drawing.Printing
{
    public class StandardPrintController : PrintController
    {
        public StandardPrintController()
        {
        }

        public override void OnEndPage(PrintDocument document, PrintPageEventArgs e)
        {
            PrintingServices.EndPage(e.GraphicsContext);
        }

        public override void OnStartPrint(PrintDocument document, PrintEventArgs e)
        {
            IntPtr dc = PrintingServices.CreateGraphicsContext(document.PrinterSettings, document.DefaultPageSettings);
            e.GraphicsContext = new GraphicsPrinter(null, dc);
            PrintingServices.StartDoc(e.GraphicsContext, document.DocumentName, string.Empty);
        }

        public override void OnEndPrint(PrintDocument document, PrintEventArgs e)
        {
            PrintingServices.EndDoc(e.GraphicsContext);
        }

        public override Graphics OnStartPage(PrintDocument document, PrintPageEventArgs e)
        {
            PrintingServices.StartPage(e.GraphicsContext);
            return e.Graphics;
        }
    }
}
