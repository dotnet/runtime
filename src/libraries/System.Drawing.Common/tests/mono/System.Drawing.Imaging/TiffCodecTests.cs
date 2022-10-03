// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// TIFF Codec class testing unit
//
// Authors:
//  Jordi Mas i Hern?ndez (jordi@ximian.com)
//  Sebastien Pouliot  <sebastien@ximian.com>
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace MonoTests.System.Drawing.Imaging
{
    public class TiffCodecTest
    {
        /* Checks bitmap features on a known 32bbp bitmap */
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Bitmap32bitsFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("almogaver32bits.tif");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);
                // MS reports 24 bpp while we report 32 bpp
                //                Assert.Equal (PixelFormat.Format24bppRgb, bmp.PixelFormat);
                Assert.Equal(173, bmp.Width);
                Assert.Equal(183, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(173, rect.Width);
                Assert.Equal(183, rect.Height);

                Assert.Equal(173, bmp.Size.Width);
                Assert.Equal(183, bmp.Size.Height);
            }
        }

        /* Checks bitmap features on a known 32bbp bitmap */
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Bitmap32bitsPixelFormat()
        {
            string sInFile = Helpers.GetTestBitmapPath("almogaver32bits.tif");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // GDI+ reports 24 bpp while libgdiplus reports 32 bpp
                Assert.Equal (PixelFormat.Format24bppRgb, bmp.PixelFormat);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Bitmap32bitsPixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("almogaver32bits.tif");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-1579559, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-1645353, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-461332, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-330005, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-2237489, bmp.GetPixel(0, 128).ToArgb());
                Assert.Equal(-1251105, bmp.GetPixel(0, 160).ToArgb());
                Assert.Equal(-3024947, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-2699070, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-2366734, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-4538413, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-6116681, bmp.GetPixel(32, 128).ToArgb());
                Assert.Equal(-7369076, bmp.GetPixel(32, 160).ToArgb());
                Assert.Equal(-13024729, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-7174020, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-51, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-16053503, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-8224431, bmp.GetPixel(64, 128).ToArgb());
                Assert.Equal(-16579326, bmp.GetPixel(64, 160).ToArgb());
                Assert.Equal(-2502457, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-9078395, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-12696508, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-70772, bmp.GetPixel(96, 96).ToArgb());
                Assert.Equal(-4346279, bmp.GetPixel(96, 128).ToArgb());
                Assert.Equal(-11583193, bmp.GetPixel(96, 160).ToArgb());
                Assert.Equal(-724763, bmp.GetPixel(128, 0).ToArgb());
                Assert.Equal(-7238268, bmp.GetPixel(128, 32).ToArgb());
                Assert.Equal(-2169612, bmp.GetPixel(128, 64).ToArgb());
                Assert.Equal(-3683883, bmp.GetPixel(128, 96).ToArgb());
                Assert.Equal(-12892867, bmp.GetPixel(128, 128).ToArgb());
                Assert.Equal(-3750464, bmp.GetPixel(128, 160).ToArgb());
                Assert.Equal(-3222844, bmp.GetPixel(160, 0).ToArgb());
                Assert.Equal(-65806, bmp.GetPixel(160, 32).ToArgb());
                Assert.Equal(-2961726, bmp.GetPixel(160, 64).ToArgb());
                Assert.Equal(-2435382, bmp.GetPixel(160, 96).ToArgb());
                Assert.Equal(-2501944, bmp.GetPixel(160, 128).ToArgb());
                Assert.Equal(-9211799, bmp.GetPixel(160, 160).ToArgb());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported), nameof(PlatformDetection.IsNotArm64Process))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/28859")]
        public void Bitmap32bitsData()
        {
            string sInFile = Helpers.GetTestBitmapPath("almogaver32bits.tif");
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

        private void Save(PixelFormat original, PixelFormat expected, bool colorCheck)
        {
            string sOutFile = $"linerect-{expected}.tif";

            // Save
            Bitmap bmp = new Bitmap(100, 100, original);
            Graphics gr = Graphics.FromImage(bmp);

            using (Pen p = new Pen(Color.BlueViolet, 2))
            {
                gr.DrawLine(p, 10.0F, 10.0F, 90.0F, 90.0F);
                gr.DrawRectangle(p, 10.0F, 10.0F, 80.0F, 80.0F);
            }

            try
            {
                bmp.Save(sOutFile, ImageFormat.Tiff);

                // Load
                using (Bitmap bmpLoad = new Bitmap(sOutFile))
                {
                    Assert.Equal(expected, bmpLoad.PixelFormat);
                    if (colorCheck)
                    {
                        Color color = bmpLoad.GetPixel(10, 10);
                        Assert.Equal(Color.FromArgb(255, 138, 43, 226), color);
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
            Save(PixelFormat.Format24bppRgb, PixelFormat.Format24bppRgb, true);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppRgb()
        {
            Save(PixelFormat.Format32bppRgb, PixelFormat.Format32bppArgb, true);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppArgb()
        {
            Save(PixelFormat.Format32bppArgb, PixelFormat.Format32bppArgb, true);
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Save_32bppPArgb()
        {
            Save(PixelFormat.Format32bppPArgb, PixelFormat.Format32bppArgb, true);
        }
    }
}
