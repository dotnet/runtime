// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.PrintPageEventArgs.cs
//
// Author:
//   Dennis Hayes (dennish@Raytek.com)
//   Herve Poussineau (hpoussineau@fr.st)
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
using System.Drawing;
namespace System.Drawing.Printing
{
    /// <summary>
    /// Summary description for PrintPageEventArgs.
    /// </summary>
    public class PrintPageEventArgs : EventArgs
    {
        bool cancel;
        Graphics graphics;
        bool hasmorePages;
        Rectangle marginBounds;
        Rectangle pageBounds;
        PageSettings pageSettings;
        GraphicsPrinter graphics_context;

        public PrintPageEventArgs(Graphics graphics, Rectangle marginBounds,
            Rectangle pageBounds, PageSettings pageSettings)
        {
            this.graphics = graphics;
            this.marginBounds = marginBounds;
            this.pageBounds = pageBounds;
            this.pageSettings = pageSettings;
        }
        public bool Cancel
        {
            get
            {
                return cancel;
            }
            set
            {
                cancel = value;
            }
        }
        public Graphics Graphics
        {
            get
            {
                return graphics;
            }
        }
        public bool HasMorePages
        {
            get
            {
                return hasmorePages;
            }
            set
            {
                hasmorePages = value;
            }
        }
        public Rectangle MarginBounds
        {
            get
            {
                return marginBounds;
            }
        }
        public Rectangle PageBounds
        {
            get
            {
                return pageBounds;
            }
        }
        public PageSettings PageSettings
        {
            get
            {
                return pageSettings;
            }
        }

        // used in PrintDocument.Print()
        internal void SetGraphics(Graphics g)
        {
            graphics = g;
        }

        internal GraphicsPrinter GraphicsContext
        {
            get { return graphics_context; }
            set { graphics_context = value; }
        }
    }
}
