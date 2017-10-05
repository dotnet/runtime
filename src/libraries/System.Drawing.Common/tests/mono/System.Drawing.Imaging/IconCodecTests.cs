// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// ICO Codec class testing unit
//
// Authors:
// 	Jordi Mas i Hernàndez (jordi@ximian.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
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

    public class IconCodecTest
    {
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Image16()
        {
            string sInFile = Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico");
            using (Image image = Image.FromFile(sInFile))
            {
                Assert.True(image.RawFormat.Equals(ImageFormat.Icon));
                // note that image is "promoted" to 32bits
                Assert.Equal(PixelFormat.Format32bppArgb, image.PixelFormat);
                Assert.Equal(73746, image.Flags);

                using (Bitmap bmp = new Bitmap(image))
                {
                    Assert.True(bmp.RawFormat.Equals(ImageFormat.MemoryBmp));
                    Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                    Assert.Equal(2, bmp.Flags);
                    Assert.Equal(0, bmp.Palette.Entries.Length);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Image16_PaletteEntries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico");
            using (Image image = Image.FromFile(sInFile))
            {
                // The values are inconsistent across Windows & Unix: GDI+ returns 0, libgdiplus returns 16.
                Assert.Equal(16, image.Palette.Entries.Length);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Image16_PaletteEntries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico");
            using (Image image = Image.FromFile(sInFile))
            {
                // The values are inconsistent across Windows & Unix: GDI+ returns 0, libgdiplus returns 16.
                Assert.Equal(0, image.Palette.Entries.Length);
            }
        }

        // simley.ico has 48x48, 32x32 and 16x16 images (in that order)
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap16Features()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                // note that image is "promoted" to 32bits
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);

                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(16, bmp.Width);
                Assert.Equal(16, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(16, rect.Width);
                Assert.Equal(16, rect.Height);

                Assert.Equal(16, bmp.Size.Width);
                Assert.Equal(16, bmp.Size.Height);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap16Features_Palette_Entries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values are inconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(16, bmp.Palette.Entries.Length);
                Assert.Equal(-16777216, bmp.Palette.Entries[0].ToArgb());
                Assert.Equal(-16777216, bmp.Palette.Entries[1].ToArgb());
                Assert.Equal(-16744448, bmp.Palette.Entries[2].ToArgb());
                Assert.Equal(-8355840, bmp.Palette.Entries[3].ToArgb());
                Assert.Equal(-16777088, bmp.Palette.Entries[4].ToArgb());
                Assert.Equal(-8388480, bmp.Palette.Entries[5].ToArgb());
                Assert.Equal(-16744320, bmp.Palette.Entries[6].ToArgb());
                Assert.Equal(-4144960, bmp.Palette.Entries[7].ToArgb());
                Assert.Equal(-8355712, bmp.Palette.Entries[8].ToArgb());
                Assert.Equal(-65536, bmp.Palette.Entries[9].ToArgb());
                Assert.Equal(-16711936, bmp.Palette.Entries[10].ToArgb());
                Assert.Equal(-256, bmp.Palette.Entries[11].ToArgb());
                Assert.Equal(-16776961, bmp.Palette.Entries[12].ToArgb());
                Assert.Equal(-65281, bmp.Palette.Entries[13].ToArgb());
                Assert.Equal(-16711681, bmp.Palette.Entries[14].ToArgb());
                Assert.Equal(-1, bmp.Palette.Entries[15].ToArgb());
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap16Features_Palette_Entries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values are inconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap16Pixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(0, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 0).ToArgb());
                Assert.Equal(-256, bmp.GetPixel(4, 4).ToArgb());
                Assert.Equal(-256, bmp.GetPixel(4, 8).ToArgb());
                Assert.Equal(-8355840, bmp.GetPixel(4, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 0).ToArgb());
                Assert.Equal(-256, bmp.GetPixel(8, 4).ToArgb());
                Assert.Equal(-256, bmp.GetPixel(8, 8).ToArgb());
                Assert.Equal(-256, bmp.GetPixel(8, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 4).ToArgb());
                Assert.Equal(-8355840, bmp.GetPixel(12, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 12).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap16Data()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_4bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(16, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(0, *(scan + 0));
                        Assert.Equal(0, *(scan + 13));
                        Assert.Equal(0, *(scan + 26));
                        Assert.Equal(0, *(scan + 39));
                        Assert.Equal(0, *(scan + 52));
                        Assert.Equal(0, *(scan + 65));
                        Assert.Equal(0, *(scan + 78));
                        Assert.Equal(0, *(scan + 91));
                        Assert.Equal(0, *(scan + 104));
                        Assert.Equal(0, *(scan + 117));
                        Assert.Equal(0, *(scan + 130));
                        Assert.Equal(0, *(scan + 143));
                        Assert.Equal(0, *(scan + 156));
                        Assert.Equal(255, *(scan + 169));
                        Assert.Equal(0, *(scan + 182));
                        Assert.Equal(0, *(scan + 195));
                        Assert.Equal(255, *(scan + 208));
                        Assert.Equal(255, *(scan + 221));
                        Assert.Equal(0, *(scan + 234));
                        Assert.Equal(128, *(scan + 247));
                        Assert.Equal(0, *(scan + 260));
                        Assert.Equal(0, *(scan + 273));
                        Assert.Equal(0, *(scan + 286));
                        Assert.Equal(255, *(scan + 299));
                        Assert.Equal(0, *(scan + 312));
                        Assert.Equal(128, *(scan + 325));
                        Assert.Equal(0, *(scan + 338));
                        Assert.Equal(0, *(scan + 351));
                        Assert.Equal(255, *(scan + 364));
                        Assert.Equal(0, *(scan + 377));
                        Assert.Equal(0, *(scan + 390));
                        Assert.Equal(255, *(scan + 403));
                        Assert.Equal(255, *(scan + 416));
                        Assert.Equal(0, *(scan + 429));
                        Assert.Equal(255, *(scan + 442));
                        Assert.Equal(0, *(scan + 455));
                        Assert.Equal(0, *(scan + 468));
                        Assert.Equal(0, *(scan + 481));
                        Assert.Equal(255, *(scan + 494));
                        Assert.Equal(0, *(scan + 507));
                        Assert.Equal(0, *(scan + 520));
                        Assert.Equal(0, *(scan + 533));
                        Assert.Equal(0, *(scan + 546));
                        Assert.Equal(255, *(scan + 559));
                        Assert.Equal(0, *(scan + 572));
                        Assert.Equal(0, *(scan + 585));
                        Assert.Equal(255, *(scan + 598));
                        Assert.Equal(0, *(scan + 611));
                        Assert.Equal(0, *(scan + 624));
                        Assert.Equal(0, *(scan + 637));
                        Assert.Equal(128, *(scan + 650));
                        Assert.Equal(0, *(scan + 663));
                        Assert.Equal(0, *(scan + 676));
                        Assert.Equal(0, *(scan + 689));
                        Assert.Equal(0, *(scan + 702));
                        Assert.Equal(0, *(scan + 715));
                        Assert.Equal(0, *(scan + 728));
                        Assert.Equal(0, *(scan + 741));
                        Assert.Equal(0, *(scan + 754));
                        Assert.Equal(0, *(scan + 767));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        // VisualPng.ico only has a 32x32 size available
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap32Features()
        {
            string sInFile = Helpers.GetTestBitmapPath("VisualPng.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);

                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(32, bmp.Width);
                Assert.Equal(32, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(32, rect.Width);
                Assert.Equal(32, rect.Height);

                Assert.Equal(32, bmp.Size.Width);
                Assert.Equal(32, bmp.Size.Height);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap32Features_PaletteEntries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("VisualPng.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values areinconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(16, bmp.Palette.Entries.Length);

                Assert.Equal(-16777216, bmp.Palette.Entries[0].ToArgb());
                Assert.Equal(-8388608, bmp.Palette.Entries[1].ToArgb());
                Assert.Equal(-16744448, bmp.Palette.Entries[2].ToArgb());
                Assert.Equal(-8355840, bmp.Palette.Entries[3].ToArgb());
                Assert.Equal(-16777088, bmp.Palette.Entries[4].ToArgb());
                Assert.Equal(-8388480, bmp.Palette.Entries[5].ToArgb());
                Assert.Equal(-16744320, bmp.Palette.Entries[6].ToArgb());
                Assert.Equal(-4144960, bmp.Palette.Entries[7].ToArgb());
                Assert.Equal(-8355712, bmp.Palette.Entries[8].ToArgb());
                Assert.Equal(-65536, bmp.Palette.Entries[9].ToArgb());
                Assert.Equal(-16711936, bmp.Palette.Entries[10].ToArgb());
                Assert.Equal(-256, bmp.Palette.Entries[11].ToArgb());
                Assert.Equal(-16776961, bmp.Palette.Entries[12].ToArgb());
                Assert.Equal(-65281, bmp.Palette.Entries[13].ToArgb());
                Assert.Equal(-16711681, bmp.Palette.Entries[14].ToArgb());
                Assert.Equal(-1, bmp.Palette.Entries[15].ToArgb());
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap32Features_PaletteEntries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("VisualPng.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values areinconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap32Pixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("VisualPng.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(0, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-8388608, bmp.GetPixel(0, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 16).ToArgb());
                Assert.Equal(-65536, bmp.GetPixel(8, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 4).ToArgb());
                Assert.Equal(-8388608, bmp.GetPixel(12, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 16).ToArgb());
                Assert.Equal(-65536, bmp.GetPixel(12, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 20).ToArgb());
                Assert.Equal(-65536, bmp.GetPixel(16, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 4).ToArgb());
                Assert.Equal(-8388608, bmp.GetPixel(20, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(24, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 20).ToArgb());
                Assert.Equal(-8388608, bmp.GetPixel(28, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 28).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap32Data()
        {
            string sInFile = Helpers.GetTestBitmapPath("VisualPng.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(32, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(0, *(scan + 0));
                        Assert.Equal(0, *(scan + 13));
                        Assert.Equal(0, *(scan + 26));
                        Assert.Equal(0, *(scan + 39));
                        Assert.Equal(0, *(scan + 52));
                        Assert.Equal(0, *(scan + 65));
                        Assert.Equal(0, *(scan + 78));
                        Assert.Equal(0, *(scan + 91));
                        Assert.Equal(0, *(scan + 104));
                        Assert.Equal(0, *(scan + 117));
                        Assert.Equal(0, *(scan + 130));
                        Assert.Equal(0, *(scan + 143));
                        Assert.Equal(0, *(scan + 156));
                        Assert.Equal(0, *(scan + 169));
                        Assert.Equal(0, *(scan + 182));
                        Assert.Equal(0, *(scan + 195));
                        Assert.Equal(0, *(scan + 208));
                        Assert.Equal(0, *(scan + 221));
                        Assert.Equal(0, *(scan + 234));
                        Assert.Equal(0, *(scan + 247));
                        Assert.Equal(0, *(scan + 260));
                        Assert.Equal(0, *(scan + 273));
                        Assert.Equal(0, *(scan + 286));
                        Assert.Equal(0, *(scan + 299));
                        Assert.Equal(0, *(scan + 312));
                        Assert.Equal(0, *(scan + 325));
                        Assert.Equal(0, *(scan + 338));
                        Assert.Equal(0, *(scan + 351));
                        Assert.Equal(0, *(scan + 364));
                        Assert.Equal(0, *(scan + 377));
                        Assert.Equal(0, *(scan + 390));
                        Assert.Equal(0, *(scan + 403));
                        Assert.Equal(0, *(scan + 416));
                        Assert.Equal(0, *(scan + 429));
                        Assert.Equal(0, *(scan + 442));
                        Assert.Equal(0, *(scan + 455));
                        Assert.Equal(0, *(scan + 468));
                        Assert.Equal(0, *(scan + 481));
                        Assert.Equal(128, *(scan + 494));
                        Assert.Equal(0, *(scan + 507));
                        Assert.Equal(0, *(scan + 520));
                        Assert.Equal(0, *(scan + 533));
                        Assert.Equal(0, *(scan + 546));
                        Assert.Equal(0, *(scan + 559));
                        Assert.Equal(128, *(scan + 572));
                        Assert.Equal(0, *(scan + 585));
                        Assert.Equal(0, *(scan + 598));
                        Assert.Equal(0, *(scan + 611));
                        Assert.Equal(0, *(scan + 624));
                        Assert.Equal(0, *(scan + 637));
                        Assert.Equal(128, *(scan + 650));
                        Assert.Equal(0, *(scan + 663));
                        Assert.Equal(0, *(scan + 676));
                        Assert.Equal(0, *(scan + 689));
                        Assert.Equal(0, *(scan + 702));
                        Assert.Equal(0, *(scan + 715));
                        Assert.Equal(0, *(scan + 728));
                        Assert.Equal(0, *(scan + 741));
                        Assert.Equal(0, *(scan + 754));
                        Assert.Equal(0, *(scan + 767));
                        Assert.Equal(0, *(scan + 780));
                        Assert.Equal(0, *(scan + 793));
                        Assert.Equal(128, *(scan + 806));
                        Assert.Equal(0, *(scan + 819));
                        Assert.Equal(0, *(scan + 832));
                        Assert.Equal(128, *(scan + 845));
                        Assert.Equal(0, *(scan + 858));
                        Assert.Equal(0, *(scan + 871));
                        Assert.Equal(0, *(scan + 884));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        // 48x48_one_entry_1bit.ico only has a 48x48 size available
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap48Features()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);

                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(48, bmp.Width);
                Assert.Equal(48, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(48, rect.Width);
                Assert.Equal(48, rect.Height);

                Assert.Equal(48, bmp.Size.Width);
                Assert.Equal(48, bmp.Size.Height);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap48Features_Palette_Entries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values are inconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(2, bmp.Palette.Entries.Length);
                Assert.Equal(-16777216, bmp.Palette.Entries[0].ToArgb());
                Assert.Equal(-1, bmp.Palette.Entries[1].ToArgb());
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap48Features_Palette_Entries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // These values are inconsistent accross Windows & Unix: 0 on Windows, 16 on Unix
                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap48Pixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-16777216, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 8).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 12).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 16).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 20).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 24).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 28).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 36).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 40).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(0, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(4, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 40).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(4, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 8).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 12).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 16).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 20).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 24).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 28).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 32).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 36).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(8, 40).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(8, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(12, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(12, 8).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 12).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 16).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 20).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 24).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 28).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 36).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(12, 40).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(12, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(16, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(16, 8).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(16, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 28).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(16, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 36).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(16, 40).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(16, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(20, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(20, 8).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(20, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 16).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(20, 20).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(20, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 28).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(20, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 36).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(20, 40).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(20, 44).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(24, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 4).ToArgb());
                Assert.Equal(-16777216, bmp.GetPixel(24, 8).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(24, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 16).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap48Data()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(48, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(0, *(scan + 0));
                        Assert.Equal(0, *(scan + 13));
                        Assert.Equal(0, *(scan + 26));
                        Assert.Equal(0, *(scan + 39));
                        Assert.Equal(0, *(scan + 52));
                        Assert.Equal(0, *(scan + 65));
                        Assert.Equal(0, *(scan + 78));
                        Assert.Equal(0, *(scan + 91));
                        Assert.Equal(0, *(scan + 104));
                        Assert.Equal(0, *(scan + 117));
                        Assert.Equal(0, *(scan + 130));
                        Assert.Equal(0, *(scan + 143));
                        Assert.Equal(0, *(scan + 156));
                        Assert.Equal(0, *(scan + 169));
                        Assert.Equal(0, *(scan + 182));
                        Assert.Equal(0, *(scan + 195));
                        Assert.Equal(0, *(scan + 208));
                        Assert.Equal(0, *(scan + 221));
                        Assert.Equal(0, *(scan + 234));
                        Assert.Equal(0, *(scan + 247));
                        Assert.Equal(0, *(scan + 260));
                        Assert.Equal(0, *(scan + 273));
                        Assert.Equal(0, *(scan + 286));
                        Assert.Equal(255, *(scan + 299));
                        Assert.Equal(255, *(scan + 312));
                        Assert.Equal(255, *(scan + 325));
                        Assert.Equal(255, *(scan + 338));
                        Assert.Equal(255, *(scan + 351));
                        Assert.Equal(255, *(scan + 364));
                        Assert.Equal(255, *(scan + 377));
                        Assert.Equal(255, *(scan + 390));
                        Assert.Equal(255, *(scan + 403));
                        Assert.Equal(255, *(scan + 416));
                        Assert.Equal(0, *(scan + 429));
                        Assert.Equal(255, *(scan + 442));
                        Assert.Equal(255, *(scan + 455));
                        Assert.Equal(255, *(scan + 468));
                        Assert.Equal(255, *(scan + 481));
                        Assert.Equal(255, *(scan + 494));
                        Assert.Equal(255, *(scan + 507));
                        Assert.Equal(255, *(scan + 520));
                        Assert.Equal(255, *(scan + 533));
                        Assert.Equal(255, *(scan + 546));
                        Assert.Equal(255, *(scan + 559));
                        Assert.Equal(0, *(scan + 572));
                        Assert.Equal(255, *(scan + 585));
                        Assert.Equal(0, *(scan + 598));
                        Assert.Equal(0, *(scan + 611));
                        Assert.Equal(0, *(scan + 624));
                        Assert.Equal(0, *(scan + 637));
                        Assert.Equal(0, *(scan + 650));
                        Assert.Equal(0, *(scan + 663));
                        Assert.Equal(0, *(scan + 676));
                        Assert.Equal(0, *(scan + 689));
                        Assert.Equal(0, *(scan + 702));
                        Assert.Equal(0, *(scan + 715));
                        Assert.Equal(255, *(scan + 728));
                        Assert.Equal(0, *(scan + 741));
                        Assert.Equal(0, *(scan + 754));
                        Assert.Equal(0, *(scan + 767));
                        Assert.Equal(0, *(scan + 780));
                        Assert.Equal(0, *(scan + 793));
                        Assert.Equal(0, *(scan + 806));
                        Assert.Equal(0, *(scan + 819));
                        Assert.Equal(0, *(scan + 832));
                        Assert.Equal(0, *(scan + 845));
                        Assert.Equal(0, *(scan + 858));
                        Assert.Equal(255, *(scan + 871));
                        Assert.Equal(0, *(scan + 884));
                        Assert.Equal(0, *(scan + 897));
                        Assert.Equal(0, *(scan + 910));
                        Assert.Equal(0, *(scan + 923));
                        Assert.Equal(0, *(scan + 936));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        // 64x64x256 only has a 64x64 size available
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap64Features()
        {
            string sInFile = Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);

                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(64, bmp.Width);
                Assert.Equal(64, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(64, rect.Width);
                Assert.Equal(64, rect.Height);

                Assert.Equal(64, bmp.Size.Width);
                Assert.Equal(64, bmp.Size.Height);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap64Features_Palette_Entries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // This value is inconsistent accross Windows & Unix: 0 on Windows, 256 on Unix
                Assert.Equal(256, bmp.Palette.Entries.Length);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap64Features_Palette_Entries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // This value is inconsistent accross Windows & Unix: 0 on Windows, 256 on Unix
                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap64Pixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(-65383, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 4).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 8).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 12).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 16).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 20).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 24).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 28).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 36).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 40).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 44).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 48).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 52).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 56).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(0, 60).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(4, 0).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 4).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 8).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 12).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 16).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 20).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 24).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 28).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 32).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 36).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 40).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 44).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 48).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 52).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(4, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 60).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(8, 0).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(8, 4).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 8).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 12).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 16).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 20).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 24).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 28).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 32).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 36).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 40).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 44).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 48).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(8, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 60).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(12, 0).ToArgb());
                Assert.Equal(-10079335, bmp.GetPixel(12, 4).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(12, 8).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 12).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 16).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 20).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 24).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 28).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 32).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 36).ToArgb());
                Assert.Equal(-33664, bmp.GetPixel(12, 40).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap64Data()
        {
            string sInFile = Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(64, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(153, *(scan + 0));
                        Assert.Equal(0, *(scan + 97));
                        Assert.Equal(255, *(scan + 194));
                        Assert.Equal(0, *(scan + 291));
                        Assert.Equal(0, *(scan + 388));
                        Assert.Equal(204, *(scan + 485));
                        Assert.Equal(204, *(scan + 582));
                        Assert.Equal(0, *(scan + 679));
                        Assert.Equal(204, *(scan + 776));
                        Assert.Equal(153, *(scan + 873));
                        Assert.Equal(0, *(scan + 970));
                        Assert.Equal(0, *(scan + 1067));
                        Assert.Equal(153, *(scan + 1164));
                        Assert.Equal(153, *(scan + 1261));
                        Assert.Equal(102, *(scan + 1358));
                        Assert.Equal(0, *(scan + 1455));
                        Assert.Equal(0, *(scan + 1552));
                        Assert.Equal(204, *(scan + 1649));
                        Assert.Equal(153, *(scan + 1746));
                        Assert.Equal(0, *(scan + 1843));
                        Assert.Equal(0, *(scan + 1940));
                        Assert.Equal(51, *(scan + 2037));
                        Assert.Equal(0, *(scan + 2134));
                        Assert.Equal(0, *(scan + 2231));
                        Assert.Equal(102, *(scan + 2328));
                        Assert.Equal(124, *(scan + 2425));
                        Assert.Equal(204, *(scan + 2522));
                        Assert.Equal(0, *(scan + 2619));
                        Assert.Equal(0, *(scan + 2716));
                        Assert.Equal(204, *(scan + 2813));
                        Assert.Equal(51, *(scan + 2910));
                        Assert.Equal(0, *(scan + 3007));
                        Assert.Equal(255, *(scan + 3104));
                        Assert.Equal(0, *(scan + 3201));
                        Assert.Equal(0, *(scan + 3298));
                        Assert.Equal(0, *(scan + 3395));
                        Assert.Equal(128, *(scan + 3492));
                        Assert.Equal(0, *(scan + 3589));
                        Assert.Equal(255, *(scan + 3686));
                        Assert.Equal(128, *(scan + 3783));
                        Assert.Equal(0, *(scan + 3880));
                        Assert.Equal(128, *(scan + 3977));
                        Assert.Equal(0, *(scan + 4074));
                        Assert.Equal(0, *(scan + 4171));
                        Assert.Equal(204, *(scan + 4268));
                        Assert.Equal(0, *(scan + 4365));
                        Assert.Equal(0, *(scan + 4462));
                        Assert.Equal(102, *(scan + 4559));
                        Assert.Equal(0, *(scan + 4656));
                        Assert.Equal(0, *(scan + 4753));
                        Assert.Equal(102, *(scan + 4850));
                        Assert.Equal(0, *(scan + 4947));
                        Assert.Equal(0, *(scan + 5044));
                        Assert.Equal(204, *(scan + 5141));
                        Assert.Equal(128, *(scan + 5238));
                        Assert.Equal(0, *(scan + 5335));
                        Assert.Equal(128, *(scan + 5432));
                        Assert.Equal(128, *(scan + 5529));
                        Assert.Equal(0, *(scan + 5626));
                        Assert.Equal(255, *(scan + 5723));
                        Assert.Equal(153, *(scan + 5820));
                        Assert.Equal(0, *(scan + 5917));
                        Assert.Equal(0, *(scan + 6014));
                        Assert.Equal(51, *(scan + 6111));
                        Assert.Equal(0, *(scan + 6208));
                        Assert.Equal(255, *(scan + 6305));
                        Assert.Equal(153, *(scan + 6402));
                        Assert.Equal(0, *(scan + 6499));
                        Assert.Equal(153, *(scan + 6596));
                        Assert.Equal(102, *(scan + 6693));
                        Assert.Equal(0, *(scan + 6790));
                        Assert.Equal(204, *(scan + 6887));
                        Assert.Equal(153, *(scan + 6984));
                        Assert.Equal(0, *(scan + 7081));
                        Assert.Equal(204, *(scan + 7178));
                        Assert.Equal(153, *(scan + 7275));
                        Assert.Equal(0, *(scan + 7372));
                        Assert.Equal(0, *(scan + 7469));
                        Assert.Equal(153, *(scan + 7566));
                        Assert.Equal(0, *(scan + 7663));
                        Assert.Equal(0, *(scan + 7760));
                        Assert.Equal(153, *(scan + 7857));
                        Assert.Equal(102, *(scan + 7954));
                        Assert.Equal(102, *(scan + 8051));
                        Assert.Equal(0, *(scan + 8148));
                        Assert.Equal(0, *(scan + 8245));
                        Assert.Equal(0, *(scan + 8342));
                        Assert.Equal(204, *(scan + 8439));
                        Assert.Equal(0, *(scan + 8536));
                        Assert.Equal(204, *(scan + 8633));
                        Assert.Equal(128, *(scan + 8730));
                        Assert.Equal(0, *(scan + 8827));
                        Assert.Equal(0, *(scan + 8924));
                        Assert.Equal(153, *(scan + 9021));
                        Assert.Equal(153, *(scan + 9118));
                        Assert.Equal(255, *(scan + 9215));
                        Assert.Equal(0, *(scan + 9312));
                        Assert.Equal(0, *(scan + 9409));
                        Assert.Equal(204, *(scan + 9506));
                        Assert.Equal(0, *(scan + 9603));
                        Assert.Equal(0, *(scan + 9700));
                        Assert.Equal(0, *(scan + 9797));
                        Assert.Equal(128, *(scan + 9894));
                        Assert.Equal(0, *(scan + 9991));
                        Assert.Equal(0, *(scan + 10088));
                        Assert.Equal(0, *(scan + 10185));
                        Assert.Equal(102, *(scan + 10282));
                        Assert.Equal(0, *(scan + 10379));
                        Assert.Equal(0, *(scan + 10476));
                        Assert.Equal(51, *(scan + 10573));
                        Assert.Equal(204, *(scan + 10670));
                        Assert.Equal(0, *(scan + 10767));
                        Assert.Equal(0, *(scan + 10864));
                        Assert.Equal(0, *(scan + 10961));
                        Assert.Equal(153, *(scan + 11058));
                        Assert.Equal(0, *(scan + 11155));
                        Assert.Equal(0, *(scan + 11252));
                        Assert.Equal(153, *(scan + 11349));
                        Assert.Equal(51, *(scan + 11446));
                        Assert.Equal(0, *(scan + 11543));
                        Assert.Equal(0, *(scan + 11640));
                        Assert.Equal(0, *(scan + 11737));
                        Assert.Equal(204, *(scan + 11834));
                        Assert.Equal(0, *(scan + 11931));
                        Assert.Equal(0, *(scan + 12028));
                        Assert.Equal(255, *(scan + 12125));
                        Assert.Equal(153, *(scan + 12222));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        // 96x96x256.ico only has a 96x96 size available
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap96Features()
        {
            string sInFile = Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);

                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(96, bmp.Width);
                Assert.Equal(96, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(96, rect.Width);
                Assert.Equal(96, rect.Height);

                Assert.Equal(96, bmp.Size.Width);
                Assert.Equal(96, bmp.Size.Height);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap96Features_Palette_Entries_Unix()
        {
            string sInFile = Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // This value is inconsistent accross Unix and Windows.
                Assert.Equal(256, bmp.Palette.Entries.Length);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap96Features_Palette_Entries_Windows()
        {
            string sInFile = Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // This value is inconsistent accross Unix and Windows.
                Assert.Equal(0, bmp.Palette.Entries.Length);
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void Bitmap96Pixels()
        {
            string sInFile = Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                // sampling values from a well known bitmap
                Assert.Equal(0, bmp.GetPixel(0, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 44).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 48).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 64).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(0, 68).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 80).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 84).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(0, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(4, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(4, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(4, 16).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(4, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(4, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(4, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(4, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(4, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 60).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(4, 64).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(4, 68).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(4, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(4, 80).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(4, 84).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(4, 88).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(4, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 0).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(8, 4).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(8, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(8, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(8, 16).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(8, 20).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(8, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(8, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 60).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(8, 64).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(8, 68).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(8, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(8, 80).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(8, 84).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(8, 88).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(8, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 0).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 4).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 16).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 20).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(12, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 28).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 56).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(12, 60).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(12, 64).ToArgb());
                Assert.Equal(-3342541, bmp.GetPixel(12, 68).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(12, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(12, 80).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(12, 84).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(12, 88).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(12, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 0).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 4).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 16).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 20).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(16, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 28).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 56).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(16, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 64).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 68).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 76).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(16, 80).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 84).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(16, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(16, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 0).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(20, 4).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(20, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(20, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(20, 16).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(20, 20).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(20, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 28).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 56).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(20, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 64).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(20, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(20, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(20, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(20, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(20, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(20, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(24, 8).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(24, 12).ToArgb());
                Assert.Equal(-3407872, bmp.GetPixel(24, 16).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(24, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 28).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 56).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(24, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(24, 64).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(24, 88).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(24, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(28, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 16).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(28, 20).ToArgb());
                Assert.Equal(-16777012, bmp.GetPixel(28, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 28).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 56).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(28, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(28, 64).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 88).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(28, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(32, 0).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(32, 4).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(32, 8).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(32, 12).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(32, 16).ToArgb());
                Assert.Equal(-16777012, bmp.GetPixel(32, 20).ToArgb());
                Assert.Equal(-16777012, bmp.GetPixel(32, 24).ToArgb());
                Assert.Equal(-16777012, bmp.GetPixel(32, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(32, 32).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 52).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(32, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(32, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(32, 64).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 88).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(32, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 0).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(36, 4).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(36, 8).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(36, 12).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(36, 16).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(36, 20).ToArgb());
                Assert.Equal(-16777012, bmp.GetPixel(36, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 28).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 36).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(36, 40).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(36, 44).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(36, 48).ToArgb());
                Assert.Equal(-3368602, bmp.GetPixel(36, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(36, 64).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 88).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(36, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 0).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(40, 4).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(40, 8).ToArgb());
                Assert.Equal(-10027264, bmp.GetPixel(40, 12).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(40, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 28).ToArgb());
                Assert.Equal(-13408717, bmp.GetPixel(40, 32).ToArgb());
                Assert.Equal(-13408717, bmp.GetPixel(40, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 44).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(40, 48).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 56).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(40, 60).ToArgb());
                Assert.Equal(-26317, bmp.GetPixel(40, 64).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(40, 68).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(40, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(40, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(40, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(40, 84).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(40, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(40, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(44, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 12).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 16).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 20).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 24).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 28).ToArgb());
                Assert.Equal(-13408717, bmp.GetPixel(44, 32).ToArgb());
                Assert.Equal(-13408717, bmp.GetPixel(44, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 40).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(44, 44).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(44, 48).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(44, 52).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(44, 56).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(44, 60).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(44, 64).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 68).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(44, 72).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(44, 76).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(44, 80).ToArgb());
                Assert.Equal(-13434829, bmp.GetPixel(44, 84).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(44, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(44, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 4).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(48, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(48, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(48, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(48, 28).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(48, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 36).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(48, 40).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(48, 44).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(48, 48).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(48, 52).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(48, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 60).ToArgb());
                // Assert.Equal(1842204, bmp.GetPixel(48, 64).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(48, 68).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(48, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 80).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 84).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 88).ToArgb());
                Assert.Equal(0, bmp.GetPixel(48, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(52, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(52, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(52, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(52, 36).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(52, 40).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(52, 44).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(52, 48).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(52, 52).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(52, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(52, 60).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(52, 64).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(52, 68).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(52, 72).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(52, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(52, 80).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(52, 84).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(52, 88).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(52, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(56, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(56, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(56, 36).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(56, 40).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(56, 44).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(56, 48).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(56, 52).ToArgb());
                Assert.Equal(-13312, bmp.GetPixel(56, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(56, 60).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(56, 64).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(56, 68).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(56, 72).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(56, 76).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(56, 80).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(56, 84).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(56, 88).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(56, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(60, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 44).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(60, 48).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 52).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 56).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 60).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 64).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(60, 68).ToArgb());
                Assert.Equal(-3355546, bmp.GetPixel(60, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(60, 76).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(60, 80).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(60, 84).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(60, 88).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(60, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(64, 40).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(64, 44).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 48).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(64, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(64, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(64, 64).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(64, 68).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(64, 80).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(64, 84).ToArgb());
                Assert.Equal(-6737101, bmp.GetPixel(64, 88).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(64, 92).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(68, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 40).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(68, 44).ToArgb());
                Assert.Equal(0, bmp.GetPixel(68, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(68, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(68, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(68, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(68, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(68, 68).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(68, 72).ToArgb());
                Assert.Equal(-16751002, bmp.GetPixel(68, 76).ToArgb());
                Assert.Equal(-16751002, bmp.GetPixel(68, 80).ToArgb());
                Assert.Equal(0, bmp.GetPixel(68, 84).ToArgb());
                Assert.Equal(0, bmp.GetPixel(68, 88).ToArgb());
                Assert.Equal(-39373, bmp.GetPixel(68, 92).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(72, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 40).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(72, 44).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(72, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 68).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(72, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(72, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(72, 80).ToArgb());
                Assert.Equal(0, bmp.GetPixel(72, 84).ToArgb());
                Assert.Equal(0, bmp.GetPixel(72, 88).ToArgb());
                Assert.Equal(-39373, bmp.GetPixel(72, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(76, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(76, 40).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(76, 44).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 68).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(76, 72).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(76, 76).ToArgb());
                Assert.Equal(0, bmp.GetPixel(76, 80).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(76, 84).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(76, 88).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(76, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(80, 0).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 36).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(80, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(80, 44).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 68).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(80, 72).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(80, 76).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(80, 80).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(80, 84).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(80, 88).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(80, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(84, 0).ToArgb());
                Assert.Equal(0, bmp.GetPixel(84, 4).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(84, 36).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(84, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(84, 44).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(84, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 68).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(84, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(84, 76).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(84, 80).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(84, 84).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(84, 88).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(84, 92).ToArgb());
                Assert.Equal(0, bmp.GetPixel(88, 0).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(88, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(88, 8).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 28).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 32).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(88, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(88, 40).ToArgb());
                Assert.Equal(-16777063, bmp.GetPixel(88, 44).ToArgb());
                Assert.Equal(0, bmp.GetPixel(88, 48).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 68).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(88, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(88, 76).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(88, 80).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(88, 84).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(88, 88).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(88, 92).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(92, 0).ToArgb());
                Assert.Equal(-3342490, bmp.GetPixel(92, 4).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(92, 8).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 12).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(92, 16).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(92, 20).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(92, 24).ToArgb());
                Assert.Equal(-52429, bmp.GetPixel(92, 28).ToArgb());
                Assert.Equal(-14935012, bmp.GetPixel(92, 32).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 36).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 40).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 44).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 48).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 52).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(92, 56).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(92, 60).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(92, 64).ToArgb());
                Assert.Equal(-6750157, bmp.GetPixel(92, 68).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 72).ToArgb());
                Assert.Equal(0, bmp.GetPixel(92, 76).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(92, 80).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(92, 84).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(92, 88).ToArgb());
                Assert.Equal(-65383, bmp.GetPixel(92, 92).ToArgb());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Bitmap96Data()
        {
            string sInFile = Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    Assert.Equal(bmp.Height, data.Height);
                    Assert.Equal(bmp.Width, data.Width);
                    Assert.Equal(PixelFormat.Format24bppRgb, data.PixelFormat);
                    Assert.Equal(96, data.Height);

                    unsafe
                    {
                        byte* scan = (byte*)data.Scan0;
                        // sampling values from a well known bitmap
                        Assert.Equal(0, *(scan + 0));
                        Assert.Equal(0, *(scan + 97));
                        Assert.Equal(0, *(scan + 194));
                        Assert.Equal(0, *(scan + 291));
                        Assert.Equal(0, *(scan + 388));
                        Assert.Equal(28, *(scan + 485));
                        Assert.Equal(0, *(scan + 582));
                        Assert.Equal(28, *(scan + 679));
                        Assert.Equal(255, *(scan + 776));
                        Assert.Equal(0, *(scan + 873));
                        Assert.Equal(255, *(scan + 970));
                        Assert.Equal(255, *(scan + 1067));
                        Assert.Equal(0, *(scan + 1164));
                        Assert.Equal(255, *(scan + 1261));
                        Assert.Equal(255, *(scan + 1358));
                        Assert.Equal(0, *(scan + 1455));
                        Assert.Equal(255, *(scan + 1552));
                        Assert.Equal(255, *(scan + 1649));
                        Assert.Equal(0, *(scan + 1746));
                        Assert.Equal(255, *(scan + 1843));
                        Assert.Equal(255, *(scan + 1940));
                        Assert.Equal(0, *(scan + 2037));
                        Assert.Equal(255, *(scan + 2134));
                        Assert.Equal(255, *(scan + 2231));
                        Assert.Equal(0, *(scan + 2328));
                        Assert.Equal(255, *(scan + 2425));
                        Assert.Equal(255, *(scan + 2522));
                        Assert.Equal(0, *(scan + 2619));
                        Assert.Equal(255, *(scan + 2716));
                        Assert.Equal(255, *(scan + 2813));
                        Assert.Equal(0, *(scan + 2910));
                        Assert.Equal(255, *(scan + 3007));
                        Assert.Equal(255, *(scan + 3104));
                        Assert.Equal(0, *(scan + 3201));
                        Assert.Equal(255, *(scan + 3298));
                        Assert.Equal(255, *(scan + 3395));
                        Assert.Equal(0, *(scan + 3492));
                        Assert.Equal(0, *(scan + 3589));
                        Assert.Equal(255, *(scan + 3686));
                        Assert.Equal(0, *(scan + 3783));
                        Assert.Equal(0, *(scan + 3880));
                        Assert.Equal(255, *(scan + 3977));
                        Assert.Equal(0, *(scan + 4074));
                        Assert.Equal(0, *(scan + 4171));
                        Assert.Equal(255, *(scan + 4268));
                        Assert.Equal(0, *(scan + 4365));
                        Assert.Equal(28, *(scan + 4462));
                        Assert.Equal(255, *(scan + 4559));
                        Assert.Equal(0, *(scan + 4656));
                        Assert.Equal(51, *(scan + 4753));
                        Assert.Equal(255, *(scan + 4850));
                        Assert.Equal(0, *(scan + 4947));
                        Assert.Equal(51, *(scan + 5044));
                        Assert.Equal(255, *(scan + 5141));
                        Assert.Equal(0, *(scan + 5238));
                        Assert.Equal(51, *(scan + 5335));
                        Assert.Equal(255, *(scan + 5432));
                        Assert.Equal(0, *(scan + 5529));
                        Assert.Equal(51, *(scan + 5626));
                        Assert.Equal(255, *(scan + 5723));
                        Assert.Equal(0, *(scan + 5820));
                        Assert.Equal(51, *(scan + 5917));
                        Assert.Equal(255, *(scan + 6014));
                        Assert.Equal(0, *(scan + 6111));
                        Assert.Equal(51, *(scan + 6208));
                        Assert.Equal(255, *(scan + 6305));
                        Assert.Equal(0, *(scan + 6402));
                        Assert.Equal(51, *(scan + 6499));
                        Assert.Equal(255, *(scan + 6596));
                        Assert.Equal(0, *(scan + 6693));
                        Assert.Equal(51, *(scan + 6790));
                        Assert.Equal(255, *(scan + 6887));
                        Assert.Equal(0, *(scan + 6984));
                        Assert.Equal(51, *(scan + 7081));
                        Assert.Equal(255, *(scan + 7178));
                        Assert.Equal(0, *(scan + 7275));
                        Assert.Equal(51, *(scan + 7372));
                        Assert.Equal(255, *(scan + 7469));
                        Assert.Equal(0, *(scan + 7566));
                        Assert.Equal(51, *(scan + 7663));
                        Assert.Equal(255, *(scan + 7760));
                        Assert.Equal(0, *(scan + 7857));
                        Assert.Equal(51, *(scan + 7954));
                        Assert.Equal(255, *(scan + 8051));
                        Assert.Equal(0, *(scan + 8148));
                        Assert.Equal(51, *(scan + 8245));
                        Assert.Equal(255, *(scan + 8342));
                        Assert.Equal(0, *(scan + 8439));
                        Assert.Equal(51, *(scan + 8536));
                        Assert.Equal(28, *(scan + 8633));
                        Assert.Equal(0, *(scan + 8730));
                        Assert.Equal(51, *(scan + 8827));
                        Assert.Equal(0, *(scan + 8924));
                        Assert.Equal(0, *(scan + 9021));
                        Assert.Equal(51, *(scan + 9118));
                        Assert.Equal(0, *(scan + 9215));
                        Assert.Equal(0, *(scan + 9312));
                        Assert.Equal(51, *(scan + 9409));
                        Assert.Equal(0, *(scan + 9506));
                        Assert.Equal(0, *(scan + 9603));
                        Assert.Equal(51, *(scan + 9700));
                        Assert.Equal(0, *(scan + 9797));
                        Assert.Equal(28, *(scan + 9894));
                        Assert.Equal(51, *(scan + 9991));
                        Assert.Equal(0, *(scan + 10088));
                        Assert.Equal(0, *(scan + 10185));
                        Assert.Equal(51, *(scan + 10282));
                        Assert.Equal(0, *(scan + 10379));
                        Assert.Equal(0, *(scan + 10476));
                        Assert.Equal(51, *(scan + 10573));
                        Assert.Equal(0, *(scan + 10670));
                        Assert.Equal(0, *(scan + 10767));
                        Assert.Equal(51, *(scan + 10864));
                        Assert.Equal(204, *(scan + 10961));
                        Assert.Equal(0, *(scan + 11058));
                        Assert.Equal(51, *(scan + 11155));
                        Assert.Equal(204, *(scan + 11252));
                        Assert.Equal(0, *(scan + 11349));
                        Assert.Equal(51, *(scan + 11446));
                        Assert.Equal(204, *(scan + 11543));
                        Assert.Equal(0, *(scan + 11640));
                        Assert.Equal(51, *(scan + 11737));
                        Assert.Equal(204, *(scan + 11834));
                        Assert.Equal(0, *(scan + 11931));
                        Assert.Equal(51, *(scan + 12028));
                        Assert.Equal(204, *(scan + 12125));
                        Assert.Equal(0, *(scan + 12222));
                        Assert.Equal(51, *(scan + 12319));
                        Assert.Equal(204, *(scan + 12416));
                        Assert.Equal(28, *(scan + 12513));
                        Assert.Equal(51, *(scan + 12610));
                        Assert.Equal(204, *(scan + 12707));
                        Assert.Equal(0, *(scan + 12804));
                        Assert.Equal(28, *(scan + 12901));
                        Assert.Equal(204, *(scan + 12998));
                        Assert.Equal(0, *(scan + 13095));
                        Assert.Equal(0, *(scan + 13192));
                        Assert.Equal(204, *(scan + 13289));
                        Assert.Equal(0, *(scan + 13386));
                        Assert.Equal(0, *(scan + 13483));
                        Assert.Equal(204, *(scan + 13580));
                        Assert.Equal(0, *(scan + 13677));
                        Assert.Equal(28, *(scan + 13774));
                        Assert.Equal(204, *(scan + 13871));
                        Assert.Equal(0, *(scan + 13968));
                        Assert.Equal(0, *(scan + 14065));
                        Assert.Equal(204, *(scan + 14162));
                        Assert.Equal(0, *(scan + 14259));
                        Assert.Equal(0, *(scan + 14356));
                        Assert.Equal(204, *(scan + 14453));
                        Assert.Equal(0, *(scan + 14550));
                        Assert.Equal(0, *(scan + 14647));
                        Assert.Equal(204, *(scan + 14744));
                        Assert.Equal(0, *(scan + 14841));
                        Assert.Equal(0, *(scan + 14938));
                        Assert.Equal(204, *(scan + 15035));
                        Assert.Equal(0, *(scan + 15132));
                        Assert.Equal(0, *(scan + 15229));
                        Assert.Equal(204, *(scan + 15326));
                        Assert.Equal(0, *(scan + 15423));
                        Assert.Equal(0, *(scan + 15520));
                        Assert.Equal(204, *(scan + 15617));
                        Assert.Equal(0, *(scan + 15714));
                        Assert.Equal(0, *(scan + 15811));
                        Assert.Equal(204, *(scan + 15908));
                        Assert.Equal(0, *(scan + 16005));
                        Assert.Equal(0, *(scan + 16102));
                        Assert.Equal(204, *(scan + 16199));
                        Assert.Equal(0, *(scan + 16296));
                        Assert.Equal(0, *(scan + 16393));
                        Assert.Equal(204, *(scan + 16490));
                        Assert.Equal(0, *(scan + 16587));
                        Assert.Equal(0, *(scan + 16684));
                        Assert.Equal(204, *(scan + 16781));
                        Assert.Equal(0, *(scan + 16878));
                        Assert.Equal(0, *(scan + 16975));
                        Assert.Equal(204, *(scan + 17072));
                        Assert.Equal(0, *(scan + 17169));
                        Assert.Equal(0, *(scan + 17266));
                        Assert.Equal(204, *(scan + 17363));
                        Assert.Equal(0, *(scan + 17460));
                        Assert.Equal(0, *(scan + 17557));
                        Assert.Equal(28, *(scan + 17654));
                        Assert.Equal(0, *(scan + 17751));
                        Assert.Equal(0, *(scan + 17848));
                        Assert.Equal(0, *(scan + 17945));
                        Assert.Equal(28, *(scan + 18042));
                        Assert.Equal(0, *(scan + 18139));
                        Assert.Equal(0, *(scan + 18236));
                        Assert.Equal(51, *(scan + 18333));
                        Assert.Equal(28, *(scan + 18430));
                        Assert.Equal(0, *(scan + 18527));
                        Assert.Equal(51, *(scan + 18624));
                        Assert.Equal(0, *(scan + 18721));
                        Assert.Equal(28, *(scan + 18818));
                        Assert.Equal(51, *(scan + 18915));
                        Assert.Equal(255, *(scan + 19012));
                        Assert.Equal(51, *(scan + 19109));
                        Assert.Equal(51, *(scan + 19206));
                        Assert.Equal(255, *(scan + 19303));
                        Assert.Equal(51, *(scan + 19400));
                        Assert.Equal(51, *(scan + 19497));
                        Assert.Equal(255, *(scan + 19594));
                        Assert.Equal(51, *(scan + 19691));
                        Assert.Equal(51, *(scan + 19788));
                        Assert.Equal(255, *(scan + 19885));
                        Assert.Equal(51, *(scan + 19982));
                        Assert.Equal(51, *(scan + 20079));
                        Assert.Equal(255, *(scan + 20176));
                        Assert.Equal(51, *(scan + 20273));
                        Assert.Equal(51, *(scan + 20370));
                        Assert.Equal(255, *(scan + 20467));
                        Assert.Equal(51, *(scan + 20564));
                        Assert.Equal(51, *(scan + 20661));
                        Assert.Equal(255, *(scan + 20758));
                        Assert.Equal(51, *(scan + 20855));
                        Assert.Equal(51, *(scan + 20952));
                        Assert.Equal(255, *(scan + 21049));
                        Assert.Equal(51, *(scan + 21146));
                        Assert.Equal(51, *(scan + 21243));
                        Assert.Equal(28, *(scan + 21340));
                        Assert.Equal(51, *(scan + 21437));
                        Assert.Equal(51, *(scan + 21534));
                        Assert.Equal(0, *(scan + 21631));
                        Assert.Equal(51, *(scan + 21728));
                        Assert.Equal(28, *(scan + 21825));
                        Assert.Equal(0, *(scan + 21922));
                        Assert.Equal(51, *(scan + 22019));
                        Assert.Equal(28, *(scan + 22116));
                        Assert.Equal(0, *(scan + 22213));
                        Assert.Equal(51, *(scan + 22310));
                        Assert.Equal(0, *(scan + 22407));
                        Assert.Equal(0, *(scan + 22504));
                        Assert.Equal(51, *(scan + 22601));
                        Assert.Equal(0, *(scan + 22698));
                        Assert.Equal(0, *(scan + 22795));
                        Assert.Equal(51, *(scan + 22892));
                        Assert.Equal(28, *(scan + 22989));
                        Assert.Equal(0, *(scan + 23086));
                        Assert.Equal(28, *(scan + 23183));
                        Assert.Equal(153, *(scan + 23280));
                        Assert.Equal(28, *(scan + 23377));
                        Assert.Equal(0, *(scan + 23474));
                        Assert.Equal(153, *(scan + 23571));
                        Assert.Equal(28, *(scan + 23668));
                        Assert.Equal(0, *(scan + 23765));
                        Assert.Equal(153, *(scan + 23862));
                        Assert.Equal(0, *(scan + 23959));
                        Assert.Equal(28, *(scan + 24056));
                        Assert.Equal(153, *(scan + 24153));
                        Assert.Equal(0, *(scan + 24250));
                        Assert.Equal(153, *(scan + 24347));
                        Assert.Equal(153, *(scan + 24444));
                        Assert.Equal(0, *(scan + 24541));
                        Assert.Equal(153, *(scan + 24638));
                        Assert.Equal(153, *(scan + 24735));
                        Assert.Equal(0, *(scan + 24832));
                        Assert.Equal(153, *(scan + 24929));
                        Assert.Equal(153, *(scan + 25026));
                        Assert.Equal(0, *(scan + 25123));
                        Assert.Equal(153, *(scan + 25220));
                        Assert.Equal(153, *(scan + 25317));
                        Assert.Equal(0, *(scan + 25414));
                        Assert.Equal(153, *(scan + 25511));
                        Assert.Equal(153, *(scan + 25608));
                        Assert.Equal(0, *(scan + 25705));
                        Assert.Equal(153, *(scan + 25802));
                        Assert.Equal(153, *(scan + 25899));
                        Assert.Equal(0, *(scan + 25996));
                        Assert.Equal(153, *(scan + 26093));
                        Assert.Equal(153, *(scan + 26190));
                        Assert.Equal(0, *(scan + 26287));
                        Assert.Equal(153, *(scan + 26384));
                        Assert.Equal(153, *(scan + 26481));
                        Assert.Equal(0, *(scan + 26578));
                        Assert.Equal(153, *(scan + 26675));
                        Assert.Equal(153, *(scan + 26772));
                        Assert.Equal(28, *(scan + 26869));
                        Assert.Equal(153, *(scan + 26966));
                        Assert.Equal(28, *(scan + 27063));
                        Assert.Equal(28, *(scan + 27160));
                        Assert.Equal(28, *(scan + 27257));
                        Assert.Equal(0, *(scan + 27354));
                        Assert.Equal(0, *(scan + 27451));
                        Assert.Equal(0, *(scan + 27548));
                        Assert.Equal(0, *(scan + 27645));
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Xp32bppIconFeatures()
        {
            string sInFile = Helpers.GetTestBitmapPath("48x48_multiple_entries_32bit.ico");
            using (Bitmap bmp = new Bitmap(sInFile))
            {
                GraphicsUnit unit = GraphicsUnit.World;
                RectangleF rect = bmp.GetBounds(ref unit);

                Assert.True(bmp.RawFormat.Equals(ImageFormat.Icon));
                // note that image is "promoted" to 32bits
                Assert.Equal(PixelFormat.Format32bppArgb, bmp.PixelFormat);
                Assert.Equal(73746, bmp.Flags);
                Assert.Equal(0, bmp.Palette.Entries.Length);
                Assert.Equal(1, bmp.FrameDimensionsList.Length);
                Assert.Equal(0, bmp.PropertyIdList.Length);
                Assert.Equal(0, bmp.PropertyItems.Length);
                Assert.Null(bmp.Tag);
                Assert.Equal(96.0f, bmp.HorizontalResolution);
                Assert.Equal(96.0f, bmp.VerticalResolution);
                Assert.Equal(16, bmp.Width);
                Assert.Equal(16, bmp.Height);

                Assert.Equal(0, rect.X);
                Assert.Equal(0, rect.Y);
                Assert.Equal(16, rect.Width);
                Assert.Equal(16, rect.Height);

                Assert.Equal(16, bmp.Size.Width);
                Assert.Equal(16, bmp.Size.Height);
            }
        }

        private void Save(PixelFormat original, PixelFormat expected, bool colorCheck)
        {
            string sOutFile = $"linerect-{expected}.ico";

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
                // there's no encoder, so we're not saving a ICO but the alpha 
                // bit get sets so it's not like saving a bitmap either
                bmp.Save(sOutFile, ImageFormat.Icon);

                // Load
                using (Bitmap bmpLoad = new Bitmap(sOutFile))
                {
                    Assert.Equal(ImageFormat.Png, bmpLoad.RawFormat);
                    Assert.Equal(expected, bmpLoad.PixelFormat);
                    if (colorCheck)
                    {
                        Color color = bmpLoad.GetPixel(10, 10);
                        Assert.Equal(Color.FromArgb(255, 255, 0, 0), color);
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
