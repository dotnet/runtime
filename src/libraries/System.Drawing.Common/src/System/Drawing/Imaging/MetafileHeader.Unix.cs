// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Imaging.MetafileHeader.cs
//
// Author: Everaldo Canuto
// eMail: everaldo.canuto@bol.com.br
// Dennis Hayes (dennish@raytek.com)
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

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace System.Drawing.Imaging
{

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    struct EnhMetafileHeader
    {
        public int type;
        public int size;
        public Rectangle bounds;
        public Rectangle frame;
        public int signature;
        public int version;
        public int bytes;
        public int records;
        public short handles;
        public short reserved;
        public int description;
        public int off_description;
        public int palette_entires;
        public Size device;
        public Size millimeters;
    }

    // hack: keep public type as Sequential while making it possible to get the required union
    [StructLayout(LayoutKind.Explicit)]
    struct MonoMetafileHeader
    {
        [FieldOffset(0)]
        public MetafileType type;
        [FieldOffset(4)]
        public int size;
        [FieldOffset(8)]
        public int version;
        [FieldOffset(12)]
        public int emf_plus_flags;
        [FieldOffset(16)]
        public float dpi_x;
        [FieldOffset(20)]
        public float dpi_y;
        [FieldOffset(24)]
        public int x;
        [FieldOffset(28)]
        public int y;
        [FieldOffset(32)]
        public int width;
        [FieldOffset(36)]
        public int height;
        [FieldOffset(40)]
        public WmfMetaHeader wmf_header;
        [FieldOffset(40)]
        public EnhMetafileHeader emf_header;
        [FieldOffset(128)]
        public int emfplus_header_size;
        [FieldOffset(132)]
        public int logical_dpi_x;
        [FieldOffset(136)]
        public int logical_dpi_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public sealed class MetafileHeader
    {

        private MonoMetafileHeader header;

        //constructor

        internal MetafileHeader(IntPtr henhmetafile)
        {
            Marshal.PtrToStructure(henhmetafile, this);
        }

        // methods

        public bool IsDisplay()
        {
            return false;
        }

        public bool IsEmf()
        {
            return (Type == MetafileType.Emf);
        }

        public bool IsEmfOrEmfPlus()
        {
            return (Type >= MetafileType.Emf);
        }

        public bool IsEmfPlus()
        {
            return (Type >= MetafileType.EmfPlusOnly);
        }

        public bool IsEmfPlusDual()
        {
            return (Type == MetafileType.EmfPlusDual);
        }

        public bool IsEmfPlusOnly()
        {
            return (Type == MetafileType.EmfPlusOnly);
        }

        public bool IsWmf()
        {
            return (Type <= MetafileType.WmfPlaceable);
        }

        public bool IsWmfPlaceable()
        {
            return (Type == MetafileType.WmfPlaceable);
        }

        // properties

        public Rectangle Bounds
        {
            get
            {
                if (this.MetafileSize == 0)
                {
                    // GDI+ compatibility; 
                    return new Rectangle();
                }

                return new Rectangle(header.x, header.y, header.width, header.height);
            }
        }

        public float DpiX
        {
            get { return header.dpi_x; }
        }

        public float DpiY
        {
            get { return header.dpi_y; }
        }

        public int EmfPlusHeaderSize
        {
            get { return header.emfplus_header_size; }
        }

        public int LogicalDpiX
        {
            get { return header.logical_dpi_x; }
        }

        public int LogicalDpiY
        {
            get { return header.logical_dpi_y; }
        }

        public int MetafileSize
        {
            get { return header.size; }
        }

        public MetafileType Type
        {
            get { return header.type; }
        }

        public int Version
        {
            get { return header.version; }
        }

        // note: this always returns a new instance (where we can change
        // properties even if they don't seems to affect anything)
        public MetaHeader WmfHeader
        {
            get
            {
                if (IsWmf())
                    return new MetaHeader(header.wmf_header);
                throw new ArgumentException("WmfHeader only available on WMF files.");
            }
        }
    }
}
