// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// JpegCodec class testing unit
//
// Authors:
// 	Jordi Mas i Hernàndez (jordi@ximian.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2004 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2007 Novell, Inc (http://www.novell.com)
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
using System.Drawing.Imaging;
using Xunit;
using System.IO;
using System.Security.Permissions;

namespace MonoTests.System.Drawing.Imaging
{
    public class JpegCodecTest
    {
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap8bbpIndexedGreyscaleFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature-greyscale.jpg");
            using (Bitmap bmp = new Bitmap(sInFile))
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

                Assert.Equal(110, bmp.PhysicalDimension.Width);
                Assert.Equal(100, bmp.PhysicalDimension.Height);

                Assert.Equal(72, bmp.HorizontalResolution);
                Assert.Equal(72, bmp.VerticalResolution);

                // This value is not consistent accross Windows & Unix
                // Assert.Equal(77896, bmp.Flags);

                ColorPalette cp = bmp.Palette;
                Assert.Equal(256, cp.Entries.Length);

                // This value is not consistent accross Windows & Unix
                // Assert.Equal(0, cp.Flags);
                for (int i = 0; i < 256; i++)
                {
                    Color c = cp.Entries[i];
                    Assert.Equal(0xFF, c.A);
                    Assert.Equal(i, c.R);
                    Assert.Equal(i, c.G);
                    Assert.Equal(i, c.B);
                }
            }
        }

        [ConditionalFact(Helpers.GdiPlusIsAvailableNotWindows7)]
        public void Bitmap8bbpIndexedGreyscalePixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature-greyscale.jpg");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-7697782, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-12171706, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-14013910, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-15132391, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-328966, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-9934744, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-10263709, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-7368817, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-4276546, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-9079435, bmp.GetPixel(64, 64).ToArgb());
                // Assert.Equal(-7697782, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-8224126, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-11053225, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-9211021, bmp.GetPixel(96, 96).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiPlusIsAvailableNotWindows7)]
        public void Bitmap8bbpIndexedGreyscaleData()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature-greyscale.jpg");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(100, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(138, *(scan + 0));
                        Assert.Equal(203, *(scan + 1009));
                        Assert.Equal(156, *(scan + 2018));
                        Assert.Equal(248, *(scan + 3027));
                        Assert.Equal(221, *(scan + 4036));
                        Assert.Equal(185, *(scan + 5045));
                        Assert.Equal(128, *(scan + 6054));
                        Assert.Equal(205, *(scan + 7063));
                        Assert.Equal(153, *(scan + 8072));
                        Assert.Equal(110, *(scan + 9081));
                        Assert.Equal(163, *(scan + 10090));
                        Assert.Equal(87, *(scan + 11099));
                        Assert.Equal(90, *(scan + 12108));
                        Assert.Equal(81, *(scan + 13117));
                        // Assert.Equal(124, *(scan + 14126));
                        Assert.Equal(99, *(scan + 15135));
                        Assert.Equal(153, *(scan + 16144));
                        Assert.Equal(57, *(scan + 17153));
                        Assert.Equal(89, *(scan + 18162));
                        Assert.Equal(71, *(scan + 19171));
                        Assert.Equal(106, *(scan + 20180));
                        Assert.Equal(55, *(scan + 21189));
                        Assert.Equal(75, *(scan + 22198));
                        Assert.Equal(77, *(scan + 23207));
                        Assert.Equal(58, *(scan + 24216));
                        Assert.Equal(69, *(scan + 25225));
                        Assert.Equal(43, *(scan + 26234));
                        Assert.Equal(55, *(scan + 27243));
                        Assert.Equal(74, *(scan + 28252));
                        Assert.Equal(145, *(scan + 29261));
                        Assert.Equal(87, *(scan + 30270));
                        Assert.Equal(85, *(scan + 31279));
                        Assert.Equal(106, *(scan + 32288));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        /* Checks bitmap features on a known 24-bits bitmap */
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap24bitFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature24bits.jpg");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.Equal(PixelFormat.Format24bppRgb, bmp.PixelFormat);
                Assert.Equal(110, bmp.Width);
                Assert.Equal(100, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(110, rect.Width);
                Assert.Equal(100, rect.Height);

                Assert.Equal(110, bmp.Size.Width);
                Assert.Equal(100, bmp.Size.Height);

                Assert.Equal(110, bmp.PhysicalDimension.Width);
                Assert.Equal(100, bmp.PhysicalDimension.Height);

                Assert.Equal(72, bmp.HorizontalResolution);
                Assert.Equal(72, bmp.VerticalResolution);

                /* note: under MS flags aren't constant between executions in this case (no palette) */
                // Assert.Equal(77960, bmp.Flags);

                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap24bitPixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("nature24bits.jpg");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-10447423, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-12171958, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-15192259, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-15131110, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-395272, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-10131359, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-10984322, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-11034683, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-3163242, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-7311538, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-12149780, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-8224378, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-11053718, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-12944166, bmp.GetPixel(96, 96).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap24bitData()
        {
            string sInFile = Helpers.GetTestBitmapPath("almogaver24bits.bmp");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(520, data.Stride);
                    Assert.Equal(183, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(217, *(scan + 0));
                        Assert.Equal(192, *(scan + 1009));
                        Assert.Equal(210, *(scan + 2018));
                        Assert.Equal(196, *(scan + 3027));
                        Assert.Equal(216, *(scan + 4036));
                        Assert.Equal(215, *(scan + 5045));
                        Assert.Equal(218, *(scan + 6054));
                        Assert.Equal(218, *(scan + 7063));
                        Assert.Equal(95, *(scan + 8072));
                        Assert.Equal(9, *(scan + 9081));
                        Assert.Equal(247, *(scan + 10090));
                        Assert.Equal(161, *(scan + 11099));
                        Assert.Equal(130, *(scan + 12108));
                        Assert.Equal(131, *(scan + 13117));
                        Assert.Equal(175, *(scan + 14126));
                        Assert.Equal(217, *(scan + 15135));
                        Assert.Equal(201, *(scan + 16144));
                        Assert.Equal(183, *(scan + 17153));
                        Assert.Equal(236, *(scan + 18162));
                        Assert.Equal(242, *(scan + 19171));
                        Assert.Equal(125, *(scan + 20180));
                        Assert.Equal(193, *(scan + 21189));
                        Assert.Equal(227, *(scan + 22198));
                        Assert.Equal(44, *(scan + 23207));
                        Assert.Equal(230, *(scan + 24216));
                        Assert.Equal(224, *(scan + 25225));
                        Assert.Equal(164, *(scan + 26234));
                        Assert.Equal(43, *(scan + 27243));
                        Assert.Equal(200, *(scan + 28252));
                        Assert.Equal(255, *(scan + 29261));
                        Assert.Equal(226, *(scan + 30270));
                        Assert.Equal(230, *(scan + 31279));
                        Assert.Equal(178, *(scan + 32288));
                        Assert.Equal(224, *(scan + 33297));
                        Assert.Equal(233, *(scan + 34306));
                        Assert.Equal(212, *(scan + 35315));
                        Assert.Equal(153, *(scan + 36324));
                        Assert.Equal(143, *(scan + 37333));
                        Assert.Equal(215, *(scan + 38342));
                        Assert.Equal(116, *(scan + 39351));
                        Assert.Equal(26, *(scan + 40360));
                        Assert.Equal(28, *(scan + 41369));
                        Assert.Equal(75, *(scan + 42378));
                        Assert.Equal(50, *(scan + 43387));
                        Assert.Equal(244, *(scan + 44396));
                        Assert.Equal(191, *(scan + 45405));
                        Assert.Equal(200, *(scan + 46414));
                        Assert.Equal(197, *(scan + 47423));
                        Assert.Equal(232, *(scan + 48432));
                        Assert.Equal(186, *(scan + 49441));
                        Assert.Equal(210, *(scan + 50450));
                        Assert.Equal(215, *(scan + 51459));
                        Assert.Equal(155, *(scan + 52468));
                        Assert.Equal(56, *(scan + 53477));
                        Assert.Equal(149, *(scan + 54486));
                        Assert.Equal(137, *(scan + 55495));
                        Assert.Equal(141, *(scan + 56504));
                        Assert.Equal(36, *(scan + 57513));
                        Assert.Equal(39, *(scan + 58522));
                        Assert.Equal(25, *(scan + 59531));
                        Assert.Equal(44, *(scan + 60540));
                        Assert.Equal(12, *(scan + 61549));
                        Assert.Equal(161, *(scan + 62558));
                        Assert.Equal(179, *(scan + 63567));
                        Assert.Equal(181, *(scan + 64576));
                        Assert.Equal(165, *(scan + 65585));
                        Assert.Equal(182, *(scan + 66594));
                        Assert.Equal(186, *(scan + 67603));
                        Assert.Equal(201, *(scan + 68612));
                        Assert.Equal(49, *(scan + 69621));
                        Assert.Equal(161, *(scan + 70630));
                        Assert.Equal(140, *(scan + 71639));
                        Assert.Equal(2, *(scan + 72648));
                        Assert.Equal(15, *(scan + 73657));
                        Assert.Equal(33, *(scan + 74666));
                        Assert.Equal(17, *(scan + 75675));
                        Assert.Equal(0, *(scan + 76684));
                        Assert.Equal(47, *(scan + 77693));
                        Assert.Equal(4, *(scan + 78702));
                        Assert.Equal(142, *(scan + 79711));
                        Assert.Equal(151, *(scan + 80720));
                        Assert.Equal(124, *(scan + 81729));
                        Assert.Equal(81, *(scan + 82738));
                        Assert.Equal(214, *(scan + 83747));
                        Assert.Equal(217, *(scan + 84756));
                        Assert.Equal(30, *(scan + 85765));
                        Assert.Equal(185, *(scan + 86774));
                        Assert.Equal(200, *(scan + 87783));
                        Assert.Equal(37, *(scan + 88792));
                        Assert.Equal(2, *(scan + 89801));
                        Assert.Equal(41, *(scan + 90810));
                        Assert.Equal(16, *(scan + 91819));
                        Assert.Equal(0, *(scan + 92828));
                        Assert.Equal(146, *(scan + 93837));
                        Assert.Equal(163, *(scan + 94846));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        private void Save(PixelFormat original, PixelFormat expected)
        {
            string sOutFile = $"linerect-{expected}.jpeg";

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
                bmp.Save(sOutFile, ImageFormat.Jpeg);

                // Load			
                using (Bitmap bmpLoad = new Bitmap(sOutFile))
                {
                    Assert.Equal(expected, bmpLoad.PixelFormat);
                    Color color = bmpLoad.GetPixel(10, 10);
                    // by default JPEG isn't lossless - so value is "near" read
                    Assert.True(color.R >= 195);
                    Assert.True(color.G < 60);
                    Assert.True(color.B < 60);
                    Assert.Equal(0xFF, color.A);
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
            Save(PixelFormat.Format24bppRgb, PixelFormat.Format24bppRgb);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppRgb()
        {
            Save(PixelFormat.Format32bppRgb, PixelFormat.Format24bppRgb);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppArgb()
        {
            Save(PixelFormat.Format32bppArgb, PixelFormat.Format24bppRgb);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppPArgb()
        {
            Save(PixelFormat.Format32bppPArgb, PixelFormat.Format24bppRgb);
        }
    }
}
