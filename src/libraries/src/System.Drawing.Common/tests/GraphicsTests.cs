// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Xunit;

namespace System.Drawing.Tests
{
    public class GraphicsTests
    {
        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHdc_FromHdc_Roundtrips()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.NotEqual(IntPtr.Zero, hdc);

                    using (Graphics graphicsCopy = Graphics.FromHdc(hdc))
                    {
                        VerifyGraphics(graphicsCopy, graphicsCopy.VisibleClipBounds);
                    }
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHdc_SameImage_ReturnsSame()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics1 = Graphics.FromImage(bitmap))
            using (Graphics graphics2 = Graphics.FromImage(bitmap))
            {
                try
                {
                    Assert.Equal(graphics1.GetHdc(), graphics2.GetHdc());
                }
                finally
                {
                    graphics1.ReleaseHdc();
                    graphics2.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHdc_NotReleased_ThrowsInvalidOperationException()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.GetHdc());
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHdc_Disposed_ThrowsObjectDisposedException()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.GetHdc());
            }
        }

        public static IEnumerable<object[]> FromHdc_TestData()
        {
            yield return new object[] { Helpers.GetDC(IntPtr.Zero) };
            yield return new object[] { Helpers.GetWindowDC(IntPtr.Zero) };

            IntPtr foregroundWindow = Helpers.GetForegroundWindow();
            yield return new object[] { Helpers.GetDC(foregroundWindow) };
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdc_ValidHdc_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdc(hdc))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdc_ValidHdcWithContext_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdc(hdc, IntPtr.Zero))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdcInternal_GetDC_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdcInternal(hdc))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHdc_ZeroHdc_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("hdc", () => Graphics.FromHdc(IntPtr.Zero));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHdcInternal_ZeroHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdcInternal(IntPtr.Zero));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHdc_ZeroHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdc(IntPtr.Zero, (IntPtr)10));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHdc_InvalidHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHwnd((IntPtr)10));
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHwndInternal((IntPtr)10));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_ValidHdc_ResetsHdc()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                graphics.ReleaseHdc();
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(hdc));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdc(hdc);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdcInternal(hdc);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_NoSuchHdc_ResetsHdc()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                graphics.ReleaseHdc((IntPtr)10);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal((IntPtr)10));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdcInternal((IntPtr)10);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc((IntPtr)10));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_OtherGraphicsHdc_Success()
        {
            using (var bitmap1 = new Bitmap(10, 10))
            using (var bitmap2 = new Bitmap(10, 10))
            using (Graphics graphics1 = Graphics.FromImage(bitmap1))
            using (Graphics graphics2 = Graphics.FromImage(bitmap2))
            {
                IntPtr hdc1 = graphics1.GetHdc();
                IntPtr hdc2 = graphics2.GetHdc();
                Assert.NotEqual(hdc1, hdc2);

                graphics1.ReleaseHdc(hdc2);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics1.ReleaseHdc(hdc1));

                graphics2.ReleaseHdc(hdc1);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics2.ReleaseHdc(hdc2));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_NoHdc_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(IntPtr.Zero));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(IntPtr.Zero));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_Disposed_ThrowsObjectDisposedException()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(IntPtr.Zero));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(IntPtr.Zero));
            }
        }

        public static IEnumerable<object[]> Hwnd_TestData()
        {
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { Helpers.GetForegroundWindow() };
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Hwnd_TestData))]
        public void FromHwnd_ValidHwnd_ReturnsExpected(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                Rectangle expected = Helpers.GetHWndRect(hWnd);
                VerifyGraphics(graphics, expected);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Hwnd_TestData))]
        public void FromHwndInternal_ValidHwnd_ReturnsExpected(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                Rectangle expected = Helpers.GetHWndRect(hWnd);
                VerifyGraphics(graphics, expected);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHwnd_InvalidHwnd_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdc((IntPtr)10));
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdcInternal((IntPtr)10));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(PixelFormat.Format16bppRgb555)]
        [InlineData(PixelFormat.Format16bppRgb565)]
        [InlineData(PixelFormat.Format24bppRgb)]
        [InlineData(PixelFormat.Format32bppArgb)]
        [InlineData(PixelFormat.Format32bppPArgb)]
        [InlineData(PixelFormat.Format32bppRgb)]
        [InlineData(PixelFormat.Format48bppRgb)]
        [InlineData(PixelFormat.Format64bppArgb)]
        [InlineData(PixelFormat.Format64bppPArgb)]
        public void FromImage_Bitmap_Success(PixelFormat format)
        {
            using (var image = new Bitmap(10, 10, format))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                VerifyGraphics(graphics, new Rectangle(Point.Empty, image.Size));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromImage_NullImage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("image", () => Graphics.FromImage(null));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(PixelFormat.Format1bppIndexed)]
        [InlineData(PixelFormat.Format4bppIndexed)]
        [InlineData(PixelFormat.Format8bppIndexed)]
        public void FromImage_IndexedImage_ThrowsException(PixelFormat format)
        {
            using (var image = new Bitmap(10, 10, format))
            {
                Assert.Throws<Exception>(() => Graphics.FromImage(image));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromImage_DisposedImage_ThrowsArgumentException()
        {
            var image = new Bitmap(10, 10);
            image.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => Graphics.FromImage(image));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromImage_Metafile_ThrowsOutOfMemoryException()
        {
            using (var image = new Metafile(Helpers.GetTestBitmapPath("telescope_01.wmf")))
            {
                Assert.Throws<OutOfMemoryException>(() => Graphics.FromImage(image));
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(PixelFormat.Format16bppArgb1555)]
        [InlineData(PixelFormat.Format16bppGrayScale)]
        public void FromImage_Invalid16BitFormat_ThrowsOutOfMemoryException(PixelFormat format)
        {
            using (var image = new Bitmap(10, 10, format))
            {
                Assert.Throws<OutOfMemoryException>(() => Graphics.FromImage(image));
            }
        }

        public static IEnumerable<object[]> CompositingMode_TestData()
        {
            yield return new object[] { CompositingMode.SourceCopy, Color.FromArgb(160, 255, 255, 255) };
            yield return new object[] { CompositingMode.SourceOver, Color.FromArgb(220, 185, 185, 185) };
        }

        [Theory]
        [MemberData(nameof(CompositingMode_TestData))]
        public void CompositingMode_Set_GetReturnsExpected(CompositingMode mode, Color expectedOverlap)
        {
            Color transparentBlack = Color.FromArgb(160, 0, 0, 0);
            Color transparentWhite = Color.FromArgb(160, 255, 255, 255);

            using (var transparentBlackBrush = new SolidBrush(transparentBlack))
            using (var transparentWhiteBrush = new SolidBrush(transparentWhite))
            using (var image = new Bitmap(3, 3))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var targetImage = new Bitmap(3, 3))
            using (Graphics targetGraphics = Graphics.FromImage(targetImage))
            {
                graphics.CompositingMode = mode;
                Assert.Equal(mode, graphics.CompositingMode);

                graphics.FillRectangle(transparentBlackBrush, new Rectangle(0, 0, 2, 2));
                graphics.FillRectangle(transparentWhiteBrush, new Rectangle(1, 1, 2, 2));

                targetGraphics.DrawImage(image, Point.Empty);
                Helpers.VerifyBitmap(targetImage, new Color[][]
                {
                    new Color[] { transparentBlack,   transparentBlack, Helpers.EmptyColor },
                    new Color[] { transparentBlack,   expectedOverlap,  transparentWhite   },
                    new Color[] { Helpers.EmptyColor, transparentWhite, transparentWhite   }
                });
            }
        }

        [Theory]
        [InlineData(CompositingMode.SourceOver - 1)]
        [InlineData(CompositingMode.SourceCopy + 1)]
        public void CompositingMode_SetInvalid_ThrowsInvalidEnumArgumentException(CompositingMode compositingMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.CompositingMode = compositingMode);
            }
        }

        [Fact]
        public void CompositingMode_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.CompositingMode);
                    Assert.Throws<InvalidOperationException>(() => graphics.CompositingMode = CompositingMode.SourceCopy);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void CompositingMode_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CompositingMode);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CompositingMode = CompositingMode.SourceCopy);
            }
        }

        public static IEnumerable<object[]> CompositingQuality_TestData()
        {
            Color transparentBlack = Color.FromArgb(160, 0, 0, 0);
            Color transparentWhite = Color.FromArgb(160, 255, 255, 255);
            var basicExpectedColors = new Color[][]
            {
                new Color[] { transparentBlack,   transparentBlack,                   Helpers.EmptyColor },
                new Color[] { transparentBlack,   Color.FromArgb(220, 185, 185, 185), transparentWhite   },
                new Color[] { Helpers.EmptyColor, transparentWhite,                   transparentWhite   }
            };

            yield return new object[] { CompositingQuality.AssumeLinear, basicExpectedColors };
            yield return new object[] { CompositingQuality.Default, basicExpectedColors };
            yield return new object[] { CompositingQuality.HighSpeed, basicExpectedColors };
            yield return new object[] { CompositingQuality.Invalid, basicExpectedColors };

            var gammaCorrectedColors = new Color[][]
            {
                new Color[] { Color.FromArgb(159, 0, 0, 0), Color.FromArgb(159, 0, 0, 0),       Color.FromArgb(0, 0, 0, 0)         },
                new Color[] { Color.FromArgb(159, 0, 0, 0), Color.FromArgb(219, 222, 222, 222), Color.FromArgb(159, 255, 255, 255) },
                new Color[] { Color.FromArgb(0, 0, 0, 0),   Color.FromArgb(159, 255, 255, 255), Color.FromArgb(159, 255, 255, 255) }
            };
            yield return new object[] { CompositingQuality.GammaCorrected, gammaCorrectedColors };
            yield return new object[] { CompositingQuality.HighQuality, gammaCorrectedColors };
        }

        [Theory]
        [MemberData(nameof(CompositingQuality_TestData))]
        public void CompositingQuality_Set_GetReturnsExpected(CompositingQuality quality, Color[][] expectedIntersectionColor)
        {
            Color transparentBlack = Color.FromArgb(160, 0, 0, 0);
            Color transparentWhite = Color.FromArgb(160, 255, 255, 255);

            using (var transparentBlackBrush = new SolidBrush(transparentBlack))
            using (var transparentWhiteBrush = new SolidBrush(transparentWhite))
            using (var image = new Bitmap(3, 3))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.CompositingQuality = quality;
                Assert.Equal(quality, graphics.CompositingQuality);

                graphics.FillRectangle(transparentBlackBrush, new Rectangle(0, 0, 2, 2));
                graphics.FillRectangle(transparentWhiteBrush, new Rectangle(1, 1, 2, 2));

                Helpers.VerifyBitmap(image, expectedIntersectionColor);
            }
        }

        [Theory]
        [InlineData(CompositingQuality.Invalid - 1)]
        [InlineData(CompositingQuality.AssumeLinear + 1)]
        public void CompositingQuality_SetInvalid_ThrowsInvalidEnumArgumentException(CompositingQuality compositingQuality)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.CompositingQuality = compositingQuality);
            }
        }

        [Fact]
        public void CompositingQuality_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.CompositingQuality);
                    Assert.Throws<InvalidOperationException>(() => graphics.CompositingQuality = CompositingQuality.AssumeLinear);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void CompositingQuality_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CompositingQuality);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CompositingQuality = CompositingQuality.AssumeLinear);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Dispose_MultipleTimesWithoutHdc_Success()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                var graphics = Graphics.FromImage(bitmap);
                graphics.Dispose();
                graphics.Dispose();

                // The backing image is not disposed.
                Assert.Equal(10, bitmap.Height);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Dispose_MultipleTimesWithHdc_Success()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                var graphics = Graphics.FromImage(bitmap);
                graphics.GetHdc();

                graphics.Dispose();
                graphics.Dispose();

                // The backing image is not disposed.
                Assert.Equal(10, bitmap.Height);
            }
        }

        [Fact]
        public void DpiX_GetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DpiX);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void DpiX_GetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DpiX);
            }
        }

        [Fact]
        public void DpiY_GetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DpiX);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void DpiY_GetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DpiX);
            }
        }

        [Theory]
        [InlineData(FlushIntention.Flush)]
        [InlineData(FlushIntention.Sync)]
        [InlineData(FlushIntention.Flush - 1)] // Not in the range of valid values of FlushIntention.
        [InlineData(FlushIntention.Sync - 1)] // Not in the range of valid values of FlushIntention.
        public void Flush_MultipleTimes_Success(FlushIntention intention)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                if (intention == FlushIntention.Flush)
                {
                    graphics.Flush();
                    graphics.Flush();
                }

                graphics.Flush(intention);
                graphics.Flush(intention);
            }
        }

        [Fact]
        public void Flush_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.Flush());
                    Assert.Throws<InvalidOperationException>(() => graphics.Flush(FlushIntention.Sync));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void Flush_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Flush());
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Flush(FlushIntention.Flush));
            }
        }

        [Theory]
        [InlineData(InterpolationMode.Bicubic, InterpolationMode.Bicubic)]
        [InlineData(InterpolationMode.Bilinear, InterpolationMode.Bilinear)]
        [InlineData(InterpolationMode.Default, InterpolationMode.Bilinear)]
        [InlineData(InterpolationMode.High, InterpolationMode.HighQualityBicubic)]
        [InlineData(InterpolationMode.HighQualityBicubic, InterpolationMode.HighQualityBicubic)]
        [InlineData(InterpolationMode.HighQualityBilinear, InterpolationMode.HighQualityBilinear)]
        [InlineData(InterpolationMode.Low, InterpolationMode.Bilinear)]
        [InlineData(InterpolationMode.NearestNeighbor, InterpolationMode.NearestNeighbor)]
        public void InterpolationMode_SetValid_GetReturnsExpected(InterpolationMode interpolationMode, InterpolationMode expectedInterpolationMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.InterpolationMode = interpolationMode;
                Assert.Equal(expectedInterpolationMode, graphics.InterpolationMode);
            }
        }

        [Theory]
        [InlineData(InterpolationMode.Invalid - 1)]
        [InlineData(InterpolationMode.HighQualityBicubic + 1)]
        public void InterpolationMode_SetInvalid_ThrowsInvalidEnumArgumentException(InterpolationMode interpolationMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.InterpolationMode = interpolationMode);
            }
        }

        [Fact]
        public void InterpolationMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.InterpolationMode = InterpolationMode.Invalid);
            }
        }

        [Fact]
        public void InterpolationMode_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.InterpolationMode);
                    Assert.Throws<InvalidOperationException>(() => graphics.InterpolationMode = InterpolationMode.HighQualityBilinear);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void InterpolationMode_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.InterpolationMode);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.InterpolationMode = InterpolationMode.HighQualityBilinear);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1000000032)]
        [InlineData(float.NaN)]
        public void PageScale_SetValid_GetReturnsExpected(float pageScale)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.PageScale = pageScale;
                Assert.Equal(pageScale, graphics.PageScale);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1000000033)]
        [InlineData(float.NegativeInfinity)]
        [InlineData(float.PositiveInfinity)]
        public void PageScale_SetInvalid_ThrowsArgumentException(float pageScale)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageScale = pageScale);
            }
        }

        [Fact]
        public void PageScale_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.PageScale);
                    Assert.Throws<InvalidOperationException>(() => graphics.PageScale = 10);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void PageScale_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageScale);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageScale = 10);
            }
        }

        [Theory]
        [InlineData(GraphicsUnit.Display)]
        [InlineData(GraphicsUnit.Document)]
        [InlineData(GraphicsUnit.Inch)]
        [InlineData(GraphicsUnit.Millimeter)]
        [InlineData(GraphicsUnit.Pixel)]
        [InlineData(GraphicsUnit.Point)]
        public void PageUnit_SetValid_GetReturnsExpected(GraphicsUnit pageUnit)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.PageUnit = pageUnit;
                Assert.Equal(pageUnit, graphics.PageUnit);
            }
        }

        [Theory]
        [InlineData(GraphicsUnit.World - 1)]
        [InlineData(GraphicsUnit.Millimeter + 1)]
        public void PageUnit_SetInvalid_ThrowsInvalidEnumArgumentException(GraphicsUnit pageUnit)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.PageUnit = pageUnit);
            }
        }

        [Fact]
        public void PageUnit_SetWorld_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageUnit = GraphicsUnit.World);
            }
        }

        [Fact]
        public void PageUnit_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.PageUnit);
                    Assert.Throws<InvalidOperationException>(() => graphics.PageUnit = GraphicsUnit.Document);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void PageUnit_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageUnit);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageUnit = GraphicsUnit.Document);
            }
        }

        [Theory]
        [InlineData(PixelOffsetMode.Default)]
        [InlineData(PixelOffsetMode.Half)]
        [InlineData(PixelOffsetMode.HighQuality)]
        [InlineData(PixelOffsetMode.HighSpeed)]
        [InlineData(PixelOffsetMode.None)]
        public void PixelOffsetMode_SetValid_GetReturnsExpected(PixelOffsetMode pixelOffsetMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.PixelOffsetMode = pixelOffsetMode;
                Assert.Equal(pixelOffsetMode, graphics.PixelOffsetMode);
            }
        }

        [Theory]
        [InlineData(PixelOffsetMode.Invalid - 1)]
        [InlineData(PixelOffsetMode.Half + 1)]
        public void PixelOffsetMode_SetInvalid_ThrowsInvalidEnumArgumentException(PixelOffsetMode pixelOffsetMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.PixelOffsetMode = pixelOffsetMode);
            }
        }

        [Fact]
        public void PixelOffsetMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PixelOffsetMode = PixelOffsetMode.Invalid);
            }
        }

        [Fact]
        public void PixelOffsetMode_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.PixelOffsetMode);
                    Assert.Throws<InvalidOperationException>(() => graphics.PixelOffsetMode = PixelOffsetMode.Default);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void PixelOffsetMode_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PixelOffsetMode);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PixelOffsetMode = PixelOffsetMode.Default);
            }
        }

        public static IEnumerable<object[]> RenderingOrigin_TestData()
        {
            Color empty = Color.FromArgb(255, 0, 0, 0);
            Color red = Color.FromArgb(Color.Red.ToArgb());

            yield return new object[]
            {
                new Point(0, 0),
                new Color[][]
                {
                    new Color[] { red, red,   red   },
                    new Color[] { red, empty, empty },
                    new Color[] { red, empty, empty }
                }
            };

            yield return new object[]
            {
                new Point(1, 1),
                new Color[][]
                {
                    new Color[] { empty, red, empty },
                    new Color[] { red,   red, red   },
                    new Color[] { empty, red, empty }
                }
            };

            var allEmpty = new Color[][]
            {
                new Color[] { empty, empty, empty },
                new Color[] { empty, empty, empty },
                new Color[] { empty, empty, empty }
            };

            yield return new object[] { new Point(-3, -3), allEmpty };
            yield return new object[] { new Point(3, 3), allEmpty };
        }

        [Theory]
        [MemberData(nameof(RenderingOrigin_TestData))]
        public void RenderingOrigin_SetToCustom_RendersExpected(Point renderingOrigin, Color[][] expectedRendering)
        {
            Color empty = Color.FromArgb(255, 0, 0, 0);
            Color red = Color.FromArgb(Color.Red.ToArgb());

            using (var image = new Bitmap(3, 3))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new HatchBrush(HatchStyle.Cross, red))
            {
                graphics.RenderingOrigin = renderingOrigin;
                Assert.Equal(renderingOrigin, graphics.RenderingOrigin);

                graphics.FillRectangle(brush, new Rectangle(0, 0, 3, 3));
                Helpers.VerifyBitmap(image, expectedRendering);
            }
        }

        [Fact]
        public void RenderingOrigin_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.RenderingOrigin);
                    Assert.Throws<InvalidOperationException>(() => graphics.RenderingOrigin = Point.Empty);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void RenderingOrigin_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.RenderingOrigin);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.RenderingOrigin = Point.Empty);
            }
        }

        [Theory]
        [InlineData(SmoothingMode.AntiAlias, SmoothingMode.AntiAlias)]
        [InlineData(SmoothingMode.Default, SmoothingMode.None)]
        [InlineData(SmoothingMode.HighQuality, SmoothingMode.AntiAlias)]
        [InlineData(SmoothingMode.HighSpeed, SmoothingMode.None)]
        [InlineData(SmoothingMode.None, SmoothingMode.None)]
        public void SmoothingMode_SetValid_GetReturnsExpected(SmoothingMode smoothingMode, SmoothingMode expectedSmoothingMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.SmoothingMode = smoothingMode;
                Assert.Equal(expectedSmoothingMode, graphics.SmoothingMode);
            }
        }

        [Theory]
        [InlineData(SmoothingMode.Invalid - 1)]
        [InlineData(SmoothingMode.AntiAlias + 1)]
        public void SmoothingMode_SetInvalid_ThrowsInvalidEnumArgumentException(SmoothingMode smoothingMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.SmoothingMode = smoothingMode);
            }
        }

        [Fact]
        public void SmoothingMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.SmoothingMode = SmoothingMode.Invalid);
            }
        }

        [Fact]
        public void SmoothingMode_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.SmoothingMode);
                    Assert.Throws<InvalidOperationException>(() => graphics.SmoothingMode = SmoothingMode.AntiAlias);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void SmoothingMode_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.SmoothingMode);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.SmoothingMode = SmoothingMode.AntiAlias);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(12)]
        public void TextContrast_SetValid_GetReturnsExpected(int textContrast)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.TextContrast = textContrast;
                Assert.Equal(textContrast, graphics.TextContrast);
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(13)]
        public void TextContrast_SetInvalid_ThrowsArgumentException(int textContrast)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TextContrast = textContrast);
            }
        }

        [Fact]
        public void TextContrast_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.TextContrast);
                    Assert.Throws<InvalidOperationException>(() => graphics.TextContrast = 5);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void TextContrast_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TextContrast);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TextContrast = 5);
            }
        }

        [Theory]
        [InlineData(TextRenderingHint.AntiAlias)]
        [InlineData(TextRenderingHint.AntiAliasGridFit)]
        [InlineData(TextRenderingHint.ClearTypeGridFit)]
        [InlineData(TextRenderingHint.SingleBitPerPixel)]
        [InlineData(TextRenderingHint.SingleBitPerPixelGridFit)]
        [InlineData(TextRenderingHint.SystemDefault)]
        public void TextRenderingHint_SetValid_GetReturnsExpected(TextRenderingHint textRenderingHint)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.TextRenderingHint = textRenderingHint;
                Assert.Equal(textRenderingHint, graphics.TextRenderingHint);
            }
        }

        [Theory]
        [InlineData(TextRenderingHint.SystemDefault - 1)]
        [InlineData(TextRenderingHint.ClearTypeGridFit + 1)]
        public void TextRenderingHint_SetInvalid_ThrowsInvalidEnumArgumentException(TextRenderingHint textRenderingHint)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => graphics.TextRenderingHint = textRenderingHint);
            }
        }

        [Fact]
        public void TextRenderingHint_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.TextRenderingHint);
                    Assert.Throws<InvalidOperationException>(() => graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void TextRenderingHint_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TextRenderingHint);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit);
            }
        }

        [Fact]
        public void Transform_SetValid_GetReturnsExpected()
        {
            Color empty = Helpers.EmptyColor;
            Color red = Color.FromArgb(Color.Red.ToArgb());

            using (var image = new Bitmap(5, 5))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(red))
            using (var matrix = new Matrix())
            {
                matrix.Scale(1f / 3, 2);
                matrix.Translate(2, 1);
                matrix.Rotate(270);

                graphics.Transform = matrix;
                graphics.FillRectangle(brush, new Rectangle(0, 0, 3, 2));
                Helpers.VerifyBitmap(image, new Color[][]
                {
                    new Color[] { empty, red,   empty, empty, empty },
                    new Color[] { empty, red,   empty, empty, empty },
                    new Color[] { empty, empty, empty, empty, empty },
                    new Color[] { empty, empty, empty, empty, empty },
                    new Color[] { empty, empty, empty, empty, empty }
                });
            }
        }

        [Fact]
        public void Transform_SetNull_ThrowsNullReferenceException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.Throws<NullReferenceException>(() => graphics.Transform = null);
            }
        }

        [Fact]
        public void Transform_SetDisposedMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var matrix = new Matrix();
                matrix.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Transform = matrix);
            }
        }

        [Fact]
        public void Transform_SetNonInvertibleMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(5, 5))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix(123, 24, 82, 16, 47, 30))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Transform = matrix);
            }
        }

        [Fact]
        public void Transform_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix())
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.Transform);
                    Assert.Throws<InvalidOperationException>(() => graphics.Transform = matrix);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [Fact]
        public void Transform_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Transform);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Transform = matrix);
            }
        }

        private static void VerifyGraphics(Graphics graphics, RectangleF expectedVisibleClipBounds)
        {
            Assert.NotNull(graphics.Clip);
            Assert.Equal(new RectangleF(-4194304, -4194304, 8388608, 8388608), graphics.ClipBounds);
            Assert.Equal(CompositingMode.SourceOver, graphics.CompositingMode);
            Assert.Equal(CompositingQuality.Default, graphics.CompositingQuality);
            Assert.Equal(96, graphics.DpiX);
            Assert.Equal(96, graphics.DpiY);
            Assert.Equal(InterpolationMode.Bilinear, graphics.InterpolationMode);
            Assert.False(graphics.IsClipEmpty);
            Assert.False(graphics.IsVisibleClipEmpty);
            Assert.Equal(1, graphics.PageScale);
            Assert.Equal(GraphicsUnit.Display, graphics.PageUnit);
            Assert.Equal(PixelOffsetMode.Default, graphics.PixelOffsetMode);
            Assert.Equal(Point.Empty, graphics.RenderingOrigin);
            Assert.Equal(SmoothingMode.None, graphics.SmoothingMode);
            Assert.Equal(4, graphics.TextContrast);
            Assert.Equal(TextRenderingHint.SystemDefault, graphics.TextRenderingHint);
            Assert.Equal(new Matrix(), graphics.Transform);
            Assert.Equal(expectedVisibleClipBounds, graphics.VisibleClipBounds);
        }
    }
}
