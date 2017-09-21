// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Imaging.BitmapData.cs
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//   Vladimir Vukicevic (vladimir@pobox.com)
//
// (C) 2002 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004, 2006 Novell, Inc (http://www.novell.com)
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

using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{
    // MUST BE KEPT IN SYNC WITH gdip.h in libgdiplus!
    // The first 6 fields MUST also match MS definition
    [StructLayout(LayoutKind.Sequential)]
    public sealed class BitmapData
    {
        private int width;
        private int height;
        private int stride;
        private PixelFormat pixel_format; // int
        private IntPtr scan0;
        private int reserved;
#pragma warning disable 169
        // *** Warning ***    don't depend on those fields in managed
        //            code as they won't exists when using MS
        //            GDI+
        private IntPtr palette;
        private int property_count;
        private IntPtr property;
        private float dpi_horz;
        private float dpi_vert;
        private int image_flags;
        private int left;
        private int top;
        private int x;
        private int y;
        private int transparent;
        // *** Warning ***
#pragma warning restore 169

        public int Height
        {
            get
            {
                return height;
            }

            set
            {
                height = value;
            }
        }

        public int Width
        {
            get
            {
                return width;
            }

            set
            {
                width = value;
            }
        }

        public PixelFormat PixelFormat
        {
            get
            {

                return pixel_format;
            }

            set
            {
                pixel_format = value;
            }
        }

        public int Reserved
        {
            get
            {
                return reserved;
            }

            set
            {
                reserved = value;
            }
        }

        public IntPtr Scan0
        {
            get
            {
                return scan0;
            }

            set
            {
                scan0 = value;
            }
        }

        public int Stride
        {
            get
            {
                return stride;
            }

            set
            {
                stride = value;
            }
        }
    }
}
