// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Xunit;

namespace System.Drawing.Tests
{
    public class TextureBrushTests
    {
        public static IEnumerable<object[]> Ctor_Bitmap_TestData()
        {
            yield return new object[] { new Bitmap(10, 10), PixelFormat.Format32bppPArgb, new Size(10, 10) };
            yield return new object[] { new Metafile(Helpers.GetTestBitmapPath("telescope_01.wmf")), PixelFormat.Format32bppArgb, new Size(490, 654) };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Bitmap_TestData))]
        public void Ctor_Bitmap(Image bitmap, PixelFormat expectedPixelFormat, Size expectedSize)
        {
            try
            {
                using (var brush = new TextureBrush(bitmap))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(bitmap, brushImage);
                    Assert.Equal(expectedPixelFormat, brushImage.PixelFormat);
                    Assert.Equal(expectedSize, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(WrapMode.Tile, brush.WrapMode);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_BitmapFromIconHandle_Success()
        {
            using (var icon = new Icon(Helpers.GetTestBitmapPath("10x16_one_entry_32bit.ico")))
            using (var image = Bitmap.FromHicon(icon.Handle))
            {
                Ctor_Bitmap(image, PixelFormat.Format32bppPArgb, new Size(11, 22));
            }
        }

        public static IEnumerable<object[]> Ctor_Image_WrapMode_TestData()
        {
            foreach (object[] data in Ctor_Bitmap_TestData())
            {
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.Clamp, data[1], data[2] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.Tile, data[1], data[2] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipX, data[1], data[2] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipXY, data[1], data[2] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipY, data[1], data[2] };
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_WrapMode_TestData))]
        public void Ctor_Image_WrapMode(Image image, WrapMode wrapMode, PixelFormat expectedPixelFormat, Size expectedSize)
        {
            try
            {
                using (var brush = new TextureBrush(image, wrapMode))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(expectedPixelFormat, brushImage.PixelFormat);
                    Assert.Equal(expectedSize, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(wrapMode, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        public static IEnumerable<object[]> Ctor_Image_Rectangle_TestData()
        {
            yield return new object[] { new Bitmap(10, 10), new Rectangle(0, 0, 10, 10) };
            yield return new object[] { new Bitmap(10, 10), new Rectangle(5, 5, 5, 5) };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_Rectangle_TestData))]
        public void Ctor_Image_Rectangle(Image image, Rectangle rectangle)
        {
            try
            {
                using (var brush = new TextureBrush(image, rectangle))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(WrapMode.Tile, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_Rectangle_TestData))]
        public void Ctor_Image_RectangleF(Image image, Rectangle rectangle)
        {
            try
            {
                using (var brush = new TextureBrush(image, (RectangleF)rectangle))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(WrapMode.Tile, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        public static IEnumerable<object[]> Ctor_Image_WrapMode_Rectangle_TestData()
        {
            foreach (object[] data in Ctor_Image_Rectangle_TestData())
            {
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.Clamp, data[1] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.Tile, data[1] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipX, data[1] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipXY, data[1] };
                yield return new object[] { ((Image)data[0]).Clone(), WrapMode.TileFlipY, data[1] };
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_WrapMode_Rectangle_TestData))]
        public void Ctor_Image_WrapMode_Rectangle(Image image, WrapMode wrapMode, Rectangle rectangle)
        {
            try
            {
                using (var brush = new TextureBrush(image, wrapMode, rectangle))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(wrapMode, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_WrapMode_Rectangle_TestData))]
        public void Ctor_Image_WrapMode_RectangleF(Image image, WrapMode wrapMode, Rectangle rectangle)
        {
            try
            {
                using (var brush = new TextureBrush(image, wrapMode, (RectangleF)rectangle))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(wrapMode, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
            }
        }

        public static IEnumerable<object[]> Ctor_Image_Rectangle_ImageAttributes_TestData()
        {
            foreach (object[] data in Ctor_Image_Rectangle_TestData())
            {
                yield return new object[] { ((Image)data[0]).Clone(), data[1], null, WrapMode.Tile };
                yield return new object[] { ((Image)data[0]).Clone(), data[1], new ImageAttributes(), WrapMode.Clamp };

                var customWrapMode = new ImageAttributes();
                customWrapMode.SetWrapMode(WrapMode.TileFlipXY);
                yield return new object[] { ((Image)data[0]).Clone(), data[1], customWrapMode, WrapMode.TileFlipXY };
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_Rectangle_ImageAttributes_TestData))]
        public void Ctor_Image_Rectangle_ImageAttributes(Image image, Rectangle rectangle, ImageAttributes attributes, WrapMode expectedWrapMode)
        {
            try
            {
                using (var brush = new TextureBrush(image, rectangle, attributes))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(expectedWrapMode, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
                attributes?.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Image_Rectangle_ImageAttributes_TestData))]
        public void Ctor_Image_RectangleF_ImageAttributes(Image image, Rectangle rectangle, ImageAttributes attributes, WrapMode expectedWrapMode)
        {
            try
            {
                using (var brush = new TextureBrush(image, (RectangleF)rectangle, attributes))
                using (var matrix = new Matrix())
                {
                    Bitmap brushImage = Assert.IsType<Bitmap>(brush.Image);
                    Assert.NotSame(image, brushImage);
                    Assert.Equal(PixelFormat.Format32bppPArgb, brushImage.PixelFormat);
                    Assert.Equal(rectangle.Size, brushImage.Size);
                    Assert.Equal(matrix, brush.Transform);
                    Assert.Equal(expectedWrapMode, brush.WrapMode);
                }
            }
            finally
            {
                image.Dispose();
                attributes?.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullImage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, WrapMode.Tile));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, RectangleF.Empty));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, Rectangle.Empty));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, RectangleF.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, Rectangle.Empty, null));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, WrapMode.Tile, RectangleF.Empty));
            AssertExtensions.Throws<ArgumentNullException>("image", () => new TextureBrush(null, WrapMode.Tile, Rectangle.Empty));
        }
        
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_DisposedImage_ThrowsArgumentException()
        {
            var image = new Bitmap(10, 10);
            image.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, WrapMode.Tile));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, RectangleF.Empty));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, Rectangle.Empty));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, RectangleF.Empty, null));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, Rectangle.Empty, null));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, WrapMode.Tile, RectangleF.Empty));
            AssertExtensions.Throws<ArgumentException>(null, () => new TextureBrush(image, WrapMode.Tile, Rectangle.Empty));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WrapMode.Tile - 1)]
        [InlineData(WrapMode.Clamp + 1)]
        public void Ctor_InvalidWrapMode_ThrowsInvalidEnumArgumentException(WrapMode wrapMode)
        {
            using (var image = new Bitmap(10, 10))
            {
                Assert.ThrowsAny<ArgumentException>(() => new TextureBrush(image, wrapMode));
                Assert.ThrowsAny<ArgumentException>(() => new TextureBrush(image, wrapMode, RectangleF.Empty));
                Assert.ThrowsAny<ArgumentException>(() => new TextureBrush(image, wrapMode, Rectangle.Empty));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, 0, 1, 1)]
        [InlineData(10, 0, 1, 1)]
        [InlineData(5, 0, 6, 1)]
        [InlineData(0, -1, 1, 1)]
        [InlineData(0, 10, 1, 1)]
        [InlineData(0, 5, 1, 6)]
        [InlineData(0, 0, 1, 0)]
        [InlineData(0, 0, 0, 1)]
        public void Ctor_InvalidRectangle_ThrowsOutOfMemoryException(int x, int y, int width, int height)
        {
            var rectangle = new Rectangle(x, y, width, height);
            using (var image = new Bitmap(10, 10))
            {
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, rectangle));
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, (RectangleF)rectangle));
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, WrapMode.Tile, rectangle));
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, WrapMode.Tile, (RectangleF)rectangle));
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, rectangle, null));
                Assert.Throws<OutOfMemoryException>(() => new TextureBrush(image, (RectangleF)rectangle, null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Invoke_Success()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image, WrapMode.Clamp))
            {
                TextureBrush clone = Assert.IsType<TextureBrush>(brush.Clone());
                Assert.NotSame(brush, clone);

                Assert.Equal(new Size(10, 10), brush.Image.Size);
                Assert.Equal(WrapMode.Clamp, clone.WrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.Clone());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Image_GetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.Image);
            }
        }

        public static IEnumerable<object[]> MultiplyTransform_TestData()
        {
            yield return new object[] { new Matrix(), new Matrix(1, 2, 3, 4, 5, 6), MatrixOrder.Prepend };
            yield return new object[] { new Matrix(), new Matrix(1, 2, 3, 4, 5, 6), MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(2, 3, 4, 5, 6, 7), MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), new Matrix(2, 3, 4, 5, 6, 7), MatrixOrder.Append };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MultiplyTransform_TestData))]
        public void MultiplyTransform_Matrix_SetsTransformToExpected(Matrix originalTransform, Matrix matrix, MatrixOrder matrixOrder)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                using (var brush = new TextureBrush(image))
                using (var expected = (Matrix)originalTransform.Clone())
                {
                    expected.Multiply(matrix, matrixOrder);
                    brush.Transform = originalTransform;

                    if (matrixOrder == MatrixOrder.Prepend)
                    {
                        TextureBrush clone = (TextureBrush)brush.Clone();
                        clone.MultiplyTransform(matrix);
                        Assert.Equal(expected, clone.Transform);
                    }
                    
                    brush.MultiplyTransform(matrix, matrixOrder);
                    Assert.Equal(expected, brush.Transform);
                }
            }
            finally
            {
                originalTransform.Dispose();
                matrix.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NullMatrix_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null));
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null, MatrixOrder.Prepend));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NotInvertibleMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            using (var matrix = new Matrix(123, 24, 82, 16, 47, 30))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix, MatrixOrder.Prepend));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_DisposedMatrix_Nop()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            {
                brush.Transform = transform;

                var matrix = new Matrix();
                matrix.Dispose();

                brush.MultiplyTransform(matrix);
                brush.MultiplyTransform(matrix, MatrixOrder.Append);

                Assert.Equal(transform, brush.Transform);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void MultiplyTransform_InvalidOrder_Nop(MatrixOrder matrixOrder)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            using (var matrix = new Matrix())
            {
                brush.Transform = transform;

                brush.MultiplyTransform(matrix, matrixOrder);
                Assert.Equal(transform, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix, MatrixOrder.Prepend));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Invoke_SetsTransformToZero()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            using (var transform = new Matrix(1, 2, 3, 4, 5, 6))
            using (var matrix = new Matrix())
            {
                brush.Transform = transform;
                brush.ResetTransform();
                Assert.Equal(matrix, brush.Transform);

                brush.ResetTransform();
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.ResetTransform());
            }
        }

        public static IEnumerable<object[]> RotateTransform_TestData()
        {
            yield return new object[] { new Matrix(), 90, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(), 90, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 360, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 360, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -45, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -45, MatrixOrder.Append };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(RotateTransform_TestData))]
        public void RotateTransform_Invoke_SetsTransformToExpected(Matrix originalTransform, float angle, MatrixOrder matrixOrder)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                using (var brush = new TextureBrush(image))
                using (Matrix expected = originalTransform.Clone())
                {
                    expected.Rotate(angle, matrixOrder);
                    brush.Transform = originalTransform;

                    if (matrixOrder == MatrixOrder.Prepend)
                    {
                        TextureBrush clone = (TextureBrush)brush.Clone();
                        clone.RotateTransform(angle);
                        Assert.Equal(expected, clone.Transform);
                    }

                    brush.RotateTransform(angle, matrixOrder);
                    Assert.Equal(expected, brush.Transform);
                }
            }
            finally
            {
                originalTransform.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void RotateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder matrixOrder)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(10, matrixOrder));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RotateTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(1));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(1, MatrixOrder.Prepend));
            }
        }

        public static IEnumerable<object[]> ScaleTransform_TestData()
        {
            yield return new object[] { new Matrix(), 2, 3, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(), 2, 3, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, 0, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, 0, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 1, 1, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 1, 1, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -2, -3, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -2, -3, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0.5, 0.75, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0.5, 0.75, MatrixOrder.Append };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ScaleTransform_TestData))]
        public void ScaleTransform_Invoke_SetsTransformToExpected(Matrix originalTransform, float scaleX, float scaleY, MatrixOrder matrixOrder)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                using (var brush = new TextureBrush(image))
                using (Matrix expected = originalTransform.Clone())
                {
                    expected.Scale(scaleX, scaleY, matrixOrder);
                    brush.Transform = originalTransform;

                    if (matrixOrder == MatrixOrder.Prepend)
                    {
                        TextureBrush clone = (TextureBrush)brush.Clone();
                        clone.ScaleTransform(scaleX, scaleY);
                        Assert.Equal(expected, clone.Transform);
                    }

                    brush.ScaleTransform(scaleX, scaleY, matrixOrder);
                    Assert.Equal(expected, brush.Transform);
                }
            }
            finally
            {
                originalTransform.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void ScaleTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder matrixOrder)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(1, 2, matrixOrder));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(1, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(1, 2, MatrixOrder.Prepend));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_SetValid_GetReturnsExpected()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            using (var matrix = new Matrix(1, 2, 3, 4, 5, 6))
            {
                brush.Transform = matrix;
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_SetNull_ThrowsArgumentNullException()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => brush.Transform = null);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_SetDisposedMatrix_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                var matrix = new Matrix();
                matrix.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform = matrix);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform);
                AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform = matrix);
            }
        }

        public static IEnumerable<object[]> TranslateTransform_TestData()
        {
            yield return new object[] { new Matrix(), 2, 3, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(), 2, 3, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, 0, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0, 0, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 1, 1, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 1, 1, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -2, -3, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), -2, -3, MatrixOrder.Append };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0.5, 0.75, MatrixOrder.Prepend };
            yield return new object[] { new Matrix(1, 2, 3, 4, 5, 6), 0.5, 0.75, MatrixOrder.Append };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(TranslateTransform_TestData))]
        public void TranslateTransform_Invoke_SetsTransformToExpected(Matrix originalTransform, float dX, float dY, MatrixOrder matrixOrder)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                using (var brush = new TextureBrush(image))
                using (Matrix expected = originalTransform.Clone())
                {
                    expected.Translate(dX, dY, matrixOrder);
                    brush.Transform = originalTransform;

                    if (matrixOrder == MatrixOrder.Prepend)
                    {
                        TextureBrush clone = (TextureBrush)brush.Clone();
                        clone.TranslateTransform(dX, dY);
                        Assert.Equal(expected, clone.Transform);
                    }

                    brush.TranslateTransform(dX, dY, matrixOrder);
                    Assert.Equal(expected, brush.Transform);
                }
            }
            finally
            {
                originalTransform.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void TranslateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder matrixOrder)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(1, 2, matrixOrder));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_Disposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            using (var matrix = new Matrix())
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(1, 2));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(1, 2, MatrixOrder.Prepend));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WrapMode.Clamp)]
        [InlineData(WrapMode.Tile)]
        [InlineData(WrapMode.TileFlipX)]
        [InlineData(WrapMode.TileFlipXY)]
        [InlineData(WrapMode.TileFlipY)]
        public void WrapMode_SetValid_GetReturnsExpected(WrapMode wrapMode)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                brush.WrapMode = wrapMode;
                Assert.Equal(wrapMode, brush.WrapMode);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WrapMode.Tile - 1)]
        [InlineData(WrapMode.Clamp + 1)]
        public void WrapMode_SetInvalid_ThrowsInvalidEnumArgumentException(WrapMode wrapMode)
        {
            using (var image = new Bitmap(10, 10))
            using (var brush = new TextureBrush(image))
            {
                Assert.ThrowsAny<ArgumentException>(() => brush.WrapMode = wrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_GetSetWhenDisposed_ThrowsArgumentException()
        {
            using (var image = new Bitmap(10, 10))
            {
                var brush = new TextureBrush(image);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode);
                AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode = WrapMode.Tile);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_Clamp_ReturnsExpected()
        {
            // R|G|_|_
            // B|Y|_|_
            // _|_|_|_
            // _|_|_|_
            Color empty = Color.FromArgb(0, 0, 0, 0);
            VerifyFillRect(WrapMode.Clamp, new Color[][]
            {
                new Color[] { Color.Red,    Color.Green,    empty,  empty },
                new Color[] { Color.Blue,   Color.Yellow,   empty,  empty },
                new Color[] { empty,        empty,          empty,  empty },
                new Color[] { empty,        empty,          empty,  empty }
            });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_Tile_ReturnsExpected()
        {
            // R|G|R|G
            // B|Y|B|Y
            // R|G|R|G
            // B|Y|B|Y
            VerifyFillRect(WrapMode.Tile, new Color[][]
            {
                new Color[] { Color.Red,  Color.Green,  Color.Red,  Color.Green  },
                new Color[] { Color.Blue, Color.Yellow, Color.Blue, Color.Yellow },
                new Color[] { Color.Red,  Color.Green,  Color.Red,  Color.Green  },
                new Color[] { Color.Blue, Color.Yellow, Color.Blue, Color.Yellow }
            });
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_TileFlipX_ReturnsExpected()
        {
            // R|G|G|R
            // B|Y|Y|B
            // R|G|G|R
            // B|Y|Y|B
            VerifyFillRect(WrapMode.TileFlipX, new Color[][]
            {
                new Color[] { Color.Red,    Color.Green,    Color.Green,    Color.Red  },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Yellow,   Color.Blue },
                new Color[] { Color.Red,    Color.Green,    Color.Green,    Color.Red  },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Yellow,   Color.Blue }
            });
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_TileFlipY_ReturnsExpected()
        {
            // R|G|R|G
            // B|Y|B|Y
            // B|Y|B|Y
            // R|G|R|G
            VerifyFillRect(WrapMode.TileFlipY, new Color[][]
            {
                new Color[] { Color.Red,    Color.Green,    Color.Red,    Color.Green  },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Blue,   Color.Yellow },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Blue,   Color.Yellow },
                new Color[] { Color.Red,    Color.Green,    Color.Red,    Color.Green  }
            });
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_TileFlipXY_ReturnsExpected()
        {
            // R|G|G|R
            // B|Y|Y|B
            // B|Y|Y|B
            // R|G|G|R
            VerifyFillRect(WrapMode.TileFlipXY, new Color[][]
            {
                new Color[] { Color.Red,    Color.Green,    Color.Green,    Color.Red  },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Yellow,   Color.Blue },
                new Color[] { Color.Blue,   Color.Yellow,   Color.Yellow,   Color.Blue },
                new Color[] { Color.Red,    Color.Green,    Color.Green,    Color.Red  }
            });
        }

        private static void VerifyFillRect(WrapMode wrapMode, Color[][] expectedColors)
        {
            using (var brushBitmap = new Bitmap(2, 2))
            {
                brushBitmap.SetPixel(0, 0, Color.Red);
                brushBitmap.SetPixel(1, 0, Color.Green);
                brushBitmap.SetPixel(0, 1, Color.Blue);
                brushBitmap.SetPixel(1, 1, Color.Yellow);

                using (var brush = new TextureBrush(brushBitmap, wrapMode))
                using (var targetImage = new Bitmap(4, 4))
                using (Graphics targetGraphics = Graphics.FromImage(targetImage))
                {
                    targetGraphics.FillRectangle(brush, new Rectangle(0, 0, 4, 4));

                    Helpers.VerifyBitmap(targetImage, expectedColors);
                }
            }
        }
    }
}
