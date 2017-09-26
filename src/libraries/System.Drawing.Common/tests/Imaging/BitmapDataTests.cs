// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class BitmapDataTests
    {
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Ctor_Default()
        {
            BitmapData bd = new BitmapData();
            Assert.Equal(0, bd.Height);
            Assert.Equal(0, bd.Width);
            Assert.Equal(0, bd.Reserved);
            Assert.Equal(IntPtr.Zero, bd.Scan0);
            Assert.Equal(0, bd.Stride);
            Assert.Equal((PixelFormat)0, bd.PixelFormat);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void Height_SetValid_ReturnsExpected(int value)
        {
            BitmapData bd = new BitmapData();
            bd.Height = value;
            Assert.Equal(value, bd.Height);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void Width_SetValid_ReturnsExpected(int value)
        {
            BitmapData bd = new BitmapData();
            bd.Width = value;
            Assert.Equal(value, bd.Width);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void Reserved_SetValid_ReturnsExpected(int value)
        {
            BitmapData bd = new BitmapData();
            bd.Reserved = value;
            Assert.Equal(value, bd.Reserved);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void Scan0_SetValid_ReturnsExpected(int value)
        {
            BitmapData bd = new BitmapData();
            bd.Scan0 = new IntPtr(value);
            Assert.Equal(new IntPtr(value), bd.Scan0);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(int.MaxValue)]
        [InlineData(0)]
        [InlineData(int.MinValue)]
        public void Stride_SetValid_ReturnsExpected(int value)
        {
            BitmapData bd = new BitmapData();
            bd.Stride = value;
            Assert.Equal(value, bd.Stride);
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.DontCare)]
        [InlineData(PixelFormat.Max)]
        [InlineData(PixelFormat.Indexed)]
        [InlineData(PixelFormat.Gdi)]
        [InlineData(PixelFormat.Format16bppRgb555)]
        [InlineData(PixelFormat.Format16bppRgb565)]
        [InlineData(PixelFormat.Format24bppRgb)]
        [InlineData(PixelFormat.Format32bppRgb)]
        [InlineData(PixelFormat.Format1bppIndexed)]
        [InlineData(PixelFormat.Format4bppIndexed)]
        [InlineData(PixelFormat.Format8bppIndexed)]
        [InlineData(PixelFormat.Alpha)]
        [InlineData(PixelFormat.Format16bppArgb1555)]
        [InlineData(PixelFormat.PAlpha)]
        [InlineData(PixelFormat.Format32bppPArgb)]
        [InlineData(PixelFormat.Extended)]
        [InlineData(PixelFormat.Format16bppGrayScale)]
        [InlineData(PixelFormat.Format48bppRgb)]
        [InlineData(PixelFormat.Format64bppPArgb)]
        [InlineData(PixelFormat.Canonical)]
        [InlineData(PixelFormat.Format32bppArgb)]
        [InlineData(PixelFormat.Format64bppArgb)]
        public void PixelFormat_SetValid_ReturnsExpected(PixelFormat pixelFormat)
        {
            BitmapData bd = new BitmapData();
            bd.PixelFormat = pixelFormat;
            Assert.Equal(pixelFormat, bd.PixelFormat);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void PixelFormat_SetInvalid_ThrowsInvalidEnumException()
        {
            BitmapData bd = new BitmapData();
            Assert.ThrowsAny<ArgumentException>(() => bd.PixelFormat = (PixelFormat)(-1));
        }
    }
}
