// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// GIF Codec class testing unit
//
// Authors:
// 	Jordi Mas i Hernàndez (jordi@ximian.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2006, 2007 Novell, Inc (http://www.novell.com)
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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace MonoTests.System.Drawing.Imaging
{
    public class GifCodecTest
    {
        /* Checks bitmap features on a known 1bbp bitmap */
        private void Bitmap8bitsFeatures(string filename)
        {
            using (Bitmap bmp = new Bitmap(filename))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.Equal(PixelFormat.Format8bppIndexed, bmp.PixelFormat);
                Assert.Equal(110, bmp.Width);
                Assert.Equal(100, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(110, rect.Width);
                Assert.Equal(100, rect.Height);

                Assert.Equal(110, bmp.Size.Width);
                Assert.Equal(100, bmp.Size.Height);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap8bitsFeatures_Gif89()
        {
            Bitmap8bitsFeatures(Helpers.GetTestBitmapPath("nature24bits.gif"));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap8bitsFeatures_Gif87()
        {
            Bitmap8bitsFeatures(Helpers.GetTestBitmapPath("nature24bits87.gif"));
        }

        private void Bitmap8bitsPixels(string filename)
        {
            using (Bitmap bmp = new Bitmap(filename))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-10644802, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-12630705, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-14537409, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-14672099, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-526863, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-10263970, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-10461317, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-9722415, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-131076, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-2702435, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-6325922, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-12411924, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-131076, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-7766649, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-11512986, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-12616230, bmp.GetPixel(96, 96).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap8bitsPixels_Gif89()
        {
            Bitmap8bitsPixels(Helpers.GetTestBitmapPath("nature24bits.gif"));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap8bitsPixels_Gif87()
        {
            Bitmap8bitsPixels(Helpers.GetTestBitmapPath("nature24bits87.gif"));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap8bitsData()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature24bits.gif");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(332, data.Stride);
                    Assert.Equal(100, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(190, *(scan + 0));
                        Assert.Equal(217, *(scan + 1009));
                        Assert.Equal(120, *(scan + 2018));
                        Assert.Equal(253, *(scan + 3027));
                        Assert.Equal(233, *(scan + 4036));
                        Assert.Equal(176, *(scan + 5045));
                        Assert.Equal(151, *(scan + 6054));
                        Assert.Equal(220, *(scan + 7063));
                        Assert.Equal(139, *(scan + 8072));
                        Assert.Equal(121, *(scan + 9081));
                        Assert.Equal(160, *(scan + 10090));
                        Assert.Equal(92, *(scan + 11099));
                        Assert.Equal(96, *(scan + 12108));
                        Assert.Equal(64, *(scan + 13117));
                        Assert.Equal(156, *(scan + 14126));
                        Assert.Equal(68, *(scan + 15135));
                        Assert.Equal(156, *(scan + 16144));
                        Assert.Equal(84, *(scan + 17153));
                        Assert.Equal(55, *(scan + 18162));
                        Assert.Equal(68, *(scan + 19171));
                        Assert.Equal(116, *(scan + 20180));
                        Assert.Equal(61, *(scan + 21189));
                        Assert.Equal(69, *(scan + 22198));
                        Assert.Equal(75, *(scan + 23207));
                        Assert.Equal(61, *(scan + 24216));
                        Assert.Equal(66, *(scan + 25225));
                        Assert.Equal(40, *(scan + 26234));
                        Assert.Equal(55, *(scan + 27243));
                        Assert.Equal(53, *(scan + 28252));
                        Assert.Equal(215, *(scan + 29261));
                        Assert.Equal(99, *(scan + 30270));
                        Assert.Equal(67, *(scan + 31279));
                        Assert.Equal(142, *(scan + 32288));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Interlaced()
        {
            string sInFile = Helpers.GetTestBitmapPath("81773-interlaced.gif");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                for (int i = 0; i < 255; i++)
                {
                    Color c = bmp.GetPixel(0, i);
                    Assert.Equal(255, c.A);
                    Assert.Equal(i, c.R);
                    Assert.Equal(i, c.G);
                    Assert.Equal(i, c.B);
                }
            }
        }

        private void Save(PixelFormat original, PixelFormat expected, bool exactColorCheck)
        {
            string sOutFile = $"linerect-{expected}.gif";

            // Save		
            Bitmap bmp = new Bitmap(100, 100, original);
            Graphics gr = Graphics.FromImage(bmp);

            using (Pen p = new Pen(Color.Red, 2))
            {
                gr.DrawLine(p, 10.0F, 10.0F, 90.0F, 90.0F);
                gr.DrawRectangle(p, 10.0F, 10.0F, 80.0F, 80.0F);
            }

            try
            {
                bmp.Save(sOutFile, ImageFormat.Gif);

                // Load
                using (Bitmap bmpLoad = new Bitmap(sOutFile))
                {
                    Assert.Equal(expected, bmpLoad.PixelFormat);
                    Color color = bmpLoad.GetPixel(10, 10);
                    if (exactColorCheck)
                    {
                        Assert.Equal(Color.FromArgb(255, 255, 0, 0), color);
                    }
                    else
                    {
                        // FIXME: we don't save a pure red (F8 instead of FF) into the file so the color-check assert will fail
                        // this is due to libgif's QuantizeBuffer. An alternative would be to make our own that checks if less than 256 colors
                        // are used in the bitmap (or else use QuantizeBuffer).
                        Assert.Equal(255, color.A);
                        Assert.True(color.R >= 248);
                        Assert.Equal(0, color.G);
                        Assert.Equal(0, color.B);
                    }
                }
            }
            finally
            {
                gr.Dispose();
                bmp.Dispose();
                try
                {
                    File.Delete(sOutFile);
                }
                catch
                {
                }
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_24bppRgb()
        {
            Save(PixelFormat.Format24bppRgb, PixelFormat.Format8bppIndexed, false);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppRgb()
        {
            Save(PixelFormat.Format32bppRgb, PixelFormat.Format8bppIndexed, false);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppArgb()
        {
            Save(PixelFormat.Format32bppArgb, PixelFormat.Format8bppIndexed, false);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppPArgb()
        {
            Save(PixelFormat.Format32bppPArgb, PixelFormat.Format8bppIndexed, false);
        }
    }
}
