// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Tests
{
    public class Graphics_DrawBezierTests : DrawingTest
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawBezier_Point()
        {
            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.White))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.DrawBezier(pen, new Point(10, 10), new Point(20, 1), new Point(35, 5), new Point(50, 10));
                ValidateImageContent(image,
                    PlatformDetection.IsWindows
                        ? new byte[] { 0xa4, 0xb9, 0x73, 0xb9, 0x6f, 0x3a, 0x85, 0x21, 0xd3, 0x65, 0x87, 0x24, 0xcf, 0x6d, 0x61, 0x94 }
                        : new byte[] { 0xcf, 0x92, 0xaa, 0xe2, 0x44, 0xd4, 0xdd, 0xae, 0xdd, 0x4c, 0x8a, 0xf5, 0xc3, 0x65, 0xac, 0xf2 });
            }
        }

        [ActiveIssue(30683, TargetFrameworkMonikers.Netcoreapp)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawBezier_Points()
        {
            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.Red))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Point[] points =
                {
                    new Point(10, 10), new Point(20, 1), new Point(35, 5), new Point(50, 10),
                    new Point(60, 15), new Point(65, 25), new Point(50, 30)
                };

                graphics.DrawBeziers(pen, points);
                ValidateImageContent(image,
                    PlatformDetection.IsWindows
                        ? new byte[] { 0xd0, 0x00, 0x08, 0x21, 0x06, 0x29, 0xd8, 0xab, 0x19, 0xc5, 0xc9, 0xf6, 0xf2, 0x69, 0x30, 0x1f }
                        : new byte[] { 0x9d, 0x24, 0x9f, 0x91, 0xa3, 0xa5, 0x60, 0xde, 0x14, 0x69, 0x42, 0xa8, 0xe6, 0xc6, 0xbf, 0xc9 });
            }
        }

        [ActiveIssue(30683, TargetFrameworkMonikers.Netcoreapp)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawBezier_PointFs()
        {
            using (Bitmap image = new Bitmap(100, 100))
            using (Pen pen = new Pen(Color.Red))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                PointF[] points =
                {
                    new PointF(10.0F, 10.0F), new PointF(20.0F, 1.0F), new PointF(35.0F, 5.0F), new PointF(50.0F, 10.0F),
                    new PointF(60.0F, 15.0F), new PointF(65.0F, 25.0F), new PointF(50.0F, 30.0F)
                };

                graphics.DrawBeziers(pen, points);
                ValidateImageContent(image,
                    PlatformDetection.IsWindows
                        ? new byte[] { 0xd0, 0x00, 0x08, 0x21, 0x06, 0x29, 0xd8, 0xab, 0x19, 0xc5, 0xc9, 0xf6, 0xf2, 0x69, 0x30, 0x1f }
                        : new byte[] { 0x9d, 0x24, 0x9f, 0x91, 0xa3, 0xa5, 0x60, 0xde, 0x14, 0x69, 0x42, 0xa8, 0xe6, 0xc6, 0xbf, 0xc9 });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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

        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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
        [ConditionalFact(Helpers.IsDrawingSupported)]
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
    }
}
