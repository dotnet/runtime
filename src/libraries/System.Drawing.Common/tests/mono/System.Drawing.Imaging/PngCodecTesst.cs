// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// PNG Codec class testing unit
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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Xunit;

namespace MonoTests.System.Drawing.Imaging
{
    [ActiveIssue(24354, TestPlatforms.AnyUnix)]
    public class PngCodecTest
    {
        private bool IsArm64Process()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix || !Environment.Is64BitProcess)
                return false;

            try
            {
                var process = new global::System.Diagnostics.Process();
                process.StartInfo.FileName = "uname";
                process.StartInfo.Arguments = "-m";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();

                return output.Trim() == "aarch64";
            }
            catch
            {
                return false;
            }
        }

        /* Checks bitmap features on a known 1bbp bitmap */
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap1bitFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("1bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.Equal(PixelFormat.Format1bppIndexed, bmp.PixelFormat);

                Assert.Equal(0, bmp.Palette.Flags);
                Assert.Equal(2, bmp.Palette.Entries.Length);
                Assert.Equal(-16777216, bmp.Palette.Entries[0].ToArgb());
                Assert.Equal(-1, bmp.Palette.Entries[1].ToArgb());

                Assert.Equal(288, bmp.Width);
                Assert.Equal(384, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(288, rect.Width);
                Assert.Equal(384, rect.Height);

                Assert.Equal(288, bmp.Size.Width);
                Assert.Equal(384, bmp.Size.Height);
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap1bitPixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("1bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-1, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 128).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 160).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 192).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 224).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 256).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 288).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(0, 320).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 352).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 128).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 160).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 192).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 224).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 256).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 288).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 320).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(32, 352).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 128).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 160).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 192).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 224).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 256).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 288).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 320).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 352).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 96).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 128).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 160).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 192).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 224).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 256).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(96, 288).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 320).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(96, 352).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 64).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 96).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 128).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 160).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 192).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 224).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(128, 256).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 288).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 320).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(128, 352).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(160, 0).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(160, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(160, 64).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(160, 96).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(160, 128).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(160, 160).ToArgb());
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap1bitData()
        {
            string sInFile = Helpers.GetTestBitmapPath("1bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(864, data.Stride);
                    Assert.Equal(384, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(255, *(scan + 0));
                        Assert.Equal(255, *(scan + 1009));
                        Assert.Equal(255, *(scan + 2018));
                        Assert.Equal(255, *(scan + 3027));
                        Assert.Equal(255, *(scan + 4036));
                        Assert.Equal(255, *(scan + 5045));
                        Assert.Equal(255, *(scan + 6054));
                        Assert.Equal(255, *(scan + 7063));
                        Assert.Equal(255, *(scan + 8072));
                        Assert.Equal(255, *(scan + 9081));
                        Assert.Equal(255, *(scan + 10090));
                        Assert.Equal(0, *(scan + 11099));
                        Assert.Equal(255, *(scan + 12108));
                        Assert.Equal(255, *(scan + 13117));
                        Assert.Equal(0, *(scan + 14126));
                        Assert.Equal(255, *(scan + 15135));
                        Assert.Equal(255, *(scan + 16144));
                        Assert.Equal(0, *(scan + 17153));
                        Assert.Equal(0, *(scan + 18162));
                        Assert.Equal(255, *(scan + 19171));
                        Assert.Equal(0, *(scan + 20180));
                        Assert.Equal(255, *(scan + 21189));
                        Assert.Equal(255, *(scan + 22198));
                        Assert.Equal(0, *(scan + 23207));
                        Assert.Equal(0, *(scan + 24216));
                        Assert.Equal(0, *(scan + 25225));
                        Assert.Equal(0, *(scan + 26234));
                        Assert.Equal(255, *(scan + 27243));
                        Assert.Equal(255, *(scan + 28252));
                        Assert.Equal(0, *(scan + 29261));
                        Assert.Equal(255, *(scan + 30270));
                        Assert.Equal(0, *(scan + 31279));
                        Assert.Equal(0, *(scan + 32288));
                        Assert.Equal(255, *(scan + 33297));
                        Assert.Equal(255, *(scan + 34306));
                        Assert.Equal(255, *(scan + 35315));
                        Assert.Equal(255, *(scan + 36324));
                        Assert.Equal(0, *(scan + 37333));
                        Assert.Equal(255, *(scan + 38342));
                        Assert.Equal(255, *(scan + 39351));
                        Assert.Equal(255, *(scan + 40360));
                        Assert.Equal(255, *(scan + 41369));
                        Assert.Equal(255, *(scan + 42378));
                        Assert.Equal(0, *(scan + 43387));
                        Assert.Equal(0, *(scan + 44396));
                        Assert.Equal(255, *(scan + 45405));
                        Assert.Equal(255, *(scan + 46414));
                        Assert.Equal(255, *(scan + 47423));
                        Assert.Equal(255, *(scan + 48432));
                        Assert.Equal(255, *(scan + 49441));
                        Assert.Equal(0, *(scan + 50450));
                        Assert.Equal(0, *(scan + 51459));
                        Assert.Equal(255, *(scan + 52468));
                        Assert.Equal(255, *(scan + 53477));
                        Assert.Equal(255, *(scan + 54486));
                        Assert.Equal(0, *(scan + 55495));
                        Assert.Equal(0, *(scan + 56504));
                        Assert.Equal(0, *(scan + 57513));
                        Assert.Equal(255, *(scan + 58522));
                        Assert.Equal(255, *(scan + 59531));
                        Assert.Equal(0, *(scan + 60540));
                        Assert.Equal(0, *(scan + 61549));
                        Assert.Equal(0, *(scan + 62558));
                        Assert.Equal(0, *(scan + 63567));
                        Assert.Equal(255, *(scan + 64576));
                        Assert.Equal(0, *(scan + 65585));
                        Assert.Equal(255, *(scan + 66594));
                        Assert.Equal(255, *(scan + 67603));
                        Assert.Equal(0, *(scan + 68612));
                        Assert.Equal(0, *(scan + 69621));
                        Assert.Equal(0, *(scan + 70630));
                        Assert.Equal(0, *(scan + 71639));
                        Assert.Equal(0, *(scan + 72648));
                        Assert.Equal(255, *(scan + 73657));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        /* Checks bitmap features on a known 2bbp bitmap */
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable2)]
        public void Bitmap2bitFeatures()
        {
            if (IsArm64Process())
                Assert.True(false, "https://bugzilla.xamarin.com/show_bug.cgi?id=41171");

            string sInFile = Helpers.GetTestBitmapPath("81674-2bpp.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                // quite a promotion! (2 -> 32)
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);

                // MS returns a random Flags value (not a good sign)
                //Assert.Equal (0, bmp.Palette.Flags);
                Assert.Equal(0, bmp.Palette.Entries.Length);

                Assert.Equal(100, bmp.Width);
                Assert.Equal(100, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(100, rect.Width);
                Assert.Equal(100, rect.Height);

                Assert.Equal(100, bmp.Size.Width);
                Assert.Equal(100, bmp.Size.Height);
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable2)]
        public void Bitmap2bitPixels()
        {
            if (IsArm64Process())
                Assert.True(false, "https://bugzilla.xamarin.com/show_bug.cgi?id=41171");

            string sInFile = Helpers.GetTestBitmapPath("81674-2bpp.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-11249559, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-11249559, bmp.GetPixel(96, 96).ToArgb());
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable2)]
        public void Bitmap2bitData()
        {
            if (IsArm64Process())
                Assert.True(false, "https://bugzilla.xamarin.com/show_bug.cgi?id=41171");

            string sInFile = Helpers.GetTestBitmapPath("81674-2bpp.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(300, data.Stride);
                    Assert.Equal(100, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(105, *(scan + 0));
                        Assert.Equal(88, *(scan + 1009));
                        Assert.Equal(255, *(scan + 2018));
                        Assert.Equal(105, *(scan + 3027));
                        Assert.Equal(88, *(scan + 4036));
                        Assert.Equal(84, *(scan + 5045));
                        Assert.Equal(255, *(scan + 6054));
                        Assert.Equal(88, *(scan + 7063));
                        Assert.Equal(84, *(scan + 8072));
                        Assert.Equal(0, *(scan + 9081));
                        Assert.Equal(0, *(scan + 10090));
                        Assert.Equal(84, *(scan + 11099));
                        Assert.Equal(0, *(scan + 12108));
                        Assert.Equal(88, *(scan + 13117));
                        Assert.Equal(84, *(scan + 14126));
                        Assert.Equal(105, *(scan + 15135));
                        Assert.Equal(88, *(scan + 16144));
                        Assert.Equal(84, *(scan + 17153));
                        Assert.Equal(0, *(scan + 18162));
                        Assert.Equal(88, *(scan + 19171));
                        Assert.Equal(84, *(scan + 20180));
                        Assert.Equal(0, *(scan + 21189));
                        Assert.Equal(88, *(scan + 22198));
                        Assert.Equal(84, *(scan + 23207));
                        Assert.Equal(105, *(scan + 24216));
                        Assert.Equal(88, *(scan + 25225));
                        Assert.Equal(0, *(scan + 26234));
                        Assert.Equal(105, *(scan + 27243));
                        Assert.Equal(88, *(scan + 28252));
                        Assert.Equal(84, *(scan + 29261));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        /* Checks bitmap features on a known 4bbp bitmap */
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap4bitFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("4bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.Equal(PixelFormat.Format4bppIndexed, bmp.PixelFormat);

                Assert.Equal(0, bmp.Palette.Flags);
                Assert.Equal(16, bmp.Palette.Entries.Length);
                Assert.Equal(-12106173, bmp.Palette.Entries[0].ToArgb());
                Assert.Equal(-10979957, bmp.Palette.Entries[1].ToArgb());
                Assert.Equal(-8879241, bmp.Palette.Entries[2].ToArgb());
                Assert.Equal(-10381134, bmp.Palette.Entries[3].ToArgb());
                Assert.Equal(-7441574, bmp.Palette.Entries[4].ToArgb());
                Assert.Equal(-6391673, bmp.Palette.Entries[5].ToArgb());
                Assert.Equal(-5861009, bmp.Palette.Entries[6].ToArgb());
                Assert.Equal(-3824008, bmp.Palette.Entries[7].ToArgb());
                Assert.Equal(-5790569, bmp.Palette.Entries[8].ToArgb());
                Assert.Equal(-6178617, bmp.Palette.Entries[9].ToArgb());
                Assert.Equal(-4668490, bmp.Palette.Entries[10].ToArgb());
                Assert.Equal(-5060143, bmp.Palette.Entries[11].ToArgb());
                Assert.Equal(-3492461, bmp.Palette.Entries[12].ToArgb());
                Assert.Equal(-2967099, bmp.Palette.Entries[13].ToArgb());
                Assert.Equal(-2175574, bmp.Palette.Entries[14].ToArgb());
                Assert.Equal(-1314578, bmp.Palette.Entries[15].ToArgb());

                Assert.Equal(288, bmp.Width);
                Assert.Equal(384, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(288, rect.Width);
                Assert.Equal(384, rect.Height);

                Assert.Equal(288, bmp.Size.Width);
                Assert.Equal(384, bmp.Size.Height);
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap4bitPixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("4bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-10381134, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(0, 96).ToArgb());
                Assert.Equal(-3824008, bmp.GetPixel(0, 128).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(0, 160).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(0, 192).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(0, 224).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(0, 256).ToArgb());
                Assert.Equal(-7441574, bmp.GetPixel(0, 288).ToArgb());
                Assert.Equal(-3492461, bmp.GetPixel(0, 320).ToArgb());
                Assert.Equal(-5861009, bmp.GetPixel(0, 352).ToArgb());
                Assert.Equal(-10381134, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-7441574, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(32, 96).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(32, 128).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(32, 160).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(32, 192).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(32, 224).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(32, 256).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(32, 288).ToArgb());
                Assert.Equal(-3492461, bmp.GetPixel(32, 320).ToArgb());
                Assert.Equal(-2175574, bmp.GetPixel(32, 352).ToArgb());
                Assert.Equal(-6178617, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 96).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 128).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 160).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 192).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 224).ToArgb());
                Assert.Equal(-5790569, bmp.GetPixel(64, 256).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 288).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(64, 320).ToArgb());
                Assert.Equal(-5790569, bmp.GetPixel(64, 352).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(96, 0).ToArgb());
                Assert.Equal(-10381134, bmp.GetPixel(96, 32).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(96, 64).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(96, 96).ToArgb());
                Assert.Equal(-7441574, bmp.GetPixel(96, 128).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(96, 160).ToArgb());
                Assert.Equal(-5790569, bmp.GetPixel(96, 192).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(96, 224).ToArgb());
                Assert.Equal(-4668490, bmp.GetPixel(96, 256).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(96, 288).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(96, 320).ToArgb());
                Assert.Equal(-3492461, bmp.GetPixel(96, 352).ToArgb());
                Assert.Equal(-5861009, bmp.GetPixel(128, 0).ToArgb());
                Assert.Equal(-7441574, bmp.GetPixel(128, 32).ToArgb());
                Assert.Equal(-7441574, bmp.GetPixel(128, 64).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 96).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 128).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 160).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 192).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 224).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 256).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 288).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 320).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(128, 352).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(160, 0).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(160, 32).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(160, 64).ToArgb());
                Assert.Equal(-1314578, bmp.GetPixel(160, 96).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(160, 128).ToArgb());
                Assert.Equal(-5790569, bmp.GetPixel(160, 160).ToArgb());
                Assert.Equal(-12106173, bmp.GetPixel(160, 192).ToArgb());
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap4bitData()
        {
            string sInFile = Helpers.GetTestBitmapPath("4bit.png");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(864, data.Stride);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(178, *(scan + 0));
                        Assert.Equal(184, *(scan + 1009));
                        Assert.Equal(235, *(scan + 2018));
                        Assert.Equal(209, *(scan + 3027));
                        Assert.Equal(240, *(scan + 4036));
                        Assert.Equal(142, *(scan + 5045));
                        Assert.Equal(139, *(scan + 6054));
                        Assert.Equal(152, *(scan + 7063));
                        Assert.Equal(235, *(scan + 8072));
                        Assert.Equal(209, *(scan + 9081));
                        Assert.Equal(240, *(scan + 10090));
                        Assert.Equal(142, *(scan + 11099));
                        Assert.Equal(199, *(scan + 12108));
                        Assert.Equal(201, *(scan + 13117));
                        Assert.Equal(97, *(scan + 14126));
                        Assert.Equal(238, *(scan + 15135));
                        Assert.Equal(240, *(scan + 16144));
                        Assert.Equal(158, *(scan + 17153));
                        Assert.Equal(119, *(scan + 18162));
                        Assert.Equal(201, *(scan + 19171));
                        Assert.Equal(88, *(scan + 20180));
                        Assert.Equal(238, *(scan + 21189));
                        Assert.Equal(240, *(scan + 22198));
                        Assert.Equal(120, *(scan + 23207));
                        Assert.Equal(182, *(scan + 24216));
                        Assert.Equal(70, *(scan + 25225));
                        Assert.Equal(71, *(scan + 26234));
                        Assert.Equal(238, *(scan + 27243));
                        Assert.Equal(240, *(scan + 28252));
                        Assert.Equal(120, *(scan + 29261));
                        Assert.Equal(238, *(scan + 30270));
                        Assert.Equal(70, *(scan + 31279));
                        Assert.Equal(71, *(scan + 32288));
                        Assert.Equal(238, *(scan + 33297));
                        Assert.Equal(240, *(scan + 34306));
                        Assert.Equal(210, *(scan + 35315));
                        Assert.Equal(238, *(scan + 36324));
                        Assert.Equal(70, *(scan + 37333));
                        Assert.Equal(97, *(scan + 38342));
                        Assert.Equal(238, *(scan + 39351));
                        Assert.Equal(240, *(scan + 40360));
                        Assert.Equal(235, *(scan + 41369));
                        Assert.Equal(238, *(scan + 42378));
                        Assert.Equal(117, *(scan + 43387));
                        Assert.Equal(158, *(scan + 44396));
                        Assert.Equal(170, *(scan + 45405));
                        Assert.Equal(240, *(scan + 46414));
                        Assert.Equal(235, *(scan + 47423));
                        Assert.Equal(209, *(scan + 48432));
                        Assert.Equal(120, *(scan + 49441));
                        Assert.Equal(71, *(scan + 50450));
                        Assert.Equal(119, *(scan + 51459));
                        Assert.Equal(240, *(scan + 52468));
                        Assert.Equal(235, *(scan + 53477));
                        Assert.Equal(209, *(scan + 54486));
                        Assert.Equal(70, *(scan + 55495));
                        Assert.Equal(71, *(scan + 56504));
                        Assert.Equal(67, *(scan + 57513));
                        Assert.Equal(240, *(scan + 58522));
                        Assert.Equal(167, *(scan + 59531));
                        Assert.Equal(67, *(scan + 60540));
                        Assert.Equal(70, *(scan + 61549));
                        Assert.Equal(71, *(scan + 62558));
                        Assert.Equal(67, *(scan + 63567));
                        Assert.Equal(240, *(scan + 64576));
                        Assert.Equal(120, *(scan + 65585));
                        Assert.Equal(182, *(scan + 66594));
                        Assert.Equal(70, *(scan + 67603));
                        Assert.Equal(120, *(scan + 68612));
                        Assert.Equal(67, *(scan + 69621));
                        Assert.Equal(70, *(scan + 70630));
                        Assert.Equal(71, *(scan + 71639));
                        Assert.Equal(90, *(scan + 72648));
                        Assert.Equal(240, *(scan + 73657));
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
            string sOutFile = $"linerect-{expected}.png";

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
                bmp.Save(sOutFile, ImageFormat.Png);

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
