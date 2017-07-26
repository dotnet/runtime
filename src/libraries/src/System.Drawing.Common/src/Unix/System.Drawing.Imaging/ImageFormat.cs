// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Imaging.ImageFormat.cs
//
// Authors:
//   Everaldo Canuto (everaldo.canuto@bol.com.br)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//   Dennis Hayes (dennish@raytek.com)
//   Jordi Mas i Hernandez (jordi@ximian.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002-4 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004,2006 Novell, Inc (http://www.novell.com)
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

using System.ComponentModel;

namespace System.Drawing.Imaging
{
#if !NETCORE
    [TypeConverter (typeof (ImageFormatConverter))]
#endif
    public sealed class ImageFormat
    {

        private Guid guid;
        private string name;

        private const string BmpGuid = "b96b3cab-0728-11d3-9d7b-0000f81ef32e";
        private const string EmfGuid = "b96b3cac-0728-11d3-9d7b-0000f81ef32e";
        private const string ExifGuid = "b96b3cb2-0728-11d3-9d7b-0000f81ef32e";
        private const string GifGuid = "b96b3cb0-0728-11d3-9d7b-0000f81ef32e";
        private const string TiffGuid = "b96b3cb1-0728-11d3-9d7b-0000f81ef32e";
        private const string PngGuid = "b96b3caf-0728-11d3-9d7b-0000f81ef32e";
        private const string MemoryBmpGuid = "b96b3caa-0728-11d3-9d7b-0000f81ef32e";
        private const string IconGuid = "b96b3cb5-0728-11d3-9d7b-0000f81ef32e";
        private const string JpegGuid = "b96b3cae-0728-11d3-9d7b-0000f81ef32e";
        private const string WmfGuid = "b96b3cad-0728-11d3-9d7b-0000f81ef32e";

        // lock(this) is bad
        // http://msdn.microsoft.com/library/en-us/dnaskdr/html/askgui06032003.asp?frame=true
        private static object locker = new object();

        private static ImageFormat BmpImageFormat;
        private static ImageFormat EmfImageFormat;
        private static ImageFormat ExifImageFormat;
        private static ImageFormat GifImageFormat;
        private static ImageFormat TiffImageFormat;
        private static ImageFormat PngImageFormat;
        private static ImageFormat MemoryBmpImageFormat;
        private static ImageFormat IconImageFormat;
        private static ImageFormat JpegImageFormat;
        private static ImageFormat WmfImageFormat;


        // constructors
        public ImageFormat(Guid guid)
        {
            this.guid = guid;
        }

        private ImageFormat(string name, string guid)
        {
            this.name = name;
            this.guid = new Guid(guid);
        }


        // methods
        public override bool Equals(object o)
        {
            ImageFormat f = (o as ImageFormat);
            if (f == null)
                return false;

            return f.Guid.Equals(guid);
        }


        public override int GetHashCode()
        {
            return guid.GetHashCode();
        }


        public override string ToString()
        {
            if (name != null)
                return name;

            return ("[ImageFormat: " + guid.ToString() + "]");
        }

        // properties
        public Guid Guid
        {
            get { return guid; }
        }


        public static ImageFormat Bmp
        {
            get
            {
                lock (locker)
                {
                    if (BmpImageFormat == null)
                        BmpImageFormat = new ImageFormat("Bmp", BmpGuid);
                    return BmpImageFormat;
                }
            }
        }

        public static ImageFormat Emf
        {
            get
            {
                lock (locker)
                {
                    if (EmfImageFormat == null)
                        EmfImageFormat = new ImageFormat("Emf", EmfGuid);
                    return EmfImageFormat;
                }
            }
        }


        public static ImageFormat Exif
        {
            get
            {
                lock (locker)
                {
                    if (ExifImageFormat == null)
                        ExifImageFormat = new ImageFormat("Exif", ExifGuid);
                    return ExifImageFormat;
                }
            }
        }


        public static ImageFormat Gif
        {
            get
            {
                lock (locker)
                {
                    if (GifImageFormat == null)
                        GifImageFormat = new ImageFormat("Gif", GifGuid);
                    return GifImageFormat;
                }
            }
        }


        public static ImageFormat Icon
        {
            get
            {
                lock (locker)
                {
                    if (IconImageFormat == null)
                        IconImageFormat = new ImageFormat("Icon", IconGuid);
                    return IconImageFormat;
                }
            }
        }


        public static ImageFormat Jpeg
        {
            get
            {
                lock (locker)
                {
                    if (JpegImageFormat == null)
                        JpegImageFormat = new ImageFormat("Jpeg", JpegGuid);
                    return JpegImageFormat;
                }
            }
        }


        public static ImageFormat MemoryBmp
        {
            get
            {
                lock (locker)
                {
                    if (MemoryBmpImageFormat == null)
                        MemoryBmpImageFormat = new ImageFormat("MemoryBMP", MemoryBmpGuid);
                    return MemoryBmpImageFormat;
                }
            }
        }


        public static ImageFormat Png
        {
            get
            {
                lock (locker)
                {
                    if (PngImageFormat == null)
                        PngImageFormat = new ImageFormat("Png", PngGuid);
                    return PngImageFormat;
                }
            }
        }


        public static ImageFormat Tiff
        {
            get
            {
                lock (locker)
                {
                    if (TiffImageFormat == null)
                        TiffImageFormat = new ImageFormat("Tiff", TiffGuid);
                    return TiffImageFormat;
                }
            }
        }


        public static ImageFormat Wmf
        {
            get
            {
                lock (locker)
                {
                    if (WmfImageFormat == null)
                        WmfImageFormat = new ImageFormat("Wmf", WmfGuid);
                    return WmfImageFormat;
                }
            }
        }
    }
}
