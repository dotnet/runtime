// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// (C) 2004 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004,2006-2007 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Tests
{
    public class BitmapTests
    {
        public static IEnumerable<object[]> Ctor_FilePath_TestData()
        {
            yield return new object[] { "16x16_one_entry_4bit.ico", 16, 16, PixelFormat.Format32bppArgb, ImageFormat.Icon };
            yield return new object[] { "bitmap_173x183_indexed_8bit.bmp", 173, 183, PixelFormat.Format8bppIndexed, ImageFormat.Bmp };
            yield return new object[] { "16x16_nonindexed_24bit.png", 16, 16, PixelFormat.Format24bppRgb, ImageFormat.Png };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_FilePath_TestData))]
        public void Ctor_FilePath(string filename, int width, int height, PixelFormat pixelFormat, ImageFormat rawFormat)
        {
            using (var bitmap = new Bitmap(Helpers.GetTestBitmapPath(filename)))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(pixelFormat, bitmap.PixelFormat);
                Assert.Equal(rawFormat, bitmap.RawFormat);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_FilePath_TestData))]
        public void Ctor_FilePath_UseIcm(string filename, int width, int height, PixelFormat pixelFormat, ImageFormat rawFormat)
        {
            foreach (bool useIcm in new bool[] { true, false })
            {
                using (var bitmap = new Bitmap(Helpers.GetTestBitmapPath(filename), useIcm))
                {
                    Assert.Equal(width, bitmap.Width);
                    Assert.Equal(height, bitmap.Height);
                    Assert.Equal(pixelFormat, bitmap.PixelFormat);
                    Assert.Equal(rawFormat, bitmap.RawFormat);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullFilePath_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new Bitmap((string)null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => new Bitmap((string)null, false));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData("", "path")]
        [InlineData("\0", "path")]
        [InlineData("NoSuchPath", null)]
        public void Ctor_InvalidFilePath_ThrowsArgumentException(string filename, string paramName)
        {
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => new Bitmap(filename));
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => new Bitmap(filename, false));
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => new Bitmap(filename, true));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Type_ResourceName()
        {
            using (var bitmap = new Bitmap(typeof(BitmapTests), "bitmap_173x183_indexed_8bit.bmp"))
            {
                Assert.Equal(173, bitmap.Width);
                Assert.Equal(183, bitmap.Height);
                Assert.Equal(PixelFormat.Format8bppIndexed, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.Bmp, bitmap.RawFormat);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullType_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new Bitmap(null, "name"));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(typeof(Bitmap), null)]
        [InlineData(typeof(Bitmap), "")]
        [InlineData(typeof(Bitmap), "bitmap_173x183_indexed_8bit.bmp")]
        [InlineData(typeof(BitmapTests), "bitmap_173x183_INDEXED_8bit.bmp")]
        [InlineData(typeof(BitmapTests), "empty.file")]
        public void Ctor_InvalidResource_ThrowsArgumentException(Type type, string resource)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(type, resource));
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_FilePath_TestData))]
        public void Ctor_Stream(string filename, int width, int height, PixelFormat pixelFormat, ImageFormat rawFormat)
        {
            using (Stream stream = File.OpenRead(Helpers.GetTestBitmapPath(filename)))
            using (var bitmap = new Bitmap(stream))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(pixelFormat, bitmap.PixelFormat);
                Assert.Equal(rawFormat, bitmap.RawFormat);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_FilePath_TestData))]
        public void Ctor_Stream_UseIcm(string filename, int width, int height, PixelFormat pixelFormat, ImageFormat rawFormat)
        {
            foreach (bool useIcm in new bool[] { true, false })
            {
                using (Stream stream = File.OpenRead(Helpers.GetTestBitmapPath(filename)))
                using (var bitmap = new Bitmap(stream, useIcm))
                {
                    Assert.Equal(width, bitmap.Width);
                    Assert.Equal(height, bitmap.Height);
                    Assert.Equal(pixelFormat, bitmap.PixelFormat);
                    Assert.Equal(rawFormat, bitmap.RawFormat);
                }
            }
        }
        
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => new Bitmap((Stream)null));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => new Bitmap((Stream)null, false));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_InvalidBytesInStream_ThrowsArgumentException()
        {
            using (var stream = new MemoryStream(new byte[0]))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(stream));
                AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(stream, false));
                AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(stream, true));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(10, 10)]
        [InlineData(5, 15)]
        public void Ctor_Width_Height(int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            Assert.Equal(width, bitmap.Width);
            Assert.Equal(height, bitmap.Height);
            Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
            Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(10, 10, PixelFormat.Format1bppIndexed)]
        [InlineData(10, 10, PixelFormat.Format8bppIndexed)]
        [InlineData(1, 1, PixelFormat.Format16bppArgb1555)]
        [InlineData(1, 1, PixelFormat.Format16bppRgb555)]
        [InlineData(1, 1, PixelFormat.Format16bppRgb565)]
        [InlineData(1, 1, PixelFormat.Format16bppGrayScale)]
        [InlineData(1, 1, PixelFormat.Format24bppRgb)]
        [InlineData(1, 1, PixelFormat.Format32bppRgb)]
        [InlineData(5, 15, PixelFormat.Format32bppArgb)]
        [InlineData(1, 1, PixelFormat.Format32bppPArgb)]
        [InlineData(10, 10, PixelFormat.Format48bppRgb)]
        [InlineData(10, 10, PixelFormat.Format4bppIndexed)]
        [InlineData(1, 1, PixelFormat.Format64bppArgb)]
        [InlineData(1, 1, PixelFormat.Format64bppPArgb)]
        public void Ctor_Width_Height_PixelFormat(int width, int height, PixelFormat pixelFormat)
        {
            using (var bitmap = new Bitmap(width, height, pixelFormat))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(pixelFormat, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        public static IEnumerable<object[]> Ctor_Width_Height_Stride_PixelFormat_Scan0_TestData()
        {
            yield return new object[] { 10, 10, 0, PixelFormat.Format8bppIndexed, IntPtr.Zero };
            yield return new object[] { 5, 15, int.MaxValue, PixelFormat.Format32bppArgb, IntPtr.Zero };
            yield return new object[] { 5, 15, int.MinValue, PixelFormat.Format24bppRgb, IntPtr.Zero };
            yield return new object[] { 1, 1, 1, PixelFormat.Format1bppIndexed, IntPtr.Zero };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Ctor_Width_Height_Stride_PixelFormat_Scan0_TestData))]
        public void Ctor_Width_Height_Stride_PixelFormat_Scan0(int width, int height, int stride, PixelFormat pixelFormat, IntPtr scan0)
        {
            using (var bitmap = new Bitmap(width, height, stride, pixelFormat, scan0))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(pixelFormat, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(ushort.MaxValue * 513)]
        [InlineData(int.MaxValue)]
        public void Ctor_InvalidWidth_ThrowsArgumentException(int width)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(width, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(width, 1, Graphics.FromImage(new Bitmap(1, 1))));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(new Bitmap(1, 1), width, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(new Bitmap(1, 1), new Size(width, 1)));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(width, 1, PixelFormat.Format16bppArgb1555));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(width, 1, 0, PixelFormat.Format16bppArgb1555, IntPtr.Zero));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(ushort.MaxValue * 513)]
        [InlineData(int.MaxValue)]
        public void Ctor_InvalidHeight_ThrowsArgumentException(int height)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, height));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, height, Graphics.FromImage(new Bitmap(1, 1))));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(new Bitmap(1, 1), 1, height));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(new Bitmap(1, 1), new Size(1, height)));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, height, PixelFormat.Format16bppArgb1555));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, height, 0, PixelFormat.Format16bppArgb1555, IntPtr.Zero));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Undefined - 1)]
        [InlineData(PixelFormat.Undefined)]
        [InlineData(PixelFormat.Gdi - 1)]
        [InlineData(PixelFormat.DontCare)]
        [InlineData(PixelFormat.Max)]
        [InlineData(PixelFormat.Indexed)]
        [InlineData(PixelFormat.Gdi)]
        [InlineData(PixelFormat.Alpha)]
        [InlineData(PixelFormat.PAlpha)]
        [InlineData(PixelFormat.Extended)]
        [InlineData(PixelFormat.Canonical)]
        public void Ctor_InvalidPixelFormat_ThrowsArgumentException(PixelFormat format)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, 1, format));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, 1, 0, format, IntPtr.Zero));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_InvalidScan0_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(1, 1, 0, PixelFormat.Format16bppArgb1555, (IntPtr)10));
        }

        public static IEnumerable<object[]> Image_TestData()
        {
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format16bppRgb555), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format16bppRgb565), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format24bppRgb), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format32bppArgb), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format32bppPArgb), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format48bppRgb), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format64bppArgb), 1, 1 };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format64bppPArgb), 1, 1 };

            yield return new object[] { new Bitmap(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico")), 16, 16 };
            yield return new object[] { new Bitmap(Helpers.GetTestBitmapPath("16x16_nonindexed_24bit.png")), 32, 48 };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Image_TestData))]
        public void Ctor_Width_Height_Graphics(Bitmap image, int width, int height)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            using (var bitmap = new Bitmap(width, height, graphics))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(PixelFormat.Format32bppPArgb, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullGraphics_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("g", "Value of 'null' is not valid for 'g'.", () => new Bitmap(1, 1, null));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Image()
        {
            using (var image = new Bitmap(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico")))
            using (var bitmap = new Bitmap(image))
            {
                Assert.Equal(16, bitmap.Width);
                Assert.Equal(16, bitmap.Height);
                Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullImageWithoutSize_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new Bitmap((Image)null));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Image_TestData))]
        public void Ctor_Image_Width_Height(Image image, int width, int height)
        {
            using (var bitmap = new Bitmap(image, width, height))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Image_TestData))]
        public void Ctor_Size(Image image, int width, int height)
        {
            using (var bitmap = new Bitmap(image, new Size(width, height)))
            {
                Assert.Equal(width, bitmap.Width);
                Assert.Equal(height, bitmap.Height);
                Assert.Equal(PixelFormat.Format32bppArgb, bitmap.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, bitmap.RawFormat);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_NullImageWithSize_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("image", () => new Bitmap(null, new Size(1, 2)));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new Bitmap(null, 1, 2));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_DisposedImage_ThrowsArgumentException()
        {
            var image = new Bitmap(1, 1);
            image.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(image));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(image, 1, 1));
            AssertExtensions.Throws<ArgumentException>(null, () => new Bitmap(image, new Size(1, 1)));
        }

        public static IEnumerable<object[]> Clone_TestData()
        {
            yield return new object[] { new Bitmap(3, 3, PixelFormat.Format32bppArgb), new Rectangle(0, 0, 3, 3), PixelFormat.Format32bppArgb };
            yield return new object[] { new Bitmap(3, 3, PixelFormat.Format32bppArgb), new Rectangle(0, 0, 3, 3), PixelFormat.Format24bppRgb };
            yield return new object[] { new Bitmap(3, 3, PixelFormat.Format1bppIndexed), new Rectangle(1, 1, 1, 1), PixelFormat.Format64bppArgb };
            yield return new object[] { new Bitmap(3, 3, PixelFormat.Format64bppPArgb), new Rectangle(1, 1, 1, 1), PixelFormat.Format16bppRgb565 };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Clone_TestData))]
        public void Clone_Rectangle_ReturnsExpected(Bitmap bitmap, Rectangle rectangle, PixelFormat targetFormat)
        {
            try
            {
                using (Bitmap clone = bitmap.Clone(rectangle, targetFormat))
                {
                    Assert.NotSame(bitmap, clone);

                    Assert.Equal(rectangle.Width, clone.Width);
                    Assert.Equal(rectangle.Height, clone.Height);
                    Assert.Equal(targetFormat, clone.PixelFormat);
                    Assert.Equal(bitmap.RawFormat, clone.RawFormat);

                    for (int x = 0; x < rectangle.Width; x++)
                    {
                        for (int y = 0; y < rectangle.Height; y++)
                        {
                            Color expectedColor = bitmap.GetPixel(rectangle.X + x, rectangle.Y + y);
                            if (Image.IsAlphaPixelFormat(targetFormat))
                            {
                                Assert.Equal(expectedColor, clone.GetPixel(x, y));
                            }
                            else
                            {
                                Assert.Equal(Color.FromArgb(255, expectedColor.R, expectedColor.G, expectedColor.B), clone.GetPixel(x, y));
                            }
                        }
                    }
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Clone_TestData))]
        public void Clone_RectangleF_ReturnsExpected(Bitmap bitmap, Rectangle rectangle, PixelFormat format)
        {
            try
            {
                using (Bitmap clone = bitmap.Clone((RectangleF)rectangle, format))
                {
                    Assert.NotSame(bitmap, clone);

                    Assert.Equal(rectangle.Width, clone.Width);
                    Assert.Equal(rectangle.Height, clone.Height);
                    Assert.Equal(format, clone.PixelFormat);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0, 1)]
        [InlineData(1, 0)]
        public void Clone_ZeroWidthOrHeightRect_ThrowsArgumentException(int width, int height)
        {
            using (var bitmap = new Bitmap(3, 3))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Clone(new Rectangle(0, 0, width, height), bitmap.PixelFormat));
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Clone(new RectangleF(0, 0, width, height), bitmap.PixelFormat));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0, 0, 4, 1)]
        [InlineData(0, 0, 1, 4)]
        [InlineData(0, 0, 1, 4)]
        [InlineData(1, 0, 3, 1)]
        [InlineData(0, 1, 1, 3)]
        [InlineData(4, 1, 1, 1)]
        [InlineData(1, 4, 1, 1)]
        public void Clone_InvalidRect_ThrowsOutOfMemoryException(int x, int y, int width, int height)
        {
            using (var bitmap = new Bitmap(3, 3))
            {
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new Rectangle(x, y, width, height), bitmap.PixelFormat));
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new RectangleF(x, y, width, height), bitmap.PixelFormat));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Max)]
        [InlineData(PixelFormat.Indexed)]
        [InlineData(PixelFormat.Gdi)]
        [InlineData(PixelFormat.Alpha)]
        [InlineData(PixelFormat.PAlpha)]
        [InlineData(PixelFormat.Extended)]
        [InlineData(PixelFormat.Format16bppGrayScale)]
        [InlineData(PixelFormat.Canonical)]
        public void Clone_InvalidPixelFormat_ThrowsOutOfMemoryException(PixelFormat format)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new Rectangle(0, 0, 1, 1), format));
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new RectangleF(0, 0, 1, 1), format));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_GrayscaleFormat_ThrowsOutOfMemoryException()
        {
            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format16bppGrayScale))
            {
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new Rectangle(0, 0, 1, 1), PixelFormat.Format32bppArgb));
                Assert.Throws<OutOfMemoryException>(() => bitmap.Clone(new RectangleF(0, 0, 1, 1), PixelFormat.Format32bppArgb));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_ValidBitmap_Success()
        {
            using (var bitmap = new Bitmap(1, 1))
            using (Bitmap clone = Assert.IsType<Bitmap>(bitmap.Clone()))
            {
                Assert.NotSame(bitmap, clone);
                Assert.Equal(1, clone.Width);
                Assert.Equal(1, clone.Height);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Clone());
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Clone(new Rectangle(0, 0, 1, 1), PixelFormat.Format32bppArgb));
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Clone(new RectangleF(0, 0, 1, 1), PixelFormat.Format32bppArgb));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetFrameCount_NewBitmap_ReturnsZero()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                Assert.Equal(1, bitmap.GetFrameCount(FrameDimension.Page));
                Assert.Equal(1, bitmap.GetFrameCount(FrameDimension.Resolution));
                Assert.Equal(1, bitmap.GetFrameCount(FrameDimension.Time));
                Assert.Equal(1, bitmap.GetFrameCount(new FrameDimension(Guid.Empty)));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetFrameCount_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetFrameCount(FrameDimension.Page));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void SelectActiveFrame_InvalidFrameIndex_ThrowsArgumentException(int index)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                Assert.Equal(0, bitmap.SelectActiveFrame(FrameDimension.Page, index));
                Assert.Equal(0, bitmap.SelectActiveFrame(FrameDimension.Resolution, index));
                Assert.Equal(0, bitmap.SelectActiveFrame(FrameDimension.Time, index));
                Assert.Equal(0, bitmap.SelectActiveFrame(new FrameDimension(Guid.Empty), index));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void SelectActiveFrame_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SelectActiveFrame(FrameDimension.Page, 0));
        }

        public static IEnumerable<object[]> GetPixel_TestData()
        {
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format1bppIndexed), 0, 0, Color.FromArgb(0, 0, 0) };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format4bppIndexed), 0, 0, Color.FromArgb(0, 0, 0) };
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format8bppIndexed), 0, 0, Color.FromArgb(0, 0, 0) };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), 0, 0, Color.FromArgb(0, 0, 0) };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), 99, 99, Color.FromArgb(0, 0, 0) };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(GetPixel_TestData))]
        public void GetPixel_ValidPixelFormat_Success(Bitmap bitmap, int x, int y, Color color)
        {
            try
            {
                Assert.Equal(color, bitmap.GetPixel(x, y));
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(1)]
        public void GetPixel_InvalidX_ThrowsArgumentOutOfRangeException(int x)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("x", () => bitmap.GetPixel(x, 0));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(1)]
        public void GetPixel_InvalidY_ThrowsArgumentOutOfRangeException(int y)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("y", () => bitmap.GetPixel(0, y));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetPixel_GrayScalePixelFormat_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format16bppGrayScale))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetPixel_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetPixel(0, 0));
        }

        public static IEnumerable<object[]> GetHbitmap_TestData()
        {
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format32bppRgb), 1, 1 };
            yield return new object[] { new Bitmap(32, 32, PixelFormat.Format32bppArgb), 32, 32 };
            yield return new object[] { new Bitmap(512, 512, PixelFormat.Format16bppRgb555), 512, 512 };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(GetHbitmap_TestData))]
        public void GetHbitmap_FromHbitmap_ReturnsExpected(Bitmap bitmap, int width, int height)
        {
            IntPtr handle = bitmap.GetHbitmap();
            try
            {
                Assert.NotEqual(IntPtr.Zero, handle);

                using (Bitmap result = Image.FromHbitmap(handle))
                {
                    Assert.Equal(width, result.Width);
                    Assert.Equal(height, result.Height);
                    Assert.Equal(PixelFormat.Format32bppRgb, result.PixelFormat);
                    Assert.Equal(ImageFormat.MemoryBmp, result.RawFormat);
                }
            }
            finally
            {
                bitmap.Dispose();
            }

            // Hbitmap survives original bitmap disposal.
            using (Bitmap result = Image.FromHbitmap(handle))
            {
                Assert.Equal(width, result.Width);
                Assert.Equal(height, result.Height);
                Assert.Equal(PixelFormat.Format32bppRgb, result.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, result.RawFormat);
            }

            // Hbitmap can be used multiple times.
            using (Bitmap result = Image.FromHbitmap(handle))
            {
                Assert.Equal(width, result.Width);
                Assert.Equal(height, result.Height);
                Assert.Equal(PixelFormat.Format32bppRgb, result.PixelFormat);
                Assert.Equal(ImageFormat.MemoryBmp, result.RawFormat);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(1, 1)]
        [InlineData(short.MaxValue, 1)]
        [InlineData(1, short.MaxValue)]
        public void GetHbitmap_Grayscale_ThrowsArgumentException(int width, int height)
        {
            using (var bitmap = new Bitmap(width, height, PixelFormat.Format16bppGrayScale))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetHbitmap());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetHbitmap_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetHbitmap());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHbitmap_InvalidHandle_ThrowsExternalException()
        {
            Assert.Throws<ExternalException>(() => Image.FromHbitmap(IntPtr.Zero));
            Assert.Throws<ExternalException>(() => Image.FromHbitmap((IntPtr)10));
        }

        public static IEnumerable<object[]> FromHicon_Icon_TestData()
        {
            yield return new object[] { new Icon(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico")), 16, 16 };
            yield return new object[] { new Icon(Helpers.GetTestBitmapPath("32x32_one_entry_4bit.ico")), 32, 32 };
            yield return new object[] { new Icon(Helpers.GetTestBitmapPath("64x64_one_entry_8bit.ico")), 64, 64 };
            yield return new object[] { new Icon(Helpers.GetTestBitmapPath("96x96_one_entry_8bit.ico")), 96, 96 };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(FromHicon_Icon_TestData))]
        public void FromHicon_IconHandle_ReturnsExpected(Icon icon, int width, int height)
        {
            IntPtr handle;
            try
            {
                using (Bitmap bitmap = GetHicon_FromHicon_ReturnsExpected(icon.Handle, width, height))
                {
                    handle = bitmap.GetHicon();
                }
            }
            finally
            {
                icon.Dispose();
            }

            // Hicon survives bitmap and icon disposal.
            GetHicon_FromHicon_ReturnsExpected(handle, width, height);
        }

        public static IEnumerable<object[]> FromHicon_TestData()
        {
            yield return new object[] { new Bitmap(1, 1, PixelFormat.Format32bppRgb).GetHicon(), 1, 1 };
            yield return new object[] { new Bitmap(32, 32, PixelFormat.Format32bppRgb).GetHicon(), 32, 32 };
            yield return new object[] { new Bitmap(512, 512, PixelFormat.Format16bppRgb555).GetHicon(), 512, 512 };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(FromHicon_TestData))]
        public Bitmap GetHicon_FromHicon_ReturnsExpected(IntPtr handle, int width, int height)
        {
            Assert.NotEqual(IntPtr.Zero, handle);

            Bitmap result = Bitmap.FromHicon(handle);
            Assert.Equal(width, result.Width);
            Assert.Equal(height, result.Height);
            Assert.Equal(PixelFormat.Format32bppArgb, result.PixelFormat);
            Assert.Equal(ImageFormat.MemoryBmp, result.RawFormat);
            Assert.Equal(335888, result.Flags);
            Assert.Empty(result.Palette.Entries);

            return result;
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetHicon_Grayscale_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format16bppGrayScale))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetHicon());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetHicon_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.GetHicon());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHicon_InvalidHandle_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromHicon(IntPtr.Zero));
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromHicon((IntPtr)10));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHicon_1bppIcon_ThrowsArgumentException()
        {
            using (var icon = new Icon(Helpers.GetTestBitmapPath("48x48_one_entry_1bit.ico")))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromHicon(icon.Handle));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromResource_InvalidHandle_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromResource(IntPtr.Zero, "Name"));
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromResource((IntPtr)10, "Name"));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromResource_InvalidBitmapName_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromResource(IntPtr.Zero, "Name"));
            AssertExtensions.Throws<ArgumentException>(null, () => Bitmap.FromResource((IntPtr)10, "Name"));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_NoColorWithMatches_SetsMatchingPixelsToTransparent()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        if (y % 2 == 0)
                        {
                            bitmap.SetPixel(x, y, Color.LightGray);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.Red);
                        }
                    }
                }

                bitmap.MakeTransparent();
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        if (y % 2 == 0)
                        {
                            Assert.Equal(Color.FromArgb(255, 211, 211, 211), bitmap.GetPixel(x, y));
                        }
                        else
                        {
                            Assert.Equal(Color.FromArgb(0, 0, 0, 0), bitmap.GetPixel(x, y));
                        }
                    }
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_CustomColorExists_SetsMatchingPixelsToTransparent()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        if (y % 2 == 0)
                        {
                            bitmap.SetPixel(x, y, Color.Blue);
                        }
                        else
                        {
                            bitmap.SetPixel(x, y, Color.Red);
                        }
                    }
                }

                bitmap.MakeTransparent(Color.Blue);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        if (y % 2 == 0)
                        {
                            Assert.Equal(Color.FromArgb(0, 0, 0, 0), bitmap.GetPixel(x, y));
                        }
                        else
                        {
                            Assert.Equal(Color.FromArgb(255, 255, 0, 0), bitmap.GetPixel(x, y));
                        }
                    }
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_CustomColorDoesntExist_DoesNothing()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        bitmap.SetPixel(x, y, Color.Blue);
                    }
                }

                bitmap.MakeTransparent(Color.Red);
                for (int x = 0; x < bitmap.Width; x++)
                {
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        Assert.Equal(Color.FromArgb(255, 0, 0, 255), bitmap.GetPixel(x, y));
                    }
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.MakeTransparent());
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.MakeTransparent(Color.Red));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ActiveIssue(21886, TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_GrayscalePixelFormat_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format16bppGrayScale))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.MakeTransparent());
                Assert.Throws<ExternalException>(() => bitmap.MakeTransparent(Color.Red));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MakeTransparent_Icon_ThrowsInvalidOperationException()
        {
            using (var bitmap = new Bitmap(Helpers.GetTestBitmapPath("16x16_one_entry_4bit.ico")))
            {
                Assert.Throws<InvalidOperationException>(() => bitmap.MakeTransparent(Color.Red));
            }
        }

        public static IEnumerable<object[]> SetPixel_TestData()
        {
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), 0, 0, Color.FromArgb(255, 128, 128, 128) };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), 99, 99, Color.FromArgb(255, 128, 128, 128) };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(SetPixel_TestData))]
        public void SetPixel_ValidPixelFormat_Success(Bitmap bitmap, int x, int y, Color color)
        {
            bitmap.SetPixel(x, y, color);
            Assert.Equal(color, bitmap.GetPixel(x, y));
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Format1bppIndexed)]
        [InlineData(PixelFormat.Format4bppIndexed)]
        [InlineData(PixelFormat.Format8bppIndexed)]
        public void SetPixel_IndexedPixelFormat_ThrowsInvalidOperationException(PixelFormat format)
        {
            using (var bitmap = new Bitmap(1, 1, format))
            {
                Assert.Throws<InvalidOperationException>(() => bitmap.SetPixel(0, 0, Color.Red));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(1)]
        public void SetPixel_InvalidX_ThrowsArgumentOutOfRangeException(int x)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("x", () => bitmap.SetPixel(x, 0, Color.Red));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(1)]
        public void SetPixel_InvalidY_ThrowsArgumentOutOfRangeException(int y)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("y", () => bitmap.SetPixel(0, y, Color.Red));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void SetPixel_GrayScalePixelFormat_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format16bppGrayScale))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SetPixel(0, 0, Color.Red));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void SetPixel_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SetPixel(0, 0, Color.Red));
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(1, 1)]
        [InlineData(float.PositiveInfinity, float.PositiveInfinity)]
        [InlineData(float.MaxValue, float.MaxValue)]
        public void SetResolution_ValidDpi_Success(float xDpi, float yDpi)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                bitmap.SetResolution(xDpi, yDpi);
            }
        }
        
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(float.NaN)]
        [InlineData(float.NegativeInfinity)]
        public void SetResolution_InvalidXDpi_ThrowsArgumentException(float xDpi)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SetResolution(xDpi, 1));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(float.NaN)]
        [InlineData(float.NegativeInfinity)]
        public void SetResolution_InvalidYDpi_ThrowsArgumentException(float yDpi)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SetResolution(1, yDpi));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void SetResolution_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.SetResolution(1, 1));
        }

        public static IEnumerable<object[]> LockBits_NotUnix_TestData()
        {
            Bitmap bitmap() => new Bitmap(2, 2, PixelFormat.Format32bppArgb);
            yield return new object[] { bitmap(), new Rectangle(1, 1, 1,1), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb, 8, 1 };
            yield return new object[] { bitmap(), new Rectangle(1, 1, 1, 1), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb, 8, 3 };
            yield return new object[] { bitmap(), new Rectangle(1, 1, 1, 1), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb, 8, 2 };

            yield return new object[] { bitmap(), new Rectangle(1, 1, 1, 1), ImageLockMode.ReadOnly - 1, PixelFormat.Format32bppArgb, 8, 0 };

            yield return new object[] { bitmap(), new Rectangle(0, 0, 2, 2), ImageLockMode.WriteOnly, PixelFormat.Format16bppGrayScale, 4, 65538 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed, 100, 65537 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed, 100, 65539 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed, 100, 65538 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb, 300, 65539 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb, 300, 65538 };
        }

        public static IEnumerable<object[]> LockBits_TestData()
        {
            Bitmap bitmap() => new Bitmap(2, 2, PixelFormat.Format32bppArgb);
            yield return new object[] { bitmap(), new Rectangle(0, 0, 2, 2), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb, 8, 1 };
            yield return new object[] { bitmap(), new Rectangle(0, 0, 2, 2), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb, 8, 3 };
            yield return new object[] { bitmap(), new Rectangle(0, 0, 2, 2), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb, 8, 2 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb, 400, 1 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb, 400, 3 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb, 400, 2 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb, 300, 65537 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb, 300, 65539 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format32bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb, 300, 65538 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb, 300, 1 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb, 300, 3 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb, 300, 2 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb, 400, 65537 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb, 400, 65539 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format24bppRgb), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb, 400, 65538 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb, 300, 65537 };

            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed, 100, 1 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed, 100, 3 };
            yield return new object[] { new Bitmap(100, 100, PixelFormat.Format8bppIndexed), new Rectangle(0, 0, 100, 100), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed, 100, 2 };


            yield return new object[] { new Bitmap(184, 184, PixelFormat.Format1bppIndexed), new Rectangle(0, 0, 184, 184), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed, 24, 1 };
            yield return new object[] { new Bitmap(184, 184, PixelFormat.Format1bppIndexed), new Rectangle(0, 0, 184, 184), ImageLockMode.ReadWrite, PixelFormat.Format1bppIndexed, 24, 3 };
            yield return new object[] { new Bitmap(184, 184, PixelFormat.Format1bppIndexed), new Rectangle(0, 0, 184, 184), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed, 24, 2 };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(LockBits_TestData))]
        public void LockBits_Invoke_Success(Bitmap bitmap, Rectangle rectangle, ImageLockMode lockMode, PixelFormat pixelFormat, int expectedStride, int expectedReserved)
        {
            Do_LockBits_Invoke_Success(bitmap, rectangle, lockMode, pixelFormat, expectedStride, expectedReserved);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(LockBits_NotUnix_TestData))]
        public void LockBits_Invoke_Success_NotUnix(Bitmap bitmap, Rectangle rectangle, ImageLockMode lockMode, PixelFormat pixelFormat, int expectedStride, int expectedReserved)
        {
            Do_LockBits_Invoke_Success(bitmap, rectangle, lockMode, pixelFormat, expectedStride, expectedReserved);
        }

        private void Do_LockBits_Invoke_Success(Bitmap bitmap, Rectangle rectangle, ImageLockMode lockMode, PixelFormat pixelFormat, int expectedStride, int expectedReserved)
        {
            try
            {
                BitmapData data = bitmap.LockBits(rectangle, lockMode, pixelFormat);
                Assert.Equal(pixelFormat, data.PixelFormat);
                Assert.Equal(rectangle.Width, data.Width);
                Assert.Equal(rectangle.Height, data.Height);
                Assert.Equal(expectedStride, data.Stride);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // "Reserved" is documented as "Reserved. Do not use.", so it's not clear whether we actually need to test this in any unit tests.
                    // Additionally, the values are not consistent accross Windows (GDI+) and Unix (libgdiplus)
                    Assert.Equal(expectedReserved, data.Reserved);
                }

                // Locking with 16bppGrayscale succeeds, but the data can't be unlocked.
                if (pixelFormat == PixelFormat.Format16bppGrayScale)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => bitmap.UnlockBits(data));
                }
                else
                {
                    bitmap.UnlockBits(data);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void LockBits_NullBitmapData_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(Rectangle.Empty, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb, null));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(2, 0, 1, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 2, 1, 1)]
        [InlineData(0, 0, -1, 1)]
        [InlineData(0, 0, 3, 1)]
        [InlineData(0, 0, 1, -1)]
        [InlineData(0, 0, 1, 3)]
        [InlineData(1, 0, 2, 1)]
        [InlineData(1, 1, 1, 0)]
        [InlineData(1, 1, 0, 1)]
        public void LockBits_InvalidRect_ThrowsArgumentException(int x, int y, int width, int height)
        {
            using (var bitmap = new Bitmap(2, 2))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(x, y, width, height), ImageLockMode.ReadOnly, bitmap.PixelFormat));

                var bitmapData = new BitmapData();
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(x, y, width, height), ImageLockMode.ReadOnly, bitmap.PixelFormat, bitmapData));
                Assert.Equal(IntPtr.Zero, bitmapData.Scan0);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.DontCare)]
        [InlineData(PixelFormat.Max)]
        [InlineData(PixelFormat.Indexed)]
        [InlineData(PixelFormat.Gdi)]
        [InlineData(PixelFormat.Alpha)]
        [InlineData(PixelFormat.PAlpha)]
        [InlineData(PixelFormat.Extended)]
        [InlineData(PixelFormat.Canonical)]
        public void LockBits_InvalidPixelFormat_ThrowsArgumentException(PixelFormat format)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                foreach (ImageLockMode lockMode in Enum.GetValues(typeof(ImageLockMode)))
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), lockMode, format));

                    var bitmapData = new BitmapData();
                    AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), lockMode, format, bitmapData));
                    Assert.Equal(IntPtr.Zero, bitmapData.Scan0);
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void LockBits_ReadOnlyGrayscale_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale));
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format16bppGrayScale, new BitmapData()));

                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadWrite, PixelFormat.Format16bppGrayScale));
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadWrite, PixelFormat.Format16bppGrayScale, new BitmapData()));

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.WriteOnly, PixelFormat.Format16bppGrayScale);
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.UnlockBits(data));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData((ImageLockMode)(-1))]
        [InlineData(ImageLockMode.UserInputBuffer + 1)]
        [InlineData(ImageLockMode.UserInputBuffer)]
        public void LockBits_InvalidLockMode_ThrowsArgumentException(ImageLockMode lockMode)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), lockMode, bitmap.PixelFormat));

                var bitmapData = new BitmapData();
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), lockMode, bitmap.PixelFormat, bitmapData));
                Assert.Equal(IntPtr.Zero, bitmapData.Scan0);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void LockBits_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb));

            var bitmapData = new BitmapData();
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb, bitmapData));
            Assert.Equal(IntPtr.Zero, bitmapData.Scan0);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void LockBits_AlreadyLocked_ThrowsInvalidOperationException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat);

                Assert.Throws<InvalidOperationException>(() => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat));
                Assert.Throws<InvalidOperationException>(() => bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat, new BitmapData()));

                Assert.Throws<InvalidOperationException>(() => bitmap.LockBits(new Rectangle(1, 1, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat));
                Assert.Throws<InvalidOperationException>(() => bitmap.LockBits(new Rectangle(1, 1, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat, new BitmapData()));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0, -1)]
        [InlineData(0, 2)]
        [InlineData(1, 2)]
        public void UnlockBits_InvalidHeightWidth_Nop(int offset, int invalidParameter)
        {
            using (var bitmap = new Bitmap(2, 2))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(offset, offset, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                data.Height = invalidParameter;
                data.Width = invalidParameter;

                bitmap.UnlockBits(data);
            }
        }
        
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void UnlockBits_Scan0Zero_Nop()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                data.Scan0 = IntPtr.Zero;

                bitmap.UnlockBits(data);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Indexed)]
        [InlineData(PixelFormat.Gdi)]
        public void UnlockBits_InvalidPixelFormat_Nop(PixelFormat format)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                data.PixelFormat = format;

                bitmap.UnlockBits(data);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void UnlockBits_NullBitmapData_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => bitmap.UnlockBits(null));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void UnlockBits_NotLocked_ThrowsExternalException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                Assert.Throws<ExternalException>(() => bitmap.UnlockBits(new BitmapData()));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void UnlockBits_AlreadyUnlocked_ThrowsExternalException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                bitmap.UnlockBits(data);

                Assert.Throws<ExternalException>(() => bitmap.UnlockBits(new BitmapData()));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void UnlockBits_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.UnlockBits(new BitmapData()));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Size_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Width);
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Height);
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Size);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Format16bppArgb1555)]
        [InlineData(PixelFormat.Format16bppRgb555)]
        [InlineData(PixelFormat.Format16bppRgb565)]
        [InlineData(PixelFormat.Format32bppArgb)]
        [InlineData(PixelFormat.Format32bppPArgb)]
        [InlineData(PixelFormat.Format32bppRgb)]
        [InlineData(PixelFormat.Format24bppRgb)]
        public void CustomPixelFormat_GetPixels_ReturnsExpected(PixelFormat format)
        {
            bool alpha = Image.IsAlphaPixelFormat(format);
            int size = Image.GetPixelFormatSize(format) / 8 * 2;
            using (var bitmap = new Bitmap(2, 1, format))
            {
                Color a = Color.FromArgb(128, 64, 32, 16);
                Color b = Color.FromArgb(192, 96, 48, 24);
                bitmap.SetPixel(0, 0, a);
                bitmap.SetPixel(1, 0, b);
                Color c = bitmap.GetPixel(0, 0);
                Color d = bitmap.GetPixel(1, 0);
                if (size == 4)
                {
                    Assert.Equal(255, c.A);
                    Assert.Equal(66, c.R);
                    if (format == PixelFormat.Format16bppRgb565)
                    {
                        Assert.Equal(32, c.G);
                    }
                    else
                    {
                        Assert.Equal(33, c.G);
                    }
                    Assert.Equal(16, c.B);

                    Assert.Equal(255, d.A);
                    Assert.Equal(99, d.R);
                    if (format == PixelFormat.Format16bppRgb565)
                    {
                        Assert.Equal(48, d.G);
                    }
                    else
                    {
                        Assert.Equal(49, d.G);
                    }
                    Assert.Equal(24, d.B);
                }
                else if (alpha)
                {
                    if (format == PixelFormat.Format32bppPArgb)
                    {
                        Assert.Equal(a.A, c.A);
                        Assert.Equal(a.R - 1, c.R);
                        Assert.Equal(a.G - 1, c.G);
                        Assert.Equal(a.B - 1, c.B);

                        Assert.Equal(b.A, d.A);
                        Assert.Equal(b.R - 1, d.R);
                        Assert.Equal(b.G - 1, d.G);
                        Assert.Equal(b.B - 1, d.B);
                    }
                    else
                    {
                        Assert.Equal(a, c);
                        Assert.Equal(b, d);
                    }
                }
                else
                {
                    Assert.Equal(Color.FromArgb(255, 64, 32, 16), c);
                    Assert.Equal(Color.FromArgb(255, 96, 48, 24), d);
                }
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, 2, 1), ImageLockMode.ReadOnly, format);
                try
                {
                    byte[] data = new byte[size];
                    Marshal.Copy(bitmapData.Scan0, data, 0, size);
                    if (format == PixelFormat.Format32bppPArgb)
                    {
                        Assert.Equal(Math.Ceiling((float)c.B * c.A / 255), data[0]);
                        Assert.Equal(Math.Ceiling((float)c.G * c.A / 255), data[1]);
                        Assert.Equal(Math.Ceiling((float)c.R * c.A / 255), data[2]);
                        Assert.Equal(c.A, data[3]);
                        Assert.Equal(Math.Ceiling((float)d.B * d.A / 255), data[4]);
                        Assert.Equal(Math.Ceiling((float)d.G * d.A / 255), data[5]);
                        Assert.Equal(Math.Ceiling((float)d.R * d.A / 255), data[6]);
                        Assert.Equal(d.A, data[7]);
                    }
                    else if (size == 4)
                    {
                        switch (format)
                        {
                            case PixelFormat.Format16bppRgb565:
                                Assert.Equal(2, data[0]);
                                Assert.Equal(65, data[1]);
                                Assert.Equal(131, data[2]);
                                Assert.Equal(97, data[3]);
                                break;
                            case PixelFormat.Format16bppArgb1555:
                                Assert.Equal(130, data[0]);
                                Assert.Equal(160, data[1]);
                                Assert.Equal(195, data[2]);
                                Assert.Equal(176, data[3]);
                                break;
                            case PixelFormat.Format16bppRgb555:
                                Assert.Equal(130, data[0]);
                                Assert.Equal(32, data[1]);
                                Assert.Equal(195, data[2]);
                                Assert.Equal(48, data[3]);
                                break;
                        }
                    }
                    else
                    {
                        int n = 0;
                        Assert.Equal(c.B, data[n++]);
                        Assert.Equal(c.G, data[n++]);
                        Assert.Equal(c.R, data[n++]);
                        if (size % 4 == 0)
                        {
                            if (format == PixelFormat.Format32bppRgb)
                            {
                                Assert.Equal(128, data[n++]);
                            }
                            else
                            {
                                Assert.Equal(c.A, data[n++]);
                            }
                        }
                        Assert.Equal(d.B, data[n++]);
                        Assert.Equal(d.G, data[n++]);
                        Assert.Equal(d.R, data[n++]);
                        if (size % 4 == 0)
                        {
                            if (format == PixelFormat.Format32bppRgb)
                            {
                                Assert.Equal(192, data[n++]);
                            }
                            else
                            {
                                Assert.Equal(d.A, data[n++]);
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
            }
        }

        public static IEnumerable<object[]> Palette_TestData()
        {
            yield return new object[] { PixelFormat.Format1bppIndexed, new int[] { -16777216, -1 } };
            yield return new object[] { PixelFormat.Format4bppIndexed, new int[] { -16777216, -8388608, -16744448, -8355840, -16777088, -8388480, -16744320, -8355712, -4144960, -65536, -16711936, -256, -16776961, -65281, -16711681, -1, } };
            yield return new object[] { PixelFormat.Format8bppIndexed, new int[] { -16777216, -8388608, -16744448, -8355840, -16777088, -8388480, -16744320, -8355712, -4144960, -65536, -16711936, -256, -16776961, -65281, -16711681, -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, -16777216, -16777165, -16777114, -16777063, -16777012, -16776961, -16764160, -16764109, -16764058, -16764007, -16763956, -16763905, -16751104, -16751053, -16751002, -16750951, -16750900, -16750849, -16738048, -16737997, -16737946, -16737895, -16737844, -16737793, -16724992, -16724941, -16724890, -16724839, -16724788, -16724737, -16711936, -16711885, -16711834, -16711783, -16711732, -16711681, -13434880, -13434829, -13434778, -13434727, -13434676, -13434625, -13421824, -13421773, -13421722, -13421671, -13421620, -13421569, -13408768, -13408717, -13408666, -13408615, -13408564, -13408513, -13395712, -13395661, -13395610, -13395559, -13395508, -13395457, -13382656, -13382605, -13382554, -13382503, -13382452, -13382401, -13369600, -13369549, -13369498, -13369447, -13369396, -13369345, -10092544, -10092493, -10092442, -10092391, -10092340, -10092289, -10079488, -10079437, -10079386, -10079335, -10079284, -10079233, -10066432, -10066381, -10066330, -10066279, -10066228, -10066177, -10053376, -10053325, -10053274, -10053223, -10053172, -10053121, -10040320, -10040269, -10040218, -10040167, -10040116, -10040065, -10027264, -10027213, -10027162, -10027111, -10027060, -10027009, -6750208, -6750157, -6750106, -6750055, -6750004, -6749953, -6737152, -6737101, -6737050, -6736999, -6736948, -6736897, -6724096, -6724045, -6723994, -6723943, -6723892, -6723841, -6711040, -6710989, -6710938, -6710887, -6710836, -6710785, -6697984, -6697933, -6697882, -6697831, -6697780, -6697729, -6684928, -6684877, -6684826, -6684775, -6684724, -6684673, -3407872, -3407821, -3407770, -3407719, -3407668, -3407617, -3394816, -3394765, -3394714, -3394663, -3394612, -3394561, -3381760, -3381709, -3381658, -3381607, -3381556, -3381505, -3368704, -3368653, -3368602, -3368551, -3368500, -3368449, -3355648, -3355597, -3355546, -3355495, -3355444, -3355393, -3342592, -3342541, -3342490, -3342439, -3342388, -3342337, -65536, -65485, -65434, -65383, -65332, -65281, -52480, -52429, -52378, -52327, -52276, -52225, -39424, -39373, -39322, -39271, -39220, -39169, -26368, -26317, -26266, -26215, -26164, -26113, -13312, -13261, -13210, -13159, -13108, -13057, -256, -205, -154, -103, -52, -1 } };
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Palette_TestData))]
        public void Palette_Get_ReturnsExpected(PixelFormat pixelFormat, int[] expectedEntries)
        {
            using (var bitmap = new Bitmap(1, 1, pixelFormat))
            {
                Assert.Equal(expectedEntries, bitmap.Palette.Entries.Select(c => c.ToArgb()));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Palette_SetNull_ThrowsNullReferenceException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                Assert.Throws<NullReferenceException>(() => bitmap.Palette = null);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Palette_Disposed_ThrowsArgumentException()
        {
            var bitmap = new Bitmap(1, 1);
            ColorPalette palette = bitmap.Palette;
            bitmap.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Palette);
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Palette = palette);
            AssertExtensions.Throws<ArgumentException>(null, () => bitmap.Size);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void LockBits_Marshalling_Success()
        {
            Color red = Color.FromArgb(Color.Red.ToArgb());
            Color blue = Color.FromArgb(Color.Blue.ToArgb());

            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format32bppRgb))
            {
                bitmap.SetPixel(0, 0, red);
                Color pixelColor = bitmap.GetPixel(0, 0);
                Assert.Equal(red, pixelColor);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    int pixelValue = Marshal.ReadByte(data.Scan0, 0);
                    pixelValue |= Marshal.ReadByte(data.Scan0, 1) << 8;
                    pixelValue |= Marshal.ReadByte(data.Scan0, 2) << 16;
                    pixelValue |= Marshal.ReadByte(data.Scan0, 3) << 24;

                    pixelColor = Color.FromArgb(pixelValue);
                    // Disregard alpha information in the test
                    pixelColor = Color.FromArgb(red.A, pixelColor.R, pixelColor.G, pixelColor.B);
                    Assert.Equal(red, pixelColor);

                    // write blue but we're locked in read-only...
                    Marshal.WriteByte(data.Scan0, 0, blue.B);
                    Marshal.WriteByte(data.Scan0, 1, blue.G);
                    Marshal.WriteByte(data.Scan0, 2, blue.R);
                    Marshal.WriteByte(data.Scan0, 3, blue.A);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                    pixelColor = bitmap.GetPixel(0, 0);
                    // Disregard alpha information in the test
                    pixelColor = Color.FromArgb(red.A, pixelColor.R, pixelColor.G, pixelColor.B);
                    // ...so we still read red after unlocking
                    Assert.Equal(red, pixelColor);
                }

                data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                try
                {
                    // write blue
                    Marshal.WriteByte(data.Scan0, 0, blue.B);
                    Marshal.WriteByte(data.Scan0, 1, blue.G);
                    Marshal.WriteByte(data.Scan0, 2, blue.R);
                    Marshal.WriteByte(data.Scan0, 3, blue.A);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                    pixelColor = bitmap.GetPixel(0, 0);
                    // Disregard alpha information in the test
                    pixelColor = Color.FromArgb(blue.A, pixelColor.R, pixelColor.G, pixelColor.B);
                    // read blue
                    Assert.Equal(blue, pixelColor);
                }
            }

            using (var bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                bitmap.SetPixel(0, 0, red);

                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    byte b = Marshal.ReadByte(data.Scan0, 0);
                    byte g = Marshal.ReadByte(data.Scan0, 1);
                    byte r = Marshal.ReadByte(data.Scan0, 2);
                    Assert.Equal(red, Color.FromArgb(red.A, r, g, b));
                    // write blue but we're locked in read-only...
                    Marshal.WriteByte(data.Scan0, 0, blue.B);
                    Marshal.WriteByte(data.Scan0, 1, blue.G);
                    Marshal.WriteByte(data.Scan0, 2, blue.R);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                    // ...so we still read red after unlocking
                    Assert.Equal(red, bitmap.GetPixel(0, 0));
                }

                data = bitmap.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                try
                {
                    // write blue
                    Marshal.WriteByte(data.Scan0, 0, blue.B);
                    Marshal.WriteByte(data.Scan0, 1, blue.G);
                    Marshal.WriteByte(data.Scan0, 2, blue.R);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                    // read blue
                    Assert.Equal(blue, bitmap.GetPixel(0, 0));
                }
            }
        }
    }
}
