// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
    public sealed partial class BitmapData
    {
        private int _width;
        private int _height;
        private int _stride;
        private PixelFormat _pixelFormat;
        private IntPtr _scan0;
        private int _reserved;
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
    }
}
