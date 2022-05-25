// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Copyright (C) 2005-2006 Novell, Inc (http://www.novell.com)
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
//

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Tests;
using System.IO;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class ImageAttributesTests
    {
        private readonly Rectangle _rectangle = new Rectangle(0, 0, 64, 64);
        private readonly Color _actualYellow = Color.FromArgb(255, 255, 255, 0);
        private readonly Color _actualGreen = Color.FromArgb(255, 0, 255, 0);
        private readonly Color _expectedRed = Color.FromArgb(255, 255, 0, 0);
        private readonly Color _expectedBlack = Color.FromArgb(255, 0, 0, 0);
        private readonly ColorMatrix _greenComponentToZeroColorMatrix = new ColorMatrix(new float[][]
        {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 0, 0, 0, 0},
            new float[] {0, 0, 1, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 0},
        });

        private readonly ColorMatrix _grayMatrix = new ColorMatrix(new float[][] {
            new float[] {1, 0, 0, 0, 0},
            new float[] {0, 2, 0, 0, 0},
            new float[] {0, 0, 3, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 0},
        });

        private readonly ColorMap[] _yellowToRedColorMap = new ColorMap[]
        {
            new ColorMap() { OldColor = Color.FromArgb(255, 255, 255, 0), NewColor = Color.FromArgb(255, 255, 0, 0) }
        };

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default_Success()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix);

                using (ImageAttributes clone = Assert.IsAssignableFrom<ImageAttributes>(imageAttr.Clone()))
                {
                    bitmap.SetPixel(0, 0, _actualYellow);
                    graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, clone);
                    Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.Clone());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrix_ColorMatrix_Success()
        {
            using (var brush = new SolidBrush(_actualGreen))
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix);
                bitmap.SetPixel(0, 0, _actualYellow);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));

                graphics.FillRectangle(brush, _rectangle);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedBlack, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> ColorMatrix_DropShadowRepaintWhenAreaIsSmallerThanTheFilteredElement_TestData()
        {
            yield return new object[] { Color.FromArgb(100, 255, 0, 0) };
            yield return new object[] { Color.FromArgb(255, 255, 155, 155) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorMatrix_DropShadowRepaintWhenAreaIsSmallerThanTheFilteredElement_TestData))]
        public void SetColorMatrix_ColorMatrixI_Success(Color color)
        {
            ColorMatrix colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] {1, 0, 0, 0, 0},
                new float[] {0, 1, 0, 0, 0},
                new float[] {0, 0, 1, 0, 0},
                new float[] {0, 0, 0, 0.5f, 0},
                new float[] {0, 0, 0, 0, 1},
            });

            using (var brush = new SolidBrush(color))
            using (var bitmapBig = new Bitmap(200, 100))
            using (var bitmapSmall = new Bitmap(100, 100))
            using (var graphicsSmallBitmap = Graphics.FromImage(bitmapSmall))
            using (var graphicsBigBitmap = Graphics.FromImage(bitmapBig))
            using (var imageAttr = new ImageAttributes())
            {
                graphicsSmallBitmap.FillRectangle(Brushes.White, 0, 0, 100, 100);
                graphicsSmallBitmap.FillEllipse(brush, 0, 0, 100, 100);
                graphicsBigBitmap.FillRectangle(Brushes.White, 0, 0, 200, 100);
                imageAttr.SetColorMatrix(colorMatrix);
                graphicsBigBitmap.DrawImage(bitmapSmall, new Rectangle(0, 0, 100, 100), 0, 0, 100, 100, GraphicsUnit.Pixel, null);
                graphicsBigBitmap.DrawImage(bitmapSmall, new Rectangle(100, 0, 100, 100), 0, 0, 100, 100, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 255, 155, 155), bitmapBig.GetPixel(50, 50));
                Assert.Equal(Color.FromArgb(255, 255, 205, 205), bitmapBig.GetPixel(150, 50));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrix_ColorMatrixFlags_Success()
        {
            var grayShade = Color.FromArgb(255, 100, 100, 100);

            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                bitmap.SetPixel(0, 0, _actualYellow);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));

                bitmap.SetPixel(0, 0, grayShade);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.SkipGrays);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(grayShade, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> ColorAdjustType_TestData()
        {
            yield return new object[] { ColorAdjustType.Default };
            yield return new object[] { ColorAdjustType.Bitmap };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void SetColorMatrix_ColorMatrixDefaultFlagType_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var brush = new SolidBrush(_actualYellow))
            using (var pen = new Pen(brush))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedBlack, bitmap.GetPixel(0, 0));

                graphics.FillRectangle(brush, _rectangle);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));

                graphics.DrawRectangle(pen, _rectangle);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> ColorAdjustTypeI_TestData()
        {
            yield return new object[] { ColorAdjustType.Brush };
            yield return new object[] { ColorAdjustType.Pen };
            yield return new object[] { ColorAdjustType.Text };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void SetColorMatrix_ColorMatrixDefaultFlagTypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var brush = new SolidBrush(_actualYellow))
            using (var pen = new Pen(brush))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrix_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrix_NullMatrix_ThrowsArgumentException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(null));
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(null, ColorMatrixFlag.Default));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorMatrix(null, ColorMatrixFlag.Default, ColorAdjustType.Default));
            }
        }

        public static IEnumerable<object[]> ColorAdjustType_InvalidTypes_TestData()
        {
            yield return new object[] { (ColorAdjustType.Default - 1) };
            yield return new object[] { ColorAdjustType.Count };
            yield return new object[] { ColorAdjustType.Any };
            yield return new object[] { (ColorAdjustType.Any + 1) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetColorMatrix_InvalidTypes_ThrowsInvalidEnumArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type));
            }
        }

        public static IEnumerable<object[]> ColorMatrixFlag_InvalidFlags_TestData()
        {
            yield return new object[] { (ColorMatrixFlag.Default - 1) };
            yield return new object[] { ColorMatrixFlag.AltGrays };
            yield return new object[] { (ColorMatrixFlag.AltGrays + 1) };
            yield return new object[] { (ColorMatrixFlag)int.MinValue };
            yield return new object[] { (ColorMatrixFlag)int.MaxValue };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2337", TestRuntimes.Mono)]
        public void SetColorMatrix_InvalidFlags_ThrowsArgumentException(ColorMatrixFlag flag)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, flag));
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, flag, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearColorMatrix_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix);
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix);
                imageAttr.ClearColorMatrix();

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> ColorAdjustType_AllTypesAllowed_TestData()
        {
            yield return new object[] { ColorAdjustType.Default };
            yield return new object[] { ColorAdjustType.Bitmap };
            yield return new object[] { ColorAdjustType.Brush };
            yield return new object[] { ColorAdjustType.Pen };
            yield return new object[] { ColorAdjustType.Text };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearColorMatrix_DefaultFlagType_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var brush = new SolidBrush(_actualYellow))
            using (var pen = new Pen(brush))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, ColorMatrixFlag.Default, type);
                imageAttr.ClearColorMatrix(type);

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));

                graphics.FillRectangle(brush, _rectangle);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualYellow, bitmap.GetPixel(0, 0));

                graphics.DrawRectangle(pen, _rectangle);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualYellow, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearColorMatrix_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorMatrix());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorMatrix(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearColorMatrix_InvalidTypes_ThrowsInvalidEnumArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorMatrix(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrices_ColorMatrixGrayMatrix_Success()
        {
            using (var brush = new SolidBrush(_actualGreen))
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix);
                bitmap.SetPixel(0, 0, _actualYellow);
                bitmap.SetPixel(1, 1, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));
                Assert.Equal(Color.FromArgb(255, 100, 0, 100), bitmap.GetPixel(1, 1));
            }
        }

        public static IEnumerable<object[]> SetColorMatrices_Flags_TestData()
        {
            yield return new object[] { ColorMatrixFlag.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 0, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 200, 255) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetColorMatrices_Flags_TestData))]
        public void SetColorMatrices_ColorMatrixGrayMatrixFlags_Success(ColorMatrixFlag flag, Color grayShade, Color expecedGrayShade)
        {
            using (var brush = new SolidBrush(_actualGreen))
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, flag);
                bitmap.SetPixel(0, 0, _actualYellow);
                bitmap.SetPixel(1, 1, grayShade);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));
                Assert.Equal(expecedGrayShade, bitmap.GetPixel(1, 1));
            }
        }

        public static IEnumerable<object[]> SetColorMatrices_FlagsTypes_TestData()
        {
            yield return new object[] { ColorMatrixFlag.Default, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 0, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 200, 255) };
            yield return new object[] { ColorMatrixFlag.Default, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 0, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 100, 200, 255) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetColorMatrices_FlagsTypes_TestData))]
        public void SetColorMatrices_ColorMatrixGrayMatrixFlagsTypes_Success
            (ColorMatrixFlag flag, ColorAdjustType type, Color grayShade, Color expecedGrayShade)
        {
            using (var brush = new SolidBrush(_actualGreen))
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, flag, type);
                bitmap.SetPixel(0, 0, _actualYellow);
                bitmap.SetPixel(1, 1, grayShade);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedRed, bitmap.GetPixel(0, 0));
                Assert.Equal(expecedGrayShade, bitmap.GetPixel(1, 1));
            }
        }

        public static IEnumerable<object[]> SetColorMatrices_FlagsTypesI_TestData()
        {
            yield return new object[] { ColorMatrixFlag.Default, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.Default, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.Default, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.SkipGrays, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorMatrixFlag.AltGrays, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetColorMatrices_FlagsTypesI_TestData))]
        public void SetColorMatrices_ColorMatrixGrayMatrixFlagsTypesI_Success(ColorMatrixFlag flag, ColorAdjustType type, Color grayShade)
        {
            using (var brush = new SolidBrush(_actualGreen))
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, flag, type);
                bitmap.SetPixel(0, 0, _actualYellow);
                bitmap.SetPixel(1, 1, grayShade);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualYellow, bitmap.GetPixel(0, 0));
                Assert.Equal(grayShade, bitmap.GetPixel(1, 1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrices_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, ColorMatrixFlag.Default));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorMatrices_NullMatrices_ThrowsArgumentException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(null, _grayMatrix));
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(null, _grayMatrix, ColorMatrixFlag.Default));
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, null, ColorMatrixFlag.AltGrays));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorMatrices(null, _grayMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, null, ColorMatrixFlag.AltGrays, ColorAdjustType.Default));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetColorMatrices_InvalidTypes_ThrowsInvalidEnumArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, ColorMatrixFlag.Default, type));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(ColorMatrixFlag.Default - 1)]
        [InlineData(ColorMatrixFlag.AltGrays + 1)]
        [InlineData((ColorMatrixFlag)int.MinValue)]
        [InlineData((ColorMatrixFlag)int.MaxValue)]
        public void SetColorMatrices_InvalidFlags_ThrowsArgumentException(ColorMatrixFlag flag)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, flag));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorMatrices(_greenComponentToZeroColorMatrix, _grayMatrix, flag, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetThreshold_Threshold_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetThreshold(0.7f);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 230, 50, 220));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 255, 0, 255), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void SetThreshold_ThresholdType_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetThreshold(0.7f, type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 230, 50, 220));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 255, 0, 255), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void SetThreshold_ThresholdTypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetThreshold(0.7f, type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 230, 50, 220));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 230, 50, 220), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetThreshold_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetThreshold(0.5f));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetThreshold_InvalidType_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetThreshold(0.5f, type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearThreshold_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetThreshold(0.7f);
                imageAttr.ClearThreshold();
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 230, 50, 220));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 230, 50, 220), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearThreshold_ThresholdTypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetThreshold(0.7f, type);
                imageAttr.ClearThreshold(type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 230, 50, 220));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 230, 50, 220), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearThreshold_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearThreshold(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearThreshold_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearThreshold(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetGamma_Gamma_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 33, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void SetGamma_GammaType_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 33, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void SetGamma_GammaTypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetGamma_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetGamma(2.2f));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetGamma(2.2f, ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetGamma_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetGamma(2.2f, type));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearGamma_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                imageAttr.ClearGamma(type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearGamma_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearGamma(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearGamma_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearGamma(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetNoOp_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix);
                imageAttr.SetNoOp();
                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void SetNoOp_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);
                imageAttr.SetNoOp(type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetNoOp_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetNoOp());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetNoOp(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetNoOp_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetNoOp(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearNoOp_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix);
                imageAttr.SetNoOp();
                imageAttr.ClearNoOp();

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_expectedBlack, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void ClearNoOp_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);
                imageAttr.SetNoOp(type);
                imageAttr.ClearNoOp(type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 33, 0, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void ClearNoOp_TypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetGamma(2.2f, type);
                imageAttr.SetColorMatrix(_greenComponentToZeroColorMatrix, ColorMatrixFlag.Default, type);
                imageAttr.SetNoOp(type);
                imageAttr.ClearNoOp(type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 255, 0));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 255, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearNoOp_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearNoOp());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearNoOp(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearNoOp_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearNoOp(type));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22784")]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorKey_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150));

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(0, 0, 0, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22784")]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void SetColorKey_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150), type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(0, 0, 0, 0), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void SetColorKey_TypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150), type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 100, 100), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetColorKey_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150)));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150), ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetColorKey_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150), type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearColorKey_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150));
                imageAttr.ClearColorKey();

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 100, 100), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearColorKey_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetColorKey(Color.FromArgb(50, 50, 50), Color.FromArgb(150, 150, 150), type);
                imageAttr.ClearColorKey(type);

                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 100, 100), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearColorKey_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorKey());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorKey(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearColorKey_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearColorKey(type));
            }
        }

        public static IEnumerable<object[]> SetOutputChannel_ColorChannelFlag_TestData()
        {
            yield return new object[] { ColorChannelFlag.ColorChannelC, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 198, 198, 198) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 108, 108, 108) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 204, 204, 204) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 207, 207, 207) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetOutputChannel_ColorChannelFlag_TestData))]
        public void SetOutputChannel_Flag_Success(ColorChannelFlag flag, Color actualColor, Color expectedColor)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(flag);

                bitmap.SetPixel(0, 0, actualColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(expectedColor, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> SetOutputChannel_ColorChannelFlagType_TestData()
        {
            yield return new object[] { ColorChannelFlag.ColorChannelC, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 198, 198, 198) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 108, 108, 108) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 204, 204, 204) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, ColorAdjustType.Default, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 207, 207, 207) };
            yield return new object[] { ColorChannelFlag.ColorChannelC, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 198, 198, 198) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 108, 108, 108) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 204, 204, 204) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, ColorAdjustType.Bitmap, Color.FromArgb(255, 100, 100, 100), Color.FromArgb(255, 207, 207, 207) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetOutputChannel_ColorChannelFlagType_TestData))]
        public void SetOutputChannel_FlagType_Success(ColorChannelFlag flag, ColorAdjustType type, Color actualColor, Color expectedColor)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(flag, type);

                bitmap.SetPixel(0, 0, actualColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(expectedColor, bitmap.GetPixel(0, 0));
            }
        }

        public static IEnumerable<object[]> SetOutputChannel_ColorChannelFlagTypeI_TestData()
        {
            yield return new object[] { ColorChannelFlag.ColorChannelC, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, ColorAdjustType.Brush, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelC, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, ColorAdjustType.Pen, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelC, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelK, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelM, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
            yield return new object[] { ColorChannelFlag.ColorChannelY, ColorAdjustType.Text, Color.FromArgb(255, 100, 100, 100) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetOutputChannel_ColorChannelFlagTypeI_TestData))]
        public void SetOutputChannel_FlagTypeI_Success(ColorChannelFlag flag, ColorAdjustType type, Color color)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(flag, type);

                bitmap.SetPixel(0, 0, color);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(color, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannel_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelY));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelY, ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetOutputChannel_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelY, type));
            }
        }

        public static IEnumerable<object[]> SetOutputChannel_InvalidColorChannelFlags_TestData()
        {
            yield return new object[] { (ColorChannelFlag)int.MinValue };
            yield return new object[] { ColorChannelFlag.ColorChannelC - 1 };
            yield return new object[] { ColorChannelFlag.ColorChannelLast };
            yield return new object[] { ColorChannelFlag.ColorChannelLast + 1 };
            yield return new object[] { (ColorChannelFlag)int.MaxValue };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SetOutputChannel_InvalidColorChannelFlags_TestData))]
        public void SetOutputChannel_InvalidFlags_ThrowsArgumentException(ColorChannelFlag flag)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannel(flag));
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannel(flag, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearOutputChannel_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelC);
                imageAttr.ClearOutputChannel();

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearOutputChannel_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelC, type);
                imageAttr.ClearOutputChannel(type);

                bitmap.SetPixel(0, 0, _actualGreen);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_actualGreen, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearOutputChannel_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannel());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannel(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearOutputChannel_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannel(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_Name_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelC);
                imageAttr.SetOutputChannelColorProfile(Helpers.GetTestColorProfilePath("RSWOP.icm"));
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 198, 198, 198), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () =>
                imageAttr.SetOutputChannelColorProfile(Helpers.GetTestColorProfilePath("RSWOP.icm")));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                imageAttr.SetOutputChannelColorProfile(Helpers.GetTestColorProfilePath("RSWOP.icm"), ColorAdjustType.Default));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_Null_ThrowsArgumentNullException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<ArgumentNullException>(() => imageAttr.SetOutputChannelColorProfile(null));
                Assert.Throws<ArgumentNullException>(() => imageAttr.SetOutputChannelColorProfile(null, ColorAdjustType.Default));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22784")]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_InvalidPath_ThrowsArgumentException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<ArgumentException>(() => imageAttr.SetOutputChannelColorProfile(string.Empty));
                Assert.Throws<ArgumentException>(() => imageAttr.SetOutputChannelColorProfile(string.Empty, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_InvalidPath_ThrowsOutOfMemoryException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<OutOfMemoryException>(() => imageAttr.SetOutputChannelColorProfile("invalidPath"));
                Assert.Throws<OutOfMemoryException>(() => imageAttr.SetOutputChannelColorProfile("invalidPath", ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetOutputChannelColorProfile_InvalidPath_ThrowsPathTooLongException()
        {
            string fileNameTooLong = new string('a', short.MaxValue);
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<PathTooLongException>(() => imageAttr.SetOutputChannelColorProfile(fileNameTooLong));
                Assert.Throws<PathTooLongException>(() => imageAttr.SetOutputChannelColorProfile(fileNameTooLong, ColorAdjustType.Default));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetOutputChannelColorProfile_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetOutputChannelColorProfile("path", type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearOutputChannelColorProfile_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelC);
                imageAttr.SetOutputChannelColorProfile(Helpers.GetTestColorProfilePath("RSWOP.icm"));
                imageAttr.ClearOutputChannelColorProfile();
                imageAttr.ClearOutputChannel();
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 100, 100), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearOutputChannelColorProfile_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetOutputChannel(ColorChannelFlag.ColorChannelC, type);
                imageAttr.SetOutputChannelColorProfile(Helpers.GetTestColorProfilePath("RSWOP.icm"), type);
                imageAttr.ClearOutputChannelColorProfile(type);
                imageAttr.ClearOutputChannel(type);
                bitmap.SetPixel(0, 0, Color.FromArgb(255, 100, 100, 100));
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(Color.FromArgb(255, 100, 100, 100), bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearOutputChannelColorProfile_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannelColorProfile());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannelColorProfile(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearOutputChannelColorProfile_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearOutputChannelColorProfile(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetRemapTable_Map_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetRemapTable(_yellowToRedColorMap);
                bitmap.SetPixel(0, 0, _yellowToRedColorMap[0].OldColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_yellowToRedColorMap[0].NewColor, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_TestData))]
        public void SetRemapTable_MapType_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetRemapTable(_yellowToRedColorMap, type);
                bitmap.SetPixel(0, 0, _yellowToRedColorMap[0].OldColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_yellowToRedColorMap[0].NewColor, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustTypeI_TestData))]
        public void SetRemapTable_MapTypeI_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetRemapTable(_yellowToRedColorMap, type);
                bitmap.SetPixel(0, 0, _yellowToRedColorMap[0].OldColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_yellowToRedColorMap[0].OldColor, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetRemapTable_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetRemapTable(_yellowToRedColorMap));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetRemapTable(_yellowToRedColorMap, ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void SetRemapTable_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetRemapTable(_yellowToRedColorMap, type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetRemapTable_NullMap_ThrowsNullReferenceException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<NullReferenceException>(() => imageAttr.SetRemapTable(null, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetRemapTable_NullMapMeber_ThrowsNullReferenceException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<NullReferenceException>(() => imageAttr.SetRemapTable(new ColorMap[1] { null }, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetRemapTable_EmptyMap_ThrowsArgumentException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetRemapTable(new ColorMap[0], ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearRemapTable_Success()
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetRemapTable(_yellowToRedColorMap);
                imageAttr.ClearRemapTable();
                bitmap.SetPixel(0, 0, _yellowToRedColorMap[0].OldColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_yellowToRedColorMap[0].OldColor, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_AllTypesAllowed_TestData))]
        public void ClearRemapTable_Type_Success(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var graphics = Graphics.FromImage(bitmap))
            using (var imageAttr = new ImageAttributes())
            {
                imageAttr.SetRemapTable(_yellowToRedColorMap, type);
                imageAttr.ClearRemapTable(type);
                bitmap.SetPixel(0, 0, _yellowToRedColorMap[0].OldColor);
                graphics.DrawImage(bitmap, _rectangle, _rectangle.X, _rectangle.Y, _rectangle.Width, _rectangle.Height, GraphicsUnit.Pixel, imageAttr);
                Assert.Equal(_yellowToRedColorMap[0].OldColor, bitmap.GetPixel(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClearRemapTable_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearRemapTable());
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearRemapTable(ColorAdjustType.Default));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void ClearRemapTable_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.ClearRemapTable(type));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetWrapMode_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetWrapMode(WrapMode.Clamp));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetWrapMode(WrapMode.Clamp, Color.Black));
            AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.SetWrapMode(WrapMode.Clamp, Color.Black, true));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetAdjustedPalette_Disposed_ThrowsArgumentException()
        {
            var imageAttr = new ImageAttributes();
            imageAttr.Dispose();

            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.GetAdjustedPalette(bitmap.Palette, ColorAdjustType.Default));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetAdjustedPalette_NullPallete_ThrowsNullReferenceException()
        {
            using (var imageAttr = new ImageAttributes())
            {
                Assert.Throws<NullReferenceException>(() => imageAttr.GetAdjustedPalette(null, ColorAdjustType.Default));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ColorAdjustType_InvalidTypes_TestData))]
        public void GetAdjustedPalette_InvalidTypes_ThrowsArgumentException(ColorAdjustType type)
        {
            using (var bitmap = new Bitmap(_rectangle.Width, _rectangle.Height))
            using (var imageAttr = new ImageAttributes())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => imageAttr.GetAdjustedPalette(bitmap.Palette, type));
            }
        }
    }
}
