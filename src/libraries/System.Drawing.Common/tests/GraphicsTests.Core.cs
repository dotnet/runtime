// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing.Drawing2D;
using System.Numerics;
using Xunit;

namespace System.Drawing.Tests
{
    public partial class GraphicsTests
    {
        private static Matrix3x2 s_testMatrix = Matrix3x2.CreateRotation(45) * Matrix3x2.CreateScale(2) * Matrix3x2.CreateTranslation(new Vector2(10, 20));


        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TransformElements_SetNonInvertibleMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(5, 5))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Matrix3x2 matrix = new Matrix3x2(123, 24, 82, 16, 47, 30);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformElements = matrix);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TransformElements_GetSetWhenBusy_ThrowsInvalidOperationException()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.TransformElements);
                    Assert.Throws<InvalidOperationException>(() => graphics.TransformElements = Matrix3x2.Identity);
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TransformElements_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformElements);
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.TransformElements = Matrix3x2.Identity);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TransformElements_RoundTrip()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.TransformElements = s_testMatrix;
                Assert.Equal(s_testMatrix, graphics.TransformElements);

                using (Matrix matrix = graphics.Transform)
                {
                    Assert.Equal(s_testMatrix, matrix.MatrixElements);
                }

                using (Matrix matrix = new Matrix())
                {
                    graphics.Transform = matrix;
                    Assert.True(graphics.TransformElements.IsIdentity);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangle_NullPen_ThrowsArgumentNullException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("pen", () => graphics.DrawRectangle(null, new RectangleF(0f, 0f, 1f, 1f)));
                // other DrawRectangle overloads tested in DrawRectangle_NullPen_ThrowsArgumentNullException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangle_DisposedPen_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var pen = new Pen(Color.Red);
                pen.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, new RectangleF(0f, 0f, 1f, 1f)));
                // other DrawRectangle overloads tested in DrawRectangle_DisposedPen_ThrowsArgumentException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangle_Busy_ThrowsInvalidOperationException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var pen = new Pen(Color.Red))
            {
                graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.DrawRectangle(pen, new RectangleF(0f, 0f, 1f, 1f)));
                    // other DrawRectangle overloads tested in DrawRectangle_Busy_ThrowsInvalidOperationException()
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangle_Disposed_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (var pen = new Pen(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.DrawRectangle(pen, new RectangleF(0f, 0f, 1f, 1f)));
                // other DrawRectangle overloads tested in DrawRectangle_Disposed_ThrowsArgumentException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_NullPen_ThrowsArgumentNullException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("brush", () => graphics.FillPie(null, new RectangleF(0, 0, 1, 1), 0, 90));
                // other FillPie overloads tested in FillPie_NullPen_ThrowsArgumentNullException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_DisposedPen_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                var brush = new SolidBrush(Color.Red);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.FillPie(brush, new RectangleF(0, 0, 1, 1), 0, 90));
                // other FillPie overloads tested in FillPie_DisposedPen_ThrowsArgumentException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_ZeroWidth_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.FillPie(brush, new RectangleF(0, 0, 0, 1), 0, 90));
                // other FillPie overloads tested in FillPie_ZeroWidth_ThrowsArgumentException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_ZeroHeight_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.Red))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => graphics.FillPie(brush, new RectangleF(0, 0, 1, 0), 0, 90));
                // other FillPie overloads tested in FillPie_ZeroHeight_ThrowsArgumentException()
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_Busy_ThrowsInvalidOperationException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.Red))
            {
                graphics.GetHdc();
                try
                {
                    Assert.Throws<InvalidOperationException>(() => graphics.FillPie(brush, new RectangleF(0, 0, 1, 1), 0, 90));
                    // other FillPie overloads tested in FillPie_Busy_ThrowsInvalidOperationException()
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPie_Disposed_ThrowsArgumentException_Core()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new SolidBrush(Color.Red))
            {
                Graphics graphics = Graphics.FromImage(image);
                graphics.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => graphics.FillPie(brush, new RectangleF(0, 0, 1, 1), 0, 90));
                // other FillPie overloads tested in FillPie_Disposed_ThrowsArgumentException()
            }
        }



    }
}
