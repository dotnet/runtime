// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
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
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class PathGradientBrushTests
    {
        private readonly Point[] _defaultIntPoints = new Point[2] { new Point(1, 2), new Point(20, 30) };
        private readonly PointF[] _defaultFloatPoints = new PointF[2] { new PointF(1, 2), new PointF(20, 30) };
        private readonly RectangleF _defaultRectangle = new RectangleF(1, 2, 19, 28);

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Points_ReturnsExpected()
        {
            using (PathGradientBrush bi = new PathGradientBrush(_defaultIntPoints))
            using (PathGradientBrush bf = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertDefaults(bi);
                Assert.Equal(WrapMode.Clamp, bi.WrapMode);
                AssertDefaults(bf);
                Assert.Equal(WrapMode.Clamp, bf.WrapMode);
            }
        }

        public static IEnumerable<object[]> WrapMode_TestData()
        {
            yield return new object[] { WrapMode.Clamp };
            yield return new object[] { WrapMode.Tile };
            yield return new object[] { WrapMode.TileFlipX };
            yield return new object[] { WrapMode.TileFlipXY };
            yield return new object[] { WrapMode.TileFlipY };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(WrapMode_TestData))]
        public void Ctor_PointsWrapMode_ReturnsExpected(WrapMode wrapMode)
        {
            using (PathGradientBrush brushInt = new PathGradientBrush(_defaultIntPoints, wrapMode))
            using (PathGradientBrush brushFloat = new PathGradientBrush(_defaultFloatPoints, wrapMode))
            {
                AssertDefaults(brushInt);
                Assert.Equal(wrapMode, brushInt.WrapMode);
                AssertDefaults(brushFloat);
                Assert.Equal(wrapMode, brushFloat.WrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_PointsNull_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("points", () => new PathGradientBrush((Point[])null));
            AssertExtensions.Throws<ArgumentNullException>("points", () => new PathGradientBrush((PointF[])null));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_PointsLengthLessThenTwo_ThrowsOutOfMemoryException(int pointsLength)
        {
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new Point[pointsLength]));
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new Point[pointsLength], WrapMode.Clamp));
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new PointF[pointsLength]));
            Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(new PointF[pointsLength], WrapMode.Clamp));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_InvalidWrapMode_ThrowsInvalidEnumArgumentException()
        {
            Assert.ThrowsAny<ArgumentException>(() => 
                new PathGradientBrush(_defaultIntPoints, (WrapMode)int.MaxValue));

            Assert.ThrowsAny<ArgumentException>(() => 
                new PathGradientBrush(_defaultFloatPoints, (WrapMode)int.MaxValue));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Path_ReturnsExpected()
        {
            using (GraphicsPath path = new GraphicsPath(_defaultFloatPoints, new byte[] { 0, 1 }))
            using (PathGradientBrush brush = new PathGradientBrush(path))
            {
                AssertDefaults(brush);
                Assert.Equal(WrapMode.Clamp, brush.WrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Path_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new PathGradientBrush((GraphicsPath)null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_PathWithLessThenTwoPoints_ThrowsOutOfMemoryException()
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(path));
                path.AddLines(new PointF[] { new PointF(1, 1) });
                Assert.Throws<OutOfMemoryException>(() => new PathGradientBrush(path));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_ReturnsExpected()
        {
            using (GraphicsPath path = new GraphicsPath(_defaultFloatPoints, new byte[] { 0, 1 }))
            using (PathGradientBrush brush = new PathGradientBrush(path))
            using (PathGradientBrush clone = Assert.IsType<PathGradientBrush>(brush.Clone()))
            {
                AssertDefaults(clone);
                Assert.Equal(WrapMode.Clamp, clone.WrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Clone());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CenterColor_ReturnsExpected()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.Equal(Color.Black.ToArgb(), brush.CenterColor.ToArgb());
                brush.CenterColor = Color.Blue;
                Assert.Equal(Color.Blue.ToArgb(), brush.CenterColor.ToArgb());
                brush.CenterColor = Color.Transparent;
                Assert.Equal(Color.Transparent.ToArgb(), brush.CenterColor.ToArgb());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CenterColor_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.CenterColor = Color.Blue);
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SurroundColors_ReturnsExpected()
        {
            Color[] expectedColors = new Color[2] { Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0) };
            Color[] sameColors = new Color[2] { Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 255, 255, 0) };
            Color[] expectedSameColors = new Color[1] { Color.FromArgb(255, 255, 255, 0) };

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SurroundColors = expectedColors;
                Assert.Equal(expectedColors, brush.SurroundColors);
                brush.SurroundColors = sameColors;
                Assert.Equal(expectedSameColors, brush.SurroundColors);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SurroundColors_CannotChange()
        {
            Color[] colors = new Color[2] { Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0) };
            Color[] defaultColors = new Color[1] { Color.FromArgb(255, 255, 255, 255) };

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SurroundColors.ToList().AddRange(colors);
                Assert.Equal(defaultColors, brush.SurroundColors);
                brush.SurroundColors[0] = Color.FromArgb(255, 0, 0, 255);
                Assert.NotEqual(Color.FromArgb(255, 0, 0, 255), brush.SurroundColors[0]);
                Assert.Equal(defaultColors, brush.SurroundColors);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SurroundColors_Disposed_ThrowsArgumentException()
        {
            Color[] colors = new Color[2] { Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0) };
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.SurroundColors = colors);
        }

        public static IEnumerable<object[]> SurroundColors_InvalidColorsLength_TestData()
        {
            yield return new object[] { new Point[2] { new Point(1, 1), new Point(2, 2) }, new Color[0] };
            yield return new object[] { new Point[2] { new Point(1, 1), new Point(2, 2) }, new Color[3] };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(SurroundColors_InvalidColorsLength_TestData))]
        public void SurroundColors_InvalidColorsLength_ThrowsArgumentException(Point[] points, Color[] colors)
        {
            using (PathGradientBrush brush = new PathGradientBrush(points))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.SurroundColors = colors);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        public void SurroundColors_Null_ThrowsArgumentNullException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentNullException>(null, () => brush.SurroundColors = null);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CenterPoint_ReturnsExpected()
        {
            PointF centralPoint = new PointF(float.MaxValue, float.MinValue);
            PointF defaultCentralPoint = new PointF(10.5f, 16f);

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints, WrapMode.TileFlipXY))
            {
                Assert.Equal(defaultCentralPoint, brush.CenterPoint);
                brush.CenterPoint = centralPoint;
                Assert.Equal(centralPoint, brush.CenterPoint);

                centralPoint.X = float.NaN;
                centralPoint.Y = float.NegativeInfinity;
                brush.CenterPoint = centralPoint;
                Assert.Equal(float.NaN, brush.CenterPoint.X);
                Assert.Equal(float.NegativeInfinity, brush.CenterPoint.Y);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CenterPoint_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.CenterPoint);
        }

        public static IEnumerable<object[]> Blend_FactorsPositions_TestData()
        {
            yield return new object[] { new float[1] { 1 }, new float[1] { 0 } };
            yield return new object[] { new float[2] { 1, 1 }, new float[2] { 0, 1 } };
            yield return new object[] { new float[3] { 1, 0, 1 }, new float[3] { 0, 3, 1 } };
            yield return new object[] { new float[1] { 1 }, new float[3] { 0, 3, 1 } };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Blend_FactorsPositions_TestData))]
        public void Blend_ReturnsExpected(float[] factors, float[] positions)
        {
            int expectedSize = factors.Length;

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints, WrapMode.TileFlipXY))
            {
                brush.Blend = new Blend { Factors = factors, Positions = positions };
                Assert.Equal(factors, brush.Blend.Factors);
                Assert.Equal(expectedSize, brush.Blend.Positions.Length);
                if (expectedSize == positions.Length && expectedSize != 1)
                {
                    Assert.Equal(factors, brush.Blend.Factors);
                    Assert.Equal(positions, brush.Blend.Positions);
                }
                else
                {
                    Assert.Equal(factors, brush.Blend.Factors);
                    Assert.Equal(1, brush.Blend.Positions.Length);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_CannotChange()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints, WrapMode.TileFlipXY))
            {
                brush.Blend.Factors = new float[0];
                Assert.Equal(1, brush.Blend.Factors.Length);
                brush.Blend.Factors = new float[2];
                Assert.Equal(1, brush.Blend.Factors.Length);
                brush.Blend.Positions = new float[0];
                Assert.Equal(1, brush.Blend.Positions.Length);
                brush.Blend.Positions = new float[2];
                Assert.Equal(1, brush.Blend.Positions.Length);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend);
        }

        public static IEnumerable<object[]> Blend_InvalidFactorsPositions_TestData()
        {
            yield return new object[] { new Blend() { Factors = new float[0], Positions = new float[0] } };
            yield return new object[] { new Blend() { Factors = new float[2], Positions = new float[2] { 1, 1 } } };
            yield return new object[] { new Blend() { Factors = new float[2], Positions = new float[2] { 0, 5 } } };
            yield return new object[] { new Blend() { Factors = new float[3], Positions = new float[3] { 0, 1, 5 } } };
            yield return new object[] { new Blend() { Factors = new float[3], Positions = new float[3] { 1, 1, 1 } } };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Blend_InvalidFactorsPositions_TestData))]
        public void Blend_InvalidFactorPositions_ThrowsArgumentException(Blend blend)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.Blend = blend);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_InvalidFactorPositionsLengthMismatch_ThrowsArgumentOutOfRangeException()
        {
            Blend invalidBlend = new Blend() { Factors = new float[2], Positions = new float[1] };

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(null, () => brush.Blend = invalidBlend);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_Null_ThrowsNullReferenceException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.Throws<NullReferenceException>(() => brush.Blend = null);
                Assert.Throws<NullReferenceException>(() => brush.Blend = new Blend() { Factors = null, Positions = null});
                Assert.Throws<NullReferenceException>(() => brush.Blend = new Blend() { Factors = null, Positions = new float[0] });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Blend_NullBlendProperites_ThrowsArgumentNullException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentNullException>("source", () => 
                    brush.Blend = new Blend() { Factors = new float[0], Positions = null });
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1f)]
        [InlineData(0f)]
        [InlineData(0.5f)]
        public void SetSigmaBellShape_Focus_Success(float focus)
        {
            float defaultScale = 1f;

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SetSigmaBellShape(focus);
                Assert.True(brush.Transform.IsIdentity);
                if (focus == 0f)
                {
                    Assert.Equal(focus, brush.Blend.Positions[0]);
                    Assert.Equal(defaultScale, brush.Blend.Factors[0]);
                    Assert.Equal(1f, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(0f, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
                else if (focus == 1f)
                {
                    Assert.Equal(0f, brush.Blend.Positions[0]);
                    Assert.Equal(0f, brush.Blend.Factors[0]);
                    Assert.Equal(focus, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(defaultScale, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
                else
                {
                    Assert.Equal(0f, brush.Blend.Positions[0]);
                    Assert.Equal(0f, brush.Blend.Factors[0]);
                    Assert.Equal(1f, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(0f, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1f, 1f)]
        [InlineData(0f, 1f)]
        [InlineData(0.5f, 1f)]
        [InlineData(1f, 0f)]
        [InlineData(0f, 0f)]
        [InlineData(0.5f, 0f)]
        [InlineData(1f, 0.5f)]
        [InlineData(0f, 0.5f)]
        [InlineData(0.5f, 0.5f)]
        public void SetSigmaBellShape_FocusScale_Success(float focus, float scale)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SetSigmaBellShape(focus);
                Assert.True(brush.Transform.IsIdentity);
                if (focus == 0f)
                {
                    Assert.Equal(256, brush.Blend.Positions.Length);
                    Assert.Equal(256, brush.Blend.Factors.Length);
                    Assert.Equal(focus, brush.Blend.Positions[0]);
                    Assert.Equal(1f, brush.Blend.Factors[0]);
                    Assert.Equal(1f, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(0f, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
                else if (focus == 1f)
                {
                    Assert.Equal(256, brush.Blend.Positions.Length);
                    Assert.Equal(256, brush.Blend.Factors.Length);
                    Assert.Equal(0f, brush.Blend.Positions[0]);
                    Assert.Equal(0f, brush.Blend.Factors[0]);
                    Assert.Equal(focus, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(1f, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
                else
                {
                    Assert.Equal(511, brush.Blend.Positions.Length);
                    Assert.Equal(511, brush.Blend.Factors.Length);
                    Assert.Equal(0f, brush.Blend.Positions[0]);
                    Assert.Equal(0f, brush.Blend.Factors[0]);
                    Assert.Equal(focus, brush.Blend.Positions[255]);
                    Assert.Equal(1f, brush.Blend.Factors[255]);
                    Assert.Equal(1f, brush.Blend.Positions[brush.Blend.Positions.Length - 1]);
                    Assert.Equal(0f, brush.Blend.Factors[brush.Blend.Factors.Length - 1]);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetSigmaBellShape_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetSigmaBellShape(1f));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetSigmaBellShape(1f, 1f));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1)]
        [InlineData(1.1f)]
        public void SetSigmaBellShape_InvalidFocus_ThrowsArgumentException(float focus)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetSigmaBellShape(focus));
                AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetSigmaBellShape(focus, 1f));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1)]
        [InlineData(1.1f)]
        public void SetSigmaBellShape_InvalidScale_ThrowsArgumentException(float scale)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>("scale", null, () => brush.SetSigmaBellShape(1f, scale));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1f)]
        [InlineData(0f)]
        [InlineData(0.5f)]
        public void SetBlendTriangularShape_Focus_Success(float focus)
        {
            float defaultScale = 1f;

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SetBlendTriangularShape(focus);                
                Assert.True(brush.Transform.IsIdentity);
                if (focus == 0f)
                {
                    Assert.Equal(new float[2] { defaultScale, 0f }, brush.Blend.Factors);
                    Assert.Equal(new float[2] { focus, 1f }, brush.Blend.Positions);
                }
                else if (focus == 1f)
                {
                    Assert.Equal(new float[2] { 0f, defaultScale }, brush.Blend.Factors);
                    Assert.Equal(new float[2] { 0f, focus }, brush.Blend.Positions);
                }
                else
                {
                    Assert.Equal(new float[3] { 0f, defaultScale, 0f }, brush.Blend.Factors);
                    Assert.Equal(new float[3] { 0f, focus, 1f }, brush.Blend.Positions);
                }
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1f, 1f)]
        [InlineData(0f, 1f)]
        [InlineData(0.5f, 1f)]
        [InlineData(1f, 0f)]
        [InlineData(0f, 0f)]
        [InlineData(0.5f, 0f)]
        [InlineData(1f, 0.5f)]
        [InlineData(0f, 0.5f)]
        [InlineData(0.5f, 0.5f)]
        public void SetBlendTriangularShape_FocusScale_Success(float focus, float scale)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.SetBlendTriangularShape(focus);
                Assert.True(brush.Transform.IsIdentity);
                Assert.True(brush.Transform.IsIdentity);
                if (focus == 0f)
                {
                    Assert.Equal(new float[2] { 1f, 0f }, brush.Blend.Factors);
                    Assert.Equal(new float[2] { focus, 1f }, brush.Blend.Positions);
                }
                else if (focus == 1f)
                {
                    Assert.Equal(new float[2] { 0f, 1f }, brush.Blend.Factors);
                    Assert.Equal(new float[2] { 0f, focus }, brush.Blend.Positions);
                }
                else
                {
                    Assert.Equal(new float[3] { 0f, 1f, 0f }, brush.Blend.Factors);
                    Assert.Equal(new float[3] { 0f, focus, 1f }, brush.Blend.Positions);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetBlendTriangularShape_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetBlendTriangularShape(1f));
            AssertExtensions.Throws<ArgumentException>(null, () => brush.SetBlendTriangularShape(1f, 1f));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1)]
        [InlineData(1.1f)]
        public void SetBlendTriangularShape_InvalidFocus_ThrowsArgumentException(float focus)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetBlendTriangularShape(focus));
                AssertExtensions.Throws<ArgumentException>("focus", null, () => brush.SetBlendTriangularShape(focus, 1f));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1)]
        [InlineData(1.1f)]
        public void SetBlendTriangularShape_InvalidScale_ThrowsArgumentException(float scale)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>("scale", null, () => brush.SetBlendTriangularShape(1f, scale));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_ReturnsExpected()
        {
            Color[] expectedColors = new Color[2] { Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0) };
            float[] expectedPositions = new float[] { 0, 1 };
            Color[] sameColors = new Color[2] { Color.FromArgb(255, 255, 255, 0), Color.FromArgb(255, 255, 255, 0) };

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.InterpolationColors = new ColorBlend() { Colors = expectedColors, Positions = expectedPositions };
                Assert.Equal(expectedColors, brush.InterpolationColors.Colors);
                Assert.Equal(expectedPositions, brush.InterpolationColors.Positions);

                brush.InterpolationColors = new ColorBlend() { Colors = sameColors, Positions = expectedPositions };
                Assert.Equal(sameColors, brush.InterpolationColors.Colors);
                Assert.Equal(expectedPositions, brush.InterpolationColors.Positions);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_CannotChange()
        {
            Color[] colors = new Color[2] { Color.FromArgb(255, 0, 0, 255), Color.FromArgb(255, 255, 0, 0) };
            Color[] defaultColors = new Color[1] { Color.Empty };

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.InterpolationColors.Colors.ToList().AddRange(colors);
                Assert.Equal(defaultColors, brush.InterpolationColors.Colors);
                brush.InterpolationColors.Colors = colors;
                Assert.Equal(defaultColors, brush.InterpolationColors.Colors);
                brush.InterpolationColors.Colors[0] = Color.Pink;
                Assert.NotEqual(Color.Pink, brush.InterpolationColors.Colors[0]);
                Assert.Equal(defaultColors, brush.InterpolationColors.Colors);
                brush.InterpolationColors.Positions = new float[0];
                Assert.Equal(1, brush.InterpolationColors.Positions.Length);
                brush.InterpolationColors.Positions = new float[2];
                Assert.Equal(1, brush.InterpolationColors.Positions.Length);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_Null_ThrowsNullReferenceException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.Throws<NullReferenceException>(() => brush.InterpolationColors = null);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_NullColors_ThrowsNullReferenceException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.Throws<NullReferenceException>(() => 
                    brush.InterpolationColors = new ColorBlend() { Colors = null, Positions = null });

                Assert.Throws<NullReferenceException>(() => 
                    brush.InterpolationColors = new ColorBlend() { Colors = null, Positions = new float[2] });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_NullPoints_ArgumentNullException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentNullException>("source", () => 
                    brush.InterpolationColors = new ColorBlend() { Colors = new Color[1], Positions = null });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_Empty_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.InterpolationColors = new ColorBlend());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_EmptyColors_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => 
                    brush.InterpolationColors = new ColorBlend() { Colors = new Color[0], Positions = new float[0] });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_PointsLengthGreaterThenColorsLength_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => 
                    brush.InterpolationColors = new ColorBlend() { Colors = new Color[1], Positions = new float[2] });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void InterpolationColors_ColorsLengthGreaterThenPointsLength_ThrowsArgumentOutOfRangeException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => 
                    brush.InterpolationColors = new ColorBlend() { Colors = new Color[2], Positions = new float[1] });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_ReturnsExpected()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix defaultMatrix = new Matrix(1, 0, 0, 1, 0, 0))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 1, 1))
            {
                Assert.Equal(defaultMatrix, brush.Transform);
                brush.Transform = matrix;
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_EmptyMatrix_ReturnsExpected()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix())
            {
                brush.Transform = matrix;
                Assert.True(brush.Transform.IsIdentity);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_Null_ArgumentNullException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentNullException>("value", "matrix", () => brush.Transform = null);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_NonInvertible_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix nonInvertible = new Matrix(123, 24, 82, 16, 47, 30))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.Transform = nonInvertible);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Success()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix defaultMatrix = new Matrix(1, 0, 0, 1, 0, 0))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 1, 1))
            {
                Assert.Equal(defaultMatrix, brush.Transform);
                brush.Transform = matrix;
                Assert.Equal(matrix, brush.Transform);
                brush.ResetTransform();
                Assert.Equal(defaultMatrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetTransform_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.ResetTransform());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_Matrix_Success()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix defaultMatrix = new Matrix(1, 0, 0, 1, 0, 0))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 1, 1))
            {
                defaultMatrix.Multiply(matrix, MatrixOrder.Prepend);
                brush.MultiplyTransform(matrix);
                Assert.Equal(defaultMatrix, brush.Transform);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Append)]
        [InlineData(MatrixOrder.Prepend)]
        public void MultiplyTransform_MatrixMatrixOrder_Success(MatrixOrder matrixOrder)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix defaultMatrix = new Matrix(1, 0, 0, 1, 0, 0))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 1, 1))
            {
                defaultMatrix.Multiply(matrix, matrixOrder);
                brush.MultiplyTransform(matrix, matrixOrder);
                Assert.Equal(defaultMatrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_Disposed_ThrowsArgumentException()
        {
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 1, 1))
            {
                PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
                brush.Dispose();

                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix, MatrixOrder.Append));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NullMatrix_ThrowsArgumentNullException()
        {
            using (var brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null));
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => brush.MultiplyTransform(null, MatrixOrder.Append));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_DisposedMatrix_Nop()
        {
            using (var brush = new PathGradientBrush(_defaultFloatPoints))
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
        public void MultiplyTransform_InvalidMatrixOrder_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 1, 1, 1, 1, 1))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(matrix, (MatrixOrder)int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MultiplyTransform_NonInvertible_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix nonInvertible = new Matrix(123, 24, 82, 16, 47, 30))
            {                
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(nonInvertible));
                AssertExtensions.Throws<ArgumentException>(null, () => brush.MultiplyTransform(nonInvertible, MatrixOrder.Append));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_Offset_Success()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Translate(20f, 30f, MatrixOrder.Prepend);
                brush.TranslateTransform(20f, 30f);
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Append)]
        [InlineData(MatrixOrder.Prepend)]
        public void TranslateTransform_OffsetMatrixOrder_Success(MatrixOrder matrixOrder)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Translate(20f, 30f, matrixOrder);
                brush.TranslateTransform(20f, 30f, matrixOrder);
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(20f, 30f, MatrixOrder.Append));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_InvalidMatrixOrder_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.TranslateTransform(20f, 30f, (MatrixOrder)int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_Scale_Success()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Scale(2, 4, MatrixOrder.Prepend);
                brush.ScaleTransform(2, 4);
                Assert.Equal(matrix, brush.Transform);

                matrix.Scale(0.5f, 0.25f, MatrixOrder.Prepend);
                brush.ScaleTransform(0.5f, 0.25f);
                Assert.True(brush.Transform.IsIdentity);

                matrix.Scale(float.MaxValue, float.MinValue, MatrixOrder.Prepend);
                brush.ScaleTransform(float.MaxValue, float.MinValue);
                Assert.Equal(matrix, brush.Transform);

                matrix.Scale(float.MinValue, float.MaxValue, MatrixOrder.Prepend);
                brush.ScaleTransform(float.MinValue, float.MaxValue);
                Assert.Equal(matrix, brush.Transform);                
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Append)]
        [InlineData(MatrixOrder.Prepend)]
        public void ScaleTransform_ScaleMatrixOrder_Success(MatrixOrder matrixOrder)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Scale(0.25f, 2, matrixOrder);
                brush.ScaleTransform(0.25f, 2, matrixOrder);
                Assert.Equal(matrix, brush.Transform);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(0.25f, 2, MatrixOrder.Append));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_InvalidMatrixOrder_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.ScaleTransform(1, 1, (MatrixOrder)int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RotateTransform_Angle_Success()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Rotate(90, MatrixOrder.Prepend);
                brush.RotateTransform(90);
                Assert.Equal(matrix, brush.Transform);

                brush.RotateTransform(270);
                Assert.True(brush.Transform.IsIdentity);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(MatrixOrder.Append)]
        [InlineData(MatrixOrder.Prepend)]
        public void RotateTransform_AngleMatrixOrder_Success(MatrixOrder matrixOrder)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            using (Matrix matrix = new Matrix(1, 0, 0, 1, 0, 0))
            {
                matrix.Rotate(45, matrixOrder);
                brush.RotateTransform(45, matrixOrder);
                Assert.Equal(matrix, brush.Transform);                
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RotateTransform_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(45, MatrixOrder.Append));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RotateTransform_InvalidMatrixOrder_ArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => brush.RotateTransform(45, (MatrixOrder)int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FocusScales_ReturnsExpected()
        {
            var point = new PointF(2.5f, 3.4f);

            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.FocusScales = point;
                Assert.Equal(point, brush.FocusScales);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FocusScales_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.FocusScales);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(WrapMode_TestData))]
        public void WrapMode_ReturnsExpected(WrapMode wrapMode)
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                brush.WrapMode = wrapMode;
                Assert.Equal(wrapMode, brush.WrapMode);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_Disposed_ThrowsArgumentException()
        {
            PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints);
            brush.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => brush.WrapMode);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void WrapMode_Invalid_InvalidEnumArgumentException()
        {
            using (PathGradientBrush brush = new PathGradientBrush(_defaultFloatPoints))
            {
                Assert.ThrowsAny<ArgumentException>(() => brush.WrapMode = (WrapMode)int.MinValue);
            }
        }

        private void AssertDefaults(PathGradientBrush brush)
        {
            Assert.Equal(_defaultRectangle, brush.Rectangle);
            Assert.Equal(new float[] { 1 }, brush.Blend.Factors);
            Assert.Equal(1, brush.Blend.Positions.Length);
            Assert.Equal(new PointF(10.5f, 16f), brush.CenterPoint);
            Assert.Equal(new Color[] { Color.Empty }, brush.InterpolationColors.Colors);
            Assert.Equal(new Color[] { Color.FromArgb(255, 255, 255, 255) }, brush.SurroundColors);
            Assert.Equal(new float[] { 0 }, brush.InterpolationColors.Positions);
            Assert.True(brush.Transform.IsIdentity);
            Assert.True(brush.FocusScales.IsEmpty);
        }
    }
}
