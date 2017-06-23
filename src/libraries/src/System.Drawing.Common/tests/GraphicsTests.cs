// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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

                Assert.Throws<ArgumentException>(null, () => graphics.GetHdc());
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
            Assert.Throws<ArgumentNullException>("hdc", () => Graphics.FromHdc(IntPtr.Zero));
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
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(hdc));
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdc(hdc);
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdcInternal(hdc);
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(hdc));
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
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal((IntPtr)10));

                hdc = graphics.GetHdc();
                graphics.ReleaseHdcInternal((IntPtr)10);
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc((IntPtr)10));
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
                Assert.Throws<ArgumentException>(null, () => graphics1.ReleaseHdc(hdc1));

                graphics2.ReleaseHdc(hdc1);
                Assert.Throws<ArgumentException>(null, () => graphics2.ReleaseHdc(hdc2));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_NoHdc_ThrowsArgumentException()
        {
            using (var bitmap = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(IntPtr.Zero));
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(IntPtr.Zero));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHdc_Disposed_ThrowsObjectDisposedException()
        {
            using (var bitmap = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.Dispose();

                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc());
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdc(IntPtr.Zero));
                Assert.Throws<ArgumentException>(null, () => graphics.ReleaseHdcInternal(IntPtr.Zero));
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
            Assert.Throws<ArgumentNullException>("image", () => Graphics.FromImage(null));
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

            Assert.Throws<ArgumentException>(null, () => Graphics.FromImage(image));
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
