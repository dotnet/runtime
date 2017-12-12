// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Drawing.Tests
{
    public class GraphicsTests
    {
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdc_ValidHdc_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdc(hdc))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdc_ValidHdcWithContext_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdc(hdc, IntPtr.Zero))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(FromHdc_TestData))]
        public void FromHdcInternal_GetDC_ReturnsExpected(IntPtr hdc)
        {
            using (Graphics graphics = Graphics.FromHdcInternal(hdc))
            {
                Rectangle expected = Helpers.GetWindowDCRect(hdc);
                VerifyGraphics(graphics, expected);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHdc_ZeroHdc_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("hdc", () => Graphics.FromHdc(IntPtr.Zero));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHdcInternal_ZeroHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdcInternal(IntPtr.Zero));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHdc_ZeroHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdc(IntPtr.Zero, (IntPtr)10));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHdc_InvalidHdc_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHwnd((IntPtr)10));
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHwndInternal((IntPtr)10));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Hwnd_TestData))]
        public void FromHwnd_ValidHwnd_ReturnsExpected(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                Rectangle expected = Helpers.GetHWndRect(hWnd);
                VerifyGraphics(graphics, expected);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(Hwnd_TestData))]
        public void FromHwndInternal_ValidHwnd_ReturnsExpected(IntPtr hWnd)
        {
            using (Graphics graphics = Graphics.FromHwnd(hWnd))
            {
                Rectangle expected = Helpers.GetHWndRect(hWnd);
                VerifyGraphics(graphics, expected);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromHwnd_InvalidHwnd_ThrowsOutOfMemoryException()
        {
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdc((IntPtr)10));
            Assert.Throws<OutOfMemoryException>(() => Graphics.FromHdcInternal((IntPtr)10));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromImage_NullImage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("image", () => Graphics.FromImage(null));
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelFormat.Format1bppIndexed)]
        [InlineData(PixelFormat.Format4bppIndexed)]
        [InlineData(PixelFormat.Format8bppIndexed)]
        public void FromImage_IndexedImage_ThrowsException(PixelFormat format)
        {
            using (var image = new Bitmap(10, 10, format))
            {
                Exception exception = AssertExtensions.Throws<ArgumentException,Exception>(() => Graphics.FromImage(image));
                if (exception is ArgumentException argumentException)
                    Assert.Equal("image", argumentException.ParamName);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromImage_DisposedImage_ThrowsArgumentException()
        {
            var image = new Bitmap(10, 10);
            image.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => Graphics.FromImage(image));
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void FromImage_Metafile_ThrowsOutOfMemoryException()
        {
            using (var image = new Metafile(Helpers.GetTestBitmapPath("telescope_01.wmf")))
            {
                Assert.Throws<OutOfMemoryException>(() => Graphics.FromImage(image));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CompositingMode.SourceOver - 1)]
        [InlineData(CompositingMode.SourceCopy + 1)]
        public void CompositingMode_SetInvalid_ThrowsInvalidEnumArgumentException(CompositingMode compositingMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.CompositingMode = compositingMode);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CompositingQuality.Invalid - 1)]
        [InlineData(CompositingQuality.AssumeLinear + 1)]
        public void CompositingQuality_SetInvalid_ThrowsInvalidEnumArgumentException(CompositingQuality compositingQuality)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.CompositingQuality = compositingQuality);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DpiX_GetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DpiX);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DpiY_GetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DpiX);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(InterpolationMode.Invalid - 1)]
        [InlineData(InterpolationMode.HighQualityBicubic + 1)]
        public void InterpolationMode_SetInvalid_ThrowsInvalidEnumArgumentException(InterpolationMode interpolationMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.InterpolationMode = interpolationMode);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void InterpolationMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.InterpolationMode = InterpolationMode.Invalid);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(GraphicsUnit.World - 1)]
        [InlineData(GraphicsUnit.Millimeter + 1)]
        public void PageUnit_SetInvalid_ThrowsInvalidEnumArgumentException(GraphicsUnit pageUnit)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.PageUnit = pageUnit);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void PageUnit_SetWorld_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PageUnit = GraphicsUnit.World);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(PixelOffsetMode.Invalid - 1)]
        [InlineData(PixelOffsetMode.Half + 1)]
        public void PixelOffsetMode_SetInvalid_ThrowsInvalidEnumArgumentException(PixelOffsetMode pixelOffsetMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.PixelOffsetMode = pixelOffsetMode);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void PixelOffsetMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.PixelOffsetMode = PixelOffsetMode.Invalid);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(SmoothingMode.Invalid - 1)]
        [InlineData(SmoothingMode.AntiAlias + 1)]
        public void SmoothingMode_SetInvalid_ThrowsInvalidEnumArgumentException(SmoothingMode smoothingMode)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.SmoothingMode = smoothingMode);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void SmoothingMode_SetToInvalid_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.SmoothingMode = SmoothingMode.Invalid);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(TextRenderingHint.SystemDefault - 1)]
        [InlineData(TextRenderingHint.ClearTypeGridFit + 1)]
        public void TextRenderingHint_SetInvalid_ThrowsInvalidEnumArgumentException(TextRenderingHint textRenderingHint)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.TextRenderingHint = textRenderingHint);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Transform_SetNull_ThrowsNullReferenceException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.Throws<NullReferenceException>(() => graphics.Transform = null);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Transform_SetNonInvertibleMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(5, 5))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix(123, 24, 82, 16, 47, 30))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Transform = matrix);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
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

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ResetTransform_Invoke_SetsTransformToIdentity()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Assert.False(graphics.Transform.IsIdentity);

                graphics.ResetTransform();
                Assert.True(graphics.Transform.IsIdentity);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ResetTransform_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.ResetTransform());
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ResetTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ResetTransform());
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_NoOrder_Success()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            using (var matrix = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Multiply(matrix);

                graphics.MultiplyTransform(matrix);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(MatrixOrder.Prepend)]
        [InlineData(MatrixOrder.Append)]
        public void MultiplyTransform_Order_Success(MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            using (var matrix = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Multiply(matrix, order);

                graphics.MultiplyTransform(matrix, order);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_NullMatrix_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => graphics.MultiplyTransform(null));
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => graphics.MultiplyTransform(null, MatrixOrder.Append));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_DisposedMatrix_Nop()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix transform = brush.Transform;

            var matrix = new Matrix();
            matrix.Dispose();

            brush.MultiplyTransform(matrix);
            brush.MultiplyTransform(matrix, MatrixOrder.Append);

            Assert.Equal(transform, brush.Transform);
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_NonInvertibleMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix(123, 24, 82, 16, 47, 30))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.MultiplyTransform(matrix));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.MultiplyTransform(matrix, MatrixOrder.Append));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void MultiplyTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.MultiplyTransform(matrix, order));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var matrix = new Matrix())
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.MultiplyTransform(matrix));
                    Assert.Throws<InvalidOperationException>(() => graphics.MultiplyTransform(matrix, MatrixOrder.Append));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void MultiplyTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.MultiplyTransform(matrix));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.MultiplyTransform(matrix, MatrixOrder.Prepend));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void TranslateTransform_NoOrder_Success(float dx, float dy)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Translate(dx, dy);

                graphics.TranslateTransform(dx, dy);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(1, 1, MatrixOrder.Prepend)]
        [InlineData(1, 1, MatrixOrder.Append)]
        [InlineData(0, 0, MatrixOrder.Prepend)]
        [InlineData(0, 0, MatrixOrder.Append)]
        [InlineData(-1, -1, MatrixOrder.Prepend)]
        [InlineData(-1, -1, MatrixOrder.Append)]
        public void TranslateTransform_Order_Success(float dx, float dy, MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Translate(dx, dy, order);

                graphics.TranslateTransform(dx, dy, order);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void TranslateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TranslateTransform(0, 0, order));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TranslateTransform_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.TranslateTransform(0, 0));
                    Assert.Throws<InvalidOperationException>(() => graphics.TranslateTransform(0, 0, MatrixOrder.Append));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TranslateTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TranslateTransform(0, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TranslateTransform(0, 0, MatrixOrder.Append));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1, -2)]
        [InlineData(1, 2)]
        public void ScaleTransform_NoOrder_Success(float sx, float sy)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Scale(sx, sy);

                graphics.ScaleTransform(sx, sy);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(1, 1, MatrixOrder.Prepend)]
        [InlineData(1, 1, MatrixOrder.Append)]
        [InlineData(-1, -1, MatrixOrder.Prepend)]
        [InlineData(-1, -1, MatrixOrder.Append)]
        public void ScaleTransform_Order_Success(float sx, float sy, MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Scale(sx, sy, order);

                graphics.ScaleTransform(sx, sy, order);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        public void ScaleTransform_ZeroZero_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ScaleTransform(0, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ScaleTransform(0, 0, MatrixOrder.Append));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void ScaleTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ScaleTransform(0, 0, order));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ScaleTransform_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.ScaleTransform(0, 0));
                    Assert.Throws<InvalidOperationException>(() => graphics.ScaleTransform(0, 0, MatrixOrder.Append));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void ScaleTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ScaleTransform(0, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.ScaleTransform(0, 0, MatrixOrder.Append));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(360)]
        public void RotateTransform_NoOrder_Success(float angle)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Rotate(angle);

                graphics.RotateTransform(angle);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(1, MatrixOrder.Prepend)]
        [InlineData(1, MatrixOrder.Append)]
        [InlineData(0, MatrixOrder.Prepend)]
        [InlineData(360, MatrixOrder.Append)]
        [InlineData(-1, MatrixOrder.Prepend)]
        [InlineData(-1, MatrixOrder.Append)]
        public void RotateTransform_Order_Success(float angle, MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;
                Matrix expectedTransform = graphics.Transform;
                expectedTransform.Rotate(angle, order);

                graphics.RotateTransform(angle, order);
                Assert.Equal(expectedTransform, graphics.Transform);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void RotateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.RotateTransform(0, order));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void RotateTransform_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.RotateTransform(0));
                    Assert.Throws<InvalidOperationException>(() => graphics.RotateTransform(0, MatrixOrder.Append));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void RotateTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.RotateTransform(0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.RotateTransform(0, MatrixOrder.Append));
            }
        }

        public static IEnumerable<object[]> CopyFromScreen_TestData()
        {
            yield return new object[] { 0, 0, 0, 0, new Size(0, 0) };
            yield return new object[] { -1, -1, 0, 0, new Size(1, 1) };
            yield return new object[] { int.MaxValue, int.MaxValue, 0, 0, new Size(1, 1) };
            yield return new object[] { int.MaxValue, int.MaxValue, 0, 0, new Size(1, 1) };
            yield return new object[] { 0, 0, -1, -1, new Size(1, 1) };
            yield return new object[] { 0, 0, int.MaxValue, int.MaxValue, new Size(1, 1) };
            yield return new object[] { 0, 0, 0, 0, new Size(-1, -1) };
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(CopyFromScreen_TestData))]
        public void CopyFromScreen_OutOfRange_DoesNotAffectGraphics(int sourceX, int sourceY, int destinationX, int destinationY, Size size)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Color plum = Color.FromArgb(Color.Plum.ToArgb());
                image.SetPixel(0, 0, plum);

                graphics.CopyFromScreen(sourceX, sourceY, destinationX, destinationY, size);
                graphics.CopyFromScreen(new Point(sourceX, sourceY), new Point(destinationX, destinationY), size);
                Assert.Equal(plum, image.GetPixel(0, 0));
            }
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0, 0, 0, 0, 10, 10)]
        [InlineData(0, 0, 0, 0, int.MaxValue, int.MaxValue)]
        [InlineData(1, 1, 2, 2, 3, 3)]
        public void CopyFromScreen_ValidRange_AffectsGraphics(int sourceX, int sourceY, int destinationX, int destinationY, int width, int height)
        {
            Size screenSize = Helpers.GetHWndRect(IntPtr.Zero).Size;

            Color color = Color.FromArgb(2, 3, 4);
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (SolidBrush brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, new Rectangle(0, 0, 10, 10));
                graphics.CopyFromScreen(sourceX, sourceY, destinationX, destinationY, new Size(width, height));

                Rectangle drawnRect = new Rectangle(destinationX, destinationY, width, height);
                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        Color imageColor = image.GetPixel(x, y);
                        if (!drawnRect.Contains(x, y))
                        {
                            Assert.Equal(color, imageColor);
                        }
                        else
                        {
                            Assert.NotEqual(color, imageColor);
                        }
                    }
                }
            }
        }

        public static IEnumerable<object[]> CopyPixelOperation_TestData()
        {
            yield return new object[] { CopyPixelOperation.NoMirrorBitmap };
            yield return new object[] { CopyPixelOperation.Blackness };
            yield return new object[] { CopyPixelOperation.NotSourceErase };
            yield return new object[] { CopyPixelOperation.NotSourceCopy };
            yield return new object[] { CopyPixelOperation.SourceErase };
            yield return new object[] { CopyPixelOperation.DestinationInvert };
            yield return new object[] { CopyPixelOperation.PatInvert };
            yield return new object[] { CopyPixelOperation.SourceInvert };
            yield return new object[] { CopyPixelOperation.SourceAnd };
            yield return new object[] { CopyPixelOperation.MergePaint };
            yield return new object[] { CopyPixelOperation.MergeCopy };
            yield return new object[] { CopyPixelOperation.SourceCopy };
            yield return new object[] { CopyPixelOperation.SourcePaint };
            yield return new object[] { CopyPixelOperation.SourceCopy };
            yield return new object[] { CopyPixelOperation.PatCopy };
            yield return new object[] { CopyPixelOperation.PatPaint };
            yield return new object[] { CopyPixelOperation.Whiteness };
            yield return new object[] { CopyPixelOperation.CaptureBlt };
            yield return new object[] { CopyPixelOperation.CaptureBlt };
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(CopyPixelOperation_TestData))]
        public void CopyFromScreen_IntsAndValidCopyPixelOperation_Success(CopyPixelOperation copyPixelOperation)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                // We don't know what the screen looks like at this point in time, so
                // just make sure that this doesn't fail.
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(1, 1), copyPixelOperation);
            }
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(CopyPixelOperation_TestData))]
        public void CopyFromScreen_PointsAndValidCopyPixelOperation_Success(CopyPixelOperation copyPixelOperation)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                // We don't know what the screen looks like at this point in time, so
                // just make sure that this doesn't fail.
                graphics.CopyFromScreen(Point.Empty, Point.Empty, new Size(1, 1), copyPixelOperation);
            }
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CopyPixelOperation.NoMirrorBitmap + 1)]
        [InlineData(CopyPixelOperation.Blackness - 1)]
        [InlineData(CopyPixelOperation.NotSourceErase - 1)]
        [InlineData(CopyPixelOperation.NotSourceCopy - 1)]
        [InlineData(CopyPixelOperation.SourceErase - 1)]
        [InlineData(CopyPixelOperation.DestinationInvert - 1)]
        [InlineData(CopyPixelOperation.PatInvert - 1)]
        [InlineData(CopyPixelOperation.SourceInvert - 1)]
        [InlineData(CopyPixelOperation.SourceAnd - 1)]
        [InlineData(CopyPixelOperation.MergePaint - 1)]
        [InlineData(CopyPixelOperation.MergeCopy - 1)]
        [InlineData(CopyPixelOperation.SourceCopy - 1)]
        [InlineData(CopyPixelOperation.SourcePaint - 1)]
        [InlineData(CopyPixelOperation.PatCopy - 1)]
        [InlineData(CopyPixelOperation.PatPaint - 1)]
        [InlineData(CopyPixelOperation.Whiteness - 1)]
        [InlineData(CopyPixelOperation.CaptureBlt - 1)]
        [InlineData(CopyPixelOperation.CaptureBlt + 1)]
        public void CopyFromScreen_InvalidCopyPixelOperation_ThrowsInvalidEnumArgumentException(CopyPixelOperation copyPixelOperation)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => graphics.CopyFromScreen(1, 2, 3, 4, Size.Empty, copyPixelOperation));
            }
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void CopyFromScreen_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.CopyFromScreen(0, 0, 0, 0, Size.Empty));
                    Assert.Throws<InvalidOperationException>(() => graphics.CopyFromScreen(0, 0, 0, 0, Size.Empty, CopyPixelOperation.DestinationInvert));
                    Assert.Throws<InvalidOperationException>(() => graphics.CopyFromScreen(Point.Empty, Point.Empty, Size.Empty));
                    Assert.Throws<InvalidOperationException>(() => graphics.CopyFromScreen(Point.Empty, Point.Empty, Size.Empty, CopyPixelOperation.DestinationInvert));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(23650)]
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void CopyFromScreen_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CopyFromScreen(0, 0, 0, 0, Size.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CopyFromScreen(0, 0, 0, 0, Size.Empty, CopyPixelOperation.MergeCopy));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CopyFromScreen(Point.Empty, Point.Empty, Size.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.CopyFromScreen(Point.Empty, Point.Empty, Size.Empty, CopyPixelOperation.MergeCopy));
            }
        }

        public static IEnumerable<object[]> TransformPoints_TestData()
        {
            yield return new object[]
            {
                CoordinateSpace.Device,
                CoordinateSpace.Page,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(1, 1), new Point(2, 2) }
            };

            yield return new object[]
            {
               CoordinateSpace.Device,
                CoordinateSpace.World,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(9, 12), new Point(13, 18) }
            };

            yield return new object[]
            {
                CoordinateSpace.Page,
                CoordinateSpace.Device,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(1, 1), new Point(2, 2) }
            };

            yield return new object[]
            {
                CoordinateSpace.Page,
                CoordinateSpace.World,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(9, 12), new Point(13, 18) }
            };

            yield return new object[]
            {
                CoordinateSpace.World,
                CoordinateSpace.Device,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(1, -1), new Point(0, -1) }
            };

            yield return new object[]
            {
                CoordinateSpace.World,
                CoordinateSpace.Page,
                new Point[] { new Point(1, 1), new Point(2, 2) },
                new Point[] { new Point(1, -1), new Point(0, -1) }
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(TransformPoints_TestData))]
        public void TransformPoints_Points_Success(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] points, Point[] expected)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.PageScale = 10;
                graphics.Transform = transform;

                graphics.TransformPoints(destSpace, srcSpace, points);
                Assert.Equal(expected, points);
            }
        }

        public static IEnumerable<object[]> TransformPointFs_TestData()
        {
            yield return new object[]
            {
                CoordinateSpace.Device,
                CoordinateSpace.Page,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new Point(1, 1), new Point(2, 2) }
            };

            yield return new object[]
            {
               CoordinateSpace.Device,
                CoordinateSpace.World,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new Point(9, 12), new Point(13, 18) }
            };

            yield return new object[]
            {
                CoordinateSpace.Page,
                CoordinateSpace.Device,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new Point(1, 1), new Point(2, 2) }
            };

            yield return new object[]
            {
                CoordinateSpace.Page,
                CoordinateSpace.World,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new Point(9, 12), new Point(13, 18) }
            };

            yield return new object[]
            {
                CoordinateSpace.World,
                CoordinateSpace.Device,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new PointF(0.5f, -1.5f), new Point(0, -1) }
            };

            yield return new object[]
            {
                CoordinateSpace.World,
                CoordinateSpace.Page,
                new PointF[] { new Point(1, 1), new Point(2, 2) },
                new PointF[] { new PointF(0.5f, -1.5f), new Point(0, -1) }
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(TransformPointFs_TestData))]
        public void TransformPoints_PointFs_Success(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] points, PointF[] expected)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.PageScale = 10;
                graphics.Transform = transform;

                graphics.TransformPoints(destSpace, srcSpace, points);
                Assert.Equal(expected, points);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CoordinateSpace.Device)]
        [InlineData(CoordinateSpace.World)]
        [InlineData(CoordinateSpace.Page)]
        public void TransformPoints_PointsAndSameCoordinateSpace_DoesNothing(CoordinateSpace space)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;

                var points = new Point[] { new Point(1, 1) };
                graphics.TransformPoints(space, space, points);
                Assert.Equal(new Point[] { new Point(1, 1) }, points);
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CoordinateSpace.Device)]
        [InlineData(CoordinateSpace.World)]
        [InlineData(CoordinateSpace.Page)]
        public void TransformPoints_PointFsAndSameCoordinateSpace_DoesNothing(CoordinateSpace space)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                graphics.Transform = transform;

                var points = new PointF[] { new PointF(1, 1) };
                graphics.TransformPoints(space, space, points);
                Assert.Equal(new PointF[] { new PointF(1, 1) }, points);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CoordinateSpace.World - 1)]
        [InlineData(CoordinateSpace.Device + 1)]
        public void TransformPoints_InvalidDestSpace_ThrowsArgumentException(CoordinateSpace destSpace)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(destSpace, CoordinateSpace.World, new Point[] { new Point(1, 1) }));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(destSpace, CoordinateSpace.World, new PointF[] { new PointF(1, 1) }));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(CoordinateSpace.World - 1)]
        [InlineData(CoordinateSpace.Device + 1)]
        public void TransformPoints_InvalidSourceSpace_ThrowsArgumentException(CoordinateSpace srcSpace)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.World, srcSpace, new Point[] { new Point(1, 1) }));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.World, srcSpace, new PointF[] { new PointF(1, 1) }));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TransformPoints_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pts", () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("pts", () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, (PointF[])null));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TransformPoints_EmptyPoints_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new Point[0]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new PointF[0]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TransformPoints_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new Point[] { Point.Empty }));
                    Assert.Throws<InvalidOperationException>(() => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new PointF[] { PointF.Empty }));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void TransformPoints_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new Point[] { Point.Empty }));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Page, new PointF[] { PointF.Empty }));
            }
        }

        public static IEnumerable<object[]> GetNearestColor_TestData()
        {
            yield return new object[] { PixelFormat.Format32bppArgb, Color.Red, Color.FromArgb(Color.Red.ToArgb()) };
            yield return new object[] { PixelFormat.Format16bppRgb555, Color.Red, Color.FromArgb(255, 248, 0, 0) };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [MemberData(nameof(GetNearestColor_TestData))]
        public void GetNearestColor_Color_ReturnsExpected(PixelFormat pixelFormat, Color color, Color expected)
        {
            using (var image = new Bitmap(10, 10, pixelFormat))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Assert.Equal(expected, graphics.GetNearestColor(color));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetNearestColor_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.GetNearestColor(Color.Red));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void GetNearestColor_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.GetNearestColor(Color.Red));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLine_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLine(null, Point.Empty, Point.Empty));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLine(null, 0, 0, 0, 0));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLine(null, PointF.Empty, PointF.Empty));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLine(null, 0f, 0f, 0f, 0f));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLine_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, Point.Empty, Point.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, 0, 0, 0, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, PointF.Empty, PointF.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, 0f, 0f, 0f, 0f));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLine_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLine(pen, Point.Empty, Point.Empty));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLine(pen, 0, 0, 0, 0));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLine(pen, PointF.Empty, PointF.Empty));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLine(pen, 0f, 0f, 0f, 0f));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLine_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, Point.Empty, Point.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, 0, 0, 0, 0));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, PointF.Empty, PointF.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLine(pen, 0f, 0f, 0f, 0f));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLines_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLines(null, new Point[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLines(null, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLines_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLines_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawLines(pen, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawLines(pen, (PointF[])null));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0)]
        [InlineData(1)]
        public void DrawLines_InvalidPointsLength_ThrowsArgumentException(int length)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new Point[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new PointF[length]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLines_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLines(pen, new Point[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawLines(pen, new PointF[2]));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawLines_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawLines(pen, new PointF[2]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawArc(null, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawArc(null, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawArc(null, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawArc(null, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_ZeroWidth_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 0, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 0, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 0, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 0f, 1f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_ZeroHeight_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 0), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 1, 0, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 0), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 1f, 0f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawArc(pen, 0, 0, 1, 1, 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawArc(pen, 0f, 0f, 1f, 1f, 0, 90));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawArc_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBezier_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawBezier(null, 1, 2, 3, 4, 5, 6, 7, 8));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawBezier(null, Point.Empty, Point.Empty, Point.Empty, Point.Empty));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawBezier(null, PointF.Empty, PointF.Empty, PointF.Empty, PointF.Empty));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBezier_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBezier(pen, 1, 2, 3, 4, 5, 6, 7, 8));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBezier(pen, Point.Empty, Point.Empty, Point.Empty, Point.Empty));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBezier(pen, PointF.Empty, PointF.Empty, PointF.Empty, PointF.Empty));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBezier_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawBezier(pen, 1, 2, 3, 4, 5, 6, 7, 8));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawBezier(pen, Point.Empty, Point.Empty, Point.Empty, Point.Empty));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawBezier(pen, PointF.Empty, PointF.Empty, PointF.Empty, PointF.Empty));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBezier_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawBeziers(null, new Point[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawBeziers(null, new PointF[2]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawBeziers(pen, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawBeziers(pen, (PointF[])null));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_EmptyPoints_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new Point[0]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new PointF[0]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawBeziers(pen, new Point[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawBeziers(pen, new PointF[2]));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawBeziers_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawBeziers(pen, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangle_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangle(null, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangle(null, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangle(null, 0f, 0f, 1f, 1f));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangle_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();


                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, 0f, 0f, 1f, 1f));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangle_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangle(pen, new Rectangle(0, 0, 1, 1)));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangle(pen, 0, 0, 1, 1));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangle(pen, 0f, 0f, 1f, 1f));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangle_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, 0f, 0f, 1f, 1f));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangles(null, new Rectangle[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangles(null, new RectangleF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new Rectangle[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new RectangleF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_NullRectangles_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("rects", () => graphics.DrawRectangles(pen, (Rectangle[])null));
                AssertExtensions.Throws<ArgumentNullException>("rects", () => graphics.DrawRectangles(pen, (RectangleF[])null));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_EmptyRectangles_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new Rectangle[0]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new RectangleF[0]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangles(pen, new Rectangle[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangles(pen, new RectangleF[2]));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawRectangles_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new Rectangle[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangles(pen, new RectangleF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawEllipse_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawEllipse(null, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawEllipse(null, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawEllipse(null, new RectangleF(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawEllipse(null, 0f, 0f, 1f, 1f));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawEllipse_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();


                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, new RectangleF(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, 0f, 0f, 1f, 1f));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawEllipse_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawEllipse(pen, new Rectangle(0, 0, 1, 1)));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawEllipse(pen, 0, 0, 1, 1));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawEllipse(pen, new RectangleF(0, 0, 1, 1)));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawEllipse(pen, 0f, 0f, 1f, 1f));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawEllipse_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, new Rectangle(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, 0, 0, 1, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, new RectangleF(0, 0, 1, 1)));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawEllipse(pen, 0f, 0f, 1f, 1f));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPie(null, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPie(null, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPie(null, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPie(null, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_ZeroWidth_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new Rectangle(0, 0, 0, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0, 0, 0, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new RectangleF(0, 0, 0, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0f, 0f, 0f, 1f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_ZeroHeight_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new Rectangle(0, 0, 1, 0), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0, 0, 1, 0, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, new RectangleF(0, 0, 1, 0), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawArc(pen, 0f, 0f, 1f, 0f, 0, 90));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPie(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPie(pen, 0, 0, 1, 1, 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPie(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPie(pen, 0f, 0f, 1f, 1f, 0, 90));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPie_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new Rectangle(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0, 0, 1, 1, 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, new RectangleF(0, 0, 1, 1), 0, 90));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPie(pen, 0f, 0f, 1f, 1f, 0, 90));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPolygon_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPolygon(null, new Point[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPolygon(null, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPolygon_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPolygon_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawPolygon(pen, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawPolygon(pen, (PointF[])null));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0)]
        [InlineData(1)]
        public void DrawPolygon_InvalidPointsLength_ThrowsArgumentException(int length)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new Point[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new PointF[length]));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPolygon_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPolygon(pen, new Point[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPolygon(pen, new PointF[2]));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPolygon_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPolygon(pen, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var graphicsPath = new GraphicsPath())
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawPath(null, graphicsPath));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var graphicsPath = new GraphicsPath())
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPath(pen, graphicsPath));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_NullPath_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => graphics.DrawPath(pen, null));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_DisposedPath_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                var graphicsPath = new GraphicsPath();
                graphicsPath.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPath(pen, graphicsPath));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            using (var graphicsPath = new GraphicsPath())
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawPath(pen, graphicsPath));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawPath_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            using (var graphicsPath = new GraphicsPath())
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawPath(pen, graphicsPath));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawCurve_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new Point[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new PointF[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new Point[2], 1));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new PointF[2], 1));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new PointF[2], 0, 2));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new Point[2], 0, 2, 1));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawCurve(null, new PointF[2], 0, 2, 1));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawCurve_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2], 0, 2, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 0, 2, 1));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawCurve_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (PointF[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (Point[])null, 1));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (PointF[])null, 1));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (PointF[])null, 0, 2));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (Point[])null, 0, 2, 1));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawCurve(pen, (PointF[])null, 0, 2, 1));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0)]
        [InlineData(1)]
        public void DrawCurve_InvalidPointsLength_ThrowsArgumentException(int length)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[length], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length], 0, length));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[length], 0, length, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length], 0, length, 1));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(4, -1, 4)]
        [InlineData(4, 0, -1)]
        [InlineData(4, 4, 0)]
        [InlineData(4, 0, 5)]
        [InlineData(4, 3, 2)]
        public void DrawCurve_InvalidOffsetCount_ThrowsArgumentException(int length, int offset, int numberOfSegments)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length], offset, numberOfSegments));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[length], offset, numberOfSegments, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[length], offset, numberOfSegments, 1));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawCurve_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new Point[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new PointF[2]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new Point[2], 1));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new PointF[2], 1));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new PointF[2], 0, 2));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new Point[2], 0, 2, 1));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawCurve(pen, new PointF[2], 0, 2, 1));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawCurve_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 0, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new Point[2], 0, 2, 1));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawCurve(pen, new PointF[2], 0, 2, 1));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawClosedCurve_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawClosedCurve(null, new Point[3]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawClosedCurve(null, new Point[3], 1, FillMode.Winding));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawClosedCurve(null, new PointF[3]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawClosedCurve(null, new PointF[3], 1, FillMode.Winding));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawClosedCurve_DisposedPen_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[3]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[3], 1, FillMode.Winding));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[3]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[3], 1, FillMode.Winding));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawClosedCurve_NullPoints_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawClosedCurve(pen, (Point[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawClosedCurve(pen, (Point[])null, 1, FillMode.Winding));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawClosedCurve(pen, (PointF[])null));
                AssertExtensions.Throws<ArgumentNullException>("points", () => graphics.DrawClosedCurve(pen, (PointF[])null, 1, FillMode.Winding));
            }
        }

        [ConditionalTheory(Helpers.GdiplusIsAvailable)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void DrawClosedCurve_InvalidPointsLength_ThrowsArgumentException(int length)
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[length], 1, FillMode.Winding));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[length]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[length], 1, FillMode.Winding));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawClosedCurve_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawClosedCurve(pen, new Point[3]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawClosedCurve(pen, new Point[3], 1, FillMode.Winding));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawClosedCurve(pen, new PointF[3]));
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawClosedCurve(pen, new PointF[3], 1, FillMode.Winding));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawClosedCurve_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[3]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new Point[3], 1, FillMode.Alternate));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[3]));
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawClosedCurve(pen, new PointF[3], 1, FillMode.Alternate));
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clear_Color_Success()
        {
            Color color = Color.FromArgb(Color.Plum.ToArgb());

            using (var image = new Bitmap(2, 2))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(color))
            {
                graphics.FillRectangle(brush, new Rectangle(0, 0, 2, 2));

                graphics.Clear(color);
                Helpers.VerifyBitmap(image, new Color[][]
                {
                    new Color[] { color, color },
                    new Color[] { color, color }
                });
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clear_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.Clear(Color.Red));
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Clear_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.Clear(Color.Red));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawString_DefaultFont_Succeeds()
        {
            using (var image = new Bitmap(50, 50))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawString("Test text", SystemFonts.DefaultFont, Brushes.White, new Point());
                Helpers.VerifyBitmapNotBlank(image);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void DrawString_CompositingModeSourceCopy_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                AssertExtensions.Throws<ArgumentException>(
                    null,
                    () => graphics.DrawString("Test text", SystemFonts.DefaultFont, Brushes.White, new Point()));
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
