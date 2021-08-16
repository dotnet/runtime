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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
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
    }
}
