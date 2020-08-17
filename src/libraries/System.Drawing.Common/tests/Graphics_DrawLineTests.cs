// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Drawing.Tests
{
    public class Graphics_DrawLineTests : DrawingTest
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/26624", TargetFrameworkMonikers.Netcoreapp)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLines_Points()
        {
            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.White))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawLines(pen, new Point[] { new Point(1, 1), new Point(1, 10), new Point(20, 5), new Point(25, 30) });
                ValidateImageContent(image,
                    PlatformDetection.IsWindows
                        ? new byte[] { 0x8e, 0xc2, 0xfb, 0xb4, 0xde, 0x5d, 0xdc, 0xd2, 0x31, 0xbd, 0xd3, 0x9a, 0xcf, 0xc1, 0xd4, 0xad }
                        : new byte[] { 0x55, 0x40, 0xd8, 0xaa, 0xc7, 0x36, 0x06, 0x18, 0x1a, 0x57, 0x2b, 0xa9, 0x5a, 0xff, 0x2b, 0xb2 });
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/26624", TargetFrameworkMonikers.Netcoreapp)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLines_PointFs()
        {
            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.White))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawLines(pen, new PointF[] { new PointF(1.0F, 1.0F), new PointF(1.0F, 10.0F), new PointF(20.0F, 5.0F), new PointF(25.0F, 30.0F) });
                ValidateImageContent(image,
                    PlatformDetection.IsWindows
                        ? new byte[] { 0x8e, 0xc2, 0xfb, 0xb4, 0xde, 0x5d, 0xdc, 0xd2, 0x31, 0xbd, 0xd3, 0x9a, 0xcf, 0xc1, 0xd4, 0xad }
                        : new byte[] { 0x55, 0x40, 0xd8, 0xaa, 0xc7, 0x36, 0x06, 0x18, 0x1a, 0x57, 0x2b, 0xa9, 0x5a, 0xff, 0x2b, 0xb2 });
           }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLine_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                graphics.GetHdc();
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLines_NullPen_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLines(null, new Point[2]));
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawLines(null, new PointF[2]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalTheory(Helpers.IsDrawingSupported)]
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLines_Busy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                graphics.GetHdc();
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

    }
}
