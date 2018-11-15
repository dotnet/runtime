// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class LinearGradientBrushTests
    {
        public static IEnumerable<object[]> Ctor_Point_TestData()
        {
            yield return new object[] { new Point(0, 0), new Point(2, 2), Color.Empty, Color.Empty, new RectangleF(0, 0, 2, 2) };
            yield return new object[] { new Point(1, 0), new Point(0, 0), Color.Empty, Color.Red, new RectangleF(0, -0.5f, 1, 1) };
            yield return new object[] { new Point(1, 2), new Point(4, 6), Color.Plum, Color.Red, new RectangleF(1, 2, 3, 4) };
            yield return new object[] { new Point(1, 2), new Point(4, 6), Color.Red, Color.Red, new RectangleF(1, 2, 3, 4) };
            yield return new object[] { new Point(-1, -2), new Point(4, 6), Color.Red, Color.Plum, new RectangleF(-1, -2, 5, 8) };
            yield return new object[] { new Point(-4, -6), new Point(1, 2), Color.Black, Color.Wheat, new RectangleF(-4, -6, 5, 8) };
            yield return new object[] { new Point(4, 6), new Point(-1, -2), Color.Black, Color.Wheat, new RectangleF(-1, -2, 5, 8) };
            yield return new object[] { new Point(4, 6), new Point(1, 2), Color.Black, Color.Wheat, new RectangleF(1, 2, 3, 4) };
        }

        [ActiveIssue(32706, TestPlatforms.AnyUnix)] 
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Point_TestData))]
        public void Ctor_PointF_PointF_Color_Color(Point point1, Point point2, Color color1, Color color2, RectangleF expectedRectangle)
        {
            var brush = new LinearGradientBrush((PointF)point1, point2, color1, color2);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(expectedRectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.False(brush.Transform.IsIdentity);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_PointF_PointF_Color_Color_FloatRanges()
        {
            var brush = new LinearGradientBrush(new PointF(float.NaN, float.NaN), new PointF(float.PositiveInfinity, float.NegativeInfinity), Color.Plum, Color.Red);
            Assert.Equal(float.PositiveInfinity, brush.Rectangle.X);
            Assert.Equal(float.NegativeInfinity, brush.Rectangle.Y);
            Assert.Equal(float.NaN, brush.Rectangle.Width);
            Assert.Equal(float.NaN, brush.Rectangle.Height);
        }

        [ActiveIssue(32706, TestPlatforms.AnyUnix)] 
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Point_TestData))]
        public void Ctor_Point_Point_Color_Color(Point point1, Point point2, Color color1, Color color2, RectangleF expectedRectangle)
        {
            var brush = new LinearGradientBrush(point1, point2, color1, color2);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(expectedRectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.False(brush.Transform.IsIdentity);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        public void Ctor_EqualPoints_ThrowsOutOfMemoryException(int x, int y)
        {
            Assert.Throws<OutOfMemoryException>(() => new LinearGradientBrush(new Point(x, y), new Point(x, y), Color.Fuchsia, Color.GhostWhite));
            Assert.Throws<OutOfMemoryException>(() => new LinearGradientBrush(new PointF(x, y), new PointF(x, y), Color.Fuchsia, Color.GhostWhite));
        }

        public static IEnumerable<object[]> Ctor_Rectangle_LinearGradientMode_TestData()
        {
            yield return new object[] { new Rectangle(0, 0, 1, 2), Color.Empty, Color.Red, LinearGradientMode.BackwardDiagonal };
            yield return new object[] { new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, LinearGradientMode.ForwardDiagonal };
            yield return new object[] { new Rectangle(-1, -2, -3, -4), Color.Red, Color.Red, LinearGradientMode.Horizontal };
            yield return new object[] { new Rectangle(1, 2, 3, 4), Color.Red, Color.Plum, LinearGradientMode.Vertical };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_LinearGradientMode_TestData))]
        public void Ctor_Rectangle_Color_Color_LinearGradientMode(Rectangle rectangle, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            var brush = new LinearGradientBrush(rectangle, color1, color2, linearGradientMode);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal(linearGradientMode == LinearGradientMode.Horizontal, brush.Transform.IsIdentity);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_LinearGradientMode_TestData))]
        public void Ctor_RectangleF_Color_Color_LinearGradientMode(Rectangle rectangle, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            var brush = new LinearGradientBrush((RectangleF)rectangle, color1, color2, linearGradientMode);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal(linearGradientMode == LinearGradientMode.Horizontal, brush.Transform.IsIdentity);
        }

        public static IEnumerable<object[]> Ctor_Rectangle_Angle_TestData()
        {
            yield return new object[] { new Rectangle(0, 0, 1, 2), Color.Empty, Color.Red, 90 };
            yield return new object[] { new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 180 };
            yield return new object[] { new Rectangle(-1, -2, -3, -4), Color.Red, Color.Red, 0 };
            yield return new object[] { new Rectangle(-1, -2, -3, -4), Color.Red, Color.Red, 360 };
            yield return new object[] { new Rectangle(1, 2, 3, 4), Color.Red, Color.Plum, 90 };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_Angle_TestData))]
        public void Ctor_Rectangle_Color_Color_Angle(Rectangle rectangle, Color color1, Color color2, float angle)
        {
            var brush = new LinearGradientBrush(rectangle, color1, color2, angle);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal((angle % 360) == 0, brush.Transform.IsIdentity);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_Angle_TestData))]
        public void Ctor_RectangleF_Color_Color_Angle(Rectangle rectangle, Color color1, Color color2, float angle)
        {
            var brush = new LinearGradientBrush((RectangleF)rectangle, color1, color2, angle);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal((angle % 360) == 0, brush.Transform.IsIdentity);
        }

        public static IEnumerable<object[]> Ctor_Rectangle_Angle_IsAngleScalable_TestData()
        {
            foreach (object[] testData in Ctor_Rectangle_Angle_TestData())
            {
                yield return new object[] { testData[0], testData[1], testData[2], testData[3], true };
                yield return new object[] { testData[0], testData[1], testData[2], testData[3], false };
            }
        }
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_Angle_IsAngleScalable_TestData))]
        public void Ctor_Rectangle_Color_Color_Angle_IsAngleScalable(Rectangle rectangle, Color color1, Color color2, float angle, bool isAngleScalable)
        {
            var brush = new LinearGradientBrush(rectangle, color1, color2, angle, isAngleScalable);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal((angle % 360) == 0, brush.Transform.IsIdentity);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Rectangle_Angle_IsAngleScalable_TestData))]
        public void Ctor_RectangleF_Color_Color_Angle_IsAngleScalable(Rectangle rectangle, Color color1, Color color2, float angle, bool isAngleScalable)
        {
            var brush = new LinearGradientBrush((RectangleF)rectangle, color1, color2, angle, isAngleScalable);

            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);

            Assert.False(brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            Assert.Equal(new Color[] { Color.FromArgb(color1.ToArgb()), Color.FromArgb(color2.ToArgb()) }, brush.LinearColors);
            Assert.Equal(rectangle, brush.Rectangle);
            Assert.Equal(WrapMode.Tile, brush.WrapMode);

            Assert.Equal((angle % 360) == 0, brush.Transform.IsIdentity);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_ZeroWidth_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 0, 4), Color.Empty, Color.Empty, 0f));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 0, 4), Color.Empty, Color.Empty, 0f));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 0, 4), Color.Empty, Color.Empty, LinearGradientMode.BackwardDiagonal));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 0, 4), Color.Empty, Color.Empty, LinearGradientMode.BackwardDiagonal));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 0, 4), Color.Empty, Color.Empty, 0, true));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 0, 4), Color.Empty, Color.Empty, 0, true));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_ZeroHeight_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 3, 0), Color.Empty, Color.Empty, 0f));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 3, 0), Color.Empty, Color.Empty, 0f));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 3, 0), Color.Empty, Color.Empty, LinearGradientMode.BackwardDiagonal));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 3, 0), Color.Empty, Color.Empty, LinearGradientMode.BackwardDiagonal));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new Rectangle(1, 2, 3, 0), Color.Empty, Color.Empty, 0, true));
            AssertExtensions.Throws<ArgumentException>(null, () => new LinearGradientBrush(new RectangleF(1, 2, 3, 0), Color.Empty, Color.Empty, 0, true));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(LinearGradientMode.Horizontal - 1)]
        [InlineData(LinearGradientMode.BackwardDiagonal + 1)]
        public void Ctor_InvalidLinearGradientMode_ThrowsEnumArgumentException(LinearGradientMode linearGradientMode)
        {
            Assert.ThrowsAny<ArgumentException>(() => new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Empty, Color.Empty, linearGradientMode));
            Assert.ThrowsAny<ArgumentException>(() => new LinearGradientBrush(new RectangleF(1, 2, 3, 4), Color.Empty, Color.Empty, linearGradientMode));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Brush_ReturnsClone()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            LinearGradientBrush clone = Assert.IsType<LinearGradientBrush>(brush.Clone());

            Assert.NotSame(clone, brush);
            Assert.Equal(brush.Blend.Factors, clone.Blend.Factors);
            Assert.Equal(brush.Blend.Positions.Length, clone.Blend.Positions.Length);
            Assert.Equal(brush.LinearColors, clone.LinearColors);
            Assert.Equal(brush.Rectangle, clone.Rectangle);
            Assert.Equal(brush.Transform, clone.Transform);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Clone());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_GetWithInterpolationColorsSet_ReturnsNull()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var blend = new ColorBlend
            {
                Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                Positions = new float[] { 0, 10, 1 }
            };

            brush.InterpolationColors = blend;
            Assert.Null(brush.Blend);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(new float[] { 1 }, new float[] { 1 })]
        [InlineData(new float[] { 0 }, new float[] { 0 })]
        [InlineData(new float[] { float.MaxValue }, new float[] { float.MaxValue })]
        [InlineData(new float[] { float.MinValue }, new float[] { float.MinValue })]
        [InlineData(new float[] { 0.5f, 0.5f }, new float[] { 0, 1 })]
        [InlineData(new float[] { 0.4f, 0.3f, 0.2f }, new float[] { 0, 0.5f, 1 })]
        [InlineData(new float[] { -1 }, new float[] { -1 })]
        [InlineData(new float[] { float.NaN }, new float[] { float.NaN })]
        [InlineData(new float[] { 1 }, new float[] { 1, 2 })]
        public void Blend_Set_Success(float[] factors, float[] positions)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var blend = new Blend
            {
                Factors = factors,
                Positions = positions
            };
            brush.Blend = blend;

            Assert.Equal(blend.Factors, brush.Blend.Factors);
            Assert.Equal(factors.Length, brush.Blend.Positions.Length);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(new float[] { 1, 2 }, new float[] { 1, 2 })]
        [InlineData(new float[] { 1, 2 }, new float[] { 1, 1 })]
        [InlineData(new float[] { 1, 2 }, new float[] { 1, 0 })]
        [InlineData(new float[] { 0, 0, 0 }, new float[] { 0, 0, 0 })]
        public void Blend_InvalidBlend_ThrowsArgumentException(float[] factors, float[] positions)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var blend = new Blend
            {
                Factors = factors,
                Positions = positions
            };
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend = blend);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_SetNullBlend_ThrowsNullReferenceException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<NullReferenceException>(() => brush.Blend = null);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_SetNullBlendFactors_ThrowsNullReferenceException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<NullReferenceException>(() => brush.Blend = new Blend { Factors = null });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_SetNullBlendPositions_ThrowsArgumentNullException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentNullException>("source", () => brush.Blend = new Blend { Factors = new float[2], Positions = null });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_SetFactorsLengthGreaterThanPositionsLength_ThrowsArgumentOutOfRangeException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => brush.Blend = new Blend { Factors = new float[2], Positions = new float[1] });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_SetInvalidBlendFactorsLength_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend = new Blend { Factors = new float[0], Positions = new float[0] });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend = new Blend());
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(true)]
        [InlineData(false)]
        public void GammaCorrection_Set_GetReturnsExpected(bool gammaCorrection)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true) { GammaCorrection = gammaCorrection };
            Assert.Equal(gammaCorrection, brush.GammaCorrection);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GammaCorrection_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.GammaCorrection);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.GammaCorrection = true);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(1)]
        [InlineData(0.5f)]
        [InlineData(-1)]
        [InlineData(float.NegativeInfinity)]
        public void InterpolationColors_SetValid_GetReturnsExpected(float value)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var blend = new ColorBlend
            {
                Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                Positions = new float[] { 0, 10, 1 }
            };

            brush.InterpolationColors = blend;
            Assert.Equal(blend.Colors.Select(c => Color.FromArgb(c.ToArgb())), brush.InterpolationColors.Colors);
            Assert.Equal(blend.Positions, brush.InterpolationColors.Positions);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetWithExistingInterpolationColors_OverwritesInterpolationColors()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new Color[] { Color.Wheat, Color.Yellow },
                    Positions = new float[] { 0, 1 }
                }
            };

            var blend = new ColorBlend
            {
                Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                Positions = new float[] { 0, 0.5f, 1f }
            };
            brush.InterpolationColors = blend;
            Assert.Equal(blend.Colors.Select(c => Color.FromArgb(c.ToArgb())), brush.InterpolationColors.Colors);
            Assert.Equal(blend.Positions, brush.InterpolationColors.Positions);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetNullBlend_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = null);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetBlendWithNullColors_ThrowsNullReferenceException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<NullReferenceException>(() => brush.InterpolationColors = new ColorBlend { Colors = null });
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        public void InterpolationColors_SetBlendWithTooFewColors_ThrowsArgumentException(int colorsLength)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = new ColorBlend { Colors = new Color[colorsLength] });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetNullBlendPositions_ThrowsNullReferenceException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<NullReferenceException>(() => brush.InterpolationColors = new ColorBlend { Colors = new Color[2], Positions = null });
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void InterpolationColors_SetInvalidBlendPositionsLength_ThrowsArgumentException(int positionsLength)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = new ColorBlend
            {
                Colors = new Color[2],
                Positions = new float[positionsLength]
            });
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(new float[] { 1, 0, 1 })]
        [InlineData(new float[] { 0, 0, 0 })]
        public void InterpolationColors_InvalidPositions_ThrowsArgumentException(float[] positions)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = new ColorBlend
            {
                Colors = new Color[positions.Length],
                Positions = positions
            });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                    Positions = new float[] { 0, 0.5f, 1 }
                }
            };
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = new ColorBlend
            {
                Colors = new Color[2],
                Positions = new float[] { 0f, 1f }
            });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetBlendTriangularShape_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                    Positions = new float[] { 0, 0.5f, 1 }
                }
            };
            Assert.NotNull(brush.InterpolationColors);

            brush.SetBlendTriangularShape(0.5f);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_SetBlend_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true)
            {
                InterpolationColors = new ColorBlend
                {
                    Colors = new Color[] { Color.Red, Color.PeachPuff, Color.PowderBlue },
                    Positions = new float[] { 0, 0.5f, 1 }
                }
            };
            Assert.NotNull(brush.InterpolationColors);

            brush.Blend = new Blend
            {
                Factors = new float[1],
                Positions = new float[1]
            };
            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void LinearColors_SetValid_GetReturnsExpected()
        {
            Color[] colors = new Color[] { Color.Red, Color.Blue, Color.AntiqueWhite };
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true) { LinearColors = colors };
            Assert.Equal(colors.Take(2).Select(c => Color.FromArgb(c.ToArgb())), brush.LinearColors);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void LinearColors_SetNull_ThrowsNullReferenceException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<NullReferenceException>(() => brush.LinearColors = null);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        public void LinearColors_SetInvalidLength_ThrowsIndexOutOfRangeException(int length)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.Throws<IndexOutOfRangeException>(() => brush.LinearColors = new Color[length]);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void LinearColors_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.LinearColors);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.LinearColors = new Color[] { Color.Red, Color.Wheat });
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Rectangle_GetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Rectangle);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_SetValid_GetReturnsExpected()
        {
            var transform = new Matrix(1, 2, 3, 4, 5, 6);
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true) { Transform = transform };
            Assert.Equal(transform, brush.Transform);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_SetNull_ThrowsArgumentNullException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentNullException>("value", "matrix", () => brush.Transform = null);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform = new Matrix());
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WrapMode.Tile)]
        [InlineData(WrapMode.TileFlipX)]
        [InlineData(WrapMode.TileFlipXY)]
        [InlineData(WrapMode.TileFlipY)]
        public void WrapMode_SetValid_GetReturnsExpected(WrapMode wrapMode)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true) { WrapMode = wrapMode };
            Assert.Equal(wrapMode, brush.WrapMode);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WrapMode.Tile - 1)]
        [InlineData(WrapMode.Clamp + 1)]
        public void WrapMode_SetInvalid_ThrowsInvalidEnumArgumentException(WrapMode wrapMode)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.ThrowsAny<ArgumentException>(() => brush.WrapMode = wrapMode);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_Clamp_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode = WrapMode.Clamp);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_GetSetDisposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode = WrapMode.TileFlipX);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Invoke_SetsTransformToIdentity()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Assert.False(brush.Transform.IsIdentity);

            brush.ResetTransform();
            Assert.True(brush.Transform.IsIdentity);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.ResetTransform());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NoOrder_Success()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var matrix = new Matrix(1, 2, 3, 4, 5, 6);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Multiply(matrix);

            brush.MultiplyTransform(matrix);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend)]
        [InlineData(MatrixOrder.Append)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void MultiplyTransform_Order_Success(MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var matrix = new Matrix(1, 2, 3, 4, 5, 6);
            Matrix expectedTransform = brush.Transform;

            if (order == MatrixOrder.Append || order == MatrixOrder.Prepend)
            {
                expectedTransform.Multiply(matrix, order);
            }
            else
            {
                // Invalid MatrixOrder is interpreted as MatrixOrder.Append.
                expectedTransform.Multiply(matrix, MatrixOrder.Append);
            }

            brush.MultiplyTransform(matrix, order);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NullMatrix_ThrowsArgumentNullException()
        {
            using (var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true))
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null));
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null, MatrixOrder.Append));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_DisposedMatrix_Nop()
        {
            using (var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true))
            using (Matrix transform = brush.Transform)
            {
                var matrix = new Matrix();
                matrix.Dispose();

                brush.MultiplyTransform(matrix);
                brush.MultiplyTransform(matrix, MatrixOrder.Append);

                Assert.Equal(transform, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NonInvertibleMatrix_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            var matrix = new Matrix(123, 24, 82, 16, 47, 30);

            AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix, MatrixOrder.Append));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(new Matrix()));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(new Matrix(), MatrixOrder.Prepend));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void TranslateTransform_NoOrder_Success(float dx, float dy)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Translate(dx, dy);

            brush.TranslateTransform(dx, dy);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1, 1, MatrixOrder.Prepend)]
        [InlineData(1, 1, MatrixOrder.Append)]
        [InlineData(0, 0, MatrixOrder.Prepend)]
        [InlineData(0, 0, MatrixOrder.Append)]
        [InlineData(-1, -1, MatrixOrder.Prepend)]
        [InlineData(-1, -1, MatrixOrder.Append)]
        public void TranslateTransform_Order_Success(float dx, float dy, MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Translate(dx, dy, order);

            brush.TranslateTransform(dx, dy, order);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void TranslateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(0, 0, order));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(0, 0, MatrixOrder.Append));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, -2)]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        public void ScaleTransform_NoOrder_Success(float sx, float sy)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Scale(sx, sy);

            brush.ScaleTransform(sx, sy);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1, 1, MatrixOrder.Prepend)]
        [InlineData(1, 1, MatrixOrder.Append)]
        [InlineData(0, 0, MatrixOrder.Prepend)]
        [InlineData(0, 0, MatrixOrder.Append)]
        [InlineData(-1, -1, MatrixOrder.Prepend)]
        [InlineData(-1, -1, MatrixOrder.Append)]
        public void ScaleTransform_Order_Success(float sx, float sy, MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Scale(sx, sy, order);

            brush.ScaleTransform(sx, sy, order);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void ScaleTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(0, 0, order));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(0, 0, MatrixOrder.Append));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(360)]
        public void RotateTransform_NoOrder_Success(float angle)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Rotate(angle);

            brush.RotateTransform(angle);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1, MatrixOrder.Prepend)]
        [InlineData(1, MatrixOrder.Append)]
        [InlineData(0, MatrixOrder.Prepend)]
        [InlineData(360, MatrixOrder.Append)]
        [InlineData(-1, MatrixOrder.Prepend)]
        [InlineData(-1, MatrixOrder.Append)]
        public void RotateTransform_Order_Success(float angle, MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            Matrix expectedTransform = brush.Transform;
            expectedTransform.Rotate(angle, order);

            brush.RotateTransform(angle, order);
            Assert.Equal(expectedTransform, brush.Transform);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Prepend - 1)]
        [InlineData(MatrixOrder.Append + 1)]
        public void RotateTransform_InvalidOrder_ThrowsArgumentException(MatrixOrder order)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(0, order));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RotateTransform_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(0));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(0, MatrixOrder.Append));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(0.5)]
        [InlineData(1)]
        [InlineData(float.NaN)]
        public void SetSigmalBellShape(float focus)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.SetSigmaBellShape(focus);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void SetSigmalBellShape_InvalidFocus_ThrowsArgumentException(float focus)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetSigmaBellShape(focus));
            AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetSigmaBellShape(focus, 1));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void SetSigmalBellShape_InvalidScale_ThrowsArgumentException(float scale)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>("scale", null, () => brush.SetSigmaBellShape(0.1f, scale));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetSigmalBellShape_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetSigmaBellShape(0));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetSigmaBellShape(0, 1));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, new float[] { 1, 0 }, new float[] { 0, 1 })]
        [InlineData(0.5, new float[] { 0, 1, 0 }, new float[] { 0, 0.5f, 1 })]
        [InlineData(1, new float[] { 0, 1 }, new float[] { 0, 1 })]
        public void SetBlendTriangularShape_Success(float focus, float[] expectedFactors, float[] expectedPositions)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 0, true);
            brush.SetBlendTriangularShape(focus);

            Assert.Equal(expectedFactors, brush.Blend.Factors);
            Assert.Equal(expectedPositions, brush.Blend.Positions);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, 1, new float[] { 1, 0 }, new float[] { 0, 1 })]
        [InlineData(0.5, 0, new float[] { 0, 0, 0 }, new float[] { 0, 0.5f, 1 })]
        [InlineData(0.5, 1, new float[] { 0, 1, 0 }, new float[] { 0, 0.5f, 1 })]
        [InlineData(1, 0.5, new float[] { 0, 0.5f }, new float[] { 0, 1 })]
        public void SetBlendTriangularShape_Scale_Success(float focus, float scale, float[] expectedFactors, float[] expectedPositions)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 0, true);
            brush.SetBlendTriangularShape(focus, scale);

            Assert.Equal(expectedFactors, brush.Blend.Factors);
            Assert.Equal(expectedPositions, brush.Blend.Positions);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void SetBlendTriangularShape_InvalidFocus_ThrowsArgumentException(float focus)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetBlendTriangularShape(focus));
            AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetBlendTriangularShape(focus, 1));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void SetBlendTriangularShape_InvalidScale_ThrowsArgumentException(float scale)
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            AssertExtensions.Throws<ArgumentException>("scale", null, () => brush.SetBlendTriangularShape(0.1f, scale));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetBlendTriangularShape_Disposed_ThrowsArgumentException()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetBlendTriangularShape(0));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetBlendTriangularShape(0, 1));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dispose_MultipleTimes_Success()
        {
            var brush = new LinearGradientBrush(new Rectangle(1, 2, 3, 4), Color.Plum, Color.Red, 45, true);
            brush.Dispose();
            brush.Dispose();
        }
    }
}
