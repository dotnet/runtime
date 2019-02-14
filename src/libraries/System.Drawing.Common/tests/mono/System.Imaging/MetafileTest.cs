// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Metafile class unit tests
//
// Authors:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace MonoTests.System.Drawing.Imaging
{

    public class MetafileTest
    {

        public const string Bitmap = "non-inverted.bmp";
        public const string WmfPlaceable = "telescope_01.wmf";
        public const string Emf = "milkmateya01.emf";

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_String()
        {
            string filename = Helpers.GetTestBitmapPath(WmfPlaceable);
            using (Metafile mf = new Metafile(filename))
            using (Metafile clone = (Metafile)mf.Clone())
            {
            }
        }

        static public void Check_MetaHeader_WmfPlaceable(MetaHeader mh)
        {
            Assert.Equal(9, mh.HeaderSize);
            Assert.Equal(98, mh.MaxRecord);
            Assert.Equal(3, mh.NoObjects);
            Assert.Equal(0, mh.NoParameters);
            Assert.Equal(1737, mh.Size);
            Assert.Equal(1, mh.Type);
            Assert.Equal(0x300, mh.Version);
        }

        public static void Check_MetafileHeader_WmfPlaceable(MetafileHeader header)
        {
            Assert.Equal(MetafileType.WmfPlaceable, header.Type);
            Assert.Equal(0x300, header.Version);
            // filesize - 22, which happens to be the size (22) of a PLACEABLEMETAHEADER struct
            Assert.Equal(3474, header.MetafileSize);

            Assert.Equal(-30, header.Bounds.X);
            Assert.Equal(-40, header.Bounds.Y);
            Assert.Equal(3096, header.Bounds.Width);
            Assert.Equal(4127, header.Bounds.Height);
            Assert.Equal(606, header.DpiX);
            Assert.Equal(606, header.DpiY);
            Assert.Equal(0, header.EmfPlusHeaderSize);
            Assert.Equal(0, header.LogicalDpiX);
            Assert.Equal(0, header.LogicalDpiY);

            Assert.NotNull(header.WmfHeader);
            Check_MetaHeader_WmfPlaceable(header.WmfHeader);

            Assert.False(header.IsDisplay());
            Assert.False(header.IsEmf());
            Assert.False(header.IsEmfOrEmfPlus());
            Assert.False(header.IsEmfPlus());
            Assert.False(header.IsEmfPlusDual());
            Assert.False(header.IsEmfPlusOnly());
            Assert.True(header.IsWmf());
            Assert.True(header.IsWmfPlaceable());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromFile_WmfPlaceable()
        {
            using (Metafile mf = new Metafile(Helpers.GetTestBitmapPath(WmfPlaceable)))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_WmfPlaceable(header1);

                MetaHeader mh1 = header1.WmfHeader;
                Check_MetaHeader_WmfPlaceable(mh1);

                MetaHeader mh2 = mf.GetMetafileHeader().WmfHeader;
                Assert.NotSame(mh1, mh2);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromFileStream_WmfPlaceable()
        {
            using (FileStream fs = File.OpenRead(Helpers.GetTestBitmapPath(WmfPlaceable)))
            using (Metafile mf = new Metafile(fs))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_WmfPlaceable(header1);

                MetaHeader mh1 = header1.WmfHeader;
                Check_MetaHeader_WmfPlaceable(mh1);

                MetaHeader mh2 = mf.GetMetafileHeader().WmfHeader;
                Assert.NotSame(mh1, mh2);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromMemoryStream_WmfPlaceable()
        {
            string filename = Helpers.GetTestBitmapPath(WmfPlaceable);
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(filename)))
            using (Metafile mf = new Metafile(ms))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_WmfPlaceable(header1);

                MetaHeader mh1 = header1.WmfHeader;
                Check_MetaHeader_WmfPlaceable(mh1);

                MetaHeader mh2 = mf.GetMetafileHeader().WmfHeader;
                Assert.NotSame(mh1, mh2);
            }
        }

        public static void Check_MetafileHeader_Emf(MetafileHeader header)
        {
            Assert.Equal(MetafileType.Emf, header.Type);
            Assert.Equal(65536, header.Version);
            // extactly the filesize
            Assert.Equal(20456, header.MetafileSize);

            Assert.Equal(0, header.Bounds.X);
            Assert.Equal(0, header.Bounds.Y);

            Assert.Throws<ArgumentException>(() => header.WmfHeader);

            Assert.False(header.IsDisplay());
            Assert.True(header.IsEmf());
            Assert.True(header.IsEmfOrEmfPlus());
            Assert.False(header.IsEmfPlus());
            Assert.False(header.IsEmfPlusDual());
            Assert.False(header.IsEmfPlusOnly());
            Assert.False(header.IsWmf());
            Assert.False(header.IsWmfPlaceable());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromFile_Emf()
        {
            using (Metafile mf = new Metafile(Helpers.GetTestBitmapPath(Emf)))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_Emf(header1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromFileStream_Emf()
        {
            using (FileStream fs = File.OpenRead(Helpers.GetTestBitmapPath(Emf)))
            using (Metafile mf = new Metafile(fs))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_Emf(header1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_FromMemoryStream_Emf()
        {
            string filename = Helpers.GetTestBitmapPath(Emf);
            using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(filename)))
            using (Metafile mf = new Metafile(ms))
            {
                MetafileHeader header1 = mf.GetMetafileHeader();
                Check_MetafileHeader_Emf(header1);
            }
        }
    }

    public class MetafileFulltrustTest
    {
        private void CheckEmptyHeader(Metafile mf, EmfType type)
        {
            MetafileHeader mh = mf.GetMetafileHeader();
            Assert.Equal(0, mh.Bounds.X);
            Assert.Equal(0, mh.Bounds.Y);
            Assert.Equal(0, mh.Bounds.Width);
            Assert.Equal(0, mh.Bounds.Height);
            Assert.Equal(0, mh.MetafileSize);
            switch (type)
            {
                case EmfType.EmfOnly:
                    Assert.Equal(MetafileType.Emf, mh.Type);
                    break;
                case EmfType.EmfPlusDual:
                    Assert.Equal(MetafileType.EmfPlusDual, mh.Type);
                    break;
                case EmfType.EmfPlusOnly:
                    Assert.Equal(MetafileType.EmfPlusOnly, mh.Type);
                    break;
                default:
                    Assert.True(false, string.Format("Unknown EmfType '{0}'", type));
                    break;
            }
        }

        private void Metafile_IntPtrEmfType(EmfType type)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        Metafile mf = new Metafile(hdc, type);
                        CheckEmptyHeader(mf, type);
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_IntPtrRectangle_Empty()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    Metafile mf = new Metafile(hdc, new Rectangle());
                    CheckEmptyHeader(mf, EmfType.EmfPlusDual);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_IntPtrRectangleF_Empty()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    Metafile mf = new Metafile(hdc, new RectangleF());
                    CheckEmptyHeader(mf, EmfType.EmfPlusDual);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
        }

        private void Metafile_StreamEmfType(Stream stream, EmfType type)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    Metafile mf = new Metafile(stream, hdc, type);
                    CheckEmptyHeader(mf, type);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_StreamIntPtrEmfType_Null()
        {
            Assert.Throws<NullReferenceException>(() => Metafile_StreamEmfType(null, EmfType.EmfOnly));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_StreamIntPtrEmfType_EmfOnly()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Metafile_StreamEmfType(ms, EmfType.EmfOnly);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Metafile_StreamIntPtrEmfType_Invalid()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Assert.Throws<ArgumentException>(() => Metafile_StreamEmfType(ms, (EmfType)int.MinValue));
            }
        }

        private void CreateFilename(EmfType type, bool single)
        {
            string name = string.Format("{0}-{1}.emf", type, single ? "Single" : "Multiple");
            string filename = Path.Combine(Path.GetTempPath(), name);
            Metafile mf;
            using (Bitmap bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        mf = new Metafile(filename, hdc, type);
                        Assert.Equal(0, new FileInfo(filename).Length);
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
                }
                long size = 0;
                using (Graphics g = Graphics.FromImage(mf))
                {
                    g.FillRectangle(Brushes.BlueViolet, 10, 10, 80, 80);
                    size = new FileInfo(filename).Length;
                    Assert.Equal(0, size);
                }

                if (!single)
                {
                    using (Graphics g = Graphics.FromImage(mf))
                    {
                        g.DrawRectangle(Pens.Azure, 10, 10, 80, 80);
                    }
                }
                mf.Dispose();
                Assert.Equal(size, new FileInfo(filename).Length);
            }
        }

        [ActiveIssue(20844)]
        public void CreateFilename_SingleGraphics_EmfOnly()
        {
            CreateFilename(EmfType.EmfOnly, true);
        }

        [ActiveIssue(20844)]
        public void CreateFilename_SingleGraphics_EmfPlusDual()
        {
            CreateFilename(EmfType.EmfPlusDual, true);
        }

        [ActiveIssue(20844)]
        public void CreateFilename_SingleGraphics_EmfPlusOnly()
        {
            CreateFilename(EmfType.EmfPlusOnly, true);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Measure()
        {
            Font test_font = new Font(FontFamily.GenericMonospace, 12);

            Metafile mf;
            using (Bitmap bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    mf = new Metafile(hdc, EmfType.EmfPlusOnly);
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
            using (Graphics g = Graphics.FromImage(mf))
            {
                string text = "this\nis a test";
                CharacterRange[] ranges = new CharacterRange[2];
                ranges[0] = new CharacterRange(0, 5);
                ranges[1] = new CharacterRange(5, 9);

                SizeF size = g.MeasureString(text, test_font);
                Assert.False(size.IsEmpty);

                StringFormat sf = new StringFormat();
                sf.FormatFlags = StringFormatFlags.NoClip;
                sf.SetMeasurableCharacterRanges(ranges);

                RectangleF rect = new RectangleF(0, 0, size.Width, size.Height);
                Region[] region = g.MeasureCharacterRanges(text, test_font, rect, sf);
                Assert.Equal(2, region.Length);
                mf.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WorldTransforms()
        {
            Metafile mf;
            using (Bitmap bmp = new Bitmap(100, 100, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    IntPtr hdc = g.GetHdc();
                    try
                    {
                        mf = new Metafile(hdc, EmfType.EmfPlusOnly);
                    }
                    finally
                    {
                        g.ReleaseHdc(hdc);
                    }
                }
                using (Graphics g = Graphics.FromImage(mf))
                {
                    Assert.True(g.Transform.IsIdentity);
                    g.ScaleTransform(2f, 0.5f);
                    Assert.False(g.Transform.IsIdentity);
                    g.RotateTransform(90);
                    g.TranslateTransform(-2, 2);
                    Matrix m = g.Transform;
                    g.MultiplyTransform(m);
                    // check
                    float[] elements = g.Transform.Elements;
                    Assert.Equal(-1f, elements[0], 5);
                    Assert.Equal(0f, elements[1], 5);
                    Assert.Equal(0f, elements[2], 5);
                    Assert.Equal(-1f, elements[3], 5);
                    Assert.Equal(-2f, elements[4], 5);
                    Assert.Equal(-3f, elements[5], 5);

                    g.Transform = m;
                    elements = g.Transform.Elements;
                    Assert.Equal(0f, elements[0], 5);
                    Assert.Equal(0.5f, elements[1], 5);
                    Assert.Equal(-2f, elements[2], 5);
                    Assert.Equal(0f, elements[3], 5);
                    Assert.Equal(-4f, elements[4], 5);
                    Assert.Equal(-1f, elements[5], 5);

                    g.ResetTransform();
                    Assert.True(g.Transform.IsIdentity);
                }
                mf.Dispose();
            }
        }
    }
}
